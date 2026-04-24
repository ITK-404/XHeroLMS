using System;
using System.Collections;
using UnityEngine;

public class APICourseLoaderService : MonoBehaviour
{
    public static APICourseLoaderService Instance;
    [SerializeField] private CourseDetailLoader _courseDetailLoader;
    [SerializeField] private CourseReviewLoader _courseReviewLoader;

    private void Awake()
    {
        Instance = this;
    }

    public void Load(string courseID, Action OnComplete, Action OnFall)
    {
        Debug.Log("Su73 dung5 API LOAD DER cho review wva detail");
        _courseDetailLoader.Load(courseID);
        _courseReviewLoader.LoadReviews(courseID);

        StartCoroutine(WaitAllDataThenShow(courseID, OnComplete, OnFall));
    }

    private IEnumerator WaitAllDataThenShow(string courseId, Action onComplete, Action OnFall)
    {
        LoadingUI.Show();

        float timeout = 10f;
        float t = 0f;

        bool isDone = false;
        bool isSuccess = false;

        yield return new WaitForSeconds(1);

        // while (!isDone && t < timeout)
        // {
        //     bool detailDone = IsCourseDetailLoaded(courseId);
        //     bool reviewDone = IsCourseReviewLoaded(courseId);

        //     bool detailError = !string.IsNullOrEmpty(CourseDetailStaticStore.LastError);
        //     bool reviewError = !string.IsNullOrEmpty(CourseReviewStaticStore.LastError);

        //     // Case 1: Detail fail -> fail luôn
        //     if (detailError)
        //     {
        //         isDone = true;
        //         isSuccess = false;
        //         break;
        //     }

        //     // Case 2: đủ điều kiện show
        //     if (detailDone && (reviewDone || reviewError))
        //     {
        //         isDone = true;
        //         isSuccess = true;
        //         break;
        //     }

        //     t += Time.unscaledDeltaTime;
        //     yield return null;
        // }

        // Timeout handling
        if (!isDone)
        {
            if (IsCourseDetailLoaded(courseId))
                isSuccess = true;
            else
                isSuccess = false;
        }

        // Final result
        if (isSuccess)
            onComplete?.Invoke();
        else
            OnFall?.Invoke();

        LoadingUI.Hide();
    }


    private bool IsCourseDetailLoaded(string courseId)
    {
        return CourseDetailStaticStore.HasData
               && !CourseDetailStaticStore.IsLoading
               && CourseDetailStaticStore.CurrentCourseId == courseId
               && CourseDetailStaticStore.CurrentDetail != null;
    }

    private bool IsCourseReviewLoaded(string courseId)
    {
        return CourseReviewStaticStore.CurrentCourseId == courseId
               && !CourseReviewStaticStore.IsLoading
               && string.IsNullOrEmpty(CourseReviewStaticStore.LastError);
    }
}