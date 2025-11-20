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
    private Vector3 defaultCameraPosition;
    private Quaternion defaultCameraRotation;
    private bool hasDefaultCameraTransform;

    [SerializeField] private CinemachineHardLookAt examLookAt;
    private Vector3 defaultLookAtOffset;
    private bool hasDefaultOffset;
    
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

        learnUI.Hide();
        videoPlayerControllerPro.ExitFullscreenUI();
        playerStandUI.HideWatchVideoUI();

        if (examCamRoutine != null)
            StopCoroutine(examCamRoutine);
        
        CreateExamPrefab();
        examCamRoutine = StartCoroutine(MoveCameraAndOpenExam());
        playerPanelUI.Hide();
    }

    private bool InitExamCamera()
    {
        // if (examCamera == null || examUIController == null)
        // {
        //     Debug.LogWarning("[CourseListView] examCamera hoặc examPanelRoot chưa gán.");
        //     return false;
        // }

        if (examLookAt == null)
            examLookAt = examCamera.GetComponent<CinemachineHardLookAt>();

        if (examLookAt == null)
        {
            Debug.LogWarning("[CourseListView] Không tìm thấy CinemachineHardLookAt trên examCamera.");
            return false;
        }

        if (!hasDefaultOffset)
        {
            defaultLookAtOffset = examLookAt.LookAtOffset;
            hasDefaultOffset = true;
        }

        if (!hasDefaultCameraTransform)
        {
            defaultCameraPosition = examCamera.position;
            defaultCameraRotation = examCamera.rotation;
            hasDefaultCameraTransform = true;
        }

        return true;
    }

    private IEnumerator MoveCameraAndOpenExam()
    {
        if (!InitExamCamera())
            yield break;

        // examUIController.HideAll();
        
        Vector3 startPos = examCamera.position;
        Vector3 endPos = new Vector3(startPos.x, 0.3f, startPos.z + 0.5f);

        float dur = Mathf.Max(0.01f, examMoveDuration);
        float t = 0f;

        // Tiến tới
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            float k = Mathf.SmoothStep(0f, 1f, t);
            examCamera.position = Vector3.Lerp(startPos, endPos, k);
            yield return null;
        }
        examCamera.position = endPos;

        // Cúi đầu
        Vector3 startOffset = examLookAt.LookAtOffset;
        Vector3 endOffset = startOffset;
        endOffset.y = -270f;

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            float k = Mathf.SmoothStep(0f, 1f, t);
            examLookAt.LookAtOffset = Vector3.Lerp(startOffset, endOffset, k);
            yield return null;
        }
        // sau này kiểm tra có có submit chưa, có bắt đầu làm bài chưa
        examLookAt.LookAtOffset = endOffset;
        examUIController.ExamQuestionManager.mainExamPanelRoot.gameObject.SetActive(true);
        yield return examUIController.StartGate();
        yield return new WaitForSecondsRealtime(0.1f);
        Debug.Log("[CourseListView] Camera đã tiến tới và cúi đầu, mở panel exam.");
    }

    private void ResetFromExam()
    {
        Debug.Log($"ResetFromExam Started {examUIController.examStarted}");
        Debug.Log($"ResetFromExam Submitted {examUIController.ExamQuestionManager.IsSubmitting}");

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
        if (!InitExamCamera())
            yield break;

        Vector3 startPos = examCamera.position;
        Quaternion startRot = examCamera.rotation;
        Vector3 startOffset = examLookAt.LookAtOffset;

        Vector3 endPos = hasDefaultCameraTransform ? defaultCameraPosition : startPos;
        Quaternion endRot = hasDefaultCameraTransform ? defaultCameraRotation : startRot;
        Vector3 endOffset = hasDefaultOffset ? defaultLookAtOffset : startOffset;

        float halfDur = Mathf.Max(0.01f, examMoveDuration) * 0.5f;
        float t;

        // Ngửa đầu
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / halfDur;
            float k = Mathf.SmoothStep(0f, 1f, t);
            examLookAt.LookAtOffset = Vector3.Lerp(startOffset, endOffset, k);
            yield return null;
        }
        examLookAt.LookAtOffset = endOffset;

        // Lùi về vị trí/rotation ban đầu
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / halfDur;
            float k = Mathf.SmoothStep(0f, 1f, t);
            examCamera.position = Vector3.Lerp(startPos, endPos, k);
            examCamera.rotation = Quaternion.Slerp(startRot, endRot, k);
            yield return null;
        }

        examCamera.position = endPos;
        examCamera.rotation = endRot;
        
        playerStandUI.ShowWatchVideoUI();
        playerPanelUI.Show();
    }

    private IEnumerator ResetExamRoutine()
    {
        //reset data và ui
        currentExam.gameObject.SetActive(false);
        // examUIController.RestartExam();
        
        yield return MoveCameraBackFromExam();

        learnUI.Show();
        videoPlayerControllerPro.EnterFullscreenUI();
        playerStandUI.ShowSitdownButton();

        Debug.Log("[CourseListView] ResetFromExam -> quay lại chế độ học (camera đã lerp về chỗ cũ).");
    }

}