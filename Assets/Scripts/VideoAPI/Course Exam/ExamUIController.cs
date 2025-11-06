using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;


public partial class ExamUIController : MonoBehaviour
{
    [Header("Routing / Strict Mode")]
    // Bật chế độ CHỈ NHẬN ID từ PlayerPrefs và/hoặc override; KHÔNG fallback sang LmsStore
    public bool strictIdsFromPlayerPrefs = true;

    // Cho phép override cứng trên Inspector (ưu tiên cao nhất)
    public string overrideExamId = "";
    public string overrideCourseId = "";

    [Header("PlayerPrefs Keys (phải trùng với nơi bạn lưu ở CourseListView)")]
    public string examIdPrefsKey = "EXAM_CURRENT_ID";

    public string courseIdPrefsKey = "EXAM_CURRENT_COURSE_ID";

    [Header("API Source")]
    // Tự build URL từ baseUrl + pathTemplate; KHÔNG dùng nếu bạn set apiUrl thủ công
    public bool autoBuildUrl = true;

    // {0} = examId, {1} = courseId (nếu endpoint cần)
    public string pathTemplate = "/lms/exam/{0}/course/{1}";

    // Nếu tắt autoBuildUrl, dùng URL này để GET trực tiếp
    public string apiUrl = "";

    [Header("Auth")]
    public bool useTokenFromStore = true;

    public string overrideAccessToken = "";

    [Header("Where to spawn")]
    public Transform content;

    [Header("Prefabs")]
    public TMP_Text prefabCauHoi;

    public AnswerButton prefabCauTraLoi;

    // Prefab chứa TMP_InputField để nhập bài tự luận
    public TMP_InputField prefabCauTraLoiTuLuan;

    [Header("Buttons & Timer")]
    public Button btnBack;

    public Button btnNext;
    public Button btnNopBai;
    public TMP_Text textDemNguoc;

    [Header("Options")]
    public bool autoStart = true;

    public string timeFormat = "{0:00}:{1:00}";
    public float requestTimeout = 15f;

    [Header("Header UI")]
    public TMP_Text textQuestionCounter; // "01/30"

    public Button btnBatDau;
    public TMP_Text textExamTitle;
    public TMP_Text textTotalQuestions;
    public TMP_Text textTotalDuration;
    public TMP_Text textPassNeed;

    [Header("Debug")]
    public bool debugVerbose = true;

    // ===== runtime =====
    private ExamPaper paper = new ExamPaper { questions = new List<ExamQuestion>() };
    private int currentIndex = 0;
    private int durationSeconds = 0;
    private Coroutine timerCo;

    private readonly Dictionary<string, HashSet<int>> selectedMap = new();
    private readonly List<AnswerButton> spawnedOptions = new();

    private readonly Dictionary<string, string> essayMap = new();

    private string examTitle = "";
    private int passPointPercent = 80;
    private bool examStarted = false;

    bool _loadingShown = false;

