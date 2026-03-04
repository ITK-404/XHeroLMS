// CourseDetailStaticStore.cs
using System;

public static class CourseDetailStaticStore
{
    public static string CurrentCourseId { get; private set; }
    public static LmsCoursePrivate CurrentCourse { get; private set; }

    public static bool IsLoading { get; private set; }
    public static string LastError { get; private set; }
    public static bool HasData => CurrentCourse != null && !string.IsNullOrEmpty(CurrentCourseId);

    /// <summary> Bắn ra khi store đổi data (load xong / reset / lỗi). </summary>
    public static event Action OnChanged;

    public static void Reset()
    {
        CurrentCourseId = null;
        CurrentCourse = null;
        IsLoading = false;
        LastError = null;
        OnChanged?.Invoke();
    }

    internal static void SetLoading(string courseId)
    {
        IsLoading = true;
        LastError = null;
        CurrentCourseId = courseId;
        // CurrentCourse giữ null khi loading để UI khỏi đọc nhầm data cũ
        CurrentCourse = null;
        OnChanged?.Invoke();
    }

    internal static void SetCourse(string courseId, LmsCoursePrivate course)
    {
        IsLoading = false;
        LastError = null;
        CurrentCourseId = courseId;
        CurrentCourse = course;
        OnChanged?.Invoke();
    }

    internal static void SetError(string courseId, string error)
    {
        IsLoading = false;
        CurrentCourseId = courseId;
        LastError = error;
        CurrentCourse = null;
        OnChanged?.Invoke();
    }
}