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
    [SerializeField] private bool descendingByStartDate = true;

    // Nếu true sẽ ẩn item dư thay vì destroy ngay. Nên bật để tránh leak/spike memory.
    [SerializeField] private bool usePooling = true;

    // Số item tạo sẵn để giảm instantiate lúc refresh đầu.
    [SerializeField] private int prewarmCount = 10;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    // Toàn bộ item đã tạo ra
    private readonly List<EduCourseElement> _items = new();

    // Chống subscribe trùng / gọi refresh lặp vô ích
    private bool _subscribed;

    private void Awake()
    {
        if (contentParent == null || coursePrefab == null)
            return;

        if (usePooling && prewarmCount > 0)
            Prewarm(prewarmCount);
    }

    private void OnEnable()
    {
        Subscribe();

        if (autoRefreshOnEnable)
            Refresh();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void OnDestroy()
    {
        Unsubscribe();
        ReleaseAllItems();
    }

    private void Subscribe()
    {
        if (_subscribed) return;

        CourseStaticStore.OnChanged += Refresh;
        _subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!_subscribed) return;

        CourseStaticStore.OnChanged -= Refresh;
        _subscribed = false;
    }

    public void Refresh()
    {
        if (contentParent == null || coursePrefab == null)
        {
            if (debugLog)
                Debug.LogWarning("[EduCourseListSpawner] contentParent/coursePrefab is null.");
            return;
        }

        var source = CourseStaticStore.GetAll();

        if (debugLog)
            Debug.Log($"[EduCourseListSpawner] CourseStaticStore count = {source?.Count}");

        if (source == null || source.Count == 0)
        {
            HideAllItems();

            if (debugLog)
                Debug.Log("[EduCourseListSpawner] No course item data.");

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

        EnsureItemCount(sorted.Count);

        for (int i = 0; i < sorted.Count; i++)
        {
            var data = sorted[i];
            var item = _items[i];

            if (item == null)
                continue;

            if (!item.gameObject.activeSelf)
                item.gameObject.SetActive(true);

            if (data != null)
                item.Setup(data);
            else
                item.gameObject.SetActive(false);
        }

        // Ẩn item dư thay vì destroy liên tục
        for (int i = sorted.Count; i < _items.Count; i++)
        {
            if (_items[i] != null && _items[i].gameObject.activeSelf)
                _items[i].gameObject.SetActive(false);
        }

        if (debugLog)
            Debug.Log($"[EduCourseListSpawner] Active items = {sorted.Count}, pooled total = {_items.Count}");
    }

    private void Prewarm(int count)
    {
        count = Mathf.Max(0, count);

        for (int i = _items.Count; i < count; i++)
        {
            var item = CreateNewItem();
            if (item != null)
                item.gameObject.SetActive(false);
        }

        if (debugLog)
            Debug.Log($"[EduCourseListSpawner] Prewarmed {_items.Count} items.");
    }

    private void EnsureItemCount(int requiredCount)
    {
        if (requiredCount <= _items.Count)
            return;

        int missing = requiredCount - _items.Count;

        for (int i = 0; i < missing; i++)
        {
            var item = CreateNewItem();
            if (item == null)
                break;
        }
    }

    private EduCourseElement CreateNewItem()
    {
        if (coursePrefab == null || contentParent == null)
            return null;

        var item = Instantiate(coursePrefab, contentParent);
        item.gameObject.SetActive(false);
        _items.Add(item);
        return item;
    }

    public void ClearItems()
    {
        if (usePooling)
        {
            HideAllItems();

            if (debugLog)
                Debug.Log($"[EduCourseListSpawner] HideAllItems done. Pooled total = {_items.Count}");

            return;
        }

        ReleaseAllItems();

        if (debugLog)
            Debug.Log("[EduCourseListSpawner] ReleaseAllItems done.");
    }

    private void HideAllItems()
    {
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i] != null && _items[i].gameObject.activeSelf)
                _items[i].gameObject.SetActive(false);
        }
    }

    private void ReleaseAllItems()
    {
        for (int i = _items.Count - 1; i >= 0; i--)
        {
            if (_items[i] != null)
                Destroy(_items[i].gameObject);
        }

        _items.Clear();
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