    // ===== submit state =====
    bool _isSubmitting = false;
    int _elapsedSeconds = 0;               // đếm thời gian làm bài (dùng cho timeSpent)
    public bool getWithCorrectAnswer = true; // khi GET kết quả

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
        if (btnBack) btnBack.onClick.AddListener(OnBack);
        if (btnNext) btnNext.onClick.AddListener(OnNext);
        if (btnNopBai) btnNopBai.onClick.AddListener(OnSubmit);
        if (btnBatDau) btnBatDau.onClick.AddListener(BeginExam);
    }

    void Start()
    {
        // Trước khi bắt đầu, khoá điều hướng
        UpdateNavButtons();
        UpdateQuestionCounter();

        if (autoStart) StartCoroutine(StartGate());
    }

    IEnumerator StartGate()
    {
        ShowLoading(true);

        // BẮT BUỘC: chỉ khi có đủ ID thì mới load
        if (!TryGetIds(out var examId, out var courseId))
        {
            if (debugVerbose)
                Debug.LogWarning("[ExamUI] Thiếu ExamID/CourseID trong PlayerPrefs/override. Không gọi API.");

            // Tắt loading và hiển thị trạng thái rỗng
            ShowNoQuestion();
            ShowLoading(false);

            // Bạn có thể disable btnBatDau cho rõ ràng
            if (btnBatDau) btnBatDau.interactable = false;
            yield break;
        }

        // Có ID -> bắt đầu
        StartExamFromApi();
        yield break;
    }

    [ContextMenu("Start exam from API")]
    public void StartExamFromApi()
    {
        StopAllCoroutines();
        if (timerCo != null) timerCo = null;

        ShowLoading(true);

        string finalUrl = autoBuildUrl ? BuildApiUrlStrict() : apiUrl;
        if (string.IsNullOrEmpty(finalUrl))
        {
            Debug.LogError("[ExamUI] Không thể xác định API URL (thiếu ID hoặc path sai).");
            ShowNoQuestion();
            ShowLoading(false);
            return;
        }

        StartCoroutine(FetchAndSetup(finalUrl));
    }


    // ========== CHỈ build URL từ override/PlayerPrefs (KHÔNG fallback LmsStore) ==========
    string BuildApiUrlStrict()
    {
        string baseUrl = GetBaseUrl();
        if (string.IsNullOrEmpty(baseUrl)) return null;
        if (!baseUrl.EndsWith("/")) baseUrl += "/";

        string path = pathTemplate.TrimStart('/');

        if (!TryGetIds(out var examId, out var courseId))
        {
            return null;
        }

        // Build URL theo template
        string finalUrl;
        if (path.Contains("{1}"))
        {
            if (string.IsNullOrEmpty(courseId))
            {
                if (debugVerbose) Debug.LogWarning("[ExamUI] courseId rỗng nhưng pathTemplate cần {1}.");
                return null;
            }

            finalUrl = baseUrl + string.Format(path, examId, courseId);
        }
        else
        {
            finalUrl = baseUrl + string.Format(path, examId);
        }

        if (debugVerbose)
            Debug.Log($"[ExamUI] Built URL (STRICT): {finalUrl} | examId={examId}, courseId={courseId}");

        return finalUrl;
    }

    // Lấy baseUrl từ LmsStore (hoặc bạn có thể đổi sang const nếu muốn)
    string GetBaseUrl()
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
        catch
        {
        }

        return null;
    }

    // Kiểm tra & lấy ID từ override/PlayerPrefs theo strict mode
    bool TryGetIds(out string examId, out string courseId)
    {
        examId = "";
        courseId = "";

        // 1) override (ưu tiên cao nhất)
        if (!string.IsNullOrEmpty(overrideExamId)) examId = overrideExamId;
        if (!string.IsNullOrEmpty(overrideCourseId)) courseId = overrideCourseId;

        // 2) PlayerPrefs (được CourseListView set)
        if (string.IsNullOrEmpty(examId))
            examId = PlayerPrefs.GetString(examIdPrefsKey, "");
        if (string.IsNullOrEmpty(courseId))
            courseId = PlayerPrefs.GetString(courseIdPrefsKey, "");

        // Nếu pathTemplate chỉ có {0} thì không bắt buộc courseId
        bool needCourse = pathTemplate != null && pathTemplate.Contains("{1}");

        if (string.IsNullOrEmpty(examId))
        {
            return false;
        }

        if (needCourse && string.IsNullOrEmpty(courseId))
        {
            return false;
        }

        return true;
    }

    public static string offlineExamFile = "exam_6698e1fc0b5596af157b45c3_66add60cb04daa3a3a3694f2.json";
    private static string offlineFilePath => Path.Combine(Application.persistentDataPath, offlineExamFile);

    public static string TryReadAllText()
    {
        string content = "";
        var path = offlineFilePath;
        if (!File.Exists(path))
        {
            Debug.Log("Dường dẫn không tồn tại");
            return content;
        }

        try
        {
            content = File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to read file: {path}\n{ex}");
        }

        return content;
    }

    // ===================== FETCH + SETUP =====================
    IEnumerator FetchAndSetup(string finalUrl)
    {
        string token = GetAccessToken();
        string offlineContent = "";

        using var req = UnityWebRequest.Get(finalUrl);
        req.timeout = Mathf.CeilToInt(Mathf.Max(1f, requestTimeout));
        if (!string.IsNullOrEmpty(token))
        {
            Debug.Log("Dùng online data (API)");
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
        }
        else
        {
            Debug.Log("Dùng offline data (local file)");
            offlineContent = TryReadAllText();
            if (string.IsNullOrWhiteSpace(offlineContent))
            {
                ShowNoQuestion();
                ShowLoading(false);
                yield break;
            }
            
        }
        
        string onlineText = req.downloadHandler?.text;
        string raw = !string.IsNullOrWhiteSpace(onlineText) ? onlineText : offlineContent;
        

        if (debugVerbose)
        {
            Debug.Log($"[ExamUI] Response length={raw.Length}");
            Debug.Log($"[ExamUI] Raw Data ={raw}");
        }

        // Parse duration + questions
        if (TryExtractExamInformation(raw)) yield break;

        if (debugVerbose)
            Debug.Log(
                $"[ExamUI] Parsed: count={paper?.Count ?? 0}, duration={durationSeconds}s, title='{examTitle}', pass%={passPointPercent}");

        // Cập nhật header (chưa bắt đầu thi)
        examStarted = false;
        ClearContent();
        UpdateHeaderInfo();
        UpdateNavButtons();
        UpdateQuestionCounter();

        ShowLoading(false);
    }

    private bool TryExtractExamInformation(string raw)
    {
        string questionsJson = ExamFormat.ExtractQuestionsArray(raw);
        durationSeconds = ExamFormat.ExtractExamDuration(raw);

        if (string.IsNullOrEmpty(questionsJson))
        {
            Debug.LogError("[ExamUI] Không tìm thấy mảng \"questions\" trong JSON response.");
            ShowNoQuestion();
            ShowLoading(false);
            return true;
        }

        paper = FallbackParseToPaper(questionsJson);

        // Header info
        string descHtml = ExamFormat.ExtractStringField(raw, "description") ?? "";
        examTitle = ExamFormat.CleanHtmlToPlainText(descHtml);
        passPointPercent = ExamFormat.ExtractIntField(raw, "passPointPercent", 80);
        return false;
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

        RenderCurrentQuestion();

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
                    if (debugVerbose)
                        Debug.Log($"[ExamUI] TokenStore.AccessToken {(string.IsNullOrEmpty(value) ? "EMPTY" : "OK")}");
                    return value;
                }
            }
        }
        catch
        {
        }

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
        if (!examStarted) return;

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
             case ExamQuestionType.ESSAY:
                RenderEssay(q);
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
            string optText = ExamFormat.CleanOptionText(q.options[i]);
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
                        if (other != btn)
                            other.ActiveSelect(false);

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
        if (btnNopBai) btnNopBai.interactable = canNav;
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
    if (_isSubmitting) return;
    StartCoroutine(SubmitExamCoroutine(timeUp: false));
}

