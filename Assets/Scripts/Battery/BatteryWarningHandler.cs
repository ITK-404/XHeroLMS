using System;
using UnityEngine;
using UnityEngine.Events;

public class BatteryWarningHandler : MonoBehaviour
{
    private const string SAVE_BATTERY_WARNING_KEY = "save_battery_warning_enabled";
    private const float WARNING_THRESHOLD = 0.2f;

    [SerializeField] private BatteryProvider batteryProvider;
    [SerializeField] private bool enabledByDefault = false;

    private bool isWarned = false;

    public bool IsEnabled { get; private set; }
    public UnityEvent onBatteryLow;
    public Action<bool> OnEnableChanged;

    private void Awake() => Load();

    private void Start()
    {
        batteryProvider.onBatteryChanged.AddListener(OnBatteryChanged);
    }

    private void OnDestroy()
    {
        batteryProvider.onBatteryChanged.RemoveListener(OnBatteryChanged);
    }

    public void SetEnabled(bool value)
    {
        IsEnabled = value;
        OnEnableChanged?.Invoke(IsEnabled);
        Save();
        
        Debug.Log($"BatteryWarningHandler enable: "+value);
    }

    private float tempBatteryLevel;
    
    private void OnBatteryChanged(float batteryLevel, BatteryStatus batteryStatus)
    {
        if (!IsEnabled) return;

        if (batteryLevel < WARNING_THRESHOLD && batteryStatus != BatteryStatus.Charging)
        {
            if (isWarned) return;
            isWarned = true;

            Debug.Log("BatteryWarningHandler battery is low");
            onBatteryLow?.Invoke();
        }
        else
        {
            isWarned = false;
        }
    }

    public void DisableByUser()
    {
        if (IsBatteryLowEnough())
        {
            Debug.Log($"BatteryWarningHandler disable by user");
            isWarned = false;
            SetEnabled(false);
        }
    }

    public bool IsBatteryLowEnough()
    {
        var batteryLevel = batteryProvider.BatteryLevel;
        var batteryStatus = batteryProvider.BatteryStatus;

        return batteryLevel < WARNING_THRESHOLD && batteryStatus != BatteryStatus.Charging;
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