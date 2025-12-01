using DG.Tweening;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class FinalExamHandler : MonoBehaviour
{
    [Header("Exam Camera & Panel")]
    [SerializeField] private GameObject examPrefab;
    [SerializeField] private Transform examCamera;      // gán Main Camera (hoặc camera bạn dùng)
    [SerializeField] private ExamUIController examUIController;  // panel bài kiểm tra (ẩn sẵn)
    [SerializeField] private float examMoveDuration = 1.5f;
    private Coroutine examCamRoutine;

    private VideoPlayerControllerPro videoPlayerControllerPro;
    private PlayerStandUI playerStandUI;
    private LearnUI learnUI;

    private string courseID;
    public Camera UICamera;
    public PlayerPanelUI playerPanelUI;
    void Awake()
    {
        learnUI = FindAnyObjectByType<LearnUI>();
        videoPlayerControllerPro = FindAnyObjectByType<VideoPlayerControllerPro>();
        playerStandUI = FindAnyObjectByType<PlayerStandUI>();
    }
    private void Start()
    {
        UICamera = PlayerCamera.Instance.playerUICamera;
    }

    void Update()
    {
        if (ExamResultReviewPanel.FlagContinue)
        {
            // reset cờ NGAY ở đây để đảm bảo chỉ chạy một lần
            ExamResultReviewPanel.FlagContinue = false;
            ResetFromExam();
        }
    }
    
    public void SetCourseID(string newCourseID) => courseID = newCourseID;
    private GameObject currentExam;
    public void CreateExamPrefab()
    {
        if (currentExam != null)
        {
            Destroy(currentExam.gameObject);
            currentExam = null;
        }
        currentExam = Instantiate(examPrefab);
        var canvas = currentExam.GetComponent<Canvas>();
        canvas.worldCamera = UICamera;
        examUIController = currentExam.GetComponentInChildren<ExamUIController>();
    }
    
    public void OnClickFinalExam(LessonUI finalItem)
    {
        QuadCinemachineController.Instance.ChangeState(ViewState.Exam);

        PlayerPrefs.SetString("EXAM_CURRENT_ID", finalItem.lessonID);
        PlayerPrefs.SetString("EXAM_CURRENT_COURSE_ID", courseID);
        PlayerPrefs.Save();

        Debug.Log($"[CourseListView] Saved ExamID={finalItem.lessonID}, CourseID={courseID}");
        // ẩn UI học tập
        learnUI.Hide();
        videoPlayerControllerPro.ExitFullscreenUI();
        playerStandUI.HideWatchVideoUI();

        // hiện UI làm bài
        if (examCamRoutine != null)
            StopCoroutine(examCamRoutine);
        
        CreateExamPrefab();
        examCamRoutine = StartCoroutine(MoveCameraAndOpenExam());
        playerPanelUI.HideAll();
    }

    private IEnumerator MoveCameraAndOpenExam()
    {
        // examUIController.HideAll();
        // đi tới camera và ngồi
        ChangeToExamCamera();
        yield return new WaitForSecondsRealtime(2);
        
        // sau này kiểm tra có có submit chưa, có bắt đầu làm bài chưa
        examUIController.ExamQuestionManager.mainExamPanelRoot.gameObject.SetActive(true);
        yield return examUIController.StartGate();
        yield return new WaitForSecondsRealtime(0.1f);
        Debug.Log("[CourseListView] Camera đã tiến tới và cúi đầu, mở panel exam.");
    }

    private void ChangeToExamCamera()
    {
        var currentCheckPoint = PlayerChairManager.Instance.currentCheckPoint;

        if (currentCheckPoint != null)
        {
            QuadCameraManager.Instance.SetupSitdownCameraByCheckPoint(currentCheckPoint.examCheckPoint.transform);
        }
    }

    private void ResetExamCamera()
    {
        var currentCheckPoint = PlayerChairManager.Instance.currentCheckPoint;

        if (currentCheckPoint != null)
        {
            QuadCameraManager.Instance.SetupSitdownCameraByCheckPoint(currentCheckPoint.checkPoint.transform);
        }
    }

    private void ResetFromExam()
    {
        Debug.Log($"ResetFromExam Started {examUIController.examStarted}");
        Debug.Log($"ResetFromExam Submitted {examUIController.ExamQuestionManager.IsSubmitting}");
        // nếu đang làm bài và chưa submit thì hiện popup xác nhận
        if (examUIController.examStarted)
        {
            if (!examUIController.ExamQuestionManager.IsSubmitting)
            {
                examUIController.ExamQuestionManager.OnSubmit();
                // check right here
                return;
            }
        }
        
        QuadCinemachineController.Instance.ChangeState(ViewState.Sitdown);
        if (examCamRoutine != null)
        {
            StopCoroutine(examCamRoutine);
            examCamRoutine = null;
        }

        if (examUIController != null)
            examUIController.HideAll();

        examCamRoutine = StartCoroutine(ResetExamRoutine());
    }

    private IEnumerator MoveCameraBackFromExam()
    {
        ResetExamCamera();
        yield return new WaitForSecondsRealtime(2);

        playerStandUI.ShowWatchVideoUI();
        playerPanelUI.ShowLoginUI();
    }

    private IEnumerator ResetExamRoutine()
    {
        //reset data và ui
        currentExam.gameObject.SetActive(false);
        // examUIController.RestartExam();
        playerStandUI.HideButtons();
        yield return MoveCameraBackFromExam();

        learnUI.Show();
        videoPlayerControllerPro.EnterFullscreenUI();
        playerStandUI.ShowSitdownButton();

        Debug.Log("[CourseListView] ResetFromExam -> quay lại chế độ học (camera đã lerp về chỗ cũ).");
    }

}