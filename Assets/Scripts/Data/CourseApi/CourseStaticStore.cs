using System;
using System.Collections.Generic;

public static class CourseStaticStore
{
    private static readonly List<CourseListItemData> _all = new();
    private static readonly Dictionary<string, CourseListItemData> _byId = new(StringComparer.OrdinalIgnoreCase);

    public static bool HasData => _all.Count > 0;
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

    public static void Clear()
    {
        _all.Clear();
        _byId.Clear();
        OnChanged?.Invoke();
    }

    public static void SetItems(IReadOnlyList<CourseListItemData> items)
    {
        _all.Clear();
        _byId.Clear();

        if (items != null)
        {
            for (int i = 0; i < items.Count; i++)
            {
                var c = items[i];
                if (c == null || string.IsNullOrEmpty(c.id))
                    continue;

                _all.Add(c);
                _byId[c.id] = c;
            }
        }

        OnChanged?.Invoke();
    }

    public static void AppendItems(IReadOnlyList<CourseListItemData> items)
    {
        if (items == null || items.Count == 0)
            return;

        bool changed = false;

        for (int i = 0; i < items.Count; i++)
        {
            var c = items[i];
            if (c == null || string.IsNullOrEmpty(c.id))
                continue;

            if (_byId.ContainsKey(c.id))
                continue;

            _all.Add(c);
            _byId[c.id] = c;
            changed = true;
        }

        if (changed)
            OnChanged?.Invoke();
    }
}