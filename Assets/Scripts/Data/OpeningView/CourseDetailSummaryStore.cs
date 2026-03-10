using System;
using System.Collections.Generic;

public static class CourseDetailSummaryStore
{
    private static readonly List<CourseDetailSummary> _items = new();
    private static readonly Dictionary<string, CourseDetailSummary> _byId = new(StringComparer.OrdinalIgnoreCase);

    public static event Action OnChanged;

    public static IReadOnlyList<CourseDetailSummary> GetAll() => _items;

    public static bool HasData => _items.Count > 0;
    public static int Count => _items.Count;

    public static CourseDetailSummary GetById(string courseId)
    {
        if (string.IsNullOrEmpty(courseId)) return null;
        return _byId.TryGetValue(courseId, out var item) ? item : null;
    }

    public static void Clear()
    {
        _items.Clear();
        _byId.Clear();
        OnChanged?.Invoke();
    }

    public static void SetAll(List<CourseDetailSummary> items)
    {
        _items.Clear();
        _byId.Clear();

        if (items != null)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                if (item == null || string.IsNullOrEmpty(item.courseId)) continue;

                _items.Add(item);
                _byId[item.courseId] = item;
            }
        }

        OnChanged?.Invoke();
    }

    public static void AddOrUpdate(CourseDetailSummary item)
    {
        if (item == null || string.IsNullOrEmpty(item.courseId)) return;

        if (_byId.TryGetValue(item.courseId, out var old))
        {
            int index = _items.IndexOf(old);
            if (index >= 0) _items[index] = item;
            _byId[item.courseId] = item;
        }
        else
        {
            _items.Add(item);
            _byId[item.courseId] = item;
        }

        OnChanged?.Invoke();
    }
}

[Serializable]
public class CourseDetailSummary
{
    public string courseId;
    public string title;
    public int learners;
    public string image;
    public string instructorName;
    public string startDateText;
    public long totalDuration;
    public int lessonCount;
}

[Serializable]
public class CourseDetailApiResponse
{
    public bool status;
    public CourseDetailApiModel course;
}

[Serializable]
public class CourseDetailApiModel
{
    public string _id;
    public string title;
    public int learners;
    public string image;
    public long totalDuration;

    public InstructorInfo instructor;
    public List<CourseStartDateItem> courseStartDate;
    public List<CourseChapter> chapters;
}

[Serializable]
public class InstructorInfo
{
    public string _id;
    public string fullName;
    public string title;
    public string description;
    public int courses;
    public int learners;
}

[Serializable]
public class CourseStartDateItem
{
    public StartDateInfo start;
    public EndDateInfo end;
    public string note;
    public string _id;
}

[Serializable]
public class StartDateInfo
{
    public int day;
    public int month;
    public int year;
}

[Serializable]
public class EndDateInfo
{
    public int day;
    public int month;
    public int year;
}

[Serializable]
public class CourseChapter
{
    public string _id;
    public string title;
    public List<CourseLesson> lessons;
}

[Serializable]
public class CourseLesson
{
    public string _id;
    public string title;
}