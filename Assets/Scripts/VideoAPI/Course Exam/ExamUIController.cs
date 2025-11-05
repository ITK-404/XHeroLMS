using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Net;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ExamUIController : MonoBehaviour
{
    [Header("API Source (auto)")]
    //Tự build URL từ LmsStore.baseUrl + pathTemplate + (examId, courseId).
    public bool autoBuildUrl = true;

    //Template URL path. {0} = examId, {1} = courseId (nếu có).
    public string pathTemplate = "/lms/exam/{0}/course/{1}";

    //ExamId đọc từ PlayerPrefs nếu không tìm được qua LmsStore và overrideExamId rỗng.
    public string examIdPrefsKey = "EXAM_CURRENT_ID";

    //CourseId đọc từ PlayerPrefs khi không tìm được qua LmsStore và override rỗng.
    public string courseIdPrefsKey = "EXAM_CURRENT_COURSE_ID";

    //Điền nếu muốn ghi đè examId (ưu tiên cao nhất).
    public string overrideExamId = "";

    [Header("API Fallback (manual)")]
    //Nếu tắt autoBuildUrl, dùng URL này để GET trực tiếp.
    public string apiUrl = "";

    //Tự lấy Bearer token từ TokenStore.AccessToken; có thể ghi đè bằng trường dưới.
    public bool useTokenFromStore = true;

    //Để trống nếu dùng TokenStore.AccessToken.
    public string overrideAccessToken = "";

    [Header("Where to spawn")]
    public Transform content;                    // nơi spawn câu hỏi + đáp án

    [Header("Prefabs")]
    public TMP_Text prefabCauHoi;                // prefab TMP_Text
    public AnswerButton prefabCauTraLoi;         // prefab AnswerButton của bạn

    [Header("Buttons & Timer")]
    public Button btnBack;
    public Button btnNext;
    public Button btnNopBai;
    public TMP_Text textDemNguoc;

    [Header("Options")]
    public bool autoStart = true;
    public string timeFormat = "{0:00}:{1:00}";  // mm:ss
    public float requestTimeout = 15f;           // giây

    [Header("UI Labels")]
    public TMP_Text textQuestionCounter;   // "01/30"
    public Button   btnBatDau;             // nút Bắt đầu thi
    public TMP_Text textExamTitle;         // tiêu đề bài thi
    public TMP_Text textTotalQuestions;    // "Số câu hỏi: 30"
    public TMP_Text textTotalDuration;     // "Thời gian: 30:00"
    public TMP_Text textPassNeed;          // "Cần đạt: 24/30 (80%)"

    [Header("Debug")]
    public bool debugVerbose = true;

    // ===== runtime =====
    private ExamPaper paper = new ExamPaper { questions = new List<ExamQuestion>() };
    private int currentIndex = 0;
    private int durationSeconds = 0;
    private Coroutine timerCo;

    private readonly Dictionary<string, HashSet<int>> selectedMap = new();
    private readonly List<AnswerButton> spawnedOptions = new();

    // Header parsed info
    private string examTitle = "";
    private int passPointPercent = 80; // default nếu API không trả

    // Flow
    private bool examStarted = false;

    bool _loadingShown = false;
    void ShowLoading(bool show)
    {
        if (show)
        {
            if (_loadingShown) return;
            LoadingUI.Show();
            _loadingShown = true;
        }
        else
        {
            if (!_loadingShown) return;
            LoadingUI.Hide();
            _loadingShown = false;
        }
    }

    void Awake()
    {
        EnsureLmsStore(); // ép khởi tạo singleton
        if (btnBack)   btnBack.onClick.AddListener(OnBack);
        if (btnNext)   btnNext.onClick.AddListener(OnNext);
        if (btnNopBai) btnNopBai.onClick.AddListener(OnSubmit);
        if (btnBatDau) btnBatDau.onClick.AddListener(BeginExam);
    }

    void Start()
    {
        // Trước khi bắt đầu, khoá các nút điều hướng
        UpdateNavButtons();
        UpdateQuestionCounter(); // sẽ hiển thị 00/00 cho tới khi có dữ liệu
        if (autoStart) StartCoroutine(StartWithWarmup());
    }

    IEnumerator StartWithWarmup()
    {
        ShowLoading(true);

        IEnumerator warmup = null;

        // Lấy IEnumerator warmup bằng reflection (bên trong try/catch, KHÔNG yield ở đây)
        try
        {
            var t = Type.GetType("LmsStore");
            var inst = t?.GetProperty("Instance")?.GetValue(null, null);
            var miWarmupAll = t?.GetMethod("WarmupAll", new Type[] {
                typeof(int), typeof(int), typeof(string), typeof(string), typeof(string), typeof(string)
            });

            if (miWarmupAll != null && inst != null)
            {
                warmup = miWarmupAll.Invoke(inst, new object[] { 0, 300, "", "", "", "" }) as IEnumerator;
            }
            else if (debugVerbose)
            {
                Debug.LogWarning("[ExamUI] WarmupAll() not found or LmsStore.Instance null. Bỏ qua warmup.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[ExamUI] WarmupAll fail: " + ex.Message);
        }

        // THỰC HIỆN yield Ở NGOÀI try/catch
        if (warmup != null)
            yield return warmup;

        StartExamFromApi();
    }

    [ContextMenu("Start exam from API")]
    public void StartExamFromApi()
    {
        StopAllCoroutines();
        if (timerCo != null) timerCo = null;

        ShowLoading(true);

        string finalUrl = autoBuildUrl ? BuildApiUrlAuto() : apiUrl;
        if (string.IsNullOrEmpty(finalUrl))
        {
            Debug.LogError("[ExamUI] Không thể xác định API URL. Kiểm tra LmsStore/baseUrl, pathTemplate, examId/courseId (PlayerPrefs) hoặc apiUrl.");
            ShowNoQuestion();

            ShowLoading(false);
            return;
        }

        StartCoroutine(FetchAndSetup(finalUrl));
    }

    string BuildApiUrlAuto()
    {
        string baseUrl = GetBaseUrlFromLmsStore();
        if (string.IsNullOrEmpty(baseUrl))
        {
            if (debugVerbose) Debug.LogWarning("[ExamUI] LmsStore.Instance.baseUrl rỗng.");
            return null;
        }
        if (!baseUrl.EndsWith("/")) baseUrl += "/";

        string path = pathTemplate.TrimStart('/');

        // ——— Lấy examId & courseId ———
        string examId = null;
        string courseId = null;

        // 1) overrideExamId (ưu tiên cao nhất)
        if (!string.IsNullOrEmpty(overrideExamId))
            examId = overrideExamId;

        // 2) LmsStore (sau warmup): tự pick cặp (examId, courseId)
        if (string.IsNullOrEmpty(examId) || (path.Contains("{1}") && string.IsNullOrEmpty(courseId)))
        {
            var pick = TryPickExamFromLmsStore();
            if (string.IsNullOrEmpty(examId))   examId = pick.examId;
            if (string.IsNullOrEmpty(courseId)) courseId = pick.courseId;
        }

        // 3) Fallback PlayerPrefs
        if (string.IsNullOrEmpty(examId))
            examId = PlayerPrefs.GetString(examIdPrefsKey, "");
        if (path.Contains("{1}") && string.IsNullOrEmpty(courseId))
            courseId = PlayerPrefs.GetString(courseIdPrefsKey, "");

        if (string.IsNullOrEmpty(examId))
        {
            Debug.LogWarning("[ExamUI] examId trống. Không thể build URL.");
            return null;
        }

        // Cho phép template chỉ có {0}
        string finalUrl;
        if (path.Contains("{1}"))
        {
            if (string.IsNullOrEmpty(courseId))
            {
                Debug.LogWarning("[ExamUI] courseId trống nhưng pathTemplate cần {1}. Hãy enroll/warmup hoặc set PlayerPrefs/override.");
                return null;
            }
            finalUrl = baseUrl + string.Format(path, examId, courseId);
        }
        else
        {
            finalUrl = baseUrl + string.Format(path, examId);
        }

        if (debugVerbose) Debug.Log($"[ExamUI] Built URL: {finalUrl}");
        return finalUrl;
    }

    void EnsureLmsStore()
    {
        try
        {
            var t = Type.GetType("LmsStore");
            var propInst = t?.GetProperty("Instance");
            _ = propInst?.GetValue(null, null);
        }
        catch { }
    }

    string GetBaseUrlFromLmsStore()
    {
        try
        {
            var t = Type.GetType("LmsStore");
            var inst = t?.GetProperty("Instance")?.GetValue(null, null);
            if (inst == null) return null;

            var field = t.GetField("baseUrl");
            if (field != null) return field.GetValue(inst) as string;

            var prop = t.GetProperty("baseUrl");
            if (prop != null) return prop.GetValue(inst, null) as string;
        }
        catch { }
        return null;
    }

    // ===== Helper reflection an toàn cho Field hoặc Property =====
    static object GetMemberValue(object obj, string name)
    {
        if (obj == null) return null;
        var type = obj.GetType();
        // field
        var fi = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (fi != null) return fi.GetValue(obj);
        // property
        var pi = type.GetProperty(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        if (pi != null) return pi.GetValue(obj, null);
        return null;
    }

    static string GetStringMember(object obj, string name)
    {
        return GetMemberValue(obj, name) as string;
    }

    // ==== Chọn (examId, courseId) từ MyCourses giống script dump ====
    private (string examId, string courseId, string title) TryPickExamFromLmsStore()
    {
        try
        {
            var t = Type.GetType("LmsStore");
            var inst = t?.GetProperty("Instance")?.GetValue(null, null);
            if (inst == null) return (null, null, null);

            var miGetMyCourses   = t.GetMethod("GetMyCourses");
            var miGetFinalExamId = t.GetMethod("GetFinalExamId", new Type[] { typeof(string) });
            var myCourses = miGetMyCourses?.Invoke(inst, null) as System.Collections.IEnumerable;
            if (myCourses == null || miGetFinalExamId == null) return (null, null, null);

            foreach (var uc in myCourses)
            {
                if (uc == null) continue;

                var course   = GetMemberValue(uc, "course");     // field or prop
                var courseId = GetStringMember(course, "_id");   // field or prop
                if (string.IsNullOrEmpty(courseId)) continue;

                var fe = miGetFinalExamId.Invoke(inst, new object[] { courseId }) as string;
                if (!string.IsNullOrEmpty(fe))
                {
                    var title = GetStringMember(course, "title");
                    if (debugVerbose) Debug.Log($"[ExamUI] Picked examId={fe}, courseId={courseId} (title={title})");
                    return (fe, courseId, title);
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[ExamUI] TryPickExamFromLmsStore fail: " + ex.Message);
        }
        return (null, null, null);
    }

    // ===================== FETCH + SETUP (Header trước, chưa bắt đầu thi) =====================
    IEnumerator FetchAndSetup(string finalUrl)
    {
        string token = GetAccessToken();
        using var req = UnityWebRequest.Get(finalUrl);
        req.timeout = Mathf.CeilToInt(Mathf.Max(1f, requestTimeout));
        if (!string.IsNullOrEmpty(token))
            req.SetRequestHeader("Authorization", "Bearer " + token);
        req.SetRequestHeader("Accept", "application/json");

        if (debugVerbose) Debug.Log($"[ExamUI] GET {finalUrl}");
        yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
        bool hasErr = req.result != UnityWebRequest.Result.Success;
#else
        bool hasErr = req.isNetworkError || req.isHttpError;
#endif
        if (hasErr)
        {
            Debug.LogError($"[ExamUI] API ERROR: {req.responseCode} {req.error}");
            ShowNoQuestion();

            ShowLoading(false);

            yield break;
        }

        string raw = req.downloadHandler.text ?? "";
        if (debugVerbose) Debug.Log($"[ExamUI] Response length={raw.Length}");

        // Parse duration + questions
        int dur;
        string questionsJson = ExtractQuestionsArray(raw, out dur);
        durationSeconds = Mathf.Max(0, dur);

        if (string.IsNullOrEmpty(questionsJson))
        {
            Debug.LogError("[ExamUI] Không tìm thấy mảng \"questions\" trong JSON response.");
            ShowNoQuestion();

            ShowLoading(false);

            yield break;
        }

        paper = FallbackParseToPaper(questionsJson);

        // Lấy mô tả (description) và làm sạch về plain text để đổ UI
        string descHtml = ExtractStringField(raw, "description") ?? "";
        examTitle = CleanHtmlToPlainText(descHtml);

        // Nếu vẫn cần pass %
        passPointPercent = ExtractIntField(raw, "passPointPercent", 80);

        if (debugVerbose) Debug.Log($"[ExamUI] Parsed questions: {paper?.Count ?? 0}, duration={durationSeconds}s, title='{examTitle}', pass%={passPointPercent}");

        // Cập nhật header (chưa bắt đầu thi)
        examStarted = false;
        ClearContent(); // dọn vùng content, chưa render câu hỏi
        UpdateHeaderInfo();
        UpdateNavButtons();      // khoá điều hướng đến khi bắt đầu
        UpdateQuestionCounter(); // hiển thị 00/NN

        ShowLoading(false);
    }

    // ===================== BẮT ĐẦU THI =====================
    void BeginExam()
    {
        if (examStarted) return;
        if (paper == null || paper.Count == 0)
        {
            Debug.LogWarning("[ExamUI] Chưa có dữ liệu câu hỏi. Không thể bắt đầu.");
            return;
        }

        examStarted = true;
        currentIndex = 0;

        // Render câu đầu tiên
        RenderCurrentQuestion();

        // Bắt đầu timer
        if (timerCo != null) StopCoroutine(timerCo);
        if (durationSeconds > 0) timerCo = StartCoroutine(TimerCountdown());
    }

    string GetAccessToken()
    {
        if (!string.IsNullOrEmpty(overrideAccessToken)) return overrideAccessToken;
        if (!useTokenFromStore) return null;

        try
        {
            var t = Type.GetType("TokenStore");
            if (t != null)
            {
                var prop = t.GetProperty("AccessToken");
                if (prop != null)
                {
                    var value = prop.GetValue(null) as string;
                    if (debugVerbose) Debug.Log($"[ExamUI] TokenStore.AccessToken {(string.IsNullOrEmpty(value) ? "EMPTY" : "OK")}");
                    return value;
                }
            }
        }
        catch { }
        return null;
    }

    void ClearContent()
    {
        if (!content) return;
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
        spawnedOptions.Clear();
    }

    void ShowNoQuestion()
    {
        ClearContent();
        SpawnQuestionText("(Không có câu hỏi)");
        UpdateNavButtons();
        UpdateQuestionCounter();
        UpdateHeaderInfo();
    }

    void RenderCurrentQuestion()
    {
        if (!examStarted)
        {
            // Chưa bắt đầu thi: không render câu hỏi
            return;
        }

        if (paper == null || paper.questions == null || paper.questions.Count == 0)
        {
            ShowNoQuestion();
            return;
        }

        currentIndex = Mathf.Clamp(currentIndex, 0, paper.questions.Count - 1);
        var q = paper.questions[currentIndex];

        ClearContent();
        SpawnQuestionText($"{currentIndex + 1}. {q.title}");

        switch (q.type)
        {
            case ExamQuestionType.SINGLE_CHOICE:
            case ExamQuestionType.MULTIPLE_CHOICE:
                RenderOptions(q);
                break;
            default:
                SpawnQuestionText($"(Type {q.type} chưa hỗ trợ UI – sẽ cập nhật sau)");
                break;
        }

        UpdateNavButtons();
        UpdateQuestionCounter();
    }

    void RenderOptions(ExamQuestion q)
    {
        if (q.options == null || q.options.Count == 0)
        {
            SpawnQuestionText("(Không có đáp án)");
            return;
        }

        if (!selectedMap.ContainsKey(q.id))
            selectedMap[q.id] = new HashSet<int>();
        var picked = selectedMap[q.id];

        bool isSingle = (q.type == ExamQuestionType.SINGLE_CHOICE);

        for (int i = 0; i < q.options.Count; i++)
        {
            string optText = CleanOptionText(q.options[i]);
            var item = Instantiate(prefabCauTraLoi, content);
            spawnedOptions.Add(item);

            if (isSingle) item.ActiveSingleChoice();
            else item.ActiveMultipleChoice();

            bool isOn = picked.Contains(i);
            item.SetText(optText);
            item.ActiveSelect(isOn);

            int optionIndex = i;
            item.OnSelectButton = (btn) =>
            {
                if (isSingle)
                {
                    foreach (var other in spawnedOptions)
                        if (other != btn) other.ActiveSelect(false);

                    picked.Clear();
                    bool turnOn = !btn.value;
                    btn.ActiveSelect(turnOn);
                    if (turnOn) picked.Add(optionIndex);
                }
                else
                {
                    bool turnOn = !btn.value;
                    btn.ActiveSelect(turnOn);
                    if (turnOn) picked.Add(optionIndex);
                    else picked.Remove(optionIndex);
                }
            };
        }
    }

    TMP_Text SpawnQuestionText(string text)
    {
        var t = Instantiate(prefabCauHoi, content);
        t.text = text ?? "";
        return t;
    }

    void UpdateNavButtons()
    {
        bool canNav = examStarted && paper != null && paper.Count > 0;
        if (btnBack) btnBack.interactable = canNav && currentIndex > 0;
        if (btnNext) btnNext.interactable = canNav && currentIndex < paper.Count - 1;
        if (btnNopBai) btnNopBai.interactable = canNav; // có thể để true luôn tùy flow
    }

    void OnBack()
    {
        if (!examStarted) return;
        if (paper == null || currentIndex <= 0) return;
        currentIndex--;
        RenderCurrentQuestion();
    }

    void OnNext()
    {
        if (!examStarted) return;
        if (paper == null || currentIndex >= paper.Count - 1) return;
        currentIndex++;
        RenderCurrentQuestion();
    }

    void OnSubmit()
    {
        Debug.Log($"[Exam] Submit. Questions answered: {selectedMap.Count}");
    }

    IEnumerator TimerCountdown()
    {
        int remain = durationSeconds;
        while (true)
        {
            if (textDemNguoc)
            {
                int mm = Mathf.Max(0, remain) / 60;
                int ss = Mathf.Max(0, remain) % 60;
                textDemNguoc.text = string.Format(timeFormat, mm, ss);
            }
            if (remain <= 0) { OnSubmit(); yield break; }
            yield return new WaitForSeconds(1f);
            remain--;
        }
    }

    // ===================== HEADER / LABELS =====================
    void UpdateHeaderInfo()
    {
        int total = paper?.Count ?? 0;

        // Title
        if (textExamTitle) textExamTitle.text = string.IsNullOrEmpty(examTitle) ? "Bài thi" : examTitle;

        // Total questions
        if (textTotalQuestions) textTotalQuestions.text = $"{total}";

        // Duration label
        if (textTotalDuration)
        {
            int mm = Mathf.Max(0, durationSeconds) / 60;
            int ss = Mathf.Max(0, durationSeconds) % 60;
            textTotalDuration.text = $"{string.Format(timeFormat, mm, ss)}";
        }

        // Pass requirement
        if (textPassNeed)
        {
            int need = Mathf.CeilToInt(total * (passPointPercent / 100f));
            textPassNeed.text = $"{need}/{total}";
        }
    }

    void UpdateQuestionCounter()
    {
        if (!textQuestionCounter) return;

        int total = paper?.Count ?? 0;
        if (total <= 0)
        {
            textQuestionCounter.text = "00/00";
            return;
        }

        int current = examStarted ? Mathf.Clamp(currentIndex + 1, 1, total) : 0;
        int width = total.ToString().Length;
        string left = current.ToString().PadLeft(width, '0');
        string right = total.ToString().PadLeft(width, '0');
        textQuestionCounter.text = $"{left}/{right}";
    }
    
    string ExtractQuestionsArray(string raw, out int durationSec)
    {
        durationSec = 0;
        if (string.IsNullOrEmpty(raw))
        {
            if (debugVerbose) Debug.LogWarning("[ExamUI] ExtractQuestionsArray: raw is NULL/Empty.");
            return null;
        }

        try
        {
            var durMatch = Regex.Match(raw, @"""duration""\s*:\s*(\d+)", RegexOptions.IgnoreCase);
            if (durMatch.Success) int.TryParse(durMatch.Groups[1].Value, out durationSec);

            var key = "\"questions\"";
            int i = raw.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (i < 0)
            {
                Debug.LogError("[ExamUI] Can't find \"questions\" key in JSON.");
                return null;
            }

            int s = raw.IndexOf('[', i);
            if (s < 0)
            {
                Debug.LogError("[ExamUI] Can't find '[' after \"questions\".");
                return null;
            }

            int depth = 0;
            for (int p = s; p < raw.Length; p++)
            {
                if (raw[p] == '[') depth++;
                else if (raw[p] == ']')
                {
                    depth--;
                    if (depth == 0)
                    {
                        string arr = raw.Substring(s, p - s + 1);
                        if (debugVerbose) Debug.Log($"[ExamUI] Extracted questions array length={arr.Length}");
                        return arr;
                    }
                }
            }

            Debug.LogError("[ExamUI] Could not match the closing ']' for questions array.");
        }
        catch (Exception ex)
        {
            Debug.LogError("[ExamUI] ExtractQuestionsArray EXCEPTION: " + ex.Message);
        }
        return null;
    }

string ExtractStringField(string raw, string field)
{
    if (string.IsNullOrEmpty(raw) || string.IsNullOrEmpty(field)) return null;
    try
    {
        // match: "<field>" : "<json-string-with-escapes>"
        var pattern = $"\"{Regex.Escape(field)}\"\\s*:\\s*\"(?<val>(?:\\\\.|[^\"\\\\])*)\"";
        var m = Regex.Match(raw, pattern, RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (m.Success)
        {
            var val = m.Groups["val"].Value;
            val = Regex.Unescape(val);
            return val; 
        }
    }
    catch { }
    return null;
}

    int ExtractIntField(string raw, string field, int fallback = 0)
    {
        if (string.IsNullOrEmpty(raw) || string.IsNullOrEmpty(field)) return fallback;
        try
        {
            var m = Regex.Match(raw, $"\"{Regex.Escape(field)}\"\\s*:\\s*(-?\\d+)", RegexOptions.IgnoreCase);
            if (m.Success && int.TryParse(m.Groups[1].Value, out int v)) return v;
        }
        catch { }
        return fallback;
    }

    string CleanOptionText(string html)
    {
        if (string.IsNullOrEmpty(html)) return "";
        html = Regex.Replace(html, @"<\s*br\s*/?>", "\n", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"</?\s*p\s*>", "", RegexOptions.IgnoreCase);
        html = Regex.Replace(html, @"<[^>]+>", "");
        return WebUtility.HtmlDecode(html).Trim();
    }

    [Serializable] private class QuestionRaw
    {
        public string _id;
        public string title;
        public string type;
        public List<string> answers;
    }

    [Serializable] private class QuestionsWrapper
    {
        public List<QuestionRaw> questions;
    }

    private ExamPaper FallbackParseToPaper(string questionsJson)
    {
        string wrapped = questionsJson.TrimStart().StartsWith("[")
            ? "{\"questions\":" + questionsJson + "}"
            : questionsJson;

        var wrapper = JsonUtility.FromJson<QuestionsWrapper>(wrapped);
        var result = new ExamPaper { questions = new List<ExamQuestion>() };

        if (wrapper?.questions == null) return result;

        foreach (var q in wrapper.questions)
        {
            var eq = new ExamQuestion
            {
                id    = string.IsNullOrEmpty(q._id) ? Guid.NewGuid().ToString() : q._id,
                title = q.title ?? "",
                type  = ToType(q.type),
                options = new List<string>()
            };

            if (q.answers != null)
                foreach (var a in q.answers)
                    eq.options.Add(CleanOptionText(a));

            result.questions.Add(eq);
        }
        return result;
    }

    private ExamQuestionType ToType(string t)
    {
        if (string.Equals(t, "SINGLE_CHOICE", StringComparison.OrdinalIgnoreCase))
            return ExamQuestionType.SINGLE_CHOICE;
        if (string.Equals(t, "MULTIPLE_CHOICE", StringComparison.OrdinalIgnoreCase))
            return ExamQuestionType.MULTIPLE_CHOICE;
        return ExamQuestionType.SINGLE_CHOICE;
    }
    string CleanHtmlToPlainText(string html)
    {
        if (string.IsNullOrEmpty(html)) return "";

        string s = html;

        // br, p
        s = Regex.Replace(s, @"<\s*br\s*/?>", "\n", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"</\s*p\s*>", "\n", RegexOptions.IgnoreCase);
        s = Regex.Replace(s, @"<\s*p[^>]*>", "", RegexOptions.IgnoreCase);

        // &nbsp; → space
        s = Regex.Replace(s, @"&nbsp;", " ", RegexOptions.IgnoreCase);

        // remove other tags
        s = Regex.Replace(s, @"<[^>]+>", "");

        // decode entities (&quot; &amp; …)
        s = WebUtility.HtmlDecode(s);

        // bỏ tất cả loại dấu nháy phổ biến
        s = Regex.Replace(s, "[\"“”‘’«»]+", "");

        // gọn khoảng trắng / dòng
        s = Regex.Replace(s, @"[ \t]+\n", "\n");
        s = Regex.Replace(s, @"\n{3,}", "\n\n");

        return s.Trim();
    }
}
