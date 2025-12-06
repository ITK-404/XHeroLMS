using System.Collections;
using UnityEngine;

public class LessonProgressTracker : MonoBehaviour
{
    public static LessonProgressTracker Instance;
    public double highestProgressTime;
    public LessonUI lessonUI;

    public VideoPlayerControllerPro videoPlayerControllerPro;
    public LmsVideoProgressApiClient lmsVideoProgressApiClient;
    private WaitForSecondsRealtime yieldWaitTime;
    public SceneLessonUI sceneLessonUI;
    private Coroutine postCoroutine;

    private bool hasPostedCompletion;

    private void Awake()
    {
        Instance = this;
        videoPlayerControllerPro.GetSkipVideoDuration += GetSkipVideoDuration;
        yieldWaitTime = new WaitForSecondsRealtime(15);

        sceneLessonUI.OnLoadCourseDone += OnLoadCourseDone;
    }

    private void OnLoadCourseDone(LmsCoursePrivate obj)
    {
        if (obj == null)
        {
            Debug.Log("Obj is null");
            return;
        }

        lmsVideoProgressApiClient.SetCourseID(obj._id);
    }

    private void OnDestroy()
    {
        videoPlayerControllerPro.GetSkipVideoDuration -= GetSkipVideoDuration;
        sceneLessonUI.OnLoadCourseDone -= OnLoadCourseDone;
    }

    private bool GetSkipVideoDuration(double second)
    {
        if (second <= 1)
        {
            return true;
        }

        if (lessonUI == null)
        {
            return false;
        }

        if (second > lessonUI.progressTime)
        {
            return false;
        }

        return true;
    }

    private void OnDisable()
    {
        if (postCoroutine != null)
        {
            StopCoroutine(postCoroutine);
            postCoroutine = null;
        }
    }

    private float lastPostDataTime;
    private float postDataInterval = 15;

    private void Update()
    {
        if (lessonUI != null)
        {
            float videoProgress = (float)videoPlayerControllerPro.videoPlayer.time;
            lessonUI.TryUpdateProgress(videoProgress);

            // Post once when progress reaches or exceeds duration
            if (!hasPostedCompletion && lessonUI.duration > 0f && lessonUI.progressTime >= lessonUI.duration)
            {
                hasPostedCompletion = true;
                lmsVideoProgressApiClient?.SendOnceBlocking(lessonUI, false);
                ChapterUIManager.Instance?.UpdateLessonProgress();
            }
        }
    }

    private IEnumerator PostDataEach15Second()
    {
        while (true)
        {
            yield return yieldWaitTime;

            if (lessonUI != null)
            {
                lmsVideoProgressApiClient.SendOnceBlocking(lessonUI);
            }
        }
    }

    public void UpdateLesson(LessonUI newLessonUI)
    {
        if (newLessonUI == null) return;

        if (lessonUI == newLessonUI) return;

        if (postCoroutine != null)
        {
            StopCoroutine(postCoroutine);
            postCoroutine = null;
        }

        // update old progress
        lmsVideoProgressApiClient?.SendOnceBlocking(lessonUI, false);

        lessonUI = newLessonUI;

        // reset one-time completion flag for the new lesson
        hasPostedCompletion = false;

        postCoroutine = StartCoroutine(PostDataEach15Second());
    }
}