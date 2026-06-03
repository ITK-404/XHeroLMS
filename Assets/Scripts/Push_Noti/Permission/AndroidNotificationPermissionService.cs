using System;
using UnityEngine;
#if UNITY_ANDROID
using UnityEngine.Android;

public class AndroidNotificationPermissionService : INotificationPermissionService
{
    private const string Permission =
        "android.permission.POST_NOTIFICATIONS";

    public bool HasPermission()
    {
        return Permission.HasUserAuthorizedPermission(Permission);
    }

    public void RequestPermission()
    {
        if (!HasPermission())
        {
            Permission.RequestUserPermission(Permission);
        }
    }
}
#endif