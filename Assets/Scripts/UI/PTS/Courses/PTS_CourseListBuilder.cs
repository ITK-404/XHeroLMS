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

    // Bật pooling để không Instantiate/Destroy liên tục.
    private bool usePooling = true;

    // Số item tạo sẵn ngay từ đầu.
    private int prewarmCount = 24;

    // Số item render ngay lập tức trong cùng frame đầu tiên.
    private int immediateRenderCount = 12;

    // Số item render thêm mỗi frame.
    private int batchSize = 8;

    // Nếu > 0 thì chờ giữa các batch. Để 0 để nhanh nhất.
    private float delayBetweenBatches = 0f;

    // Tạm tắt layout trong lúc build để giảm rebuild UI.
    private bool disableLayoutWhileBuilding = true;

    // Nếu không search thì build từ CourseStaticStore.
    private bool buildFromStoreOnEnable = true;

    // Cache list mặc định 1 lần, clear search sẽ restore từ cache.
    private bool freezeInitialFallbackSnapshot = true;

    // Sort mặc định khi build fallback từ store.
    private bool applyDefaultPrioritySortOnFallback = true;

    // Debug
    private bool enableProfilerLog = false;

    // Log thời gian bind nhóm item đầu.
    private bool debugFirstVisibleItemsReadyTime = true;

    // Số item đầu cần đo thời gian hiển thị.
    private int debugFirstVisibleItemCount = 10;

    // Log thêm 1 mốc sau khi frame đầu tiên render xong.
    [SerializeField] private bool debugLogEndOfFrameAfterFirstVisible = true;

    private readonly List<PTS_SimpleCourseUI> _items = new();
    private readonly List<CourseModels.CourseLite> _boundCourses = new();
    private readonly List<CourseModels.CourseLite> _fallbackCache = new();

    private readonly List<LayoutGroup> _layoutGroups = new();
    private readonly List<ContentSizeFitter> _sizeFitters = new();

    private CourseSearch _search;
    private Coroutine _buildCoroutine;
    private Coroutine _firstVisibleDebugCoroutine;
    private int _buildVersion;
    private bool _hasFallbackCache;
    private bool _isShowingFallback;

    private void Awake()
    {
        CacheLayoutDrivers();

        if (usePooling && prewarmCount > 0)
            Prewarm(prewarmCount);
    }

    private void OnEnable()
    {
        BindSearch();
        RefreshNow();
    }

    private void OnDisable()
    {
        StopBuildCoroutine();
        UnbindSearch();
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

    private void HandleSearchResultsChanged(List<CourseModels.CourseLite> results)
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

    public void BuildFromList(IReadOnlyList<CourseModels.CourseLite> list)
    {
        _isShowingFallback = false;
        BuildNow(list);
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

        CacheFallbackIfNeeded(forceRefresh: false);

        _isShowingFallback = true;
        BuildNow(_fallbackCache);
    }

    private void CacheFallbackIfNeeded(bool forceRefresh)
    {
        if (!forceRefresh && _hasFallbackCache && freezeInitialFallbackSnapshot)
            return;

        _fallbackCache.Clear();

        var all = CourseStaticStore.GetAll();
        if (all != null)
        {
            for (int i = 0; i < all.Count; i++)
            {
                var c = all[i];
                if (c != null)
                    _fallbackCache.Add(c);
            }
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
    }

    // =========================================================
    // BUILD CORE
    // =========================================================

    private void BuildNow(IReadOnlyList<CourseModels.CourseLite> list)
    {
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

        // reset mốc đo ảnh thật sự cho PTS_SimpleCourseUI
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

        // Hiện ngay nhóm đầu tiên trong chính frame hiện tại
        BindRange(list, 0, instantCount, currentVersion);

        LogFirstVisibleReadyIfNeeded(
            startTime,
            currentVersion,
            count,
            Mathf.Min(instantCount, debugFirstVisibleItemCount),
            "InstantFirstBatch"
        );

        // Nếu không còn gì để build tiếp thì kết thúc luôn
        if (instantCount >= count)
        {
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

        _buildCoroutine = StartCoroutine(BuildRemaining(list, instantCount, count, currentVersion, startTime));
    }

    private IEnumerator BuildRemaining(
        IReadOnlyList<CourseModels.CourseLite> list,
        int current,
        int totalCount,
        int version,
        float startTime)
    {
        // Nhường 1 frame để user thấy nhóm đầu tiên ngay
        yield return null;

        int safeBatchSize = Mathf.Max(1, batchSize);

        while (current < totalCount)
        {
            if (version != _buildVersion)
                yield break;

            int next = Mathf.Min(current + safeBatchSize, totalCount);

            EnsureCapacity(next);
            BindRange(list, current, next, version);

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

    private void BindRange(IReadOnlyList<CourseModels.CourseLite> list, int start, int endExclusive, int version)
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

            // Chỉ bind lại nếu object course khác thật sự
            if (!ReferenceEquals(_boundCourses[i], course))
            {
                item.Bind(course);
                _boundCourses[i] = course;
            }
        }
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

    private void BuildWithoutPooling(IReadOnlyList<CourseModels.CourseLite> list, int count)
    {
        ClearAllDestroy();

        if (count <= 0)
            return;

        for (int i = 0; i < count; i++)
        {
            var c = list[i];
            if (c == null) continue;

            var item = Instantiate(itemPrefab, contentParent);
            item.gameObject.SetActive(true);
            item.Bind(c);
            _items.Add(item);
        }

        _boundCourses.Clear();
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
    // UTILS
    // =========================================================

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