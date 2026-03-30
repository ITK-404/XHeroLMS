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
        float timeout = 10f;
        float t = 0f;
        yield return new WaitForSeconds(2);
        while (t < timeout)
        {
            bool detailDone = IsCourseDetailLoaded(courseId);
            bool reviewDone = IsCourseReviewLoaded(courseId);

            bool detailError = !string.IsNullOrEmpty(CourseDetailStaticStore.LastError);
            bool reviewError = !string.IsNullOrEmpty(CourseReviewStaticStore.LastError);

            if (detailError)
                yield break;

            bool canShow = detailDone && (reviewDone || reviewError);

            if (canShow)
            {
                onComplete?.Invoke();
                yield break;
            }

            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (IsCourseDetailLoaded(courseId))
            onComplete?.Invoke();
        else
            OnFall?.Invoke();
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