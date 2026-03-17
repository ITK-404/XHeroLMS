using System;
using UnityEngine;

public class LetterButtonUI : MonoBehaviour
{
    [SerializeField] private bool isHaveNotify = false;
    [SerializeField] private ShakeNotification _shakeNotification;
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
        bool newHaveNotify = HasUnreadNotifications();

        if (!forceApply && newHaveNotify == previousNotify)
            return;

        isHaveNotify = newHaveNotify;
        ApplyNotifyState(forceApply);
    }

    private bool HasUnreadNotifications()
    {
        if (TryParseUnreadCount(NotificationsStaticStore.UnreadAll, out int unreadAll))
            return unreadAll > 0;

        var items = NotificationsStaticStore.Items;
        if (items == null || items.Count == 0)
            return false;

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item == null)
                continue;

            if (!item.isRead)
                return true;
        }

        return false;
    }

    private bool TryParseUnreadCount(string value, out int result)
    {
        result = 0;

        if (string.IsNullOrWhiteSpace(value))
            return false;

        return int.TryParse(value.Trim(), out result);
    }

    private void ApplyNotifyState(bool force = false)
    {
        if (!force && previousNotify == isHaveNotify)
            return;

        if (_shakeNotification != null)
        {
            if (isHaveNotify)
                _shakeNotification.StartShake();
            else
                _shakeNotification.StopShake();
        }

        if (dotNotify != null)
            dotNotify.SetActive(isHaveNotify);

        previousNotify = isHaveNotify;
    }
}