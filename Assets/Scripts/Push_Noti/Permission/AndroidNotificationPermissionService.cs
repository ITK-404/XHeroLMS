using UnityEngine;

#if UNITY_ANDROID
using UnityEngine.Android;

public class AndroidNotificationPermissionService : INotificationPermissionService
{
    private const string PostNotificationsPermission =
        "android.permission.POST_NOTIFICATIONS";

    public bool HasPermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Android 13+ mới cần quyền POST_NOTIFICATIONS
        if (GetAndroidSdkInt() < 33)
            return true;

        return UnityEngine.Android.Permission.HasUserAuthorizedPermission(PostNotificationsPermission);
#else
        return true;
#endif
    }

    public void RequestPermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (GetAndroidSdkInt() < 33)
            return;

        if (!HasPermission())
        {
            UnityEngine.Android.Permission.RequestUserPermission(PostNotificationsPermission);
        }
#endif
    }

    private int GetAndroidSdkInt()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                return version.GetStatic<int>("SDK_INT");
            }
        }
        catch
        {
            return 0;
        }
#else
        return 0;
#endif
    }
}
#endif