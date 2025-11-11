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
    // ===================== Inspector =====================
    [Header("Where to spawn")]
    public Transform content;

    [Header("Prefabs")]
    public TMP_Text prefabCauHoi;
    public AnswerButton prefabCauTraLoi;
    public TMP_InputField prefabCauTraLoiTuLuan;

    [Header("Question Nav")]
    [SerializeField] private GameObject navRoot;
    [SerializeField] private Transform navContent;
    [SerializeField] private ExamInfoElement navItemPrefab;

    [Header("Buttons & Timer")]
    public TMP_Text textQuestionCounter; // "01/30"
    public Button btnBack;
    public Button btnNext;
    public Button btnNopBai;

    public Image multiple_hint;
    public bool getWithCorrectAnswer = true;

    [Header("Panels")]
    public ExamConfirmPanel confirmPanel;
    public GameObject mainConfirmPanel;
    public GameObject mainExamPanelRoot;

    [Header("Result UI")]
    [SerializeField] private ExamResultUI resultUI;
    [SerializeField] private ExamResultReviewPanel reviewPanel;

    [Header("Review/Parsing options")]
    [SerializeField] private bool flipCorrectFromServer = false; // hiện tại chưa dùng, giữ để không break inspector

    [Header("Timer UI")]
    [SerializeField] private GameObject timerRoot;

    // ===================== State =====================
    private bool _isReviewMode;
    private bool _isSubmitting;
    private int _lastQuestionIndexBeforeSubmit = -1;

    public ExamUIController _examUIController;

    private readonly Dictionary<string, HashSet<int>> selectedMap = new();
    private readonly Dictionary<string, string> essayMap = new();
    private readonly List<AnswerButton> spawnedOptions = new();

    private readonly Dictionary<string, int> _qidToIndex = new();
    public readonly List<ExamInfoElement> _navItems = new();

    // Regex parse JSON (compiled để giảm GC mỗi lần submit)
    private static readonly Regex ReInt = new Regex("\"(?<k>[^\"]+)\"\\s*:\\s*(-?\\d+)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex ReBool = new Regex("\"(?<k>(is_passed|passed|isPassed|pass))\"\\s*:\\s*(true|false)",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // ===================== Lifecycle =====================
    private void Awake()
    {
        _examUIController = GetComponent<ExamUIController>();
        if (navRoot) navRoot.SetActive(false);
    }

    // ===================== Public (called outside) =====================
    public void SetReviewMode(bool enabled)
    {
        _isReviewMode = enabled;
        if (btnNopBai) btnNopBai.gameObject.SetActive(!enabled);
        if (timerRoot) timerRoot.SetActive(!enabled);
        if (navRoot) navRoot.SetActive(true); // luôn cho hiện nav khi làm bài hoặc review
    }

    public void ShowNoQuestion()
    {
        ClearContent();
        SpawnQuestionText("(Không có câu hỏi)");
        UpdateNavButtons();
        UpdateQuestionCounter();
        _examUIController?.UpdateHeaderInfo();
    }

    public void RenderCurrentQuestion()
    {
        if (_isReviewMode || _examUIController == null || !_examUIController.examStarted) return;

        var paper = _examUIController.Paper;
        var qs = paper?.questions;
        if (qs == null || qs.Count == 0) { ShowNoQuestion(); return; }

        if (navRoot) navRoot.SetActive(true);

        BuildQuestionIndexMapOnce();
        RebuildQuestionNavIfNeeded();

        _examUIController.currentIndex = Mathf.Clamp(_examUIController.currentIndex, 0, qs.Count - 1);
        var q = qs[_examUIController.currentIndex];

        ClearContent();
        SpawnQuestionText($"{_examUIController.currentIndex + 1}. {q.title}");

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
                SpawnQuestionText($"(Type {q.type} chưa hỗ trợ UI)");
                break;
        }

        UpdateNavButtons();
        RefreshAllNavStates();
        UpdateQuestionCounter();
    }

    public void OnBack() => Move(-1);
    public void OnNext() => Move(+1);

    public void OnSubmit()
    {
        if (_isReviewMode || _examUIController == null || !_examUIController.examStarted) return;

        _lastQuestionIndexBeforeSubmit = _examUIController.currentIndex;

        if (!confirmPanel)
        {
            Debug.LogWarning("[ExamUI] Chưa gán ExamConfirmPanel!");
            return;
        }

        if (mainExamPanelRoot) mainExamPanelRoot.SetActive(false);
        if (mainConfirmPanel) mainConfirmPanel.SetActive(true);

        confirmPanel.examPanelRoot = mainExamPanelRoot;
        confirmPanel.gameObject.SetActive(true);
        confirmPanel.Show(_examUIController, selectedMap, essayMap, _lastQuestionIndexBeforeSubmit);
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

        if (_lastQuestionIndexBeforeSubmit >= 0 && _examUIController?.Paper != null)
        {
            _examUIController.currentIndex = _lastQuestionIndexBeforeSubmit;
            RenderCurrentQuestion();
        }
    }

    // ===================== Core Renders =====================
    private void RenderOptions(ExamQuestion q)
    {
        if (q.options == null || q.options.Count == 0)
        {
            SpawnQuestionText("(Không có đáp án)");
            return;
        }

        var picked = selectedMap.TryGetValue(q.id, out var set)
            ? set
            : (selectedMap[q.id] = new HashSet<int>());

        bool isSingle = q.type == ExamQuestionType.SINGLE_CHOICE;

        for (int i = 0; i < q.options.Count; i++)
        {
            var btn = Instantiate(prefabCauTraLoi, content);
            spawnedOptions.Add(btn);

            if (isSingle) btn.ActiveSingleChoice(); else btn.ActiveMultipleChoice();

            btn.SetText(CleanOption(q.options[i]));
            btn.ActiveSelect(picked.Contains(i));

            int optionIndex = i;
            btn.OnSelectButton = b =>
            {
                bool turnOn = !b.value;

                if (isSingle)
                {
                    foreach (var other in spawnedOptions) if (other != b) other.ActiveSelect(false);
                    picked.Clear();
                }

                b.ActiveSelect(turnOn);
                if (turnOn) picked.Add(optionIndex); else picked.Remove(optionIndex);

                RefreshSingleNavStateByQuestionId(q.id);
            };
        }
    }

    private void RenderEssay(ExamQuestion q)
    {
        SpawnQuestionText("(Nhập câu trả lời của bạn bên dưới)");

        if (!prefabCauTraLoiTuLuan)
        {
            SpawnQuestionText("(Thiếu prefabCauTraLoiTuLuan)");
            return;
        }

        var input = Instantiate(prefabCauTraLoiTuLuan, content);
        input.text = essayMap.TryGetValue(q.id, out var saved) ? saved : "";
        input.onValueChanged.RemoveAllListeners();
        input.onValueChanged.AddListener(val =>
        {
            essayMap[q.id] = val ?? "";
            RefreshSingleNavStateByQuestionId(q.id);
        });
    }

    // ===================== Submit / Result =====================
    [Serializable] public class ResultItem { public string questionId; public List<string> result; }
    [Serializable] public class SubmitBody { public string examId; public List<ResultItem> results = new(); public int timeSpent; }

    public IEnumerator SubmitExamCoroutine(bool timeUp)
    {
        if (_examUIController == null || !_examUIController.TryGetIds(out var examId, out var courseId))
        {
            Debug.LogError("[ExamUI] Submit failed: thiếu examId/courseId.");
            yield break;
        }

        if (_isSubmitting) yield break;
        _isSubmitting = true;

        _examUIController.ShowLoading(true);
        if (btnNopBai) btnNopBai.gameObject.SetActive(false);

        // PUT
        var submitUrl = BuildSubmitUrl(courseId);
        if (string.IsNullOrEmpty(submitUrl))
        {
            Debug.LogError("[ExamUI] Submit failed: không build được URL.");
            _isSubmitting = false; _examUIController.ShowLoading(false);
            yield break;
        }

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
            bool hasErr = req.result != UnityWebRequest.Result.Success;
#else
            bool hasErr = req.isNetworkError || req.isHttpError;
#endif
            if (hasErr)
            {
                Debug.LogError($"[ExamUI] Submit ERROR: {req.responseCode} {req.error}\n{req.downloadHandler?.text}");
                _isSubmitting = false; _examUIController.ShowLoading(false);
                yield break;
            }
        }

        // GET result (kèm đáp án đúng nếu bật cờ)
        var getUrl = BuildGetResultUrl(courseId, getWithCorrectAnswer);
        if (!string.IsNullOrEmpty(getUrl))
        {
            using (var getReq = UnityWebRequest.Get(getUrl))
            {
                getReq.timeout = Mathf.CeilToInt(Mathf.Max(1f, _examUIController.requestTimeout));
                if (!string.IsNullOrEmpty(token)) getReq.SetRequestHeader("Authorization", "Bearer " + token);

                StopTimerSafe();
                SetReviewMode(true);

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
                    ShowResultFromJson(resultJson);
                    TryOpenReview(resultJson);
                }
            }
        }

        _examUIController.ShowLoading(false);
        _isSubmitting = false;

        // Bật nav buttons khi vào review
        if (btnBack) btnBack.gameObject.SetActive(true);
        if (btnNext) btnNext.gameObject.SetActive(true);
        if (btnNopBai) btnNopBai.gameObject.SetActive(false);
    }

    private SubmitBody BuildSubmitBody(string examId)
    {
        var body = new SubmitBody { examId = examId, timeSpent = Mathf.Max(0, _examUIController?._elapsedSeconds ?? 0) };
        var qs = _examUIController?.Paper?.questions;
        if (qs == null) return body;

        foreach (var q in qs)
        {
            var item = new ResultItem { questionId = q.id, result = new List<string>() };

            switch (q.type)
            {
                case ExamQuestionType.SINGLE_CHOICE:
                case ExamQuestionType.MULTIPLE_CHOICE:
                    if (selectedMap.TryGetValue(q.id, out var picked))
                    {
                        foreach (var idx in picked)
                            if (q.options != null && idx >= 0 && idx < q.options.Count)
                                item.result.Add(q.options[idx] ?? ""); // gửi RAW
                    }
                    break;

                case ExamQuestionType.ESSAY:
                    if (essayMap.TryGetValue(q.id, out var essay) && !string.IsNullOrWhiteSpace(essay))
                        item.result.Add(essay);
                    break;
            }

            body.results.Add(item);
        }
        return body;
    }

    private void ShowResultFromJson(string json)
    {
        if (!resultUI) return;

        var sum = ParseExamResultSummary(json);

        // Fallback tự tính nếu server không trả đủ
        bool needFallbackCount = (sum.correct < 0 || sum.wrong < 0);
        var paper = _examUIController?.Paper;
        if (needFallbackCount && paper?.questions != null)
        {
            var correctIndexMap = ParseCorrectAnswerIndicesFromJson(json, paper);
            var correctTextMap = ParseCorrectAnswerTextsFromJson(json);

            (int corr, int wr, int skip) = ComputeSummaryFromDetails(
                paper, selectedMap, correctIndexMap, correctTextMap);

            sum.correct = corr;
            sum.wrong = wr;
            sum.skipped = skip;
            sum.total = paper.Count;
        }

        // Hoàn thiện số liệu
        if (sum.total <= 0) sum.total = _examUIController?.Paper?.Count ?? 0;
        if (sum.skipped < 0) sum.skipped = Mathf.Max(0, sum.total - CountAnsweredLocal());
        if (sum.correct < 0) sum.correct = 0;
        if (sum.wrong < 0) sum.wrong = Mathf.Max(0, sum.total - sum.correct - sum.skipped);

        if (!HasPassFlagInJson(json))
        {
            int req = Mathf.CeilToInt(sum.total * Mathf.Clamp(_examUIController?.passPointPercent ?? 0, 0, 100) / 100f);
            sum.passed = (sum.correct >= req);
        }

        // Render UI
        resultUI.textCorrectAnswer.text   = $"<color=#49D17D>{sum.correct}</color> câu";
        resultUI.textInCorrectAnswer.text = $"<color=#FF6B6B>{sum.wrong}</color> câu";
        resultUI.skipAnswer.text          = $"<color=#F4A42B>{sum.skipped}</color> câu";
        resultUI.SetTotalAnswerPass(sum.correct, sum.total);
        if (sum.passed) resultUI.ShowSuccess(); else resultUI.ShowUnSuccess();
        resultUI.Show();
    }

    // ===================== Review open =====================
    private void TryOpenReview(string json)
    {
        if (!reviewPanel || _examUIController?.Paper == null) return;

        var paper = _examUIController.Paper;
        var correctIndexMap = ParseCorrectAnswerIndicesFromJson(json, paper);
        var correctTextMap  = ParseCorrectAnswerTextsFromJson(json);

        reviewPanel.ShowReview(
            paper,
            CloneUserPicked(),
            correctIndexMap,
            0,
            correctTextMap
        );
    }

    // ===================== Nav =====================
    private void Move(int delta)
    {
        if (_isReviewMode || _examUIController == null || !_examUIController.examStarted) return;

        var paper = _examUIController.Paper;
        if (paper == null) return;

        int idx = Mathf.Clamp(_examUIController.currentIndex + delta, 0, paper.Count - 1);
        if (idx == _examUIController.currentIndex) return;

        _examUIController.currentIndex = idx;
        RenderCurrentQuestion();
        RefreshAllNavStates();
    }

    public void RebuildQuestionNavIfNeeded()
    {
        var qs = _examUIController?.Paper?.questions;
        if (qs == null || navContent == null || navItemPrefab == null) return;

        if (_navItems.Count == qs.Count && navContent.childCount == qs.Count) return;

        for (int i = navContent.childCount - 1; i >= 0; i--)
            Destroy(navContent.GetChild(i).gameObject);
        _navItems.Clear();

        for (int i = 0; i < qs.Count; i++)
        {
            var el = Instantiate(navItemPrefab, navContent);
            el.SetQuestionIndexText(i + 1);

            int targetIndex = i;
            var b = el.GetButton();
            if (b)
            {
                b.onClick.RemoveAllListeners();
                b.onClick.AddListener(() => SelectQuestion(targetIndex));
            }

            el.SetUnansweredButton();
            _navItems.Add(el);
        }
    }

    private void SelectQuestion(int idx)
    {
        var paper = _examUIController?.Paper;
        if (paper == null || _isReviewMode || !_examUIController.examStarted) return;

        idx = Mathf.Clamp(idx, 0, paper.Count - 1);
        if (_examUIController.currentIndex == idx) return;

        _examUIController.currentIndex = idx;
        RenderCurrentQuestion();
        RefreshAllNavStates();
        UpdateNavButtons();
    }

    public void ReviewHighlightNavIndex(int index)
    {
        if (_navItems == null || _navItems.Count == 0) return;

        for (int i = 0; i < _navItems.Count; i++)
        {
            var el = _navItems[i];
            if (!el) continue;

            if (i == index) el.ShowSelectedAnswerButton();
            else el.SetUnansweredButton();
        }
    }

    private void RefreshAllNavStates()
    {
        var qs = _examUIController?.Paper?.questions;
        if (qs == null) return;

        for (int i = 0; i < qs.Count; i++)
        {
            var el = (i < _navItems.Count) ? _navItems[i] : null;
            if (!el) continue;

            if (i == _examUIController.currentIndex) el.ShowSelectedAnswerButton();
            else if (IsAnswered(qs[i].id, qs[i].type)) el.SetAnsweredButton();
            else el.SetUnansweredButton();
        }
    }

    private void RefreshSingleNavStateByQuestionId(string qid)
    {
        if (!_qidToIndex.TryGetValue(qid, out int idx)) return;
        var qs = _examUIController?.Paper?.questions;
        if (qs == null || idx < 0 || idx >= qs.Count || idx >= _navItems.Count) return;

        var el = _navItems[idx];
        if (!el) return;

        if (idx == _examUIController.currentIndex) el.ShowSelectedAnswerButton();
        else if (IsAnswered(qid, qs[idx].type)) el.SetAnsweredButton();
        else el.SetUnansweredButton();
    }

    // ===================== Utils =====================
    protected void StopTimerSafe()
    {
        if (_examUIController != null) _examUIController.examStarted = false;
    }

    public void ClearContent()
    {
        if (!content) return;
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
        spawnedOptions.Clear();
    }

    private TMP_Text SpawnQuestionText(string text)
    {
        var t = Instantiate(prefabCauHoi, content);
        t.text = text ?? "";
        return t;
    }

    public void UpdateNavButtons()
    {
        if (_isReviewMode || _examUIController == null) return;

        bool ready = _examUIController.examStarted && _examUIController.Paper != null && _examUIController.Paper.Count > 0;
        if (btnBack)   btnBack.interactable   = ready && _examUIController.currentIndex > 0;
        if (btnNext)   btnNext.interactable   = ready && _examUIController.currentIndex < _examUIController.Paper.Count - 1;
        if (btnNopBai) btnNopBai.interactable = ready;
    }

    public void UpdateQuestionCounter()
    {
        if (!textQuestionCounter || _examUIController == null) return;

        int total = _examUIController.Paper?.Count ?? 0;
        int current = (_examUIController.examStarted && total > 0)
            ? Mathf.Clamp(_examUIController.currentIndex + 1, 1, total)
            : 0;

        textQuestionCounter.text = FormatCounter(current, total);
    }

    private static string FormatCounter(int current, int total)
    {
        int width = Mathf.Max(2, total.ToString().Length);
        return $"{current.ToString().PadLeft(width, '0')}/{total.ToString().PadLeft(width, '0')}";
    }

    private void BuildQuestionIndexMapOnce()
    {
        var qs = _examUIController?.Paper?.questions;
        if (qs == null || _qidToIndex.Count == qs.Count) return;

        _qidToIndex.Clear();
        for (int i = 0; i < qs.Count; i++)
            _qidToIndex[qs[i].id] = i;
    }

    private bool IsAnswered(string qid, ExamQuestionType type)
    {
        return type switch
        {
            ExamQuestionType.SINGLE_CHOICE or ExamQuestionType.MULTIPLE_CHOICE
                => selectedMap.TryGetValue(qid, out var set) && set != null && set.Count > 0,
            ExamQuestionType.ESSAY
                => essayMap.TryGetValue(qid, out var txt) && !string.IsNullOrWhiteSpace(txt),
            _ => false
        };
    }

    private int CountAnsweredLocal()
    {
        int count = 0;
        var qs = _examUIController?.Paper?.questions;
        if (qs == null) return 0;

        foreach (var q in qs)
            if (IsAnswered(q.id, q.type)) count++;

        return count;
    }

    private Dictionary<string, HashSet<int>> CloneUserPicked()
    {
        var copy = new Dictionary<string, HashSet<int>>();
        foreach (var kv in selectedMap) copy[kv.Key] = new HashSet<int>(kv.Value);
        return copy;
    }

    private static string CleanOption(string s) => ExamFormat.CleanOptionText(s);

    // ===================== Parse + Compare =====================
    private struct ExamResultSummary { public int correct, wrong, skipped, total; public bool passed; }

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

    private bool HasPassFlagInJson(string json) => ReBool.IsMatch(json ?? "");

    private int TryInt(string json, params string[] keys)
    {
        if (string.IsNullOrEmpty(json)) return -1;
        foreach (Match m in ReInt.Matches(json))
        {
            string k = m.Groups["k"].Value;
            foreach (var want in keys)
            {
                if (!k.Equals(want, StringComparison.OrdinalIgnoreCase)) continue;
                if (int.TryParse(m.Groups[0].Captures.Count > 0 ? m.Groups[0].Value.Split(':')[1] : m.Groups[0].Value, out _)) { }
                if (int.TryParse(m.Groups[m.Groups.Count - 1].Value, out int v)) return v;
                if (int.TryParse(m.Value.Substring(m.Value.LastIndexOf(':') + 1), out v)) return v;
            }
        }
        return -1;
    }

    private bool TryBool(string json, params string[] keys)
    {
        if (string.IsNullOrEmpty(json)) return false;
        foreach (Match m in ReBool.Matches(json))
        {
            string k = m.Groups[1].Value;
            foreach (var want in keys)
                if (k.Equals(want, StringComparison.OrdinalIgnoreCase))
                    return m.Value.Contains("true", StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    // DTO theo schema server
    [Serializable] private class QuestionNode { public string _id; public List<string> answers; public List<string> correctAnswer; public string title; public string type; }
    [Serializable] private class ExamNode { public List<QuestionNode> questions; }
    [Serializable] private class ResultExamNode { public ExamNode exam; }
    [Serializable] private class DataNode { public ResultExamNode resultExam; }
    [Serializable] private class RootNode { public bool status; public DataNode data; }

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
                map[q._id] = q.correctAnswer != null ? new List<string>(q.correctAnswer) : new List<string>();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ExamUI] ParseCorrectAnswerTextsFromJson failed: {e.Message}");
        }
        return map;
    }

    private Dictionary<string, List<int>> ParseCorrectAnswerIndicesFromJson(string json, ExamPaper paper)
    {
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

                var correctNorms = new HashSet<string>();
                foreach (var s in sq.correctAnswer)
                {
                    var cleaned = (ExamFormat.CleanOptionText(s) ?? "").Trim();
                    if (!string.IsNullOrEmpty(cleaned))
                        correctNorms.Add(ExamResultReviewPanel.NormalizeForCompare(cleaned));
                }

                var idxs = new List<int>();
                for (int i = 0; i < clientQ.options.Count; i++)
                {
                    string optClean = ExamFormat.CleanOptionText(clientQ.options[i]) ?? "";
                    string optNorm = ExamResultReviewPanel.NormalizeForCompare(optClean);
                    if (correctNorms.Contains(optNorm)) idxs.Add(i);
                }

                map[sq._id] = idxs; // 0-based
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[ExamUI] ParseCorrectAnswerIndicesFromJson failed: {e.Message}");
        }

        return map;
    }

    private (int correct, int wrong, int skipped) ComputeSummaryFromDetails(
        ExamPaper paper,
        Dictionary<string, HashSet<int>> userPicked,
        Dictionary<string, List<int>> correctIndexMap,
        Dictionary<string, List<string>> correctTextMap)
    {
        int c = 0, w = 0, s = 0;

        // chuẩn bị map text đúng -> set normalized
        var correctTextSets = new Dictionary<string, HashSet<string>>();
        if (correctTextMap != null)
        {
            foreach (var kv in correctTextMap)
            {
                var set = new HashSet<string>();
                var list = kv.Value ?? new List<string>();
                foreach (var t in list)
                {
                    var cleaned = (ExamFormat.CleanOptionText(t) ?? "");
                    cleaned = Regex.Replace(cleaned, @"\s+", " ").Trim();
                    if (!string.IsNullOrEmpty(cleaned))
                        set.Add(ExamResultReviewPanel.NormalizeForCompare(cleaned));
                }
                correctTextSets[kv.Key] = set;
            }
        }

        foreach (var q in paper.questions)
        {
            userPicked.TryGetValue(q.id, out var uSet);
            if (uSet == null || uSet.Count == 0) { s++; continue; }

            var corrIdx = new HashSet<int>();
            if (correctIndexMap != null && correctIndexMap.TryGetValue(q.id, out var listIdx))
                foreach (var x in listIdx) corrIdx.Add(x);

            correctTextSets.TryGetValue(q.id, out var corrTxt);

            bool isCorrect = IsExactlyCorrectLocal(q, uSet, corrIdx, corrTxt);
            if (isCorrect) c++; else w++;
        }

        return (c, w, s);
    }

    // So khớp đúng/sai: gộp đáp án đúng từ index (0-based) + text normalized
    private static bool IsExactlyCorrectLocal(
        ExamQuestion q,
        HashSet<int> userSet,
        HashSet<int> correctIndexSet0Based,
        HashSet<string> correctTextSet)
    {
        if (q?.options == null) return false;
        userSet ??= new HashSet<int>();

        var combinedCorrect = new HashSet<int>();
        if (correctIndexSet0Based != null)
            foreach (var idx in correctIndexSet0Based) combinedCorrect.Add(idx);

        if (correctTextSet != null)
        {
            for (int i = 0; i < q.options.Count; i++)
            {
                string optClean = ExamFormat.CleanOptionText(q.options[i]) ?? "";
                string optNorm = ExamResultReviewPanel.NormalizeForCompare(optClean);
                if (correctTextSet.Contains(optNorm)) combinedCorrect.Add(i);
            }
        }

        if (userSet.Count == 0 || combinedCorrect.Count == 0) return false;
        if (userSet.Count != combinedCorrect.Count) return false;

        foreach (var v in userSet) if (!combinedCorrect.Contains(v)) return false;
        return true;
    }
    
    private string BuildSubmitUrl(string courseId)
    {
        var baseUrl = _examUIController?.GetBaseUrl();
        if (string.IsNullOrEmpty(baseUrl)) return null;
        if (!baseUrl.EndsWith("/")) baseUrl += "/";
        return $"{baseUrl}lms/result-exam/{courseId}";
    }

    private string BuildGetResultUrl(string courseId, bool withCorrect)
    {
        var url = BuildSubmitUrl(courseId);
        return string.IsNullOrEmpty(url) ? null : (withCorrect ? $"{url}?mode=show_correct_answer" : url);
    }
}
