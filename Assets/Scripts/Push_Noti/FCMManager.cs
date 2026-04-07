using System;
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

    public static event Action OnPushNotificationReceived;
    public static event Action OnAppResumed;

    public const string PlayerPrefsFcmTokenKey = "FCM_DEVICE_TOKEN";

    public static bool IsInitialized => _instance != null && _instance._initialized;

    private void Awake()
    {
        Debug.Log($"[FCM] Awake() | object={gameObject.name} | instanceId={GetInstanceID()} | scene={gameObject.scene.name}");

        if (_instance != null && _instance != this)
        {
            Debug.LogWarning(
                $"[FCM] Duplicate FCMManager detected -> destroy duplicate. " +
                $"current={GetInstanceID()} existing={_instance.GetInstanceID()} currentObj={gameObject.name} existingObj={_instance.gameObject.name}");
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        Debug.Log($"[FCM] Main instance assigned. object={gameObject.name} | instanceId={GetInstanceID()}");

#if UNITY_ANDROID
        RegisterAndroidNotificationChannel();
#endif

#if UNITY_IOS
        RegisterIOSNotificationHooks();
#endif
    }

    private void Start()
    {
        Debug.Log($"[FCM] Start() | object={gameObject.name} | instanceId={GetInstanceID()}");

#if UNITY_IOS
        if (requestIOSAuthorizationOnStart)
            RequestIOSAuthorization();
#endif

        InitializeFirebase();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        _appInForeground = !pauseStatus;
        Debug.Log($"[FCM] OnApplicationPause({pauseStatus}) | foreground={_appInForeground}");
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        _appInForeground = hasFocus;
        Debug.Log($"[FCM] OnApplicationFocus({hasFocus}) | object={gameObject.name} | instanceId={GetInstanceID()}");

        if (!hasFocus) return;

        Debug.Log("[FCM] App resumed.");
        OnAppResumed?.Invoke();
    }

    private void OnDestroy()
    {
        Debug.Log($"[FCM] OnDestroy() | object={gameObject.name} | instanceId={GetInstanceID()}");

#if UNITY_IOS
        UnregisterIOSNotificationHooks();
#endif

        if (_instance == this)
        {
            _instance = null;
            Debug.Log("[FCM] Main instance cleared.");
        }

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
        Debug.Log("[FCM] Android notification channel registered: " + androidChannelId);
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
        Debug.Log("[FCM] Android local notification sent. id=" + id + " | title=" + title + " | body=" + body);
    }
#endif

#if UNITY_IOS
    private void RegisterIOSNotificationHooks()
    {
        iOSNotificationCenter.OnNotificationReceived += OnIOSLocalNotificationReceived;
        Debug.Log("[FCM] iOS notification hooks registered.");
    }

    private void UnregisterIOSNotificationHooks()
    {
        iOSNotificationCenter.OnNotificationReceived -= OnIOSLocalNotificationReceived;
        Debug.Log("[FCM] iOS notification hooks unregistered.");
    }

    private void OnIOSLocalNotificationReceived(iOSNotification notification)
    {
        if (notification == null) return;
        Debug.Log("[FCM] iOS local notification received by OS. title=" + notification.Title + " body=" + notification.Body);
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

        var timeTrigger = new iOSNotificationTimeIntervalTrigger
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
            CategoryIdentifier = "fcm_foreground",
            ThreadIdentifier = "fcm_foreground_thread",
            Trigger = timeTrigger
        };

        iOSNotificationCenter.ScheduleNotification(notification);
        Debug.Log("[FCM] iOS local notification scheduled. title=" + title + " | body=" + body);
    }
