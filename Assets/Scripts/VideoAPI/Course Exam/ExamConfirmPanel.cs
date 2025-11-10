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

    [Header("List of Questions")]
    [SerializeField] private Transform contentQuestions;
    [SerializeField] private ExamInfoElement prefabQuestionButton;

    [Header("Buttons")]
    [SerializeField] private Button btnBackToExam;
    [SerializeField] private Button btnSubmitFinal;

    [Header("Roots")]
    //Panel confirm thật sự cần ẩn/hiện. Nếu rỗng sẽ dùng chính GameObject của script.
    [SerializeField] private GameObject confirmPanelRoot;
    //Panel kết quả (ExamResultUI) sẽ được đẩy lên top và bật khi nộp.
    [SerializeField] private GameObject resultUIPanelRoot;

    [Header("Bring result panel to top")]
    //Ép result panel có Canvas riêng và override sorting để luôn nằm trên cùng.
    [SerializeField] private bool forceTopCanvas = true;
    [SerializeField] private int resultSortingOrder = 5000;

    // Truyền từ Manager
    [HideInInspector] public GameObject examPanelRoot;

    private ExamUIController _uiController;
    private ExamQuestionManager _manager;

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

        string examTitle      = uiController.examTitle;
        int   durationSeconds = uiController.DurationScends;
        int   durationMinutes = Mathf.CeilToInt(durationSeconds / 60f);
        int   passPercent     = uiController.passPointPercent;

        int requiredCorrect = Mathf.CeilToInt(total * Mathf.Clamp(passPercent, 0, 100) / 100f);

        txtExamTitle.text     = string.IsNullOrEmpty(examTitle) ? "(Không có tiêu đề)" : examTitle;
        txtTotalQuestion.text = $"{total} câu";
        txtTotalTime.text     = $"{durationMinutes} phút";
        txtRequirement.text   = $"{requiredCorrect}/{total}";
        txtDoneCount.text     = $"{doneCount}/{total}";

        foreach (Transform child in contentQuestions) Destroy(child.gameObject);

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
            // Ẩn confirm
            HideConfirmOnly();

            // ẩn panel làm bài để tránh che sau lưng
            if (examPanelRoot) examPanelRoot.SetActive(false);

            // top + bật
            ShowResultPanelOnTop();

            // PUT -> GET -> ExamQuestionManager
            _manager.SubmitExamNow();
        });
    }

    // --- UI helpers ---

    private void HideConfirmOnly()
    {
        var go = confirmPanelRoot ? confirmPanelRoot : gameObject;
        go.SetActive(false);
    }

    private void ShowResultPanelOnTop()
    {
        var result = resultUIPanelRoot ? resultUIPanelRoot : gameObject;
        // Đẩy lên cuối cùng trong cùng parent 
        result.transform.SetAsLastSibling();

        if (forceTopCanvas)
        {
            var cv = result.GetComponent<Canvas>();
            if (!cv) cv = result.AddComponent<Canvas>();
            cv.overrideSorting = true;
            cv.sortingOrder = resultSortingOrder;

            // đảm bảo nhận input và mờ nền
            if (!result.GetComponent<GraphicRaycaster>())
                result.AddComponent<GraphicRaycaster>();

            var cg = result.GetComponent<CanvasGroup>();
            if (!cg) cg = result.AddComponent<CanvasGroup>();
            cg.interactable = true;
            cg.blocksRaycasts = true;
            cg.ignoreParentGroups = false;
        }

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
