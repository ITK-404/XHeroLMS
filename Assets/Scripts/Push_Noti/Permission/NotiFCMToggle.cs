using UnityEngine;

public class NotiFCMToggle : MonoBehaviour
{
    [SerializeField] private ToggleSwitch toggleSwitch;

    private INotificationPermissionService permissionService;

    private void Awake()
    {
        permissionService = NotificationPermissionFactory.Create();

        toggleSwitch.onToggleOn.AddListener(TryGetPermission);
        toggleSwitch.onToggleOff.AddListener(DisablePermission);

        RefreshUI();
    }

    private void OnDestroy()
    {
        toggleSwitch.onToggleOn.RemoveListener(TryGetPermission);
        toggleSwitch.onToggleOff.RemoveListener(DisablePermission);
    }

    private void RefreshUI()
    {
        bool hasPermission = permissionService?.HasPermission() ?? false;

        toggleSwitch.ForceSetState(hasPermission);
        toggleSwitch.AnimateSlider();
    }

    private void TryGetPermission()
    {
        // if (permissionService == null)
        //     return;
        //
        // if (permissionService.HasPermission())
        //     return;
        //
        // permissionService.RequestPermission();
        OpenAppSettings();

        RefreshUI();
    }

    private void DisablePermission()
    {
        // Không thể revoke permission bằng code.
        // Chỉ có thể dẫn user tới Settings.

        OpenAppSettings();

        RefreshUI();
    }

    private void OpenAppSettings()
    {
        Application.OpenURL("app-settings:");
    }
}