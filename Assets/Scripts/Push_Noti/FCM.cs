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

            // Mặc định token registration on init là bật, nhưng set lại cũng không sao
            FirebaseMessaging.TokenRegistrationOnInitEnabled = true;

            // Xin quyền thông báo
            if (permissionRequester == null)
                permissionRequester = GetComponent<NotificationPermissionRequester>();

            if (permissionRequester != null)
                permissionRequester.RequestPermissionIfNeeded();
            else
                Debug.LogWarning("[FCM] NotificationPermissionRequester is missing.");

            // Lấy token chủ động để chắc chắn có log/token gửi server
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
                Debug.Log("[FCM] FCM Token: " + token);
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
        Debug.Log("[FCM] Received Registration Token: " + token.Token);

        // TODO: gửi token mới lên server nếu token refresh
    }

    private void OnMessageReceived(object sender, MessageReceivedEventArgs e)
    {
        Debug.Log("[FCM] Received a new message from: " + e.Message.From);

        if (e.Message.Notification != null)
        {
            Debug.Log("[FCM] Notification Title: " + e.Message.Notification.Title);
            Debug.Log("[FCM] Notification Body: " + e.Message.Notification.Body);
        }

        if (e.Message.Data != null && e.Message.Data.Count > 0)
        {
            foreach (var pair in e.Message.Data)
            {
                Debug.Log($"[FCM] Data: {pair.Key} = {pair.Value}");
            }
        }

        // TODO:
        // - nếu app đang foreground: hiện popup/in-app UI của bạn
        // - nếu muốn điều hướng theo payload: xử lý tại đây
    }
}