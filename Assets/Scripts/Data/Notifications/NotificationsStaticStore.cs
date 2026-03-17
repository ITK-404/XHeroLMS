using System;
using System.Collections.Generic;

public static class NotificationsStaticStore
{
    public static string CurrentTab { get; private set; } = "system";
    public static bool IsLoading { get; private set; }
    public static string LastError { get; private set; }

    public static string UnreadAll { get; private set; }
    public static string UnreadPersonal { get; private set; }
    public static string UnreadSystem { get; private set; }
    public static string UnreadMerchant { get; private set; }

    private static readonly List<NotificationMailItem> _items = new();
    public static IReadOnlyList<NotificationMailItem> Items => _items;
    public static bool HasData => _items.Count > 0;

    public static event Action OnChanged;

    public static void Reset()
    {
        CurrentTab = "system";
        IsLoading = false;
        LastError = null;

        UnreadAll = null;
        UnreadPersonal = null;
        UnreadSystem = null;
        UnreadMerchant = null;

        _items.Clear();
        OnChanged?.Invoke();
    }

    public static void SetLoading(string tab)
    {
        CurrentTab = NormalizeTab(tab);
        IsLoading = true;
        LastError = null;

        // Giữ nguyên data cũ để tránh UI nhấp nháy khi load lại
        OnChanged?.Invoke();
    }

    public static void SetLoadedWithoutNotify(string tab)
    {
        CurrentTab = NormalizeTab(tab);
        IsLoading = false;
        LastError = null;
    }

    public static void SetData(string tab, NotificationMailDataWrap wrap)
    {
        CurrentTab = NormalizeTab(tab);
        IsLoading = false;
        LastError = null;

        _items.Clear();

        if (wrap != null)
        {
            if (wrap.totalUnread != null)
            {
                UnreadAll = wrap.totalUnread.all;
                UnreadPersonal = wrap.totalUnread.personal;
                UnreadSystem = wrap.totalUnread.system;
                UnreadMerchant = wrap.totalUnread.merchant;
            }
            else
            {
                UnreadAll = null;
                UnreadPersonal = null;
                UnreadSystem = null;
                UnreadMerchant = null;
            }

            if (wrap.data != null)
                _items.AddRange(wrap.data);
        }
        else
        {
            UnreadAll = null;
            UnreadPersonal = null;
            UnreadSystem = null;
            UnreadMerchant = null;
        }

        OnChanged?.Invoke();
    }

    public static void SetError(string tab, string error)
    {
        CurrentTab = NormalizeTab(tab);
        IsLoading = false;
        LastError = error;
        OnChanged?.Invoke();
    }

    public static bool IsSameData(string tab, NotificationMailDataWrap newWrap)
    {
        string normalizedTab = NormalizeTab(tab);
        if (!string.Equals(CurrentTab, normalizedTab, StringComparison.OrdinalIgnoreCase))
            return false;

        if (!IsSameUnread(newWrap))
            return false;

        var newItems = newWrap != null ? newWrap.data : null;
        int newCount = newItems != null ? newItems.Count : 0;

        if (_items.Count != newCount)
            return false;

        for (int i = 0; i < _items.Count; i++)
        {
            if (!IsSameItem(_items[i], newItems[i]))
                return false;
        }

        return true;
    }

    private static bool IsSameUnread(NotificationMailDataWrap wrap)
    {
        string all = wrap?.totalUnread?.all;
        string personal = wrap?.totalUnread?.personal;
        string system = wrap?.totalUnread?.system;
        string merchant = wrap?.totalUnread?.merchant;

        return string.Equals(UnreadAll, all, StringComparison.Ordinal) &&
               string.Equals(UnreadPersonal, personal, StringComparison.Ordinal) &&
               string.Equals(UnreadSystem, system, StringComparison.Ordinal) &&
               string.Equals(UnreadMerchant, merchant, StringComparison.Ordinal);
    }

    private static bool IsSameItem(NotificationMailItem a, NotificationMailItem b)
    {
        if (ReferenceEquals(a, b))
            return true;

        if (a == null || b == null)
            return false;

        if (!string.Equals((a._id ?? "").Trim(), (b._id ?? "").Trim(), StringComparison.Ordinal))
            return false;

        if (!string.Equals(a.title ?? "", b.title ?? "", StringComparison.Ordinal))
            return false;

        if (!string.Equals(a.text ?? "", b.text ?? "", StringComparison.Ordinal))
            return false;

        if (!string.Equals((a.label ?? "").Trim(), (b.label ?? "").Trim(), StringComparison.OrdinalIgnoreCase))
            return false;

        if (a.isRead != b.isRead)
            return false;

        if (!IsSameTime(a.time, b.time))
            return false;

        return true;
    }

    private static bool IsSameTime(NotificationMailTime a, NotificationMailTime b)
    {
        if (ReferenceEquals(a, b))
            return true;

        if (a == null || b == null)
            return false;

        return string.Equals(a.time ?? "", b.time ?? "", StringComparison.Ordinal) &&
               string.Equals(a.day ?? "", b.day ?? "", StringComparison.Ordinal);
    }

    private static string NormalizeTab(string tab)
    {
        return string.IsNullOrWhiteSpace(tab) ? "system" : tab.Trim().ToLower();
    }
}