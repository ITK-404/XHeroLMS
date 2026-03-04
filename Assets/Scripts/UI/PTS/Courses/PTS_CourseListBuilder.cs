using System;
using System.Collections.Generic;
using UnityEngine;

public class PTS_CourseListBuilder : MonoBehaviour
{
    [Header("Prefab & Parent")]
    [SerializeField] private PTS_SimpleCourseUI itemPrefab;
    [SerializeField] private Transform contentParent;

    [Header("Build Options")]
    [SerializeField] private bool clearOldChildren = true;

    private readonly List<PTS_SimpleCourseUI> _spawned = new();

    private void Start()
    {
        Build();
        CourseStaticStore.OnChanged += Build;
    }

    private void OnDestroy()
    {
        CourseStaticStore.OnChanged -= Build;
    }

    [ContextMenu("Build")]
    public void Build()
    {
        if (itemPrefab == null || contentParent == null)
        {
            Debug.LogWarning("[PTS] Missing itemPrefab/contentParent");
            return;
        }

        var all = CourseStaticStore.GetAll();
        if (all == null || all.Count == 0)
        {
            Debug.LogWarning("[PTS] CourseStaticStore has no data");
            ClearSpawned();
            return;
        }

        if (clearOldChildren) ClearSpawned();

        // copy + sort theo ưu tiên zoom > offline > online
        var list = new List<CourseModels.CourseLite>(all.Count);
        for (int i = 0; i < all.Count; i++)
        {
            var c = all[i];
            if (c != null) list.Add(c);
        }

        list.Sort((a, b) =>
        {
            int pa = LearningModePriority(a?.learningMode);
            int pb = LearningModePriority(b?.learningMode);
            int cmp = pa.CompareTo(pb);
            if (cmp != 0) return cmp;

            float sa = a != null ? a.stars : 0f;
            float sb = b != null ? b.stars : 0f;
            return sb.CompareTo(sa);
        });

        for (int i = 0; i < list.Count; i++)
        {
            var course = list[i];

            var item = Instantiate(itemPrefab, contentParent);
            item.gameObject.SetActive(true);
            item.Bind(course);

            _spawned.Add(item);
        }

        Debug.Log($"[PTS] Built {list.Count} course items.");
    }

    private static int LearningModePriority(string mode)
    {
        // số càng nhỏ càng ưu tiên trước
        if (string.IsNullOrEmpty(mode)) return 999;

        mode = mode.Trim().ToLowerInvariant();

        // Ưu tiên theo yêu cầu: zoom, offline, online
        if (mode == "zoom") return 0;
        if (mode == "offline") return 1;
        if (mode == "online") return 2;

        // fallback: các mode khác xếp sau
        return 100;
    }

    private void ClearSpawned()
    {
        for (int i = 0; i < _spawned.Count; i++)
        {
            if (_spawned[i] != null) Destroy(_spawned[i].gameObject);
        }
        _spawned.Clear();
    }
}