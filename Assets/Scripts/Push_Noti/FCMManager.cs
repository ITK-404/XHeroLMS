using System;
using UnityEngine;
using Firebase;
using Firebase.Extensions;
using Firebase.Messaging;

public class FCMManager : MonoBehaviour
{
    [SerializeField] private NotificationPermissionRequester permissionRequester;

    private bool _initialized;
    private bool _eventsRegistered;

    public static event Action OnPushNotificationReceived;
    public static event Action OnAppResumed;

    public const string PlayerPrefsFcmTokenKey = "FCM_DEVICE_TOKEN";

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        InitializeFirebase();
    }

    private void OnDestroy()
    {
        UnregisterFirebaseEvents();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) return;
        Debug.Log("[FCM] App resumed.");

        OnAppResumed?.Invoke();
    }

    private void InitializeFirebase()
    {
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
            if (dependencyStatus != DependencyStatus.Available)
            {
                Debug.LogError("[FCM] Could not resolve all Firebase dependencies: " + dependencyStatus);
                return;
            }

            FirebaseApp app = FirebaseApp.DefaultInstance;
            _initialized = true;

            Debug.Log("[FCM] Firebase initialized: " + app.Name);

            RegisterFirebaseEvents();

            FirebaseMessaging.TokenRegistrationOnInitEnabled = true;

            if (permissionRequester == null)
                permissionRequester = GetComponent<NotificationPermissionRequester>();

            if (permissionRequester != null)
                permissionRequester.RequestPermissionIfNeeded();
            else
                Debug.LogWarning("[FCM] NotificationPermissionRequester is missing.");

            FirebaseMessaging.GetTokenAsync().ContinueWithOnMainThread(tokenTask =>
            {
                if (tokenTask.IsFaulted)
                {
                    Debug.LogError("[FCM] GetTokenAsync faulted: " + tokenTask.Exception);
                    return;
                }

                if (tokenTask.IsCanceled)
                {
                    Debug.LogWarning("[FCM] GetTokenAsync canceled.");
                    return;
                }

                string token = tokenTask.Result;
                SaveFcmToken(token, "GetTokenAsync");
            });
        });
    }

    private void RegisterFirebaseEvents()
    {
        if (_eventsRegistered) return;

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
    }

    private void OnTokenReceived(object sender, TokenReceivedEventArgs token)
    {
        SaveFcmToken(token != null ? token.Token : string.Empty, "TokenReceived");
    }

    private void SaveFcmToken(string token, string source)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            Debug.LogWarning("[FCM] " + source + " token is null/empty");
            return;
        }

        PlayerPrefs.SetString(PlayerPrefsFcmTokenKey, token);
        PlayerPrefs.Save();

        Debug.Log("[FCM] " + source + " token saved: " + token);
    }

    public static string GetSavedFcmToken()
    {
        return PlayerPrefs.GetString(PlayerPrefsFcmTokenKey, "");
    }

    private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        if (e == null || e.Message == null)
        {
            Debug.LogWarning("[FCM] MessageReceived args/message is null.");
            return;
        }

        OnPushNotificationReceived?.Invoke();
    }
}