using System.Collections.Generic;
using UnityEngine;

public class PTS_CourseListBuilder : MonoBehaviour
{
    [Header("Prefab & Parent")]
    [SerializeField] private PTS_SimpleCourseUI itemPrefab;
    [SerializeField] private Transform contentParent;
    public Transform ContentParent => contentParent;
    [Header("Build Options")]
    [SerializeField] private bool usePooling = true;

    [SerializeField] private int prewarmCount = 20;

    [SerializeField] private bool buildFromStoreOnStartIfNoSearch = true;
    [SerializeField] private bool applyDefaultPrioritySortOnFallback = true;

    private readonly List<PTS_SimpleCourseUI> _items = new();
    private CourseSearch _search;

    private void Awake()
    {
        if (usePooling && prewarmCount > 0)
            Prewarm(prewarmCount);
    }

    private void OnEnable()
    {
        TryBindSearch();

        if (_search != null)
        {
            var last = _search.LastResults;
            if (last != null && last.Count > 0)
            {
                BuildFromList(last);
                return;
            }
        }

        if (buildFromStoreOnStartIfNoSearch)
            BuildFromStoreFallback();
    }

    private void OnDisable()
    {
        UnbindSearch();
    }

    private void TryBindSearch()
    {
        _search = CourseSearch.Instance;
        if (_search == null)
            _search = FindFirstObjectByType<CourseSearch>();

        if (_search != null)
        {
            _search.OnResultsChanged -= HandleSearchResultsChanged;
            _search.OnResultsChanged += HandleSearchResultsChanged;

            _search.SearchNow();
        }
    }

    private void UnbindSearch()
    {
        if (_search != null)
            _search.OnResultsChanged -= HandleSearchResultsChanged;
        _search = null;
    }

    private void HandleSearchResultsChanged(List<CourseModels.CourseLite> results)
    {
        BuildFromList(results);
    }

    // ---------- search results ----------
    public void BuildFromList(IReadOnlyList<CourseModels.CourseLite> list)
    {
        if (itemPrefab == null || contentParent == null)
        {
            Debug.LogWarning("[PTS] Missing itemPrefab/contentParent");
            return;
        }

        int count = (list == null) ? 0 : list.Count;

        if (!usePooling)
        {
            // fallback old-way if you really want
            ClearAllDestroy();
            if (count == 0) return;

            for (int i = 0; i < count; i++)
            {
                var c = list[i];
                if (c == null) continue;

                var item = Instantiate(itemPrefab, contentParent);
                item.gameObject.SetActive(true);
                item.Bind(c);
                _items.Add(item);
            }
            return;
        }

        // POOLING WAY
        EnsureItemCount(count);

        // Bind + show needed
        for (int i = 0; i < count; i++)
        {
            var course = list[i];
            var item = _items[i];

            if (course == null)
            {
                item.gameObject.SetActive(false);
                continue;
            }

            if (!item.gameObject.activeSelf) item.gameObject.SetActive(true);
            item.Bind(course);
        }

        for (int i = count; i < _items.Count; i++)
        {
            if (_items[i].gameObject.activeSelf)
                _items[i].gameObject.SetActive(false);
        }
    }

    private void Prewarm(int count)
    {
        if (itemPrefab == null || contentParent == null) return;

        EnsureItemCount(count);
        for (int i = 0; i < _items.Count; i++)
            _items[i].gameObject.SetActive(false);
    }

    private void EnsureItemCount(int needed)
    {
        while (_items.Count < needed)
        {
            var item = Instantiate(itemPrefab, contentParent);
            item.gameObject.SetActive(false);
            _items.Add(item);
        }
    }

    private void ClearAllDestroy()
    {
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i] != null)
                Destroy(_items[i].gameObject);
        }
        _items.Clear();
    }

    // ---------- Fallback ----------
    private void BuildFromStoreFallback()
    {
        var all = CourseStaticStore.GetAll();
        if (all == null || all.Count == 0)
        {
            if (usePooling)
            {
                // hide all
                for (int i = 0; i < _items.Count; i++)
                    if (_items[i] != null) _items[i].gameObject.SetActive(false);
            }
            else
            {
                ClearAllDestroy();
            }

            Debug.LogWarning("[PTS] CourseStaticStore has no data");
            return;
        }

        var list = new List<CourseModels.CourseLite>(all.Count);
        for (int i = 0; i < all.Count; i++)
        {
            var c = all[i];
            if (c != null) list.Add(c);
        }

        if (applyDefaultPrioritySortOnFallback)
        {
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
        }

        BuildFromList(list);
    }

    private static int LearningModePriority(string mode)
    {
        if (string.IsNullOrEmpty(mode)) return 999;
        mode = mode.Trim().ToLowerInvariant();

        if (mode == "zoom") return 0;
        if (mode == "offline") return 1;
        if (mode == "online") return 2;
        return 100;
    }
}