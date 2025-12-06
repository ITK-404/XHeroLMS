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
    [Header("Auth")]
    public bool useTokenFromStore = true;

    public string overrideAccessToken = "";

    public int currentIndex = 0;

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

    [Header("Options")]
    public bool autoStart = true;

    // public string timeFormat = "{0:00}:{1:00}";
    public float requestTimeout = 15f;

    [Header("Debug")]
    public bool debugVerbose = true;

    // ===== runtime =====
    private ExamPaper paper = new ExamPaper { questions = new List<ExamQuestion>() };
    public ExamPaper Paper => paper;
    private int durationSeconds = 0;
    public int DurationScends => durationSeconds;
    public Coroutine timerCo;

    public string examTitle = "";
    public string examName  = "";
    public int passPointPercent = 80;
    public bool examStarted = false;

    bool _loadingShown = false;
    public int _elapsedSeconds = 0;               // đếm thời gian làm bài (dùng cho timeSpent)

    private ExamQuestionManager _examQuestionManager;
    private ExamTitleManager _examTitleManager;
    private ExamExportJson _examExportJson;

    public ExamQuestionManager ExamQuestionManager => _examQuestionManager;
    
    public void ShowLoading(bool show)
    {
        if (show)
        {
            if (_loadingShown) return;
            LoadingUI.Show(
                timeoutSeconds: 60f,
                timeoutMessage: "Không thể tải nội dung.\nVui lòng kiểm tra kết nối mạng hoặc thử lại.",
                timeoutHeader:  "Lỗi Mạng"
            );
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
        _examQuestionManager = GetComponent<ExamQuestionManager>();
        _examTitleManager = GetComponent<ExamTitleManager>();
        _examExportJson = GetComponent<ExamExportJson>();
        if (_examQuestionManager.btnBack) _examQuestionManager.btnBack.onClick.AddListener(_examQuestionManager.OnBack);
        if (_examQuestionManager.btnNext) _examQuestionManager.btnNext.onClick.AddListener(_examQuestionManager.OnNext);
        if (_examQuestionManager.btnNopBai) _examQuestionManager.btnNopBai.onClick.AddListener(_examQuestionManager.OnSubmit);
        if (_examTitleManager.btnBatDau) _examTitleManager.btnBatDau.onClick.AddListener(_examTitleManager.BeginExam);

        // correctCheck.gameObject.SetActive(false);
        // inCorrectCheck.gameObject.SetActive(false);
    }

    void Start()
    {
        // Trước khi bắt đầu, khoá điều hướng
        _examQuestionManager.UpdateNavButtons();
        _examQuestionManager.UpdateQuestionCounter();
        _examTitleManager.UpdateHeaderInfo();

        if (autoStart) StartCoroutine(StartGate());
    }

    public IEnumerator StartGate()
    {
        ShowLoading(true);

        // BẮT BUỘC: chỉ khi có đủ ID thì mới load
        if (!TryGetIds(out var examId, out var courseId))
        {
            if (debugVerbose)
                Debug.LogWarning("[ExamUI] Thiếu ExamID/CourseID trong PlayerPrefs/override. Không gọi API.");

            // Tắt loading và hiển thị trạng thái rỗng
            _examQuestionManager.ShowNoQuestion();
            ShowLoading(false);

            // Bạn có thể disable btnBatDau cho rõ ràng
            if (_examTitleManager.btnBatDau) _examTitleManager.btnBatDau.interactable = false;
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
            _examQuestionManager.ShowNoQuestion();
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
    public string GetBaseUrl()
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
    public bool TryGetIds(out string examId, out string courseId)
    {
        examId = "";
        courseId = "";

        // override (ưu tiên cao nhất)
        if (!string.IsNullOrEmpty(overrideExamId)) examId = overrideExamId;
        if (!string.IsNullOrEmpty(overrideCourseId)) courseId = overrideCourseId;

        // PlayerPrefs (được CourseListView set)
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
                _examQuestionManager.ShowNoQuestion();
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
                _examQuestionManager.ShowNoQuestion();
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
                $"[ExamUI] Parsed: count={paper?.Count ?? 0}, duration={durationSeconds}s, title='{examTitle}', pass%={passPointPercent}, name='{examName}'");

        // Cập nhật header (chưa bắt đầu thi)
        // bật exam canvas lên
        
        examStarted = false;
        _examQuestionManager.ClearContent();
        UpdateHeaderInfo();
        _examQuestionManager.UpdateNavButtons();
        _examQuestionManager.UpdateQuestionCounter();

        ShowLoading(false);
    }

    // ============== PATCH: class tạm để parse đúng data.title, data.description, v.v. ==============
    [Serializable]
    private class ApiResp
    {
        public bool status;
        public DataObj data;
    }

    [Serializable]
    private class DataObj
    {
        public ScheduleOpen scheduleOpen;
        public string[] tag;
        public int duration;
        public int pointPerQuestion;
        public int passPointPercent;
        public string status;
        public int attemptCount;
        public int showAnswerMode;
        public int showQuestionMode;
        public int showRecommendMode;
        public bool isQuestionRandom;
        public List<QuestionStub> questions; // chỉ để JsonUtility parse, không dùng ở đây
        public string _id;
        public string title;                // tên đề thi đúng nằm ở đây
        public string description;          // HTML
        public string passText;
        public string failText;
        public string createdAt;
        public string updatedAt;
        public string img;
        public string createdBy;
    }

    [Serializable]
    private class ScheduleOpen
    {
        public bool isEnable;
        public long startTime;
        public long endTime;
    }

    [Serializable]
    private class QuestionStub
    {
        public List<string> answers;
        public List<string> tag;
        public string _id;
        public string title;    // title của CÂU HỎI (không dùng để set examName)
        public string keyword;
        public string type;
        public string explain;
        public string createdBy;
        public string createdAt;
        public string updatedAt;
        public int __v;
    }
    // ==============================================================================================

    private bool TryExtractExamInformation(string raw)
    {
        // Lấy mảng câu hỏi như cũ
        string questionsJson = ExamFormat.ExtractQuestionsArray(raw);
        // Lấy duration kiểu cũ (để fallback)
        int extractedDuration = ExamFormat.ExtractExamDuration(raw);

        if (string.IsNullOrEmpty(questionsJson))
        {
            Debug.LogError("[ExamUI] Không tìm thấy mảng \"questions\" trong JSON response.");
            _examQuestionManager.ShowNoQuestion();
            ShowLoading(false);
            return true;
        }

        // Parse paper như cũ
        paper = FallbackParseToPaper(questionsJson);

        // ===== parse có cấu trúc để lấy đúng data.title / data.description / data.duration / passPointPercent =====
        bool structuredOk = false;
        try
        {
            var resp = JsonUtility.FromJson<ApiResp>(raw);
            if (resp != null && resp.data != null)
            {
                // Name/Title của BÀI THI
                examName = resp.data.title ?? "";

                // Description HTML -> giữ pipeline vệ sinh HTML cũ
                string descHtml = resp.data.description ?? "";
                examTitle = ExamFormat.CleanHtmlToPlainText(descHtml);

                // Ưu tiên duration/pass từ backend nếu hợp lệ; nếu không, dùng giá trị cũ
                durationSeconds = resp.data.duration > 0 ? resp.data.duration : extractedDuration;
                passPointPercent = resp.data.passPointPercent > 0 ? resp.data.passPointPercent : ExamFormat.ExtractIntField(raw, "passPointPercent", 80);

                structuredOk = true;
            }
        }
        catch (Exception e)
        {
            if (debugVerbose) Debug.LogWarning("[ExamUI] Parse structured fields failed, will fallback. " + e);
        }

        if (!structuredOk)
        {
            string descHtml = ExamFormat.ExtractStringField(raw, "description") ?? "";
            examTitle = ExamFormat.CleanHtmlToPlainText(descHtml);
            examName  = ExamFormat.ExtractStringField(raw, "title") ?? "";
            durationSeconds = extractedDuration;
            passPointPercent = ExamFormat.ExtractIntField(raw, "passPointPercent", 80);
        }

        return false;
    }

    public string GetAccessToken()
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

    public void UpdateHeaderInfo()
    {
        _examTitleManager.UpdateHeaderInfo();
    }

    public void SaveJsonToFile(string baseName, string json)
    {
        _examExportJson.SaveJsonToFile(baseName, json);
    }
    public void RenderCurrentQuestion()
    {
        _examQuestionManager.RenderCurrentQuestion();
    }
    public IEnumerator SubmitExamCoroutine(bool timeUp)
    {
        yield return _examQuestionManager.SubmitExamCoroutine(timeUp);
    }

    // ============== RESTART EXAM (UPDATED) ==============
    public void RestartExam()
    {
        // 1. Dừng timer cũ (nếu có)
        if (timerCo != null)
        {
            StopCoroutine(timerCo);
            timerCo = null;
        }

        // 2. Dọn UI + state bên QuestionManager
        if (_examQuestionManager != null)
        {
            _examQuestionManager.HideReviewPanelIfAny();   // ẩn panel xem lại
            _examQuestionManager.HideResultPanelIfAny();   // ẩn panel kết quả (SUCCESS/FAIL)
            _examQuestionManager.ResetStateForNewAttempt();
            _examQuestionManager.SetReviewMode(false);
        }

        // 3. Reset state controller
        examStarted     = false;
        currentIndex    = 0;
        _elapsedSeconds = 0;

        // 4. Cập nhật header/nav
        UpdateHeaderInfo();
        _examQuestionManager.UpdateNavButtons();
        _examQuestionManager.UpdateQuestionCounter();

        // 5. Bắt đầu lại bài thi
        if (_examTitleManager != null)
        {
            _examTitleManager.BeginExam();   // bên trong nhớ set examStarted = true + RenderCurrentQuestion()
        }
        else if (_examQuestionManager != null)
        {
            // fallback nếu không có ExamTitleManager
            examStarted = true;
            _examQuestionManager.RenderCurrentQuestion();
        }
    }

    public void HideAll()
    {
        _examQuestionManager.gameObject.SetActive(false);
        _examTitleManager.gameObject.SetActive(false);
    }
    public Dictionary<string, Dictionary<int, int>> GetMatchingUserPairsSnapshot()
    {
        if (_examQuestionManager == null)
            return new Dictionary<string, Dictionary<int, int>>();

        return _examQuestionManager.GetMatchingUserPairsSnapshot();
    }

}
