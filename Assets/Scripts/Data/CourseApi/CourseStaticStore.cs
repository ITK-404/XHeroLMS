using System;
using System.Collections.Generic;
using UnityEngine;

public static class CourseStaticStore
{
    // ====== Lite cache ======
    private static readonly List<CourseModels.CourseLite> _all = new();
    private static readonly Dictionary<string, CourseModels.CourseLite> _byId = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, CourseModels.CourseLite> _bySku = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, CourseModels.CourseLite> _bySeo = new(StringComparer.OrdinalIgnoreCase);

    public static bool HasData => _all.Count > 0;
    public static int Count => _all.Count;

    public static event Action OnChanged;

    public static IReadOnlyList<CourseModels.CourseLite> GetAll() => _all;

    public static CourseModels.CourseLite GetById(string id)
        => (!string.IsNullOrEmpty(id) && _byId.TryGetValue(id, out var c)) ? c : null;

    public static CourseModels.CourseLite GetBySku(string sku)
        => (!string.IsNullOrEmpty(sku) && _bySku.TryGetValue(sku, out var c)) ? c : null;

    public static CourseModels.CourseLite GetBySeoUrl(string seoUrl)
        => (!string.IsNullOrEmpty(seoUrl) && _bySeo.TryGetValue(seoUrl, out var c)) ? c : null;

    public static List<CourseModels.CourseLite> GetByGroup(string groupKey)
    {
        var result = new List<CourseModels.CourseLite>();
        if (string.IsNullOrEmpty(groupKey)) return result;

        for (int i = 0; i < _all.Count; i++)
        {
            var c = _all[i];
            if (c == null) continue;

            if (!string.IsNullOrEmpty(c.group) &&
                string.Equals(c.group, groupKey, StringComparison.OrdinalIgnoreCase))
                result.Add(c);
        }
        return result;
    }

    public static void Clear()
    {
        _all.Clear();
        _byId.Clear();
        _bySku.Clear();
        _bySeo.Clear();

        _detailById.Clear();
        _detailLru.Clear();
        _inflight.Clear();

        OnChanged?.Invoke();
    }

    public static void SetCoursesLite(CourseModels.CourseLite[] courses)
    {
        _all.Clear();
        _byId.Clear();
        _bySku.Clear();
        _bySeo.Clear();

        if (courses != null)
        {
            for (int i = 0; i < courses.Length; i++)
            {
                var c = courses[i];
                if (c == null) continue;

                _all.Add(c);

                if (!string.IsNullOrEmpty(c._id)) _byId[c._id] = c;
                if (!string.IsNullOrEmpty(c.sku)) _bySku[c.sku] = c;

                var seoUrl = c.seo != null ? c.seo.url : null;
                if (!string.IsNullOrEmpty(seoUrl)) _bySeo[seoUrl] = c;
            }
        }

        OnChanged?.Invoke();
    }

    // ====== Detail (heavy) cache: description/banner/paymentOptions ======
    public static int MaxDetailCache = 50;

    private static readonly Dictionary<string, CourseModels.CourseDetail> _detailById = new(StringComparer.OrdinalIgnoreCase);
    private static readonly LinkedList<string> _detailLru = new();
    private static readonly HashSet<string> _inflight = new(StringComparer.OrdinalIgnoreCase);

    public static bool TryGetDetail(string courseId, out CourseModels.CourseDetail detail)
        => _detailById.TryGetValue(courseId, out detail);

    public static string GetDescriptionCached(string courseId)
    {
        if (string.IsNullOrEmpty(courseId)) return null;
        return _detailById.TryGetValue(courseId, out var d) ? d.description : null;
    }

    /// <summary>
    /// Gọi khi bạn cần description. Nếu đã cache -> callback ngay.
    /// Nếu chưa -> gọi API detail và cache (LRU).
    /// CourseStaticStore.RequestDescription(
    ///        runner: this,
    ///        api: detailApiClient,
    ///        courseId: id,
    ///        onDone: desc => detailText.text = desc ?? "(no description)"
    ///    );
    /// </summary>
    public static void RequestDescription(MonoBehaviour runner, CourseDetailApiClient api, string courseId, Action<string> onDone, Action<string> onError = null)
    {
        if (onDone == null) onDone = _ => { };

        if (string.IsNullOrEmpty(courseId))
        {
            onError?.Invoke("courseId null/empty");
            onDone(null);
            return;
        }

        if (_detailById.TryGetValue(courseId, out var cached))
        {
            TouchDetail(courseId);
            onDone(cached != null ? cached.description : null);
            return;
        }

        if (runner == null || api == null)
        {
            onError?.Invoke("runner/api is null");
            onDone(null);
            return;
        }

        // tránh gọi trùng cùng 1 courseId
        if (_inflight.Contains(courseId))
        {
            // có thể: đợi lần sau UI gọi lại, hoặc bạn nâng cấp thành event queue
            onDone(null);
            return;
        }

        _inflight.Add(courseId);

        runner.StartCoroutine(api.FetchCourseDetail(courseId,
            onDone: detail =>
            {
                _inflight.Remove(courseId);

                if (detail != null)
                {
                    PutDetail(courseId, detail);
                    onDone(detail.description);
                }
                else
                {
                    onDone(null);
                }
            },
            onError: err =>
            {
                _inflight.Remove(courseId);
                onError?.Invoke(err);
                onDone(null);
            }));
    }

    private static void PutDetail(string courseId, CourseModels.CourseDetail detail)
    {
        _detailById[courseId] = detail;

        TouchDetail(courseId);

        while (_detailLru.Count > MaxDetailCache)
        {
            var last = _detailLru.Last.Value;
            _detailLru.RemoveLast();
            _detailById.Remove(last);
        }
    }

    private static void TouchDetail(string courseId)
    {
        _detailLru.Remove(courseId);
        _detailLru.AddFirst(courseId);
    }
}