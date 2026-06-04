using System;
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

    private void OnEnable()
    {
        RefreshUI();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        RefreshUI();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
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
        Debug.Log($"NotiFCMToggle: {hasPermission}");
        toggleSwitch.SetState(hasPermission, false);
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
#if UNITY_IOS
    Application.OpenURL("app-settings:");
#elif UNITY_ANDROID
        using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
        using (AndroidJavaObject currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
        using (AndroidJavaObject intent = new AndroidJavaObject("android.content.Intent",
                   "android.settings.APPLICATION_DETAILS_SETTINGS"))
        {
            AndroidJavaObject uri = new AndroidJavaClass("android.net.Uri")
                .CallStatic<AndroidJavaObject>("fromParts", "package", Application.identifier, null);
        
            intent.Call<AndroidJavaObject>("setData", uri);
            intent.Call<AndroidJavaObject>("addFlags", 0x10000000); // FLAG_ACTIVITY_NEW_TASK
            currentActivity.Call("startActivity", intent);
        }
#endif
    }
    
}