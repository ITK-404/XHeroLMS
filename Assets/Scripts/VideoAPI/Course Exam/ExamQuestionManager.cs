using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ExamQuestionManager : MonoBehaviour
{
    [Header("Where to spawn")]
    public Transform content;

    [Header("Prefabs")]
    public TMP_Text prefabCauHoi;
    public AnswerButton prefabCauTraLoi;
    public TMP_InputField prefabCauTraLoiTuLuan;

    [Header("Buttons & Timer")]
    public TMP_Text textQuestionCounter; // "01/30"
    public Button btnBack;
    public Button btnNext;
    public Button btnNopBai;

    public Image multiple_hint;
    public bool getWithCorrectAnswer = true;
    bool _isReviewMode = false;

    [Header("Panels")]
    public ExamConfirmPanel confirmPanel;
    public GameObject mainConfirmPanel;
    public GameObject mainExamPanelRoot;

    [Header("Result UI")]
    [SerializeField] private ExamResultUI resultUI;
    [SerializeField] private ExamResultReviewPanel reviewPanel;

    [Header("Review/Parsing options")]
    [SerializeField] private bool flipCorrectFromServer = false;

    [Header("Timer UI")]
    [SerializeField] private GameObject timerRoot;

    private int _lastQuestionIndexBeforeSubmit = -1;

    bool _isSubmitting = false;
    private ExamUIController _examUIController;

    private readonly Dictionary<string, HashSet<int>> selectedMap = new();
    private readonly List<AnswerButton> spawnedOptions = new();
    private readonly Dictionary<string, string> essayMap = new();

    public void SetReviewMode(bool enabled)
    {
        _isReviewMode = enabled;
        if (btnNopBai) btnNopBai.gameObject.SetActive(!enabled);
        if (timerRoot)  timerRoot.SetActive(!enabled);
    }

    protected void StopTimerSafe()
    {
        if (_examUIController != null) _examUIController.examStarted = false;
    }

    private void Awake()
    {
        _examUIController = GetComponent<ExamUIController>();
    }

    public void ClearContent()
    {
        if (!content) return;
        for (int i = content.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(content.GetChild(i).gameObject);
        spawnedOptions.Clear();
    }

    public void ShowNoQuestion()
    {
        ClearContent();
        SpawnQuestionText("(Không có câu hỏi)");
        UpdateNavButtons();
        UpdateQuestionCounter();
        _examUIController.UpdateHeaderInfo();
    }

    public void RenderCurrentQuestion()
    {
        if (_isReviewMode || !_examUIController.examStarted) return;

        var paper = _examUIController.Paper;
        var qs = paper?.questions;
        if (qs == null || qs.Count == 0) { ShowNoQuestion(); return; }

        _examUIController.currentIndex = Mathf.Clamp(_examUIController.currentIndex, 0, qs.Count - 1);
        var q = qs[_examUIController.currentIndex];

        ClearContent();
        SpawnQuestionText($"{_examUIController.currentIndex + 1}. {q.title}");

        switch (q.type)
        {
            case ExamQuestionType.SINGLE_CHOICE:
            case ExamQuestionType.MULTIPLE_CHOICE: RenderOptions(q); break;
            case ExamQuestionType.ESSAY: RenderEssay(q); break;
            default: SpawnQuestionText($"(Type {q.type} chưa hỗ trợ UI – sẽ cập nhật sau)"); break;
        }

        UpdateNavButtons();
        UpdateQuestionCounter();
    }

    void RenderOptions(ExamQuestion q)
    {
        if (q.options == null || q.options.Count == 0) { SpawnQuestionText("(Không có đáp án)"); return; }

        var picked = selectedMap.TryGetValue(q.id, out var set) ? set : (selectedMap[q.id] = new HashSet<int>());
        bool isSingle = q.type == ExamQuestionType.SINGLE_CHOICE;

        for (int i = 0; i < q.options.Count; i++)
        {
            var item = Instantiate(prefabCauTraLoi, content);
            spawnedOptions.Add(item);

            if (isSingle) item.ActiveSingleChoice();
            else          item.ActiveMultipleChoice();

            item.SetText(ExamFormat.CleanOptionText(q.options[i]));
            item.ActiveSelect(picked.Contains(i));

            int optionIndex = i;
            item.OnSelectButton = btn =>
            {
                bool turnOn = !btn.value;

                if (isSingle)
                {
                    foreach (var other in spawnedOptions) if (other != btn) other.ActiveSelect(false);
                    picked.Clear();
                }

                btn.ActiveSelect(turnOn);
                if (turnOn) picked.Add(optionIndex); else picked.Remove(optionIndex);
            };
        }
    }

    TMP_Text SpawnQuestionText(string text)
    {
        var t = UnityEngine.Object.Instantiate(prefabCauHoi, content);
        t.text = text ?? "";
        return t;
    }

    public void UpdateNavButtons()
    {
        if (_isReviewMode) return;
        bool canNav = _examUIController.examStarted && _examUIController.Paper != null && _examUIController.Paper.Count > 0;

        if (btnBack)   btnBack.interactable   = canNav && _examUIController.currentIndex > 0;
        if (btnNext)   btnNext.interactable   = canNav && _examUIController.currentIndex < _examUIController.Paper.Count - 1;
        if (btnNopBai) btnNopBai.interactable = canNav;
    }

    void Move(int delta)
    {
        if (_isReviewMode || !_examUIController.examStarted) return;
        var paper = _examUIController.Paper; if (paper == null) return;

        int idx = Mathf.Clamp(_examUIController.currentIndex + delta, 0, paper.Count - 1);
        if (idx == _examUIController.currentIndex) return;
        _examUIController.currentIndex = idx;
        RenderCurrentQuestion();
    }

    public void OnBack() => Move(-1);
    public void OnNext() => Move(+1);

    public void OnSubmit()
    {
        if (_isReviewMode) return;

        if (!_examUIController.examStarted) return;
        _lastQuestionIndexBeforeSubmit = _examUIController.currentIndex;
        if (confirmPanel != null)
        {
            if (mainExamPanelRoot) mainExamPanelRoot.SetActive(false);
            if (mainConfirmPanel) mainConfirmPanel.SetActive(true);
            confirmPanel.examPanelRoot = mainExamPanelRoot;
            confirmPanel.gameObject.SetActive(true);
            confirmPanel.Show(_examUIController, selectedMap, essayMap, _lastQuestionIndexBeforeSubmit);
        }
        else
        {
            Debug.LogWarning("[ExamUI] Chưa gán ExamConfirmPanel!");
        }
    }

    public void SubmitExamNow()
    {
        if (_isSubmitting) return;

        StopTimerSafe();
        SetReviewMode(true);

        if (mainConfirmPanel) mainConfirmPanel.SetActive(false);
        if (mainExamPanelRoot) mainExamPanelRoot.SetActive(true);

        StartCoroutine(SubmitExamCoroutine(timeUp: false));
    }

    public void ReturnToLastQuestion()
    {
        if (mainConfirmPanel) mainConfirmPanel.SetActive(false);
        if (mainExamPanelRoot) mainExamPanelRoot.SetActive(true);

        if (_lastQuestionIndexBeforeSubmit >= 0)
        {
            _examUIController.currentIndex = _lastQuestionIndexBeforeSubmit;
            RenderCurrentQuestion();
        }
    }

    [Serializable] public class ResultItem { public string questionId; public List<string> result; }
    [Serializable] public class SubmitBody { public string examId; public List<ResultItem> results = new(); public int timeSpent; }

    SubmitBody BuildSubmitBody(string examId)
    {
        var body = new SubmitBody { examId = examId, timeSpent = Mathf.Max(0, _examUIController._elapsedSeconds) };

        if (_examUIController.Paper?.questions == null) return body;

        foreach (var q in _examUIController.Paper.questions)
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
        var baseUrl = _examUIController.GetBaseUrl();
        if (string.IsNullOrEmpty(baseUrl)) return null;
        if (!baseUrl.EndsWith("/")) baseUrl += "/";
        return $"{baseUrl}lms/result-exam/{courseId}";
    }

    string BuildGetResultUrl(string courseId, bool withCorrect)
    {
        var url = BuildSubmitUrl(courseId);
        return string.IsNullOrEmpty(url) ? null : withCorrect ? $"{url}?mode=show_correct_answer" : url;
    }

    public IEnumerator SubmitExamCoroutine(bool timeUp)
    {
        if (!_examUIController.TryGetIds(out var examId, out var courseId)) { Debug.LogError("[ExamUI] Submit failed: thiếu examId/courseId."); yield break; }
        if (_isSubmitting) yield break;
        _isSubmitting = true;

        _examUIController.ShowLoading(true);
        btnNopBai?.gameObject.SetActive(false);

        var submitUrl = BuildSubmitUrl(courseId);
        if (string.IsNullOrEmpty(submitUrl)) { Debug.LogError("[ExamUI] Submit failed: không build được URL."); _isSubmitting = false; _examUIController.ShowLoading(false); yield break; }

        var json = JsonUtility.ToJson(BuildSubmitBody(examId));
        var token = _examUIController.GetAccessToken();

        using (var req = new UnityWebRequest(submitUrl, "PUT"))
        {
            req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
            req.downloadHandler = new DownloadHandlerBuffer();
            req.timeout = Mathf.CeilToInt(Mathf.Max(1f, _examUIController.requestTimeout));
            req.SetRequestHeader("Content-Type", "application/json");
            if (!string.IsNullOrEmpty(token)) req.SetRequestHeader("Authorization", "Bearer " + token);
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogError($"[ExamUI] Submit ERROR: {req.responseCode} {req.error}\n{req.downloadHandler?.text}");
                _isSubmitting = false; _examUIController.ShowLoading(false); yield break;
            }
        }

        var getUrl = BuildGetResultUrl(courseId, getWithCorrectAnswer);
        if (!string.IsNullOrEmpty(getUrl))
        {
            using (var getReq = UnityWebRequest.Get(getUrl))
            {
                getReq.timeout = Mathf.CeilToInt(Mathf.Max(1f, _examUIController.requestTimeout));
                if (!string.IsNullOrEmpty(token)) getReq.SetRequestHeader("Authorization", "Bearer " + token);

                StopTimerSafe(); SetReviewMode(true);
                yield return getReq.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
                if (getReq.result != UnityWebRequest.Result.Success)
#else
                if (getReq.isNetworkError || getReq.isHttpError)
#endif
                    Debug.LogError($"[ExamUI] Get Result ERROR: {getReq.responseCode} {getReq.error}");
                else
                {
                    var resultJson = getReq.downloadHandler?.text ?? "";
                    ShowResultFromJson(resultJson);
                    TryOpenReview(resultJson);
                }
            }
        }

        _examUIController.ShowLoading(false);
        _isSubmitting = false;

        btnBack?.gameObject.SetActive(true);
        btnNext?.gameObject.SetActive(true);
        btnNopBai?.gameObject.SetActive(false);
    }

    void RenderEssay(ExamQuestion q)
    {
        SpawnQuestionText("(Nhập câu trả lời của bạn bên dưới)");

        if (prefabCauTraLoiTuLuan == null)
        {
            SpawnQuestionText("(Thiếu prefabCauTraLoiTuLuan)");
            return;
        }

        var input = UnityEngine.Object.Instantiate(prefabCauTraLoiTuLuan, content);

        if (essayMap.TryGetValue(q.id, out var saved))
            input.text = saved;
        else
            input.text = "";

        input.onValueChanged.RemoveAllListeners();
        input.onValueChanged.AddListener((val) => { essayMap[q.id] = val ?? ""; });
    }

    public void UpdateQuestionCounter()
    {
        if (!textQuestionCounter) return;

        int total = _examUIController.Paper?.Count ?? 0;
        if (total <= 0)
        {
            textQuestionCounter.text = "00/00";
            return;
        }

        int current = _examUIController.examStarted ? Mathf.Clamp(_examUIController.currentIndex + 1, 1, total) : 0;
        int width = total.ToString().Length;
        string left = current.ToString().PadLeft(width, '0');
        string right = total.ToString().PadLeft(width, '0');
        textQuestionCounter.text = $"{left}/{right}";
    }

    // -------------------- RESULT HANDLING --------------------

    private struct ExamResultSummary
    {
        public int correct;
        public int wrong;
        public int skipped;
        public int total;
        public bool passed;
    }

    private void ShowResultFromJson(string json)
    {
        if (!resultUI)
        {
            Debug.LogWarning("[ExamUI] resultUI chưa được gán – bỏ qua hiển thị kết quả.");
            return;
        }

        var sum = ParseExamResultSummary(json);

        if (sum.total <= 0)   sum.total   = _examUIController.Paper?.Count ?? 0;
        if (sum.skipped < 0)  sum.skipped = Mathf.Max(0, sum.total - CountAnsweredLocal());
        if (sum.correct < 0)  sum.correct = 0;
        if (sum.wrong   < 0)  sum.wrong   = Mathf.Max(0, sum.total - sum.correct - sum.skipped);

        if (!HasPassFlagInJson(json))
        {
            int req = Mathf.CeilToInt(sum.total * Mathf.Clamp(_examUIController.passPointPercent, 0, 100) / 100f);
            sum.passed = (sum.correct >= req);
        }

        resultUI.textCorrectAnswer.text   = $"<color=#49D17D>{sum.correct}</color> câu";
        resultUI.textInCorrectAnswer.text = $"<color=#FF6B6B>{sum.wrong}</color> câu";
        resultUI.skipAnswer.text          = $"<color=#F4A42B>{sum.skipped}</color> câu";
        resultUI.SetTotalAnswerPass(sum.correct, sum.total);

        if (sum.passed) resultUI.ShowSuccess();
        else            resultUI.ShowUnSuccess();

        resultUI.Show();
    }

    private int CountAnsweredLocal()
    {
        int count = 0;
        if (_examUIController?.Paper?.questions == null) return 0;

        foreach (var q in _examUIController.Paper.questions)
        {
            switch (q.type)
            {
                case ExamQuestionType.SINGLE_CHOICE:
                case ExamQuestionType.MULTIPLE_CHOICE:
                    if (selectedMap.TryGetValue(q.id, out var picked) && picked != null && picked.Count > 0)
                        count++;
                    break;
                case ExamQuestionType.ESSAY:
                    if (essayMap.TryGetValue(q.id, out var txt) && !string.IsNullOrWhiteSpace(txt))
                        count++;
                    break;
            }
        }
        return count;
    }

    private ExamResultSummary ParseExamResultSummary(string json)
    {
        var s = new ExamResultSummary
        {
            correct = TryInt(json, "score", "correct", "correctCount", "totalCorrect"),
            wrong   = TryInt(json, "wrong", "incorrect", "incorrectCount", "totalWrong"),
            skipped = TryInt(json, "skip", "skipped", "not_answer", "unanswered", "skipCount"),
            total   = TryInt(json, "total", "totalQuestion", "questionCount", "totalQuestions"),
            passed  = TryBool(json, "is_passed", "passed", "isPassed", "pass")
        };
        return s;
    }

    private bool HasPassFlagInJson(string json)
    {
        return Regex.IsMatch(json ?? "", "\"(is_passed|passed|isPassed|pass)\"\\s*:\\s*(true|false)", RegexOptions.IgnoreCase);
    }

    private int TryInt(string json, params string[] keys)
    {
        if (string.IsNullOrEmpty(json)) return -1;
        foreach (var k in keys)
        {
            var m = Regex.Match(json, $"\"{Regex.Escape(k)}\"\\s*:\\s*(-?\\d+)");
            if (m.Success && int.TryParse(m.Groups[1].Value, out int v))
                return v;
        }
        return -1;
    }

    private bool TryBool(string json, params string[] keys)
    {
        if (string.IsNullOrEmpty(json)) return false;
        foreach (var k in keys)
        {
            var m = Regex.Match(json, $"\"{Regex.Escape(k)}\"\\s*:\\s*(true|false)", RegexOptions.IgnoreCase);
            if (m.Success) return m.Groups[1].Value.ToLower() == "true";
        }
        return false;
    }

    // ---------------- REVIEW OPEN ----------------

    private void TryOpenReview(string json)
    {
        if (reviewPanel == null || _examUIController?.Paper == null) return;

        var paper = _examUIController.Paper;

        // Map index đúng (nếu API trả đúng dạng text, mình map qua index bằng so sánh CleanText)
        var correctIndexMap = ParseCorrectAnswerIndicesFromJson(json, paper);

        // Map TEXT đúng (đã normalize để đối chiếu trong review)
        var correctTextMap = ParseCorrectAnswerTextsFromJson(json);

        var userPickedCopy = CloneUserPicked();

        reviewPanel.ShowReview(
            paper,
            userPickedCopy,
            correctIndexMap,
            0,
            correctTextMap
        );
    }

    // JSON: details[].questionId, details[].correct[] (string)
    [System.Serializable] private class DetailItem { public string questionId; public List<string> correct; }
    [System.Serializable] private class ResultRoot { public List<DetailItem> details; }

    private Dictionary<string, List<string>> ParseCorrectAnswerTextsFromJson(string json)
    {
        var map = new Dictionary<string, List<string>>();
        if (string.IsNullOrEmpty(json)) return map;

        try
        {
            var root = JsonUtility.FromJson<RootNode>(json);
            var qs = root?.data?.resultExam?.exam?.questions;
            if (qs == null) return map;

            foreach (var q in qs)
            {
                var list = new List<string>();
                if (q.correctAnswer != null)
                {
                    foreach (var s in q.correctAnswer)
                        list.Add(s ?? "");
                }
                map[q._id] = list;        // key = question _id
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ExamUI] ParseCorrectAnswerTextsFromJson failed: {e.Message}");
        }
        return map;
    }

    private Dictionary<string, List<int>> ParseCorrectAnswerIndicesFromJson(string json, ExamPaper paper)
    {
        // Map theo TEXT: correctAnswer[] (text/HTML) -> so khớp với options của paper để ra index 0-based
        var map = new Dictionary<string, List<int>>();
        if (string.IsNullOrEmpty(json) || paper?.questions == null) return map;

        try
        {
            var root = JsonUtility.FromJson<RootNode>(json);
            var serverQs = root?.data?.resultExam?.exam?.questions;
            if (serverQs == null) return map;

            foreach (var sq in serverQs)
            {
                var clientQ = paper.questions.Find(x => x.id == sq._id);
                if (clientQ?.options == null || sq.correctAnswer == null) continue;

                // Chuẩn hoá danh sách đáp án đúng từ server (bóc HTML, normalize)
                var correctNorms = new HashSet<string>();
                foreach (var s in sq.correctAnswer)
                {
                    var cleaned = ExamFormat.CleanOptionText(s) ?? "";
                    cleaned = cleaned.Trim();
                    if (!string.IsNullOrEmpty(cleaned))
                        correctNorms.Add(ExamResultReviewPanel.NormalizeForCompare(cleaned));
                }

                var idxs = new List<int>();
                for (int i = 0; i < clientQ.options.Count; i++)
                {
                    string optClean = ExamFormat.CleanOptionText(clientQ.options[i]) ?? "";
                    string optNorm = ExamResultReviewPanel.NormalizeForCompare(optClean);
                    if (correctNorms.Contains(optNorm))
                        idxs.Add(i);
                }

                map[sq._id] = idxs;  // index 0-based
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ExamUI] ParseCorrectAnswerIndicesFromJson failed: {e.Message}");
        }

        return map;
    }

    private Dictionary<string, HashSet<int>> CloneUserPicked()
    {
        var copy = new Dictionary<string, HashSet<int>>();
        foreach (var kv in selectedMap) copy[kv.Key] = new HashSet<int>(kv.Value);
        return copy;
    }
    // === DTO cho schema mới ===
    [Serializable] private class QuestionNode {
        public string _id;
        public List<string> answers;
        public List<string> correctAnswer;
        public string title;
        public string type;
    }
    [Serializable] private class ExamNode {
        public List<QuestionNode> questions;
    }
    [Serializable] private class ResultExamNode {
        public ExamNode exam;
    }
    [Serializable] private class DataNode {
        public ResultExamNode resultExam;
    }
    [Serializable] private class RootNode {
        public bool status;
        public DataNode data;
    }

}
