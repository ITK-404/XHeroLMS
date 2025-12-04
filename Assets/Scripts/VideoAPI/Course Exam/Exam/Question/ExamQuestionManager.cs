using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExamQuestionManager : MonoBehaviour
{
    // ===================== Inspector =====================
    [Header("Where to spawn")]
    public Transform content;

    [Header("Prefabs")]
    public TMP_Text    prefabCauHoi;
    public AnswerButton prefabCauTraLoi;
    public GameObject   prefabCauTraLoiTuLuan;

    [Tooltip("Prefab cho câu hỏi MATCHING (chứa UI + script SetupQuestion, v.v.)")]
    public GameObject prefabMatching;

    [Header("Question Nav")]
    [SerializeField] private GameObject     navRoot;
    [SerializeField] private Transform      navContent;
    [SerializeField] private ExamInfoElement navItemPrefab;

    [Header("Buttons & Timer")]
    public TMP_Text textQuestionCounter; // "01/30"
    public Button   btnBack;
    public Button   btnNext;
    public Button   btnNopBai;

    public Image  multiple_hint;
    [Tooltip("Khi submit xong gọi GET result với đáp án đúng")]
    public bool getWithCorrectAnswer = true;

    [Header("Type Hint")]
    [SerializeField] protected GameObject typeHintRoot;
    [SerializeField] private   TMP_Text   typeHintText;

    [Header("Panels")]
    public ExamConfirmPanel confirmPanel;
    public GameObject       mainConfirmPanel;
    public GameObject       mainExamPanelRoot;

    [Header("Result UI")]
    [SerializeField] private ExamResultUI          resultUI;
    [SerializeField] private ExamResultReviewPanel reviewPanel;

    [Header("Timer UI")]
    [SerializeField] private GameObject timerRoot;

    // ======= RANDOM QUESTION SETTINGS =======
    [Header("Random Question Settings")]
    [Tooltip("Bật lên nếu muốn random câu hỏi từ bộ API.")]
    [SerializeField] private bool randomizeQuestions = true;

    [Tooltip("Số câu cần dùng cho đề thi. Nếu <= 0 hoặc lớn hơn tổng câu thì sẽ dùng tất cả.")]
    private int numberOfQuestions = 0;

    private CertificatesExamUI _certificatesExamUI;
    // ========================================

    // ===================== State =====================
    private bool _isReviewMode;
    private bool _isSubmitting;
    private int  _lastQuestionIndexBeforeSubmit = -1;

    public ExamUIController _examUIController;

    // user state
    private readonly Dictionary<string, HashSet<int>> selectedMap    = new();
    public  readonly Dictionary<string, string>       essayMap       = new();
    private readonly List<AnswerButton>               spawnedOptions = new();

    // MATCHING: q.id -> (leftIndex -> rightIndex)
    private readonly Dictionary<string, Dictionary<int, int>> matchingMap =
        new Dictionary<string, Dictionary<int, int>>();

    private readonly Dictionary<string, int> _qidToIndex = new();
    public  readonly List<ExamInfoElement>   _navItems    = new();

    public bool IsSubmitting => _isSubmitting;

    // services
    private QuestionRandomizer    _randomizer;
    private ExamSubmissionService _submissionService;

    // ===================== Lifecycle =====================
    private void Awake()
    {
        _certificatesExamUI = FindAnyObjectByType<CertificatesExamUI>();
        _examUIController   = GetComponent<ExamUIController>();

        // init randomizer & submission service
        _randomizer = new QuestionRandomizer(randomizeQuestions, numberOfQuestions);
        _submissionService = new ExamSubmissionService(
            _examUIController,
            resultUI,
            reviewPanel,
            () => getWithCorrectAnswer,
            selectedMap,
            essayMap,
            _certificatesExamUI
        );

        if (navRoot) navRoot.SetActive(false);
    }

    private void Start()
    {
        numberOfQuestions = _examUIController?.Paper.Count ?? 0;
    }

    // ===================== Public (called outside) =====================
    public void SetReviewMode(bool enabled)
    {
        _isReviewMode = enabled;
        if (btnNopBai) btnNopBai.gameObject.SetActive(!enabled);
        if (timerRoot) timerRoot.SetActive(!enabled);
        if (navRoot)   navRoot.SetActive(true); // luôn cho hiện nav khi làm bài hoặc review
    }

    public void ShowNoQuestion()
    {
        ClearContent();
        SpawnQuestionText("(Không có câu hỏi)");
        UpdateNavButtons();
        UpdateQuestionCounter();
        _examUIController?.UpdateHeaderInfo();

        if (typeHintRoot) typeHintRoot.SetActive(false);
    }

    public void RenderCurrentQuestion()
    {
        if (_isReviewMode || _examUIController == null || !_examUIController.examStarted) return;

        var paper = _examUIController.Paper;
        var qs    = paper?.questions;
        if (qs == null || qs.Count == 0)
        {
            ShowNoQuestion();
            return;
        }

        // Random / cắt câu (chỉ 1 lần / attempt)
        _randomizer.ApplyRandomQuestionFilterIfNeeded(paper);

        // Update ref sau random
        paper = _examUIController.Paper;
        qs    = paper?.questions;
        if (qs == null || qs.Count == 0)
        {
            ShowNoQuestion();
            return;
        }

        if (navRoot) navRoot.SetActive(true);

        BuildQuestionIndexMapOnce();
        RebuildQuestionNavIfNeeded();

        _examUIController.currentIndex =
            Mathf.Clamp(_examUIController.currentIndex, 0, qs.Count - 1);
        var q = qs[_examUIController.currentIndex];

        Debug.Log($"[Render] QID: {q.id}, Type: {q.type}, Options: {(q.options?.Count ?? 0)}");

        ClearContent();
        SpawnQuestionText($"{_examUIController.currentIndex + 1}. {q.title}");

        UpdateTypeHint(q.type);

        switch (q.type)
        {
            case ExamQuestionType.SINGLE_CHOICE:
            case ExamQuestionType.MULTIPLE_CHOICE:
                // Nếu không có đáp án trắc nghiệm -> xem như câu tự luận
                if (q.options == null || q.options.Count == 0)
                {
                    RenderEssay(q);
                }
                else
                {
                    RenderOptions(q);
                }
                break;

            case ExamQuestionType.ESSAY:
                RenderEssay(q);
                break;

            case ExamQuestionType.MATCHING:
                RenderMatching(q);
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
        if (mainConfirmPanel)  mainConfirmPanel.SetActive(true);

        confirmPanel.examPanelRoot = mainExamPanelRoot;
        confirmPanel.gameObject.SetActive(true);
        confirmPanel.Show(_examUIController, selectedMap, essayMap, _lastQuestionIndexBeforeSubmit);
    }

    public void SubmitExamNow()
    {
        if (_isSubmitting) return;

        StopTimerSafe();
        SetReviewMode(true);

        if (mainConfirmPanel)  mainConfirmPanel.SetActive(false);
        if (mainExamPanelRoot) mainExamPanelRoot.SetActive(true);

        StartCoroutine(SubmitExamRoutine(false));
    }

    private IEnumerator SubmitExamRoutine(bool timeUp)
    {
        if (_examUIController == null) yield break;

        _isSubmitting = true;
        _examUIController.ShowLoading(true);
        if (btnNopBai) btnNopBai.gameObject.SetActive(false);

        yield return _submissionService.SubmitExamCoroutine(timeUp);

        _examUIController.ShowLoading(false);
        _isSubmitting = false;

        // Bật nav buttons khi vào review
        if (btnBack) btnBack.gameObject.SetActive(true);
        if (btnNext) btnNext.gameObject.SetActive(true);
        if (btnNopBai) btnNopBai.gameObject.SetActive(false);
    }

    public void ReturnToLastQuestion()
    {
        if (mainConfirmPanel)  mainConfirmPanel.SetActive(false);
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

            if (isSingle) btn.ActiveSingleChoice();
            else          btn.ActiveMultipleChoice();

            btn.SetText(CleanOption(q.options[i]));
            btn.ActiveSelect(picked.Contains(i));

            int optionIndex = i;
            btn.OnSelectButton = b =>
            {
                bool turnOn = !b.value;

                if (isSingle)
                {
                    foreach (var other in spawnedOptions)
                        if (other != b) other.ActiveSelect(false);
                    picked.Clear();
                }

                b.ActiveSelect(turnOn);
                if (turnOn) picked.Add(optionIndex);
                else        picked.Remove(optionIndex);

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

        // Instantiate prefab (GameObject) chứa TMP_InputField
        var go = Instantiate(prefabCauTraLoiTuLuan, content);

        // Tìm TMP_InputField trong chính nó hoặc con của nó
        var input = go.GetComponentInChildren<TMP_InputField>();
        if (input == null)
        {
            SpawnQuestionText("(Prefab câu trả lời tự luận không có TMP_InputField)");
            return;
        }

        // Set lại text từ essayMap nếu có
        input.text = essayMap.TryGetValue(q.id, out var saved) ? saved : "";

        // Gán listener để lưu lại câu trả lời
        input.onValueChanged.RemoveAllListeners();
        input.onValueChanged.AddListener(val =>
        {
            essayMap[q.id] = val ?? "";
            RefreshSingleNavStateByQuestionId(q.id);
        });
    }

private void RenderMatching(ExamQuestion q)
{
    if (!prefabMatching)
    {
        SpawnQuestionText("(Thiếu prefabMatching cho câu MATCHING)");
        return;
    }

    var go = Instantiate(prefabMatching, content);

    var handler = go.GetComponent<MatchingElementHandler>();
    if (handler != null)
    {
        // lấy state đã lưu nếu có
        matchingMap.TryGetValue(q.id, out var currentPairs);

        handler.SetupQuestion(
            q,
            currentPairs,
            updatedPairs => { SetMatchingAnswer(q.id, updatedPairs); }
        );
    }
    else
    {
        // fallback nếu prefab chưa gán script đúng
        go.SendMessage("SetupQuestion", q, SendMessageOptions.DontRequireReceiver);
    }
}

    /// <summary>
    /// Dùng cho MATCHING – gọi từ script trên prefabMatching khi user thay đổi đáp án.
    /// </summary>
    public void SetMatchingAnswer(string questionId, Dictionary<int, int> pairs)
    {
        if (pairs == null || pairs.Count == 0)
        {
            matchingMap.Remove(questionId);
        }
        else
        {
            matchingMap[questionId] = pairs;
        }

        RefreshSingleNavStateByQuestionId(questionId);
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
        EventHub.RaiseExamClampItem(idx);
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
            else            el.SetUnansweredButton();
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
        else if (IsAnswered(qid, qs[idx].type))    el.SetAnsweredButton();
        else                                       el.SetUnansweredButton();

        EventHub.RaiseExamCenterItem(idx);
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

        bool ready = _examUIController.examStarted &&
                     _examUIController.Paper != null &&
                     _examUIController.Paper.Count > 0;

        if (btnBack)   btnBack.interactable   = ready && _examUIController.currentIndex > 0;
        if (btnNext)   btnNext.interactable   = ready && _examUIController.currentIndex < _examUIController.Paper.Count - 1;
        if (btnNopBai) btnNopBai.interactable = ready;
    }

    public void UpdateQuestionCounter()
    {
        if (!textQuestionCounter || _examUIController == null) return;

        int total   = _examUIController.Paper?.Count ?? 0;
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
                => selectedMap.TryGetValue(qid, out var set) &&
                   set != null && set.Count > 0,

            ExamQuestionType.ESSAY
                => essayMap.TryGetValue(qid, out var txt) &&
                   !string.IsNullOrWhiteSpace(txt),

            ExamQuestionType.MATCHING
                => matchingMap.TryGetValue(qid, out var pairs) &&
                   pairs != null && pairs.Count > 0,

            _ => false
        };
    }

    private static string CleanOption(string s) => ExamFormat.CleanOptionText(s);

    // ========== RESET STATE KHI THI LẠI ==========
    public void ResetStateForNewAttempt()
    {
        _isReviewMode = false;
        _isSubmitting = false;
        _lastQuestionIndexBeforeSubmit = -1;

        _randomizer?.ResetForNewAttempt(_examUIController?.Paper);

        selectedMap.Clear();
        essayMap.Clear();
        matchingMap.Clear();
        _qidToIndex.Clear();

        if (navContent != null)
        {
            for (int i = navContent.childCount - 1; i >= 0; i--)
                Destroy(navContent.GetChild(i).gameObject);
        }
        _navItems.Clear();

        ClearContent();

        if (navRoot) navRoot.SetActive(false);

        if (btnBack)   btnBack.gameObject.SetActive(true);
        if (btnNext)   btnNext.gameObject.SetActive(true);
        if (btnNopBai) btnNopBai.gameObject.SetActive(true);
    }

    public void HideReviewPanelIfAny()
    {
        if (reviewPanel != null)
        {
            reviewPanel.HideReview();
        }
    }

    public void HideResultPanelIfAny()
    {
        if (resultUI != null && resultUI.gameObject != null)
        {
            resultUI.gameObject.SetActive(false);
        }
    }

    private void UpdateTypeHint(ExamQuestionType type)
    {
        if (typeHintRoot == null || typeHintText == null) return;

        switch (type)
        {
            case ExamQuestionType.SINGLE_CHOICE:
                typeHintRoot.SetActive(true);
                typeHintText.text = "Chỉ chọn 1 đáp án";
                break;

            case ExamQuestionType.MULTIPLE_CHOICE:
                typeHintRoot.SetActive(true);
                typeHintText.text = "Có thể chọn nhiều đáp án";
                break;

            case ExamQuestionType.MATCHING:
                typeHintRoot.SetActive(true);
                typeHintText.text = "Nối các cặp tương ứng";
                break;

            default:
                typeHintRoot.SetActive(false);
                break;
        }
    }

    public IEnumerator SubmitExamCoroutine(bool timeUp)
    {
        yield return _submissionService.SubmitExamCoroutine(timeUp);
    }
}