IEnumerator TimerCountdown()
{
    int remain = durationSeconds;
    _elapsedSeconds = 0;

    while (true)
    {
        if (textDemNguoc)
        {
            int mm = Mathf.Max(0, remain) / 60;
            int ss = Mathf.Max(0, remain) % 60;
            textDemNguoc.text = string.Format(timeFormat, mm, ss);
        }

        if (durationSeconds <= 0)
        {
            // không giới hạn: chỉ tăng elapsed và chờ user nộp
            _elapsedSeconds++;
            yield return new WaitForSeconds(1f);
            continue;
        }

        if (remain <= 0)
        {
            // Hết giờ -> cưỡng chế nộp
            StartCoroutine(SubmitExamCoroutine(timeUp: true));
            yield break;
        }

        yield return new WaitForSeconds(1f);
        _elapsedSeconds++;
        remain--;
    }
}

    void UpdateHeaderInfo()
    {
        int total = paper?.Count ?? 0;

        if (textExamTitle) textExamTitle.text = string.IsNullOrEmpty(examTitle) ? "Bài thi" : examTitle;

        if (textTotalQuestions) textTotalQuestions.text = $"{total}";

        if (textTotalDuration)
        {
            int mm = Mathf.Max(0, durationSeconds) / 60;
            int ss = Mathf.Max(0, durationSeconds) % 60;
            textTotalDuration.text = $"{string.Format(timeFormat, mm, ss)}";
        }

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
    void RenderEssay(ExamQuestion q)
    {
        // Hướng dẫn nho nhỏ (tuỳ chọn)
        SpawnQuestionText("(Nhập câu trả lời của bạn bên dưới)");

        if (prefabCauTraLoiTuLuan == null)
        {
            SpawnQuestionText("(Thiếu prefabCauTraLoiTuLuan)");
            return;
        }

        // Khởi tạo input
        var input = Instantiate(prefabCauTraLoiTuLuan, content);

        // Điền lại nội dung nếu đã lưu trước đó
        if (essayMap.TryGetValue(q.id, out var saved))
            input.text = saved;
        else
            input.text = "";

        // Lắng nghe thay đổi để lưu realtime
        input.onValueChanged.RemoveAllListeners();
        input.onValueChanged.AddListener((val) =>
        {
            essayMap[q.id] = val ?? "";
        });
    }

    [Serializable]
    public class ResultItem
    {
        public string questionId;
        public List<string> result;
    }

    [Serializable]
    public class SubmitBody
    {
        public string examId;
        public List<ResultItem> results = new List<ResultItem>();
        public int timeSpent; // giây
    }
    SubmitBody BuildSubmitBody(string examId)
    {
        var body = new SubmitBody
        {
            examId = examId,
            timeSpent = Mathf.Max(0, _elapsedSeconds)
        };

        if (paper?.questions == null) return body;

        foreach (var q in paper.questions)
        {
            var item = new ResultItem { questionId = q.id, result = new List<string>() };

            switch (q.type)
            {
                case ExamQuestionType.SINGLE_CHOICE:
                case ExamQuestionType.MULTIPLE_CHOICE:
                    if (selectedMap.TryGetValue(q.id, out var picked))
                    {
                        foreach (var idx in picked)
                        {
                            if (q.options != null && idx >= 0 && idx < q.options.Count)
                            {
                                // Nếu API cần chỉ số: item.result.Add(idx.ToString());
                                // Nếu API cần text (theo ví dụ swagger): dùng text sạch
                                var txt = ExamFormat.CleanOptionText(q.options[idx]);
                                item.result.Add(txt ?? "");
                            }
                        }
                    }
                    break;

                case ExamQuestionType.ESSAY:
                    if (essayMap.TryGetValue(q.id, out var essay) && !string.IsNullOrWhiteSpace(essay))
                        item.result.Add(essay);
                    break;
            }

            if (item.result == null) item.result = new List<string>();
            body.results.Add(item);
        }

        return body;
    }
    string BuildSubmitUrl(string courseId)
    {
        string baseUrl = GetBaseUrl();
        if (string.IsNullOrEmpty(baseUrl)) return null;
        if (!baseUrl.EndsWith("/")) baseUrl += "/";
        return baseUrl + "lms/result-exam/" + courseId;
    }

    string BuildGetResultUrl(string courseId, bool withCorrect)
    {
        var url = BuildSubmitUrl(courseId);
        if (string.IsNullOrEmpty(url)) return null;
        if (withCorrect) url += "?mode=show_correct_answer";
        return url;
    }
    [Header("Logging / JSON export")]
    public bool saveJsonToFile = true;
    public string jsonFolderName = "ExamLogs";
    public bool prettyPrintJson = true;

    IEnumerator SubmitExamCoroutine(bool timeUp)
    {
        if (!TryGetIds(out var examId, out var courseId))
        {
            Debug.LogError("[ExamUI] Submit failed: thiếu examId/courseId.");
            yield break;
        }

        if (_isSubmitting) yield break;
        _isSubmitting = true;

        ShowLoading(true);
        btnNopBai?.gameObject.SetActive(false);
        
        var body = BuildSubmitBody(examId);
        string json = JsonUtility.ToJson(body);
        if (debugVerbose) Debug.Log("[ExamUI] Submit JSON: " + json);

        if (saveJsonToFile)
        {
            var name = $"submit_{examId}_course_{overrideCourseId}";
            if (string.IsNullOrEmpty(overrideCourseId) && TryGetIds(out _, out var cid)) name = $"submit_{examId}_course_{cid}";
            SaveJsonToFile(name, json);
        }

            // PUT /lms/result-exam/{courseId}
            string submitUrl = BuildSubmitUrl(courseId);
        if (string.IsNullOrEmpty(submitUrl))
        {
            Debug.LogError("[ExamUI] Submit failed: không build được URL.");
            _isSubmitting = false;
            ShowLoading(false);
            yield break;
        }

        string token = GetAccessToken();

        using (var req = new UnityWebRequest(submitUrl, "PUT"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.timeout = Mathf.CeilToInt(Mathf.Max(1f, requestTimeout));
            req.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(token))
                req.SetRequestHeader("Authorization", "Bearer " + token);

            if (debugVerbose) Debug.Log("[ExamUI] PUT " + submitUrl);
            yield return req.SendWebRequest();

    #if UNITY_2020_2_OR_NEWER
            bool hasErr = req.result != UnityWebRequest.Result.Success;
    #else
            bool hasErr = req.isNetworkError || req.isHttpError;
    #endif
            if (hasErr)
            {
                Debug.LogError($"[ExamUI] Submit ERROR: {req.responseCode} {req.error}\n{req.downloadHandler?.text}");
                _isSubmitting = false;
                ShowLoading(false);
                yield break;
            }
        }

        if (debugVerbose) Debug.Log("[ExamUI] Submit OK.");

        // GET /lms/result-exam/{courseId}?mode=show_correct_answer
        string getUrl = BuildGetResultUrl(courseId, getWithCorrectAnswer);
        if (!string.IsNullOrEmpty(getUrl))
        {
            using (var getReq = UnityWebRequest.Get(getUrl))
            {
                getReq.timeout = Mathf.CeilToInt(Mathf.Max(1f, requestTimeout));
                if (!string.IsNullOrEmpty(token))
                    getReq.SetRequestHeader("Authorization", "Bearer " + token);

                if (debugVerbose) Debug.Log("[ExamUI] GET " + getUrl);
                yield return getReq.SendWebRequest();

    #if UNITY_2020_2_OR_NEWER
                bool hasErr2 = getReq.result != UnityWebRequest.Result.Success;
    #else
                bool hasErr2 = getReq.isNetworkError || getReq.isHttpError;
    #endif
                if (hasErr2)
                {
                    Debug.LogError($"[ExamUI] Get Result ERROR: {getReq.responseCode} {getReq.error}");
                }
                else
                {
                    var resultJson = getReq.downloadHandler?.text ?? "";
                    Debug.Log("[ExamUI] Result JSON:\n" + resultJson);
                    if (saveJsonToFile)
                    {
                        var name = $"result_{examId}_course_{courseId}" ;
                        SaveJsonToFile(name, resultJson);
                    }
                }
            }
        }

        ShowLoading(false);
        _isSubmitting = false;

        // Khoá điều hướng sau khi nộp (tuỳ chọn)
        btnBack?.gameObject.SetActive(false);
        btnNext?.gameObject.SetActive(false);
        btnNopBai?.gameObject.SetActive(false);
    }
    string GetJsonDir()
    {
        var dir = Path.Combine(Application.persistentDataPath, jsonFolderName);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
        return dir;
    }

    static string MakeSafeFilename(string s)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
            s = s.Replace(c, '_');
        return s;
    }

    static string PrettyJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return json;
        var indent = 0;
        var quoted = false;
        var sb = new StringBuilder();

        for (int i = 0; i < json.Length; i++)
        {
            char ch = json[i];

            switch (ch)
            {
                case '"':
                    sb.Append(ch);
                    bool escaped = false;
                    int j = i;
                    while (j > 0 && json[--j] == '\\') escaped = !escaped;
                    if (!escaped) quoted = !quoted;
                    break;

                case '{':
                case '[':
                    sb.Append(ch);
                    if (!quoted)
                    {
                        sb.Append('\n');
                        indent++;
                        sb.Append(new string(' ', indent * 2));
                    }
                    break;

                case '}':
                case ']':
                    if (!quoted)
                    {
                        sb.Append('\n');
                        indent = Math.Max(0, indent - 1);
                        sb.Append(new string(' ', indent * 2));
                        sb.Append(ch);
                    }
                    else sb.Append(ch);
                    break;

                case ',':
                    sb.Append(ch);
                    if (!quoted)
                    {
                        sb.Append('\n');
                        sb.Append(new string(' ', indent * 2));
                    }
                    break;

                case ':':
                    sb.Append(quoted ? ":" : ": ");
                    break;

                default:
                    sb.Append(ch);
                    break;
            }
        }
        return sb.ToString();
    }

    void SaveJsonToFile(string baseName, string json)
    {
        try
        {
            var dir = GetJsonDir();
            var stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var file = MakeSafeFilename($"{baseName}_{stamp}.json");
            var path = Path.Combine(dir, file);

            var data = prettyPrintJson ? PrettyJson(json) : json;
            File.WriteAllText(path, data, new UTF8Encoding(encoderShouldEmitUTF8Identifier:false));

            Debug.Log($"[ExamUI] JSON saved: {path}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ExamUI] SaveJsonToFile error: {ex}");
        }
    }

}