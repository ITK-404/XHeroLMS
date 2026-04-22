using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Firebase;
using Firebase.Extensions;
using Firebase.Messaging;

#if UNITY_ANDROID
using Unity.Notifications.Android;
#endif

#if UNITY_IOS
using Unity.Notifications.iOS;
#endif

public class FCMManager : MonoBehaviour
{
    [SerializeField] private NotificationPermissionRequester permissionRequester;

    [Header("Foreground Local Notification")]
    [SerializeField] private bool showLocalNotificationWhenAppIsOpen = true;

#if UNITY_ANDROID
    [Header("Android Local Notification")]
    [SerializeField] private string androidChannelId = "fcm_default_channel";
    [SerializeField] private string androidChannelName = "General Notifications";
    [SerializeField] private string androidChannelDescription = "Notifications from Firebase";
    [SerializeField] private string androidSmallIcon = "default";
#endif

#if UNITY_IOS
    [Header("iOS Local Notification")]
    [SerializeField] private bool requestIOSAuthorizationOnStart = true;
#endif

    private static FCMManager _instance;

    private bool _initialized;
    private bool _eventsRegistered;
    private bool _appInForeground = true;

    private string _lastHandledMessageId;
    private float _lastHandledRealtime;

    private string _currentFcmToken;

    public static event Action OnPushNotificationReceived;
    public static event Action OnAppResumed;
    public static event Action OnOpenedFromNotification;
    public static event Action<string> OnFcmTokenReady;

    public const string PlayerPrefsFcmTokenKey = "FCM_DEVICE_TOKEN";

    public static bool IsInitialized => _instance != null && _instance._initialized;
    public static bool HasUsableToken => !string.IsNullOrWhiteSpace(GetBestKnownFcmToken());

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        _currentFcmToken = PlayerPrefs.GetString(PlayerPrefsFcmTokenKey, "");

#if UNITY_ANDROID
        RegisterAndroidNotificationChannel();
#endif

#if UNITY_IOS
        RegisterIOSNotificationHooks();
#endif
    }

    private void Start()
    {
#if UNITY_IOS
        if (requestIOSAuthorizationOnStart)
            RequestIOSAuthorization();
#endif

        InitializeFirebase();

#if UNITY_ANDROID
        CheckIfOpenedFromAndroidNotification();
#endif
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        _appInForeground = !pauseStatus;
        Debug.Log($"[FCM] OnApplicationPause({pauseStatus}) | foreground={_appInForeground}");
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        _appInForeground = hasFocus;
        Debug.Log($"[FCM] OnApplicationFocus({hasFocus})");

        if (!hasFocus) return;

        OnAppResumed?.Invoke();

#if UNITY_ANDROID
        CheckIfOpenedFromAndroidNotification();
#endif
    }

    private void OnDestroy()
    {
#if UNITY_IOS
        UnregisterIOSNotificationHooks();
#endif

        if (_instance == this)
            _instance = null;

        UnregisterFirebaseEvents();
    }

#if UNITY_ANDROID
    private void RegisterAndroidNotificationChannel()
    {
        var channel = new AndroidNotificationChannel
        {
            Id = androidChannelId,
            Name = androidChannelName,
            Importance = Importance.High,
            Description = androidChannelDescription,
        };

        AndroidNotificationCenter.RegisterNotificationChannel(channel);
        Debug.Log("[FCM] Android channel registered: " + androidChannelId);
    }

    private void ShowAndroidLocalNotification(string title, string body)
    {
        if (string.IsNullOrWhiteSpace(title))
            title = "Thông báo mới";

        if (string.IsNullOrWhiteSpace(body))
            body = "Bạn có thông báo mới.";

        var notification = new AndroidNotification
        {
            Title = title,
            Text = body,
            FireTime = DateTime.Now,
            SmallIcon = string.IsNullOrWhiteSpace(androidSmallIcon) ? null : androidSmallIcon,
        };

        int id = AndroidNotificationCenter.SendNotification(notification, androidChannelId);
        Debug.Log($"[FCM] Android local notification sent. id={id} | title={title}");
    }

    private void CheckIfOpenedFromAndroidNotification()
    {
        var intentData = AndroidNotificationCenter.GetLastNotificationIntent();
        if (intentData == null) return;

        Debug.Log("[FCM] App opened from Android notification.");
        OnOpenedFromNotification?.Invoke();
    }
#endif

