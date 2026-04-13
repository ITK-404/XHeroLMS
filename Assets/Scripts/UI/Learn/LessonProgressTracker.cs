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
        if (second > lessonUI.progressTime) return false;

        return true;
    }

    private void Update()
    {
        if (lessonUI != null)
        {
            float videoProgress = (float)videoPlayerControllerPro.videoPlayer.time;
            lessonUI.TryUpdateProgress(videoProgress);

            // khi xem xong
            if (!hasPostedCompletion &&
                lessonUI.duration > 0 &&
                lessonUI.progressTime >= lessonUI.duration)
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
                lmsVideoProgressApiClient.SendProgress(lessonUI);
            }
        }
    }

    public void UpdateLesson(LessonUI newLessonUI)
    {
        if (newLessonUI == null)
        {
            lessonUI = newLessonUI;
            return;
        }

        
        if (newLessonUI.type == CourseListView.FinalExamType)
        {
            Debug.Log("Ban da chon final exam, khong can update progress time");
            return;
        }
        
        // stop cũ
        if (postCoroutine != null)
        {
            StopCoroutine(postCoroutine);
            postCoroutine = null;
        }

        // gửi progress bài trước
        if (lessonUI != null)
            lmsVideoProgressApiClient.SendProgress(lessonUI);

        // gán bài mới
        lessonUI = newLessonUI;
        hasPostedCompletion = false;

        // bắt đầu timer mới
        postCoroutine = StartCoroutine(PostDataEvery15s());
    }

    private void OnDisable()
    {
        if (postCoroutine != null)
        {
            StopCoroutine(postCoroutine);
        }
    }
}
