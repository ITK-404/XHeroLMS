using System;
using System.Collections.Generic;
using UnityEngine;

public static class CourseReviewStaticStore
{
    public static string CurrentCourseId { get; private set; }
    public static List<LmsCourseReviewItem> Reviews { get; private set; } = new();
    public static ReviewStatistics Statistics { get; private set; }

    public static bool IsLoading { get; private set; }
    public static string LastError { get; private set; }

    public static bool HasData =>
        !string.IsNullOrEmpty(CurrentCourseId) &&
        Reviews != null;

    public static event Action OnChanged;

    public static void Reset()
    {
        CurrentCourseId = null;
        Reviews = new List<LmsCourseReviewItem>();
        Statistics = null;
        IsLoading = false;
        LastError = null;
        OnChanged?.Invoke();
    }

    public static void ClearOnlyData()
    {
        Reviews = new List<LmsCourseReviewItem>();
        Statistics = null;
        LastError = null;
        OnChanged?.Invoke();
    }

    internal static void SetLoading(string courseId, bool clearOldData = true)
    {
        CurrentCourseId = courseId;
        IsLoading = true;
        LastError = null;

        if (clearOldData)
        {
            Reviews = new List<LmsCourseReviewItem>();
            Statistics = null;
        }

        OnChanged?.Invoke();
    }

    internal static void SetData(string courseId, List<LmsCourseReviewItem> reviews, ReviewStatistics statistics)
    {
        CurrentCourseId = courseId;
        Reviews = reviews ?? new List<LmsCourseReviewItem>();
        Statistics = statistics;
        IsLoading = false;
        LastError = null;

        Debug.Log($"[CourseReviewStaticStore] SetData courseId={courseId} reviews={Reviews.Count} total={(Statistics != null ? Statistics.total : -1)}");

        OnChanged?.Invoke();
    }

    internal static void SetError(string courseId, string error)
    {
        CurrentCourseId = courseId;
        Reviews = new List<LmsCourseReviewItem>();
        Statistics = null;
        IsLoading = false;
        LastError = error;
        OnChanged?.Invoke();
    }
}