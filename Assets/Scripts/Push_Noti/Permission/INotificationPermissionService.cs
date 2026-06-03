public interface INotificationPermissionService
{
    bool HasPermission();
    void RequestPermission();
}