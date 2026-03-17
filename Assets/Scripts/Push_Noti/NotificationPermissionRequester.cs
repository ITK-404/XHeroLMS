using UnityEngine;

public class NotificationPermissionRequester : MonoBehaviour
{
#if UNITY_ANDROID && !UNITY_EDITOR
    private const string PostNotificationsPermission = "android.permission.POST_NOTIFICATIONS";
#endif

    private void Start()
    {
        RequestNotificationPermissionIfNeeded();
    }

    private void RequestNotificationPermissionIfNeeded()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
            {
                int sdkInt = version.GetStatic<int>("SDK_INT");

                // Chỉ cần xin từ Android 13 (API 33) trở lên
                if (sdkInt < 33)
                    return;
            }

            if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(PostNotificationsPermission))
            {
                UnityEngine.Android.Permission.RequestUserPermission(PostNotificationsPermission);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("RequestNotificationPermissionIfNeeded error: " + e.Message);
        }
#endif
    }
}