#if UNITY_IOS
using Unity.Notifications.iOS;

public class IOSNotificationPermissionService : INotificationPermissionService
{
    public bool HasPermission()
    {
        return iOSNotificationCenter.GetNotificationSettings()
                   .AuthorizationStatus ==
               AuthorizationStatus.Authorized;
    }

    public void RequestPermission()
    {
        var request = new AuthorizationRequest(
            AuthorizationOption.Alert |
            AuthorizationOption.Badge |
            AuthorizationOption.Sound,
            true);

        // Có thể poll request.IsFinished
    }
}
#endif

public static class NotificationPermissionFactory
{
    public static INotificationPermissionService Create()
    {
#if UNITY_ANDROID
        return new AndroidNotificationPermissionService();
#elif UNITY_IOS
        return new IOSNotificationPermissionService();
#else
        return null;
#endif
    }
}