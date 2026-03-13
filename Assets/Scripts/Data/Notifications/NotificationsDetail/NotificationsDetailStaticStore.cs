using System;

public static class NotificationsDetailStaticStore
{
    public static string CurrentId { get; private set; }
    public static bool IsLoading { get; private set; }
    public static string LastError { get; private set; }

    public static NotificationMailItem CurrentDetail { get; private set; }
    public static bool HasData => CurrentDetail != null;

    public static event Action OnChanged;

    public static void Reset()
    {
        CurrentId = null;
        IsLoading = false;
        LastError = null;
        CurrentDetail = null;
        OnChanged?.Invoke();
    }

    public static void SetLoading(string id)
    {
        CurrentId = id;
        IsLoading = true;
        LastError = null;
        CurrentDetail = null;
        OnChanged?.Invoke();
    }

    public static void SetData(string id, NotificationMailItem item)
    {
        CurrentId = id;
        IsLoading = false;
        LastError = null;
        CurrentDetail = item;
        OnChanged?.Invoke();
    }

    public static void SetError(string id, string error)
    {
        CurrentId = id;
        IsLoading = false;
        LastError = error;
        CurrentDetail = null;
        OnChanged?.Invoke();
    }
}