using System;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;

public class AndroidNotificationPermissionService : INotificationPermissionService
{
    private const string NotificationPermission =
        "android.permission.POST_NOTIFICATIONS";

    public bool HasPermission()
    {
        return Permission.HasUserAuthorizedPermission(NotificationPermission);
    }

    public void RequestPermission()
    {
        if (!HasPermission())
        {
            Permission.RequestUserPermission(NotificationPermission);
        }
    }
}
#endif