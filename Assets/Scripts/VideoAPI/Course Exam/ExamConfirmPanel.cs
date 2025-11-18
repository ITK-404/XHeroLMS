using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ExamConfirmPanel : MonoBehaviour
{
    [Header("Summary Info")]
    [SerializeField] private TMP_Text txtExamTitle;
    [SerializeField] private TMP_Text txtTotalQuestion;
    [SerializeField] private TMP_Text txtTotalTime;
    [SerializeField] private TMP_Text txtRequirement;
    [SerializeField] private TMP_Text txtDoneCount;

    [Header("Timer Countdown (optional)")]
    [SerializeField] private TMP_Text txtSpentTime;

    [Header("List of Questions")]
    [SerializeField] private Transform contentQuestions;
    [SerializeField] private ExamInfoElement prefabQuestionButton;

    [Header("Buttons")]
    [SerializeField] private Button btnBackToExam;
    [SerializeField] private Button btnSubmitFinal;

    [Header("Roots")]
    [SerializeField] private GameObject confirmPanelRoot;
    [SerializeField] private GameObject resultUIPanelRoot;

    [Header("Bring result panel to top")]
    [SerializeField] private bool forceTopCanvas = true;
    [SerializeField] private int resultSortingOrder = 5000;

    [HideInInspector] public GameObject examPanelRoot;

    private ExamUIController _uiController;
    private ExamQuestionManager _manager;
    
    private Coroutine _countdownMirrorCo;

    public void Show(
        ExamUIController uiController,
        Dictionary<string, HashSet<int>> selectedMap,
        Dictionary<string, string> essayMap,
        int currentIndex)
    {
        _uiController = uiController;
        _manager = uiController.GetComponent<ExamQuestionManager>();

        var paper = uiController.Paper;
        if (paper == null || paper.questions == null)
        {
            Debug.LogError("[ExamConfirmPanel] Paper null!");
            return;
        }

        int total = paper.questions.Count;
        int doneCount = CountAnswered(paper, selectedMap, essayMap);

        string examTitle = uiController.examTitle;
        string examName       = uiController.examName;
        int   durationSeconds = uiController.DurationScends;
        int   durationMinutes = Mathf.CeilToInt(durationSeconds / 60f);
        int   passPercent     = uiController.passPointPercent;
        int requiredCorrect = Mathf.CeilToInt(total * Mathf.Clamp(passPercent, 0, 100) / 100f);

        // txtExamTitle.text     = string.IsNullOrEmpty(examTitle) ? "(Không có tiêu đề)" : examTitle;
        txtExamTitle.text = !string.IsNullOrWhiteSpace(examName) ? examName : "(Không có tên khóa học!)";

        txtTotalQuestion.text = $"{total} câu";
        txtTotalTime.text     = $"{durationMinutes} phút";
        txtRequirement.text   = $"{requiredCorrect}/{total}";
        txtDoneCount.text     = $"{doneCount}/{total}";

        // --- Bắt đầu đếm ngược ---
        if (txtSpentTime)
        {
            if (_countdownMirrorCo != null) StopCoroutine(_countdownMirrorCo);
            _countdownMirrorCo = StartCoroutine(MirrorCountdownText());
        }

        // === Danh sách câu hỏi ===
        foreach (Transform child in contentQuestions)
            Destroy(child.gameObject);

        for (int i = 0; i < total; i++)
        {
            var q = paper.questions[i];
            var element = Instantiate(prefabQuestionButton, contentQuestions);
            element.SetQuestionIndexText(i + 1);

            bool isAnswered = IsQuestionAnswered(q, selectedMap, essayMap);
            if (i == currentIndex) element.ShowSelectedAnswerButton();
            else if (isAnswered)   element.SetAnsweredButton();
            else                   element.SetUnansweredButton();

            int index = i;
            var btn = element.GetButton();
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    if (examPanelRoot) examPanelRoot.SetActive(true);
                    _uiController.currentIndex = index;
                    _manager.RenderCurrentQuestion();
                    HideConfirmOnly();
                });
            }
        }

        // --- Buttons ---
        btnBackToExam.onClick.RemoveAllListeners();
        btnBackToExam.onClick.AddListener(() =>
        {
            if (examPanelRoot) examPanelRoot.SetActive(true);
            _manager.ReturnToLastQuestion();
            HideConfirmOnly();
        });

        btnSubmitFinal.onClick.RemoveAllListeners();
        btnSubmitFinal.onClick.AddListener(() =>
        {
            HideConfirmOnly();

            if (examPanelRoot) examPanelRoot.SetActive(false);

            ShowResultPanelOnTop();
            _manager.SubmitExamNow();
        });
    }

    // textDemNguoc -> txtSpentTime trong khi panel đang mở
    private IEnumerator MirrorCountdownText()
    {
        var titleMgr = _uiController ? _uiController.GetComponent<ExamTitleManager>() : null;
        while (true)
        {
            // nếu panel bị ẩn đi, dừng mirror
            var root = confirmPanelRoot ? confirmPanelRoot : gameObject;
            if (!root.activeInHierarchy) break;

            if (txtSpentTime && titleMgr && titleMgr.textDemNguoc)
                txtSpentTime.text = titleMgr.textDemNguoc.text;
                
            yield return new WaitForSeconds(0.2f);
        }
        _countdownMirrorCo = null;
    }

    private void HideConfirmOnly()
    {
        var go = confirmPanelRoot ? confirmPanelRoot : gameObject;

        // dừng mirror khi đóng panel
        if (_countdownMirrorCo != null) { StopCoroutine(_countdownMirrorCo); _countdownMirrorCo = null; }

        go.SetActive(false);
    }

    private void ShowResultPanelOnTop()
    {
        var result = resultUIPanelRoot ? resultUIPanelRoot : gameObject;
        //result.transform.SetAsLastSibling();

        //if (forceTopCanvas)
        //{
        //    var cv = result.GetComponent<Canvas>();
        //    if (!cv) cv = result.AddComponent<Canvas>();
        //    cv.overrideSorting = true;
        //    cv.sortingOrder = resultSortingOrder;

        //    if (!result.GetComponent<GraphicRaycaster>())
        //        result.AddComponent<GraphicRaycaster>();

        //    var cg = result.GetComponent<CanvasGroup>();
        //    if (!cg) cg = result.AddComponent<CanvasGroup>();
        //    cg.interactable = true;
        //    cg.blocksRaycasts = true;
        //    cg.ignoreParentGroups = false;
        //}

        result.SetActive(true);
    }

    private int CountAnswered(ExamPaper paper,
        Dictionary<string, HashSet<int>> selectedMap,
        Dictionary<string, string> essayMap)
    {
        int count = 0;
        foreach (var q in paper.questions)
            if (IsQuestionAnswered(q, selectedMap, essayMap)) count++;
        return count;
    }

    private bool IsQuestionAnswered(ExamQuestion q,
        Dictionary<string, HashSet<int>> selectedMap,
        Dictionary<string, string> essayMap)
    {
        switch (q.type)
        {
            case ExamQuestionType.SINGLE_CHOICE:
            case ExamQuestionType.MULTIPLE_CHOICE:
                return selectedMap.ContainsKey(q.id) && selectedMap[q.id].Count > 0;
            case ExamQuestionType.ESSAY:
                return essayMap.ContainsKey(q.id) && !string.IsNullOrWhiteSpace(essayMap[q.id]);
            default:
                return false;
        }
    }
}
