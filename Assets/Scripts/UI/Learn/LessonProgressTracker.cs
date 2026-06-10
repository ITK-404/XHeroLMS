using System.Collections;
using UnityEngine;

public class LessonProgressTracker : MonoBehaviour
{
    public static LessonProgressTracker Instance;

    public LessonUI lessonUI;
    public VideoPlayerControllerPro videoPlayerControllerPro;
    public LmsVideoProgressApiClient lmsVideoProgressApiClient;
    public SceneLessonUI sceneLessonUI;

    private Coroutine postCoroutine;
    private bool hasPostedCompletion;
    private bool selectedLessonWasAlreadyComplete;
    private float sessionPlaybackMaxTime;

    private WaitForSecondsRealtime wait15s;
    private string courseID;

    public string CourseID
    {
        get => courseID;
    }

    private void Awake()
    {
        Instance = this;

        wait15s = new WaitForSecondsRealtime(15);

        videoPlayerControllerPro.GetSkipVideoDuration += GetSkipVideoDuration;
        sceneLessonUI.OnLoadCourseDone += OnLoadCourseDone;
    }

    private void OnLoadCourseDone(LmsCoursePrivate obj)
    {
        if (obj != null)
        {
            lmsVideoProgressApiClient.SetCourseID(obj._id);
            courseID = obj._id;
        }
    }

    private void OnDestroy()
    {
        videoPlayerControllerPro.GetSkipVideoDuration -= GetSkipVideoDuration;
        sceneLessonUI.OnLoadCourseDone -= OnLoadCourseDone;
        Instance = null;
    }

    private bool GetSkipVideoDuration(double second)
    {
        if (second <= 1) return true;
        if (lessonUI == null) return false;
        // if (second > lessonUI.progressTime) return false;

        return true;
    }

    private void Update()
    {
        if (lessonUI != null)
        {
            if (videoPlayerControllerPro == null || videoPlayerControllerPro.videoPlayer == null)
                return;

            float videoProgress = (float)videoPlayerControllerPro.videoPlayer.time;
            sessionPlaybackMaxTime = Mathf.Max(sessionPlaybackMaxTime, videoProgress);

            lessonUI.TryUpdateProgress(videoProgress);

            if (!hasPostedCompletion &&
                !selectedLessonWasAlreadyComplete &&
                lessonUI.duration > 0 &&
                lessonUI.IsPlaybackComplete(sessionPlaybackMaxTime))
            {
                hasPostedCompletion = true;

                lmsVideoProgressApiClient.SendProgress(lessonUI);
                ChapterUIManager.Instance?.UpdateLessonProgress();
            }
        }
    }

    private IEnumerator PostDataEvery15s()
    {
        while (true)
        {
            yield return wait15s;

            if (lessonUI != null)
            {
                if (!selectedLessonWasAlreadyComplete && !hasPostedCompletion)
                    lmsVideoProgressApiClient.SendProgress(lessonUI);
            }
        }
    }

    public void UpdateLesson(LessonUI newLessonUI)
    {
        if (postCoroutine != null)
        {
            StopCoroutine(postCoroutine);
            postCoroutine = null;
        }

        if (newLessonUI == null)
        {
            lessonUI = null;
            selectedLessonWasAlreadyComplete = false;
            hasPostedCompletion = false;
            sessionPlaybackMaxTime = 0f;
            return;
        }

        
        if (newLessonUI.type == CourseListView.FinalExamType)
        {
            Debug.Log("Ban da chon final exam, khong can update progress time");
            return;
        }
        
        if (lessonUI != null && !selectedLessonWasAlreadyComplete && !hasPostedCompletion)
            lmsVideoProgressApiClient.SendProgress(lessonUI);

        lessonUI = newLessonUI;
        selectedLessonWasAlreadyComplete = lessonUI.IsLessonDone();
        hasPostedCompletion = selectedLessonWasAlreadyComplete;
        sessionPlaybackMaxTime = 0f;

        if (!selectedLessonWasAlreadyComplete)
            postCoroutine = StartCoroutine(PostDataEvery15s());
    }

    private void OnDisable()
    {
        if (postCoroutine != null)
        {
            StopCoroutine(postCoroutine);
            postCoroutine = null;
        }
    }
}
