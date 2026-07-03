using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PTS_CourseListBuilder : MonoBehaviour
{
    [Header("Prefab & Parent")]
    [SerializeField] private PTS_SimpleCourseUI itemPrefab;
    [SerializeField] private Transform contentParent;
    public Transform ContentParent => contentParent;

    private bool usePooling = true;
    private int prewarmCount = 24;
    private int immediateRenderCount = 12;
    private int batchSize = 8;
    private float delayBetweenBatches = 0f;
    private bool disableLayoutWhileBuilding = true;
    private bool buildFromStoreOnEnable = true;
    private bool freezeInitialFallbackSnapshot = true;
    private bool applyDefaultPrioritySortOnFallback = true;

    [Header("Image Priority")]
    [SerializeField] private int priorityImageCount = 6;
    [SerializeField] private int deferredImageBatchSize = 4;
    [SerializeField] private float deferredImageDelay = 0.05f;

    [Header("Debug")]
    [SerializeField] private bool enableProfilerLog = false;
    [SerializeField] private bool debugFirstVisibleItemsReadyTime = true;
    [SerializeField] private int debugFirstVisibleItemCount = 10;
    [SerializeField] private bool debugLogEndOfFrameAfterFirstVisible = true;
    [SerializeField] private bool debugLogDedup = true;

    private readonly List<PTS_SimpleCourseUI> _items = new();
    private readonly List<CourseListItemData> _boundCourses = new();
    private readonly List<CourseListItemData> _fallbackCache = new();
    private readonly List<CourseListItemData> _searchBuffer = new();

    private readonly List<CourseListItemData> _dedupBuffer = new();
    private readonly HashSet<string> _dedupKeys = new();

    private readonly List<LayoutGroup> _layoutGroups = new();
    private readonly List<ContentSizeFitter> _sizeFitters = new();

    private CourseSearch _search;
    private Coroutine _buildCoroutine;
    private Coroutine _deferredImageCoroutine;
    private Coroutine _firstVisibleDebugCoroutine;
    private Coroutine _waitForStoreCoroutine;
    private int _buildVersion;
    private int _cachedStoreVersion = -1;
    private bool _hasFallbackCache;
    private bool _isShowingFallback;
    private bool _subscribedStore;

    private void Awake()
    {
        CacheLayoutDrivers();
        NormalizeTemplateAndClearDuplicates();
    }

    private IEnumerator Start()
    {
        yield return null; // đợi Destroy hoàn tất cuối frame trước

        if (usePooling && prewarmCount > 0)
            Prewarm(prewarmCount);
    }


    private void OnEnable()
    {
        CacheLayoutDrivers();
        SubscribeStore();
        BindSearch();
        CourseBootstrapLoader.EnsureLoaded();
        RefreshNow();
    }

    private void OnDisable()
    {
        StopBuildCoroutine();
        StopWaitForStore();
        UnbindSearch();
        UnsubscribeStore();
    }

    private void SubscribeStore()
    {
        if (_subscribedStore)
            return;

        CourseStaticStore.OnChanged += HandleCourseStoreChanged;
        _subscribedStore = true;
    }

    private void UnsubscribeStore()
    {
        if (!_subscribedStore)
            return;

        CourseStaticStore.OnChanged -= HandleCourseStoreChanged;
        _subscribedStore = false;
    }

    private void HandleCourseStoreChanged()
    {
        if (!isActiveAndEnabled)
            return;

        if (_search != null && _search.IsSearchActive)
            return;

        _hasFallbackCache = false;
        RestoreFallbackNow();
    }

    // =========================================================
    // SEARCH BINDING
    // =========================================================

    private void BindSearch()
    {
        _search = CourseSearch.Instance;
        if (_search == null)
            _search = FindFirstObjectByType<CourseSearch>();

        if (_search != null)
        {
            _search.OnResultsChanged -= HandleSearchResultsChanged;
            _search.OnResultsChanged += HandleSearchResultsChanged;
        }
    }

    private void UnbindSearch()
    {
        if (_search != null)
            _search.OnResultsChanged -= HandleSearchResultsChanged;

        _search = null;
    }

    private void HandleSearchResultsChanged(List<CourseListItemData> results)
    {
        if (_search == null || !_search.IsSearchActive)
        {
            RestoreFallbackNow();
            return;
        }

        _isShowingFallback = false;
        BuildNow(results);
    }

    // =========================================================
    // PUBLIC API
    // =========================================================

    public void RefreshNow()
    {
        if (_search != null && _search.IsSearchActive)
        {
            _isShowingFallback = false;
            BuildNow(_search.LastResults);
            return;
        }

        RestoreFallbackNow();
    }

    public void BuildFromList(IReadOnlyList<CourseListItemData> list)
    {
        _isShowingFallback = false;
        BuildNow(list);
    }

    public void BuildFromCourseLiteList(IReadOnlyList<CourseModels.CourseLite> list)
    {
        _isShowingFallback = false;
        BuildNow(ConvertCourseLiteList(list));
    }

    public void ForceRefreshFallbackCacheAndRebuild()
    {
        _hasFallbackCache = false;
        CacheFallbackIfNeeded(forceRefresh: true);
        RestoreFallbackNow();
    }

    // =========================================================
    // FALLBACK
    // =========================================================

    private void RestoreFallbackNow()
    {
        if (!buildFromStoreOnEnable)
            return;

        CourseBootstrapLoader.EnsureLoaded();

        if (!CourseStaticStore.HasData)
        {
            StartWaitForStore();

            _isShowingFallback = true;
            if (_fallbackCache.Count == 0)
                BuildNow(_fallbackCache);

            return;
        }

        StopWaitForStore();
        CacheFallbackIfNeeded(forceRefresh: false);

        _isShowingFallback = true;
        BuildNow(_fallbackCache);
    }

    private void StartWaitForStore()
    {
        if (_waitForStoreCoroutine != null || !gameObject.activeInHierarchy)
            return;

        _waitForStoreCoroutine = StartCoroutine(CoWaitForStoreThenRefresh());
    }

    private void StopWaitForStore()
    {
        if (_waitForStoreCoroutine == null)
            return;

        StopCoroutine(_waitForStoreCoroutine);
        _waitForStoreCoroutine = null;
    }

    private IEnumerator CoWaitForStoreThenRefresh()
    {
        while (!CourseStaticStore.HasData)
        {
            CourseBootstrapLoader.EnsureLoaded();
            yield return new WaitForSecondsRealtime(0.5f);
        }

        _waitForStoreCoroutine = null;

        if (!isActiveAndEnabled)
            yield break;

        _hasFallbackCache = false;
        RestoreFallbackNow();
    }

    private void CacheFallbackIfNeeded(bool forceRefresh)
    {
        if (!forceRefresh &&
            _hasFallbackCache &&
            freezeInitialFallbackSnapshot &&
            _cachedStoreVersion == CourseStaticStore.Version)
            return;

        var all = CourseStaticStore.GetAll();
        if (all == null || all.Count == 0)
        {
            if (forceRefresh)
            {
                _fallbackCache.Clear();
                _hasFallbackCache = false;
                _cachedStoreVersion = -1;
            }

            return;
        }

        _fallbackCache.Clear();

        var filtered = RemoveDuplicateCourses(all, "FallbackCache");
        for (int i = 0; i < filtered.Count; i++)
            _fallbackCache.Add(filtered[i]);

        if (_fallbackCache.Count == 0)
        {
            _hasFallbackCache = false;
            _cachedStoreVersion = -1;
            return;
        }

        if (applyDefaultPrioritySortOnFallback)
        {
            _fallbackCache.Sort((a, b) =>
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

        _hasFallbackCache = true;
        _cachedStoreVersion = CourseStaticStore.Version;
    }

    // =========================================================
    // BUILD CORE
    // =========================================================

    private void BuildNow(IReadOnlyList<CourseListItemData> sourceList)
    {
        var list = RemoveDuplicateCourses(sourceList, _isShowingFallback ? "BuildNow-Fallback" : "BuildNow-Search");

        StopBuildCoroutine();
        _buildVersion++;

        if (itemPrefab == null || contentParent == null)
        {
            Debug.LogWarning("[PTS] Missing itemPrefab/contentParent");
            return;
        }

        int count = list != null ? list.Count : 0;
        float startTime = Time.realtimeSinceStartup;
        int currentVersion = _buildVersion;

        PTS_SimpleCourseUI.DebugImageMeasureStartTime = startTime;
        PTS_SimpleCourseUI.DebugImageMeasureVersion++;
        PTS_SimpleCourseUI.DebugTrackFirstNImages = Mathf.Max(1, debugFirstVisibleItemCount);

        if (!usePooling)
        {
            BuildWithoutPooling(list, count);

            LogFirstVisibleReadyIfNeeded(
                startTime,
                currentVersion,
                count,
                Mathf.Min(count, debugFirstVisibleItemCount),
                "BuildWithoutPooling"
            );

            if (enableProfilerLog)
            {
                float ms = (Time.realtimeSinceStartup - startTime) * 1000f;
                Debug.Log($"[PTS] BuildWithoutPooling count={count} fallback={_isShowingFallback} time={ms:F2} ms");
            }
            return;
        }

        if (disableLayoutWhileBuilding)
            SetLayoutDriversEnabled(false);

        HideUnusedItems(count);

        int instantCount = Mathf.Clamp(immediateRenderCount, 0, count);
        EnsureCapacity(instantCount);

        int immediateImageCount = Mathf.Clamp(priorityImageCount, 0, count);

        BindRange(list, 0, instantCount, currentVersion, immediateImageCount);

        LogFirstVisibleReadyIfNeeded(
            startTime,
            currentVersion,
            count,
            Mathf.Min(instantCount, debugFirstVisibleItemCount),
            "InstantFirstBatch"
        );

        if (instantCount >= count)
        {
            StartDeferredImageLoadIfNeeded(immediateImageCount, count, currentVersion);

            if (disableLayoutWhileBuilding)
            {
                SetLayoutDriversEnabled(true);
                ForceRebuildLayout();
            }

            if (enableProfilerLog)
            {
                float ms = (Time.realtimeSinceStartup - startTime) * 1000f;
                Debug.Log($"[PTS] InstantBuild count={count} fallback={_isShowingFallback} time={ms:F2} ms");
            }

            return;
        }

        StartDeferredImageLoadIfNeeded(immediateImageCount, count, currentVersion);

        _buildCoroutine = StartCoroutine(BuildRemaining(list, instantCount, count, currentVersion, immediateImageCount, startTime));
    }

    private IEnumerator BuildRemaining(
        IReadOnlyList<CourseListItemData> list,
        int current,
        int totalCount,
        int version,
        int immediateImageCount,
        float startTime)
    {
        yield return null;

        int safeBatchSize = Mathf.Max(1, batchSize);

        while (current < totalCount)
        {
            if (version != _buildVersion)
                yield break;

            int next = Mathf.Min(current + safeBatchSize, totalCount);

            EnsureCapacity(next);
            BindRange(list, current, next, version, immediateImageCount);

            current = next;

            if (current < totalCount)
            {
                if (delayBetweenBatches > 0f)
                    yield return new WaitForSeconds(delayBetweenBatches);
                else
                    yield return null;
            }
        }

        if (version == _buildVersion && disableLayoutWhileBuilding)
        {
            SetLayoutDriversEnabled(true);
            ForceRebuildLayout();
        }

        if (enableProfilerLog)
        {
            float ms = (Time.realtimeSinceStartup - startTime) * 1000f;
            Debug.Log($"[PTS] ProgressiveBuild count={totalCount} fallback={_isShowingFallback} time={ms:F2} ms");
        }

        _buildCoroutine = null;
    }

    private void StopBuildCoroutine()
    {
        if (_buildCoroutine != null)
        {
            StopCoroutine(_buildCoroutine);
            _buildCoroutine = null;
        }

        if (_deferredImageCoroutine != null)
        {
            StopCoroutine(_deferredImageCoroutine);
            _deferredImageCoroutine = null;
        }

        if (_firstVisibleDebugCoroutine != null)
        {
            StopCoroutine(_firstVisibleDebugCoroutine);
            _firstVisibleDebugCoroutine = null;
        }

        if (disableLayoutWhileBuilding)
            SetLayoutDriversEnabled(true);
    }

    // =========================================================
    // BIND
    // =========================================================

    private void BindRange(IReadOnlyList<CourseListItemData> list, int start, int endExclusive, int version, int immediateImageCount)
    {
        for (int i = start; i < endExclusive; i++)
        {
            if (version != _buildVersion)
                return;

            var item = _items[i];
            if (item == null)
                continue;

            var course = list[i];

            if (course == null)
            {
                if (item.gameObject.activeSelf)
                    item.gameObject.SetActive(false);

                _boundCourses[i] = null;
                continue;
            }

            if (!item.gameObject.activeSelf)
                item.gameObject.SetActive(true);

            if (!ReferenceEquals(_boundCourses[i], course))
            {
                item.Bind(course, i < immediateImageCount);
                _boundCourses[i] = course;
            }
            else if (i < immediateImageCount)
            {
                item.LoadImageNow();
            }
            else if (item.NeedsImageLoad())
            {
                item.LoadImageNow();
            }
        }
    }

    private void StartDeferredImageLoadIfNeeded(int startIndex, int itemCount, int version)
    {
        if (startIndex >= itemCount)
            return;

        _deferredImageCoroutine = StartCoroutine(LoadDeferredImages(startIndex, itemCount, version));
    }

    private IEnumerator LoadDeferredImages(int startIndex, int itemCount, int version)
    {
        if (deferredImageDelay > 0f)
            yield return new WaitForSecondsRealtime(deferredImageDelay);
        else
            yield return null;

        int batchSize = Mathf.Max(1, deferredImageBatchSize);

        for (int i = startIndex; i < itemCount; i++)
        {
            while (version == _buildVersion && i >= _items.Count)
            {
                yield return null;
            }

            if (version != _buildVersion)
                yield break;

            var item = i < _items.Count ? _items[i] : null;
            if (item != null && item.gameObject.activeInHierarchy)
                item.LoadImageNow();

            if ((i - startIndex + 1) % batchSize == 0)
                yield return null;
        }

        _deferredImageCoroutine = null;
    }

    // =========================================================
    // DEBUG TIMING
    // =========================================================

    private void LogFirstVisibleReadyIfNeeded(
        float buildStartTime,
        int version,
        int totalCount,
        int firstVisibleReadyCount,
        string phase)
    {
        if (!debugFirstVisibleItemsReadyTime)
            return;

        float elapsedMs = (Time.realtimeSinceStartup - buildStartTime) * 1000f;
        Debug.Log(
            $"[PTS][FirstVisibleReady] phase={phase} " +
            $"readyCount={firstVisibleReadyCount}/{totalCount} " +
            $"fallback={_isShowingFallback} " +
            $"version={version} " +
            $"time={elapsedMs:F2} ms ({elapsedMs / 1000f:F3} s)"
        );

        if (debugLogEndOfFrameAfterFirstVisible)
        {
            if (_firstVisibleDebugCoroutine != null)
                StopCoroutine(_firstVisibleDebugCoroutine);

            _firstVisibleDebugCoroutine = StartCoroutine(
                CoLogFirstVisibleEndOfFrame(buildStartTime, version, totalCount, firstVisibleReadyCount, phase)
            );
        }
    }

    private IEnumerator CoLogFirstVisibleEndOfFrame(
        float buildStartTime,
        int version,
        int totalCount,
        int firstVisibleReadyCount,
        string phase)
    {
        yield return new WaitForEndOfFrame();

        if (version != _buildVersion)
            yield break;

        float elapsedMs = (Time.realtimeSinceStartup - buildStartTime) * 1000f;
        Debug.Log(
            $"[PTS][FirstVisibleReadyEOF] phase={phase} " +
            $"readyCount={firstVisibleReadyCount}/{totalCount} " +
            $"fallback={_isShowingFallback} " +
            $"version={version} " +
            $"time={elapsedMs:F2} ms ({elapsedMs / 1000f:F3} s)"
        );

        _firstVisibleDebugCoroutine = null;
    }

    // =========================================================
    // POOL
    // =========================================================

    private void Prewarm(int count)
    {
        EnsureCapacity(count);

        for (int i = 0; i < _items.Count; i++)
        {
            var item = _items[i];
            if (item != null && item.gameObject.activeSelf)
                item.gameObject.SetActive(false);
        }

        for (int i = 0; i < _boundCourses.Count; i++)
            _boundCourses[i] = null;
    }

    private void EnsureCapacity(int needed)
    {
        EnsureItemCount(needed);
        EnsureBoundCacheCount(needed);
    }

    private void EnsureItemCount(int needed)
    {
        while (_items.Count < needed)
        {
            var item = Instantiate(itemPrefab, contentParent);
            item.name = "Simple Course Element_Runtime";
            item.gameObject.SetActive(false);
            _items.Add(item);
        }
    }

    private void EnsureBoundCacheCount(int needed)
    {
        while (_boundCourses.Count < needed)
            _boundCourses.Add(null);
    }

    private void HideUnusedItems(int activeCount)
    {
        for (int i = activeCount; i < _items.Count; i++)
        {
            var item = _items[i];
            if (item != null && item.gameObject.activeSelf)
                item.gameObject.SetActive(false);
        }

        for (int i = activeCount; i < _boundCourses.Count; i++)
            _boundCourses[i] = null;
    }

    // =========================================================
    // NO POOL MODE
    // =========================================================

    private void BuildWithoutPooling(IReadOnlyList<CourseListItemData> list, int count)
    {
        ClearAllDestroy();

        if (count <= 0)
            return;

        int immediateImageCount = Mathf.Clamp(priorityImageCount, 0, count);

        for (int i = 0; i < count; i++)
        {
            var c = list[i];
            if (c == null) continue;

            var item = Instantiate(itemPrefab, contentParent);
            item.gameObject.SetActive(true);
            item.Bind(c, i < immediateImageCount);
            _items.Add(item);
        }

        _boundCourses.Clear();

        StartDeferredImageLoadIfNeeded(immediateImageCount, count, _buildVersion);
    }

    private void ClearAllDestroy()
    {
        for (int i = 0; i < _items.Count; i++)
        {
            if (_items[i] != null)
                Destroy(_items[i].gameObject);
        }

        _items.Clear();
        _boundCourses.Clear();
    }

    // =========================================================
    // LAYOUT OPTIMIZATION
    // =========================================================

    private void CacheLayoutDrivers()
    {
        _layoutGroups.Clear();
        _sizeFitters.Clear();

        if (contentParent == null)
            return;

        contentParent.GetComponents(_layoutGroups);
        contentParent.GetComponents(_sizeFitters);
    }

    private void SetLayoutDriversEnabled(bool enabled)
    {
        if (!disableLayoutWhileBuilding)
            return;

        for (int i = 0; i < _layoutGroups.Count; i++)
        {
            if (_layoutGroups[i] != null)
                _layoutGroups[i].enabled = enabled;
        }

        for (int i = 0; i < _sizeFitters.Count; i++)
        {
            if (_sizeFitters[i] != null)
                _sizeFitters[i].enabled = enabled;
        }
    }

    private void ForceRebuildLayout()
    {
        if (contentParent is RectTransform rt)
            LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
    }

    // =========================================================
    // CONVERT
    // =========================================================

    private IReadOnlyList<CourseListItemData> ConvertCourseLiteList(IReadOnlyList<CourseModels.CourseLite> source)
    {
        _searchBuffer.Clear();

        if (source == null)
            return _searchBuffer;

        for (int i = 0; i < source.Count; i++)
        {
            var mapped = CourseModels.ToListItem(source[i]);
            if (mapped != null)
                _searchBuffer.Add(mapped);
        }

        return _searchBuffer;
    }

    // =========================================================
    // DEDUP
    // =========================================================

    private IReadOnlyList<CourseListItemData> RemoveDuplicateCourses(IReadOnlyList<CourseListItemData> source, string tag)
    {
        _dedupBuffer.Clear();
        _dedupKeys.Clear();

        if (source == null)
            return _dedupBuffer;

        for (int i = 0; i < source.Count; i++)
        {
            var c = source[i];
            if (c == null)
                continue;

            string key = BuildCourseDedupKey(c, i);

            if (_dedupKeys.Add(key))
            {
                _dedupBuffer.Add(c);
            }
            else if (debugLogDedup)
            {
                Debug.LogWarning($"[PTS][DEDUP][{tag}] Skip duplicate key={key} title={c.title}");
            }
        }

        return _dedupBuffer;
    }

private string BuildCourseDedupKey(CourseListItemData c, int index)
{
    if (c == null)
        return $"null_{index}";

    string seo = SafeKey(c.seoUrl);
    string id = SafeKey(c.id);
    string title = SafeKey(c.title);
    string mode = SafeKey(c.learningMode);

    long cur = c.currentPrice;
    long org = c.originalPrice;

    // Ưu tiên seoUrl vì thường unique theo course thật
    if (!string.IsNullOrEmpty(seo))
        return $"seo:{seo}";

    // Nếu không có seo thì dùng title + mode + price
    if (!string.IsNullOrEmpty(title))
        return $"title:{title}|mode:{mode}|cur:{cur}|org:{org}";

    // Cuối cùng mới dùng id
    if (!string.IsNullOrEmpty(id))
        return $"id:{id}";

    return $"fallback_index_{index}";
}

private string SafeKey(string s)
{
    return string.IsNullOrWhiteSpace(s)
        ? string.Empty
        : s.Trim().ToLowerInvariant();
}

    // =========================================================
    // TEMPLATE / UTILS
    // =========================================================
    private void NormalizeTemplateAndClearDuplicates()
    {
        if (itemPrefab == null || contentParent == null)
            return;

        PTS_SimpleCourseUI templateInHierarchy = null;

        // Nếu itemPrefab đang là object nằm trong contentParent
        if (itemPrefab.transform.parent == contentParent)
            templateInHierarchy = itemPrefab;

        List<PTS_SimpleCourseUI> foundItems = new List<PTS_SimpleCourseUI>();
        for (int i = 0; i < contentParent.childCount; i++)
        {
            var child = contentParent.GetChild(i);
            var ui = child.GetComponent<PTS_SimpleCourseUI>();
            if (ui != null)
                foundItems.Add(ui);
        }

        if (templateInHierarchy != null)
        {
            // Giữ lại đúng template trong hierarchy, xóa phần còn lại
            for (int i = foundItems.Count - 1; i >= 0; i--)
            {
                if (foundItems[i] == null) continue;
                if (foundItems[i] == templateInHierarchy) continue;

                Destroy(foundItems[i].gameObject);
            }

            templateInHierarchy.gameObject.SetActive(false);
        }
        else
        {
            // itemPrefab là prefab asset từ Project
            // => xóa sạch mọi item đang có sẵn trong contentParent
            for (int i = foundItems.Count - 1; i >= 0; i--)
            {
                if (foundItems[i] != null)
                    Destroy(foundItems[i].gameObject);
            }
        }

        _items.Clear();
        _boundCourses.Clear();
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
