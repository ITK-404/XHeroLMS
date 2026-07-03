using System;
using System.Collections.Generic;

public static class CourseStaticStore
{
    private const double LoadStaleSeconds = 30.0;

    private static readonly List<CourseListItemData> _all = new();
    private static readonly Dictionary<string, CourseListItemData> _byId = new(StringComparer.OrdinalIgnoreCase);
    private static DateTime _loadStartedUtc;

    public static bool HasData => _all.Count > 0;
    public static bool HasLoaded { get; private set; }
    public static bool IsLoading { get; private set; }
    public static string LastError { get; private set; }
    public static int Version { get; private set; }
    public static int Count => _all.Count;

    public static event Action OnChanged;

    public static IReadOnlyList<CourseListItemData> GetAll() => _all;

    public static CourseListItemData GetById(string id)
        => (!string.IsNullOrEmpty(id) && _byId.TryGetValue(id, out var c)) ? c : null;

    public static List<CourseListItemData> GetByGroup(string groupKey)
    {
        var result = new List<CourseListItemData>();
        if (string.IsNullOrEmpty(groupKey)) return result;

        for (int i = 0; i < _all.Count; i++)
        {
            var c = _all[i];
            if (c == null) continue;

            if (!string.IsNullOrEmpty(c.group) &&
                string.Equals(c.group, groupKey, StringComparison.OrdinalIgnoreCase))
            {
                result.Add(c);
            }
        }

        return result;
    }

    public static bool TryBeginLoad()
    {
        if (IsLoading)
        {
            if (!IsLoadingStale())
                return false;

            IsLoading = false;
            LastError = null;
        }

        IsLoading = true;
        LastError = null;
        _loadStartedUtc = DateTime.UtcNow;
        return true;
    }

    public static bool IsLoadingStale()
    {
        if (!IsLoading || _loadStartedUtc == default)
            return false;

        return (DateTime.UtcNow - _loadStartedUtc).TotalSeconds >= LoadStaleSeconds;
    }

    public static void SetLoadError(string error)
    {
        bool changed = IsLoading || !StringEquals(LastError, error);

        IsLoading = false;
        LastError = error;
        _loadStartedUtc = default;

        if (changed)
            NotifyChanged();
    }

    public static void Clear()
    {
        bool changed = _all.Count > 0 || _byId.Count > 0 || HasLoaded || !string.IsNullOrEmpty(LastError);

        _all.Clear();
        _byId.Clear();
        HasLoaded = false;
        IsLoading = false;
        LastError = null;
        _loadStartedUtc = default;

        if (changed)
            NotifyChanged();
    }

    public static void SetItems(IReadOnlyList<CourseListItemData> items)
    {
        var nextAll = new List<CourseListItemData>();
        var nextById = new Dictionary<string, CourseListItemData>(StringComparer.OrdinalIgnoreCase);

        if (items != null)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var c = items[i];
                if (c == null || string.IsNullOrEmpty(c.id))
                    continue;

                if (nextById.ContainsKey(c.id))
                    continue;

                nextAll.Add(c);
                nextById[c.id] = c;
            }
        }

        bool changed = !HasLoaded || !HasSameCourseKeys(nextAll);

        _all.Clear();
        _all.AddRange(nextAll);

        _byId.Clear();
        foreach (var kv in nextById)
            _byId[kv.Key] = kv.Value;

        HasLoaded = true;
        IsLoading = false;
        LastError = null;
        _loadStartedUtc = default;

        if (changed)
            NotifyChanged();
    }

    public static void AppendItems(IReadOnlyList<CourseListItemData> items)
    {
        if (items == null || items.Count == 0)
            return;

        var merged = new List<CourseListItemData>(_all);

        for (int i = 0; i < items.Count; i++)
        {
            var c = items[i];
            if (c == null || string.IsNullOrEmpty(c.id) || _byId.ContainsKey(c.id))
                continue;

            merged.Add(c);
        }

        SetItems(merged);
    }

    private static void NotifyChanged()
    {
        Version++;
        OnChanged?.Invoke();
    }

    private static bool HasSameCourseKeys(IReadOnlyList<CourseListItemData> next)
    {
        if (_all.Count != next.Count)
            return false;

        for (int i = 0; i < next.Count; i++)
        {
            var a = _all[i];
            var b = next[i];

            if (!StringEquals(a?.id, b?.id)) return false;
            if (!StringEquals(a?.title, b?.title)) return false;
            if (!StringEquals(a?.image, b?.image)) return false;
            if (!StringEquals(a?.learningMode, b?.learningMode)) return false;
        }

        return true;
    }

    private static bool StringEquals(string a, string b)
    {
        return string.Equals(a ?? "", b ?? "", StringComparison.Ordinal);
    }
}
