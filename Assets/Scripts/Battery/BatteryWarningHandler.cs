// BatteryWarningHandler.cs - Kiểm tra battery + event

using UnityEngine;
using UnityEngine.Events;

public class BatteryWarningHandler : MonoBehaviour
{
    private const string SAVE_BATTERY_WARNING_KEY = "save_battery_warning_enabled";
    private const float WARNING_THRESHOLD = 0.2f;
    private const float CHECK_INTERVAL = 60f;

    [SerializeField] private bool enabledByDefault = false;

    public bool IsEnabled { get; private set; }
    public UnityEvent onBatteryLow;

    private void Awake() => Load();

    private void Start()
    {
        if (IsEnabled)
            InvokeRepeating(nameof(CheckBattery), 0f, CHECK_INTERVAL);
    }

    public void SetEnabled(bool value)
    {
        IsEnabled = value;
        Save();

        if (IsEnabled)
            InvokeRepeating(nameof(CheckBattery), 0f, CHECK_INTERVAL);
        else
            CancelInvoke(nameof(CheckBattery));
    }

    private void CheckBattery()
    {
        if (SystemInfo.batteryLevel < WARNING_THRESHOLD &&
            SystemInfo.batteryStatus != BatteryStatus.Charging)
        {
            onBatteryLow?.Invoke();
        }
    }

    private void Save()
    {
        PlayerPrefs.SetInt(SAVE_BATTERY_WARNING_KEY, IsEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void Load()
    {
        IsEnabled = PlayerPrefs.GetInt(SAVE_BATTERY_WARNING_KEY, enabledByDefault ? 1 : 0) == 1;
    }
}