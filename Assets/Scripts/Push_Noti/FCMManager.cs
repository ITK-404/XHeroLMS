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

#if UNITY_IOS
    // OFF để tránh cảm giác spam/đúp.
    private bool showIOSForegroundBannerWhenAppIsOpen = false;

    private bool requestIOSAuthorizationOnStart = true;
#endif

#if UNITY_ANDROID
    [Header("Android Local Notification")]
    [SerializeField] private string androidChannelId = "fcm_default_channel";
    [SerializeField] private string androidChannelName = "General Notifications";
    [SerializeField] private string androidChannelDescription = "Notifications from Firebase";
    [SerializeField] private string androidSmallIcon = "default";
#endif

    private static FCMManager _instance;

    private bool _initialized;
    private bool _eventsRegistered;
    private bool _appInForeground = true;
    private bool _openedFromNotificationRaised;

    private string _currentFcmToken;

    private readonly Dictionary<string, float> _handledMessageIds = new Dictionary<string, float>();
    private const float DuplicateWindowSeconds = 10f;

#if UNITY_IOS
    private AuthorizationRequest _iosAuthRequest;
    private string _lastIosForegroundTitle;
    private string _lastIosForegroundBody;
    private float _lastIosForegroundShownTime;
#endif

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
        _appInForeground = Application.isFocused;

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
            StartCoroutine(RequestIOSAuthorizationCoroutine());
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
        DisposeIOSAuthorizationRequest();
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
        if (intentData == null)
            return;

        if (_openedFromNotificationRaised)
            return;

        _openedFromNotificationRaised = true;
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

    private IEnumerator RequestIOSAuthorizationCoroutine()
    {
        DisposeIOSAuthorizationRequest();

        var options = AuthorizationOption.Alert | AuthorizationOption.Badge | AuthorizationOption.Sound;
        _iosAuthRequest = new AuthorizationRequest(options, true);

        while (!_iosAuthRequest.IsFinished)
            yield return null;

        Debug.Log(
            "[FCM] iOS authorization finished. " +
            "granted=" + _iosAuthRequest.Granted +
            " error=" + _iosAuthRequest.Error +
            " deviceToken=" + Safe(_iosAuthRequest.DeviceToken)
        );

        DisposeIOSAuthorizationRequest();
    }

    private void DisposeIOSAuthorizationRequest()
    {
        if (_iosAuthRequest != null)
        {
            _iosAuthRequest.Dispose();
            _iosAuthRequest = null;
        }
    }

    private void ShowIOSForegroundLocalNotification(string title, string body)
    {
        if (string.IsNullOrWhiteSpace(title))
            title = "Thông báo mới";

        if (string.IsNullOrWhiteSpace(body))
            body = "Bạn có thông báo mới.";

        // Chặn spam cùng nội dung trong thời gian ngắn
        if (_lastIosForegroundTitle == title &&
            _lastIosForegroundBody == body &&
            Time.realtimeSinceStartup - _lastIosForegroundShownTime < 3f)
        {
            Debug.Log("[FCM] iOS foreground local notification skipped (same content too soon).");
            return;
        }

        _lastIosForegroundTitle = title;
        _lastIosForegroundBody = body;
        _lastIosForegroundShownTime = Time.realtimeSinceStartup;

        var trigger = new iOSNotificationTimeIntervalTrigger
        {
            TimeInterval = TimeSpan.FromSeconds(0.1f),
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
        Debug.Log("[FCM] iOS foreground local notification scheduled.");
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
            FirebaseMessaging.TokenRegistrationOnInitEnabled = true;

            RegisterFirebaseEvents();

            if (permissionRequester == null)
                permissionRequester = GetComponent<NotificationPermissionRequester>();

            if (permissionRequester != null)
                permissionRequester.RequestPermissionIfNeeded();

            // Chủ động lấy token thật sau khi init xong
            RequestCurrentToken("InitializeFirebase");
        });
    }

    public void RequestCurrentToken(string source = "ManualRequest")
    {
        if (!_initialized)
        {
            Debug.LogWarning("[FCM] RequestCurrentToken ignored because Firebase is not initialized yet. source=" + source);
            return;
        }

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

        onDone?.Invoke(GetBestKnownFcmToken());
    }

    private void RegisterFirebaseEvents()
    {
        if (_eventsRegistered)
            return;

        FirebaseMessaging.TokenReceived += OnTokenReceived;
        FirebaseMessaging.MessageReceived += OnMessageReceived;
        _eventsRegistered = true;

        Debug.Log("[FCM] Firebase messaging events registered.");
    }

    private void UnregisterFirebaseEvents()
    {
        if (!_eventsRegistered)
            return;

        FirebaseMessaging.TokenReceived -= OnTokenReceived;
        FirebaseMessaging.MessageReceived -= OnMessageReceived;
        _eventsRegistered = false;

        Debug.Log("[FCM] Firebase messaging events unregistered.");
    }

    private void OnTokenReceived(object sender, TokenReceivedEventArgs tokenArgs)
    {
        string token = tokenArgs != null ? tokenArgs.Token : string.Empty;
        SaveFcmToken(token, "OnTokenReceived");

        // Gọi lại GetTokenAsync để chắc chắn lấy token usable mới nhất
        if (_initialized)
            RequestCurrentToken("OnTokenReceivedRefresh");
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

        ExtractVisibleContent(msg, out string title, out string body);
        bool hasVisibleContent = !string.IsNullOrWhiteSpace(title) || !string.IsNullOrWhiteSpace(body);

        Debug.Log(BuildMessageLog(msg, title, body));

        // App đang mở: xử lý foreground
        if (_appInForeground && showLocalNotificationWhenAppIsOpen && hasVisibleContent)
        {
#if UNITY_ANDROID
            ShowAndroidLocalNotification(title, body);
#elif UNITY_IOS
            // iOS mặc định nên để OFF để tránh cảm giác spam/đúp
            if (showIOSForegroundBannerWhenAppIsOpen)
                ShowIOSForegroundLocalNotification(title, body);
            else
                Debug.Log("[FCM] iOS foreground message received. Skipped local banner to avoid duplicate/spam.");
#else
            Debug.Log("[FCM] Foreground local notification not implemented on this platform.");
#endif
        }

        // Quan trọng: event này vẫn luôn bắn để app đang mở có thể reload UI/inbox/chat
        OnPushNotificationReceived?.Invoke();
    }

    private static void ExtractVisibleContent(FirebaseMessage msg, out string title, out string body)
    {
        title = null;
        body = null;

        if (msg.Notification != null)
        {
            title = msg.Notification.Title;
            body = msg.Notification.Body;
        }

        if (msg.Data == null || msg.Data.Count <= 0)
            return;

        foreach (KeyValuePair<string, string> kv in msg.Data)
        {
            if (string.IsNullOrWhiteSpace(title) &&
                kv.Key.Equals("title", StringComparison.OrdinalIgnoreCase))
            {
                title = kv.Value;
            }

            if (string.IsNullOrWhiteSpace(body) &&
                (kv.Key.Equals("body", StringComparison.OrdinalIgnoreCase) ||
                 kv.Key.Equals("message", StringComparison.OrdinalIgnoreCase)))
            {
                body = kv.Value;
            }
        }
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

    private bool IsDuplicateMessage(string messageId)
    {
        if (string.IsNullOrWhiteSpace(messageId))
            return false;

        float now = Time.realtimeSinceStartup;

        if (_handledMessageIds.Count > 0)
        {
            List<string> expiredKeys = null;

            foreach (var kv in _handledMessageIds)
            {
                if (now - kv.Value > DuplicateWindowSeconds)
                {
                    expiredKeys ??= new List<string>();
                    expiredKeys.Add(kv.Key);
                }
            }

            if (expiredKeys != null)
            {
                for (int i = 0; i < expiredKeys.Count; i++)
                    _handledMessageIds.Remove(expiredKeys[i]);
            }
        }

        if (_handledMessageIds.TryGetValue(messageId, out float lastTime))
        {
            if (now - lastTime < DuplicateWindowSeconds)
                return true;
        }

        _handledMessageIds[messageId] = now;
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