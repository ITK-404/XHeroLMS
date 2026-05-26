using System;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("Behavior")]
    [Tooltip("Bật true nếu muốn lúc mở app vẫn hiện dot/rung theo unread count từ API. Với flow FCM -> click reset -> FCM mới thì nên để false.")]
    [SerializeField] private bool useApiUnreadFallback = false;

    [Tooltip("Khi có FCM mới thì restart shake kể cả đang rung.")]
    [SerializeField] private bool restartShakeOnEveryFcm = true;

    [Header("Runtime State")]
    [SerializeField] private bool isHaveNotify = false;

    [Header("Refs")]
    [SerializeField] private ShakeNotification shakeNotification;
    [SerializeField] private GameObject dotNotify;
    [SerializeField] private Button targetButton;

    private bool hasFcmSignal = false;
    private bool previousNotify = false;

    private void Awake()
    {
        if (targetButton == null)
            targetButton = GetComponent<Button>();

        UpdateNotifyCheck();
    }

    private void OnEnable()
    {
        NotificationsStaticStore.OnChanged += HandleNotificationsChanged;
        FCMManager.OnPushNotificationReceived += HandlePushNotificationReceived;

        if (targetButton != null)
            targetButton.onClick.AddListener(HandleButtonClicked);

        RefreshNotifyState(forceApply: true);
    }

    private void OnDisable()
    {
        NotificationsStaticStore.OnChanged -= HandleNotificationsChanged;
        FCMManager.OnPushNotificationReceived -= HandlePushNotificationReceived;

        if (targetButton != null)
            targetButton.onClick.RemoveListener(HandleButtonClicked);
    }

    private void HandlePushNotificationReceived()
    {
        Debug.Log("[LetterButtonUI] FCM received -> show notification signal");

        hasFcmSignal = true;
        SetNotifyState(true, forceApply: true, restartShake: restartShakeOnEveryFcm);
    }

    private void HandleNotificationsChanged()
    {
        Debug.Log("[LetterButtonUI] NotificationsStaticStore changed");
        RefreshNotifyState(forceApply: false);
    }

    private void RefreshNotifyState(bool forceApply)
    {
        bool newHaveNotify = hasFcmSignal;

        if (!newHaveNotify && useApiUnreadFallback)
            newHaveNotify = HasUnreadNotificationsFromApi();

        SetNotifyState(newHaveNotify, forceApply, restartShake: false);
    }

    private void SetNotifyState(bool active, bool forceApply, bool restartShake)
    {
        bool stateChanged = active != previousNotify;

        isHaveNotify = active;
        UpdateNotifyCheck();

        if (!forceApply && !stateChanged && !restartShake)
            return;

        if (shakeNotification == null)
        {
            Debug.LogWarning("[LetterButtonUI] shakeNotification is NULL");
            previousNotify = isHaveNotify;
            return;
        }

        if (isHaveNotify)
        {
            Debug.Log("[LetterButtonUI] Start shake notification");

            if (restartShake)
                shakeNotification.StopShake();

            shakeNotification.StartShake();
        }
        else
        {
            Debug.Log("[LetterButtonUI] Stop shake notification");
            shakeNotification.StopShake();
        }

        previousNotify = isHaveNotify;
    }

    public void HandleButtonClicked()
    {
        Debug.Log("[LetterButtonUI] Button clicked -> reset notification signal");
        ResetNotificationSignal();
    }

    public void ResetNotificationSignal()
    {
        hasFcmSignal = false;
        isHaveNotify = false;
        previousNotify = false;

        UpdateNotifyCheck();

        if (shakeNotification != null)
            shakeNotification.StopShake();
    }

    private bool HasUnreadNotificationsFromApi()
    {
        string unreadValue = GetUnreadValueBySource();
        Debug.Log($"[LetterButtonUI] unreadValue from GetUnreadValueBySource: '{unreadValue}'");

        if (TryParseUnreadCount(unreadValue, out int unreadCount))
        {
            Debug.Log($"[LetterButtonUI] Parsed unreadCount: {unreadCount}, HasUnread: {unreadCount > 0}");
            return unreadCount > 0;
        }

        Debug.Log("[LetterButtonUI] TryParseUnreadCount failed, fallback to item loop");

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

        if (value.EndsWith("+"))
            value = value.Substring(0, value.Length - 1);

        return int.TryParse(value, out result);
    }

    private void UpdateNotifyCheck()
    {
        if (dotNotify != null)
            dotNotify.SetActive(isHaveNotify);
    }
}