#if UNITY_IOS
    private void RegisterIOSNotificationHooks()
    {
        iOSNotificationCenter.OnNotificationReceived += OnIOSLocalNotificationReceived;
    }

    private void UnregisterIOSNotificationHooks()
    {
        iOSNotificationCenter.OnNotificationReceived -= OnIOSLocalNotificationReceived;
    }

    private void OnIOSLocalNotificationReceived(iOSNotification notification)
    {
        if (notification == null) return;
        Debug.Log("[FCM] iOS local notification received by OS. title=" + notification.Title);
    }

    private void RequestIOSAuthorization()
    {
        var authorizationOption = AuthorizationOption.Alert | AuthorizationOption.Badge | AuthorizationOption.Sound;
        using var req = new AuthorizationRequest(authorizationOption, true);
        Debug.Log("[FCM] iOS authorization requested.");
    }

    private void ShowIOSLocalNotification(string title, string body)
    {
        if (string.IsNullOrWhiteSpace(title))
            title = "Thông báo mới";

        if (string.IsNullOrWhiteSpace(body))
            body = "Bạn có thông báo mới.";

        var trigger = new iOSNotificationTimeIntervalTrigger
        {
            TimeInterval = new TimeSpan(0, 0, 1),
            Repeats = false
        };

        var notification = new iOSNotification
        {
            Identifier = Guid.NewGuid().ToString("N"),
            Title = title,
            Body = body,
            ShowInForeground = true,
            ForegroundPresentationOption = PresentationOption.Alert | PresentationOption.Sound,
            ThreadIdentifier = "fcm_foreground_thread",
            Trigger = trigger
        };

        iOSNotificationCenter.ScheduleNotification(notification);
        Debug.Log("[FCM] iOS local notification scheduled.");
    }
