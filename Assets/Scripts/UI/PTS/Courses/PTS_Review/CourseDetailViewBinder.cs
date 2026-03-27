using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using TMPro;
using UnityEngine;

public class CourseDetailViewBinder : MonoBehaviour
{
    [Header("UI - Data")]
    [SerializeField] private TextMeshProUGUI txtInstructor;
    [SerializeField] private TextMeshProUGUI txtDuration;
    [SerializeField] private TextMeshProUGUI txtTotalLessons;
    [SerializeField] private TextMeshProUGUI txtStar;

    private void OnEnable()
    {
        CourseDetailStaticStore.OnChanged += HandleStoreChanged;
        RefreshUI();
    }

    private void OnDisable()
    {
        CourseDetailStaticStore.OnChanged -= HandleStoreChanged;
    }

    private void HandleStoreChanged() => RefreshUI();

    private void RefreshUI()
    {
        if (!CourseDetailStaticStore.HasData)
        {
            ApplyPlaceholders();
            return;
        }

        ApplyCourse(CourseDetailStaticStore.CurrentDetail);
    }

    private void ApplyPlaceholders()
    {
        SetText(txtInstructor, "—");
        SetText(txtDuration, "—");
        SetText(txtTotalLessons, "0 bài học");
        SetText(txtStar, "0.0");
    }

    private void ApplyCourse(CourseModels.CourseDetail course)
    {
        if (course == null)
        {
            ApplyPlaceholders();
            return;
        }

        string instructorName =
            (course.instructor != null && !string.IsNullOrEmpty(course.instructor.fullName))
                ? course.instructor.fullName
                : "—";
        SetText(txtInstructor, instructorName);

        SetText(txtDuration, FormatDurationSecondsSmart(course.totalDuration));
        Debug.Log("Duration raw = " + course.totalDuration);

        int totalLessons = CountLessonsSmart(course.chapters);
        SetText(txtTotalLessons, FormatTotalLessons(totalLessons));

        SetText(txtStar, FormatStarWithEvaluatePeople(course.stars, course.evaluate));
    }

    private static void SetText(TextMeshProUGUI t, string v)
    {
        if (t != null) t.text = v ?? "";
    }

    private static string FormatDurationSecondsSmart(int totalSeconds)
    {
        if (totalSeconds <= 0) return "Chưa có";

        var t = TimeSpan.FromSeconds(totalSeconds);
        int hours = (int)t.TotalHours;
        int minutes = t.Minutes;

        if (hours > 0) return $"{hours} giờ {minutes} phút";
        return $"{t.Minutes} phút";
    }

    private static string FormatTotalLessons(int lessons)
    {
        if (lessons < 0) lessons = 0;
        return $"{lessons} bài học";
    }

    private static string FormatStarWithEvaluatePeople(float stars, int evaluate)
    {
        string starsText = stars > 0 ? stars.ToString("0.0", CultureInfo.InvariantCulture) : "0.0";

        // evaluate = 0 -> chỉ hiện sao
        // if (evaluate <= 0) return starsText;

        return $"{starsText} ({(int)evaluate} người đánh giá)";
    }

    private static int CountLessonsSmart(List<CourseModels.CourseChapter> chapters)
    {
        if (chapters == null || chapters.Count == 0) return 0;

        int count = 0;

        for (int i = 0; i < chapters.Count; i++)
        {
            var ch = chapters[i];
            if (ch == null) continue;

            if (ch.lessons != null && ch.lessons.Count > 0)
            {
                count += ch.lessons.Count;
                continue;
            }

            count += TryCountFirstListProperty(ch,
                "lessons",
                "items",
                "contents",
                "children",
                "videos",
                "lessonsList",
                "lessonList");
        }

        return count;
    }

    private static int TryCountFirstListProperty(object obj, params string[] names)
    {
        if (obj == null) return 0;

        var type = obj.GetType();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        for (int i = 0; i < names.Length; i++)
        {
            var name = names[i];

            var f = type.GetField(name, flags);
            if (f != null)
            {
                if (f.GetValue(obj) is IList list) return list.Count;
            }

            var p = type.GetProperty(name, flags);
            if (p != null && p.CanRead)
            {
                try
                {
                    if (p.GetValue(obj, null) is IList list) return list.Count;
                }
                catch { }
            }
        }

        var fields = type.GetFields(flags);
        for (int i = 0; i < fields.Length; i++)
        {
            object v = fields[i].GetValue(obj);
            if (v is IList list) return list.Count;
        }

        var props = type.GetProperties(flags);
        for (int i = 0; i < props.Length; i++)
        {
            var p = props[i];
            if (!p.CanRead) continue;
            try
            {
                object v = p.GetValue(obj, null);
                if (v is IList list) return list.Count;
            }
            catch { }
        }

        return 0;
    }
}