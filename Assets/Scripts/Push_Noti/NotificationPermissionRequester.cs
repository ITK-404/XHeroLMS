using System;
using UnityEngine;
using Firebase.Messaging;
using Firebase.Extensions;

public class NotificationPermissionRequester : MonoBehaviour
{
#if UNITY_ANDROID && !UNITY_EDITOR
    private const string PostNotificationsPermission = "android.permission.POST_NOTIFICATIONS";
#endif

    private bool _requestedThisSession;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void RequestPermissionIfNeeded()
    {
        if (_requestedThisSession) return;
        _requestedThisSession = true;

#if UNITY_ANDROID && !UNITY_EDITOR
        RequestAndroidNotificationPermission();
#elif UNITY_IOS && !UNITY_EDITOR
        RequestIOSNotificationPermission();
#else
        Debug.Log("[NotificationPermissionRequester] Editor/unsupported platform -> skip permission request.");
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private void RequestAndroidNotificationPermission()
    {
        try
        {
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                int sdkInt = version.GetStatic<int>("SDK_INT");

                // Android 12 trở xuống không cần runtime permission cho notification
                if (sdkInt < 33)
                {
                    Debug.Log("[NotificationPermissionRequester] Android < 13, no runtime notification permission needed.");
                    return;
                }
            }

            if (UnityEngine.Android.Permission.HasUserAuthorizedPermission(PostNotificationsPermission))
            {
                Debug.Log("[NotificationPermissionRequester] Android notification permission already granted.");
                return;
            }

            Debug.Log("[NotificationPermissionRequester] Requesting Android POST_NOTIFICATIONS permission...");
            UnityEngine.Android.Permission.RequestUserPermission(PostNotificationsPermission);
        }
        catch (Exception e)
        {
            Debug.LogError("[NotificationPermissionRequester] Android permission request error: " + e);
        }
    }
#endif

#if UNITY_IOS && !UNITY_EDITOR
    private void RequestIOSNotificationPermission()
    {
        try
        {
            Debug.Log("[NotificationPermissionRequester] Requesting iOS notification permission...");

            FirebaseMessaging.RequestPermissionAsync()
                .ContinueWithOnMainThread(task =>
                {
                    if (task.IsFaulted)
                    {
                        Debug.LogError("[NotificationPermissionRequester] iOS RequestPermissionAsync faulted: " + task.Exception);
                        return;
                    }

                    if (task.IsCanceled)
                    {
                        Debug.LogWarning("[NotificationPermissionRequester] iOS RequestPermissionAsync canceled.");
                        return;
                    }

                    Debug.Log("[NotificationPermissionRequester] iOS notification permission prompt finished.");
                });
        }
        catch (Exception e)
        {
            Debug.LogError("[NotificationPermissionRequester] iOS permission request error: " + e);
        }
    }
#endif
}