#endif

    private void InitializeFirebase()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("[FCM] Firebase init failed: " + task.Exception);
                return;
            }

            if (task.Result != DependencyStatus.Available)
            {
                Debug.LogError("[FCM] Firebase dependencies unavailable: " + task.Result);
                return;
            }

            var app = FirebaseApp.DefaultInstance;
            if (app == null)
            {
                Debug.LogError("[FCM] FirebaseApp.DefaultInstance is null.");
                return;
            }

            _initialized = true;
            RegisterFirebaseEvents();

            FirebaseMessaging.TokenRegistrationOnInitEnabled = true;

            if (permissionRequester == null)
                permissionRequester = GetComponent<NotificationPermissionRequester>();

            if (permissionRequester != null)
                permissionRequester.RequestPermissionIfNeeded();

        });
    }

    public void RequestCurrentToken(string source = "ManualRequest")
    {
        FirebaseMessaging.GetTokenAsync().ContinueWithOnMainThread(tokenTask =>
        {
            if (tokenTask.IsFaulted || tokenTask.IsCanceled)
            {
                Debug.LogError("[FCM] GetTokenAsync failed (" + source + "): " + tokenTask.Exception);
                return;
            }

            string token = tokenTask.Result;
            SaveFcmToken(token, source);
        });
    }

    public static string GetBestKnownFcmToken()
    {
        if (_instance != null && !string.IsNullOrWhiteSpace(_instance._currentFcmToken))
            return _instance._currentFcmToken;

        return PlayerPrefs.GetString(PlayerPrefsFcmTokenKey, "");
    }

    public static string GetSavedFcmToken()
    {
        return GetBestKnownFcmToken();
    }

    public static IEnumerator WaitForReadyToken(float timeoutSeconds, Action<string> onDone)
    {
        float start = Time.realtimeSinceStartup;

        if (_instance != null && _instance._initialized)
            _instance.RequestCurrentToken("WaitForReadyToken");

        while (Time.realtimeSinceStartup - start < timeoutSeconds)
        {
            string token = GetBestKnownFcmToken();
            if (!string.IsNullOrWhiteSpace(token))
            {
                onDone?.Invoke(token);
                yield break;
            }

            yield return null;
        }

        string fallback = GetBestKnownFcmToken();
        onDone?.Invoke(fallback);
    }

    private void RegisterFirebaseEvents()
    {
        if (_eventsRegistered) return;

        FirebaseMessaging.TokenReceived += OnTokenReceived;
        FirebaseMessaging.MessageReceived += OnMessageReceived;
        _eventsRegistered = true;
    }

    private void UnregisterFirebaseEvents()
    {
        if (!_eventsRegistered) return;

        FirebaseMessaging.TokenReceived -= OnTokenReceived;
        FirebaseMessaging.MessageReceived -= OnMessageReceived;
        _eventsRegistered = false;
    }

    private void OnTokenReceived(object sender, TokenReceivedEventArgs tokenArgs)
    {
        string token = tokenArgs != null ? tokenArgs.Token : string.Empty;
        RequestCurrentToken("InitializeFirebase");
    }

    private void SaveFcmToken(string token, string source)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            Debug.LogWarning("[FCM] Empty token from " + source);
            return;
        }

        bool changed = !string.Equals(_currentFcmToken, token, StringComparison.Ordinal);
        _currentFcmToken = token;

        PlayerPrefs.SetString(PlayerPrefsFcmTokenKey, token);
        PlayerPrefs.Save();

        Debug.Log("[FCM] Token saved from " + source + " | preview=" + GetTokenPreview(token) + " | changed=" + changed);

        OnFcmTokenReady?.Invoke(token);
    }

    private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        if (e == null || e.Message == null)
        {
            Debug.LogWarning("[FCM] MessageReceived args/message is null.");
            return;
        }

        var msg = e.Message;

        if (IsDuplicateMessage(msg.MessageId))
        {
            Debug.Log("[FCM] Duplicate message ignored: " + Safe(msg.MessageId));
            return;
        }

        string title = null;
        string body = null;

        if (msg.Notification != null)
        {
            title = msg.Notification.Title;
            body = msg.Notification.Body;
        }

        if (msg.Data != null && msg.Data.Count > 0)
        {
            foreach (KeyValuePair<string, string> kv in msg.Data)
            {
                if (string.IsNullOrWhiteSpace(title) &&
                    kv.Key.Equals("title", StringComparison.OrdinalIgnoreCase))
                    title = kv.Value;

                if (string.IsNullOrWhiteSpace(body) &&
                    (kv.Key.Equals("body", StringComparison.OrdinalIgnoreCase) ||
                     kv.Key.Equals("message", StringComparison.OrdinalIgnoreCase)))
                    body = kv.Value;
            }
        }

        Debug.Log(BuildMessageLog(msg, title, body));

        if (_appInForeground && showLocalNotificationWhenAppIsOpen)
        {
            ShowForegroundLocalNotification(title, body);
        }

        OnPushNotificationReceived?.Invoke();
    }

    private string BuildMessageLog(FirebaseMessage msg, string title, string body)
    {
        var sb = new StringBuilder();
        sb.AppendLine("========== [FCM] MESSAGE RECEIVED ==========");
        sb.AppendLine("[FCM] From: " + Safe(msg.From));
        sb.AppendLine("[FCM] To: " + Safe(msg.To));
        sb.AppendLine("[FCM] MessageId: " + Safe(msg.MessageId));
        sb.AppendLine("[FCM] MessageType: " + Safe(msg.MessageType));
        sb.AppendLine("[FCM] CollapseKey: " + Safe(msg.CollapseKey));
        sb.AppendLine("[FCM] TTL: " + msg.TimeToLive);
        sb.AppendLine("[FCM] Title: " + Safe(title));
        sb.AppendLine("[FCM] Body: " + Safe(body));

        if (msg.Data != null && msg.Data.Count > 0)
        {
            sb.AppendLine("[FCM] Data payload:");
            foreach (var kv in msg.Data)
                sb.AppendLine($"  - {kv.Key} = {kv.Value}");
        }
        else
        {
            sb.AppendLine("[FCM] Data payload: empty");
        }

        sb.AppendLine("===========================================");
        return sb.ToString();
    }

    private void ShowForegroundLocalNotification(string title, string body)
    {
#if UNITY_ANDROID
        ShowAndroidLocalNotification(title, body);
#elif UNITY_IOS
        ShowIOSLocalNotification(title, body);
#else
        Debug.Log("[FCM] Foreground local notification not implemented on this platform.");
#endif
    }

    private bool IsDuplicateMessage(string messageId)
    {
        if (string.IsNullOrWhiteSpace(messageId))
            return false;

        if (_lastHandledMessageId == messageId &&
            Time.realtimeSinceStartup - _lastHandledRealtime < 5f)
        {
            return true;
        }

        _lastHandledMessageId = messageId;
        _lastHandledRealtime = Time.realtimeSinceStartup;
        return false;
    }

    private static string Safe(string value)
    {
        return string.IsNullOrEmpty(value) ? "(null/empty)" : value;
    }

    private static string GetTokenPreview(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return "(empty)";

        if (token.Length <= 16)
            return token;

        return token.Substring(0, 8) + "..." + token.Substring(token.Length - 8);
    }
}