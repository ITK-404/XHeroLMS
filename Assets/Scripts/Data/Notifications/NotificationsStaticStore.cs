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
        CurrentTab = string.IsNullOrWhiteSpace(tab) ? "system" : tab;
        IsLoading = true;
        LastError = null;
        _items.Clear();
        OnChanged?.Invoke();
    }

    public static void SetData(string tab, NotificationMailDataWrap wrap)
    {
        CurrentTab = string.IsNullOrWhiteSpace(tab) ? "system" : tab;
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

            if (wrap.data != null)
                _items.AddRange(wrap.data);
        }

        OnChanged?.Invoke();
    }

    public static void SetError(string tab, string error)
    {
        CurrentTab = string.IsNullOrWhiteSpace(tab) ? "system" : tab;
        IsLoading = false;
        LastError = error;
        _items.Clear();
        OnChanged?.Invoke();
    }
}