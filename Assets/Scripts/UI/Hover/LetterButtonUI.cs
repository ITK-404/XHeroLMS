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
        ApplyNotifyState(force: true);
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
        RefreshNotifyState(forceApply: false);
    }

    private void RefreshNotifyState(bool forceApply)
    {
        bool newHaveNotify = HasUnreadNotificationsFromApi();

        if (!forceApply && newHaveNotify == previousNotify)
            return;

        isHaveNotify = newHaveNotify;
        ApplyNotifyState(forceApply);
    }

    private bool HasUnreadNotificationsFromApi()
    {
        // Ưu tiên unread count từ API
        string unreadValue = GetUnreadValueBySource();

        if (TryParseUnreadCount(unreadValue, out int unreadCount))
            return unreadCount > 0;

        // Fallback: nếu unread count parse không được thì duyệt item từ API
        var items = NotificationsStaticStore.Items;
        if (items == null || items.Count == 0)
            return false;

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item == null)
                continue;

            if (!MatchSource(item))
                continue;

            if (!item.isRead)
                return true;
        }

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

        if (dotNotify != null)
            dotNotify.SetActive(isHaveNotify);

        previousNotify = isHaveNotify;
    }
}