#endif

    private void InitializeFirebase()
    {
        Debug.Log("[FCM] InitializeFirebase() start");
        Debug.Log("[FCM] Application.identifier = " + Application.identifier);
        Debug.Log("[FCM] FirebaseApp.DefaultInstance == null ? " + (FirebaseApp.DefaultInstance == null));

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("[FCM] CheckAndFixDependenciesAsync faulted: " + task.Exception);
                return;
            }

            if (task.IsCanceled)
            {
                Debug.LogWarning("[FCM] CheckAndFixDependenciesAsync canceled.");
                return;
            }

            var dependencyStatus = task.Result;
            Debug.Log("[FCM] Dependency status: " + dependencyStatus);

            if (dependencyStatus != DependencyStatus.Available)
            {
                Debug.LogError("[FCM] Could not resolve all Firebase dependencies: " + dependencyStatus);
                return;
            }

            FirebaseApp app = FirebaseApp.DefaultInstance;
            if (app == null)
            {
                Debug.LogError("[FCM] FirebaseApp.DefaultInstance is NULL after dependency check.");
                return;
            }

            _initialized = true;

            Debug.Log("[FCM] Firebase initialized: " + app.Name);

            LogFirebaseOptions(app);

            RegisterFirebaseEvents();

            FirebaseMessaging.TokenRegistrationOnInitEnabled = true;
            Debug.Log("[FCM] TokenRegistrationOnInitEnabled = true");

            if (permissionRequester == null)
                permissionRequester = GetComponent<NotificationPermissionRequester>();

            if (permissionRequester != null)
            {
                Debug.Log("[FCM] Requesting notification permission if needed...");
                permissionRequester.RequestPermissionIfNeeded();
            }
            else
            {
                Debug.LogWarning("[FCM] NotificationPermissionRequester is missing.");
            }

            RequestCurrentToken("InitializeFirebase/GetTokenAsync");
        });
    }

    private void LogFirebaseOptions(FirebaseApp app)
    {
        if (app == null)
        {
            Debug.LogError("[FCM] LogFirebaseOptions() app is null.");
            return;
        }

        var options = app.Options;
        if (options == null)
        {
            Debug.LogError("[FCM] Firebase options is NULL. Runtime config may not be loaded.");
            return;
        }

        Debug.Log("========== [FCM] FIREBASE OPTIONS ==========");
        Debug.Log("[FCM] AppId: " + Safe(options.AppId));
        Debug.Log("[FCM] ProjectId: " + Safe(options.ProjectId));
        Debug.Log("[FCM] ApiKey: " + Safe(options.ApiKey));
        Debug.Log("[FCM] StorageBucket: " + Safe(options.StorageBucket));
        Debug.Log("[FCM] DatabaseUrl: " + (options.DatabaseUrl != null ? options.DatabaseUrl.ToString() : "(null)"));
        Debug.Log("[FCM] MessageSenderId: " + GetSenderIdFromAppId(options.AppId));
        Debug.Log("========== [FCM] END FIREBASE OPTIONS ==========");
    }

    public void RequestCurrentToken(string source = "ManualRequest")
    {
        Debug.Log("[FCM] RequestCurrentToken called. Source = " + source);

        FirebaseMessaging.GetTokenAsync().ContinueWithOnMainThread(tokenTask =>
        {
            if (tokenTask.IsFaulted)
            {
                Debug.LogError("[FCM] GetTokenAsync faulted (" + source + "): " + tokenTask.Exception);
                return;
            }

            if (tokenTask.IsCanceled)
            {
                Debug.LogWarning("[FCM] GetTokenAsync canceled (" + source + ")");
                return;
            }

            string token = tokenTask.Result;

            if (string.IsNullOrWhiteSpace(token))
            {
                Debug.LogWarning("[FCM] GetTokenAsync returned empty token (" + source + ")");
                return;
            }

            Debug.Log("[FCM] GetTokenAsync success (" + source + ")");
            SaveFcmToken(token, source);
        });
    }

    private void RegisterFirebaseEvents()
    {
        if (_eventsRegistered)
        {
            Debug.Log("[FCM] Firebase messaging events already registered.");
            return;
        }

        FirebaseMessaging.TokenReceived += OnTokenReceived;
        FirebaseMessaging.MessageReceived += OnMessageReceived;
        _eventsRegistered = true;

        Debug.Log("[FCM] Firebase messaging events registered.");
    }

    private void UnregisterFirebaseEvents()
    {
        if (!_eventsRegistered) return;

        FirebaseMessaging.TokenReceived -= OnTokenReceived;
        FirebaseMessaging.MessageReceived -= OnMessageReceived;
        _eventsRegistered = false;

        Debug.Log("[FCM] Firebase messaging events unregistered.");
    }

    private void OnTokenReceived(object sender, TokenReceivedEventArgs tokenArgs)
    {
        string token = tokenArgs != null ? tokenArgs.Token : string.Empty;

        Debug.Log("[FCM] OnTokenReceived fired.");
        SaveFcmToken(token, "TokenReceived");
    }

    private void SaveFcmToken(string token, string source)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            Debug.LogWarning("[FCM] " + source + " token is null/empty");
            return;
        }

        string oldToken = PlayerPrefs.GetString(PlayerPrefsFcmTokenKey, "");
        bool changed = !string.Equals(oldToken, token, StringComparison.Ordinal);

        PlayerPrefs.SetString(PlayerPrefsFcmTokenKey, token);
        PlayerPrefs.Save();

        Debug.Log("[FCM] " + source + " token saved.");
        Debug.Log("[FCM] Token changed: " + changed);
        Debug.Log("[FCM] Token length: " + token.Length);
        Debug.Log("[FCM] Token preview: " + GetTokenPreview(token));

        if (!string.IsNullOrEmpty(oldToken))
            Debug.Log("[FCM] Old token preview: " + GetTokenPreview(oldToken));
    }

    public static string GetSavedFcmToken()
    {
        string token = PlayerPrefs.GetString(PlayerPrefsFcmTokenKey, "");

        if (string.IsNullOrEmpty(token))
            Debug.LogWarning("[FCM] GetSavedFcmToken() -> empty");
        else
            Debug.Log("[FCM] GetSavedFcmToken() -> " + GetTokenPreview(token));

        return token;
    }

    public static string GetSavedFcmTokenPreview()
    {
        string token = PlayerPrefs.GetString(PlayerPrefsFcmTokenKey, "");
        return GetTokenPreview(token);
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

        Debug.Log("========== [FCM] MESSAGE RECEIVED ==========");
        Debug.Log("[FCM] From: " + Safe(msg.From));
        Debug.Log("[FCM] To: " + Safe(msg.To));
        Debug.Log("[FCM] MessageId: " + Safe(msg.MessageId));
        Debug.Log("[FCM] MessageType: " + Safe(msg.MessageType));
        Debug.Log("[FCM] CollapseKey: " + Safe(msg.CollapseKey));
        Debug.Log("[FCM] TTL: " + msg.TimeToLive);

        string title = null;
        string body = null;

        if (msg.Notification != null)
        {
            title = msg.Notification.Title;
            body = msg.Notification.Body;

            Debug.Log("[FCM] Notification.Title: " + Safe(title));
            Debug.Log("[FCM] Notification.Body: " + Safe(body));
        }
        else
        {
            Debug.Log("[FCM] Notification payload: null");
        }

        if (msg.Data != null && msg.Data.Count > 0)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("[FCM] Data payload:");

            foreach (var kv in msg.Data)
            {
                sb.AppendLine("  - " + kv.Key + " = " + kv.Value);

                if (string.IsNullOrWhiteSpace(title) && kv.Key.Equals("title", StringComparison.OrdinalIgnoreCase))
                    title = kv.Value;

                if (string.IsNullOrWhiteSpace(body) &&
                    (kv.Key.Equals("body", StringComparison.OrdinalIgnoreCase) ||
                     kv.Key.Equals("message", StringComparison.OrdinalIgnoreCase)))
                    body = kv.Value;
            }

            Debug.Log(sb.ToString());
        }
        else
        {
            Debug.Log("[FCM] Data payload: empty");
        }

        if (_appInForeground && showLocalNotificationWhenAppIsOpen)
        {
            ShowForegroundLocalNotification(title, body);
        }

        Debug.Log("========== [FCM] END MESSAGE ==========");
        OnPushNotificationReceived?.Invoke();
    }

    private void ShowForegroundLocalNotification(string title, string body)
    {
#if UNITY_ANDROID
        ShowAndroidLocalNotification(title, body);
#elif UNITY_IOS
        ShowIOSLocalNotification(title, body);
#else
        Debug.Log("[FCM] Foreground local notification is not implemented on this platform.");
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

    private static string GetSenderIdFromAppId(string appId)
    {
        if (string.IsNullOrWhiteSpace(appId))
            return "(cannot parse sender id from empty app id)";

        var parts = appId.Split(':');
        if (parts.Length >= 2 && !string.IsNullOrWhiteSpace(parts[1]))
            return parts[1];

        return "(cannot parse sender id)";
    }
}