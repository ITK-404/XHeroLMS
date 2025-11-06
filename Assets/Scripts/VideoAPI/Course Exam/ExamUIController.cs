using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
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

    private string examTitle = "";
    private int passPointPercent = 80;
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

    // ===================== FETCH + SETUP =====================
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

            if (remain <= 0)
            {
                OnSubmit();
                yield break;
            }

            yield return new WaitForSeconds(1f);
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
}