using System;
using UnityEngine;

public class LetterButtonUI : MonoBehaviour
{
    public enum NotifySource
    {
        All,
        Personal,
        System,
        Merchant
    }

    [Header("API Source")]
    [SerializeField] private NotifySource notifySource = NotifySource.System;

    [Header("Runtime State")]
    [SerializeField] private bool isHaveNotify = false;

    [Header("Refs")]
    [SerializeField] private ShakeNotification shakeNotification;
    [SerializeField] private GameObject dotNotify;

    private bool previousNotify = false;

    private void Awake()
    {
        // refresh state
        UpdateNotifyCheck();
    }

    private void OnEnable()
    {
        NotificationsStaticStore.OnChanged += HandleNotificationsChanged;
        RefreshNotifyState(forceApply: true);
    }

    private void OnDisable()
    {
        NotificationsStaticStore.OnChanged -= HandleNotificationsChanged;
    }

    private void HandleNotificationsChanged()
    {
        Debug.Log($"Letter Button UI: Changed");
        RefreshNotifyState(forceApply: false);
    }

    private void RefreshNotifyState(bool forceApply)
    {
        bool newHaveNotify = HasUnreadNotificationsFromApi();

        if (!forceApply && newHaveNotify == previousNotify)
            return;

        isHaveNotify = newHaveNotify;
        ApplyNotifyState(forceApply);
        UpdateNotifyCheck();
    }

    private bool HasUnreadNotificationsFromApi()
{
    // Ưu tiên unread count từ API
    string unreadValue = GetUnreadValueBySource();
    Debug.Log($"[LetterButtonUI] unreadValue from GetUnreadValueBySource: '{unreadValue}'");

    if (TryParseUnreadCount(unreadValue, out int unreadCount))
    {
        Debug.Log($"[LetterButtonUI] Parsed unreadCount: {unreadCount}, HasUnread: {unreadCount > 0}");
        return unreadCount > 0;
    }

    Debug.Log("[LetterButtonUI] TryParseUnreadCount failed, fallback to item loop");

    // Fallback: nếu unread count parse không được thì duyệt item từ API
    var items = NotificationsStaticStore.Items;
    Debug.Log($"[LetterButtonUI] Items count: {(items == null ? "NULL" : items.Count.ToString())}");

    if (items == null || items.Count == 0)
        return false;

    for (int i = 0; i < items.Count; i++)
    {
        var item = items[i];
        if (item == null)
        {
            Debug.Log($"[LetterButtonUI] Item[{i}] is null, skip");
            continue;
        }

        if (!MatchSource(item))
        {
            Debug.Log($"[LetterButtonUI] Item[{i}] MatchSource=false, skip");
            continue;
        }

        Debug.Log($"[LetterButtonUI] Item[{i}] MatchSource=true, isRead={item.isRead}");

        if (!item.isRead)
        {
            Debug.Log($"[LetterButtonUI] Found unread item at index {i}, return true");
            return true;
        }
    }

    Debug.Log("[LetterButtonUI] No unread item found, return false");
    return false;
}

    private string GetUnreadValueBySource()
    {
        switch (notifySource)
        {
            case NotifySource.Personal:
                return NotificationsStaticStore.UnreadPersonal;

            case NotifySource.System:
                return NotificationsStaticStore.UnreadSystem;

            case NotifySource.Merchant:
                return NotificationsStaticStore.UnreadMerchant;

            case NotifySource.All:
            default:
                return NotificationsStaticStore.UnreadAll;
        }
    }

    private bool MatchSource(NotificationMailItem item)
    {
        if (item == null)
            return false;

        if (notifySource == NotifySource.All)
            return true;

        string label = (item.label ?? "").Trim().ToLower();

        switch (notifySource)
        {
            case NotifySource.Personal:
                return label == "personal";

            case NotifySource.System:
                return label == "system";

            case NotifySource.Merchant:
                return label == "merchant";

            default:
                return true;
        }
    }

    private bool TryParseUnreadCount(string value, out int result)
    {
        result = 0;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        value = value.Trim();

        // support "9+"
        if (value.EndsWith("+"))
            value = value.Substring(0, value.Length - 1);

        return int.TryParse(value, out result);
    }

    private void ApplyNotifyState(bool force = false)
    {
        if (!force && previousNotify == isHaveNotify)
            return;

        if (shakeNotification != null)
        {
            if (isHaveNotify)
                shakeNotification.StartShake();
            else
                shakeNotification.StopShake();
        }

        previousNotify = isHaveNotify;
    }

    private void UpdateNotifyCheck()
    {
        if (dotNotify != null)
            dotNotify.SetActive(isHaveNotify);
    }
}