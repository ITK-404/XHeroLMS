using System;
using System.Collections.Generic;
using UnityEngine;

public class EduCourseListSpawner : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private EduCourseElement coursePrefab;

    [Header("Options")]
    [SerializeField] private bool autoRefreshOnEnable = true;
    [SerializeField] private bool clearOldItems = true;
    [SerializeField] private bool descendingByStartDate = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private readonly List<EduCourseElement> _spawnedItems = new();

    private void OnEnable()
    {
        CourseStaticStore.OnChanged += Refresh;

        if (autoRefreshOnEnable)
            Refresh();
    }

    private void OnDisable()
    {
        CourseStaticStore.OnChanged -= Refresh;
    }

    public void Refresh()
    {
        if (contentParent == null || coursePrefab == null)
        {
            if (debugLog) Debug.LogWarning("[EduCourseListSpawner] contentParent/coursePrefab is null.");
            return;
        }

        var source = CourseStaticStore.GetAll();

        if (debugLog)
            Debug.Log($"[EduCourseListSpawner] CourseStaticStore count = {source?.Count}");

        if (clearOldItems)
            ClearItems();

        if (source == null || source.Count == 0)
        {
            if (debugLog) Debug.Log("[EduCourseListSpawner] No course item data.");
            return;
        }

        var sorted = new List<CourseListItemData>(source);

        sorted.Sort((a, b) =>
        {
            DateTime da = GetFirstStartDate(a);
            DateTime db = GetFirstStartDate(b);

            int compare = DateTime.Compare(da, db);
            return descendingByStartDate ? -compare : compare;
        });

        for (int i = 0; i < sorted.Count; i++)
        {
            var data = sorted[i];
            if (data == null) continue;

            var item = Instantiate(coursePrefab, contentParent);
            item.Setup(data); // EduCourseElement.Setup(CourseListItemData)
            _spawnedItems.Add(item);
        }

        if (debugLog)
            Debug.Log($"[EduCourseListSpawner] Spawned {_spawnedItems.Count} items.");
    }

    public void ClearItems()
    {
        for (int i = _spawnedItems.Count - 1; i >= 0; i--)
        {
            if (_spawnedItems[i] != null)
                Destroy(_spawnedItems[i].gameObject);
        }

        _spawnedItems.Clear();
    }

    private DateTime GetFirstStartDate(CourseListItemData course)
    {
        if (course == null || course.courseStartDate == null || course.courseStartDate.Count == 0)
            return DateTime.MinValue;

        var first = course.courseStartDate[0];
        if (first == null || first.start == null)
            return DateTime.MinValue;

        int day = first.start.day;
        int month = first.start.month;
        int year = first.start.year;

        if (day <= 0 || month <= 0 || year <= 0)
            return DateTime.MinValue;

        try
        {
            return new DateTime(year, month, day);
        }
        catch
        {
            return DateTime.MinValue;
        }
    }
}