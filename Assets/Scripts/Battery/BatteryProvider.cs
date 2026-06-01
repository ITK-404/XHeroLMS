using UnityEngine;
using UnityEngine.Events;

public class BatteryProvider : MonoBehaviour
{
    private const float CHECK_INTERVAL = 1f;

#if UNITY_EDITOR
    [SerializeField] private bool isDebug = false;
    [SerializeField] private BatteryStatus debugBatteryStatus;
    [SerializeField] private float debugBatteryLevel = 0.5f;
#endif

    public float BatteryLevel { get; private set; }
    public BatteryStatus BatteryStatus { get; private set; }

    public UnityEvent<float, BatteryStatus> onBatteryChanged;

    private void Start()
    {
        InvokeRepeating(nameof(CheckBattery), 0f, CHECK_INTERVAL);
    }

    private void CheckBattery()
    {
        var batteryLevel = SystemInfo.batteryLevel;
        var batteryStatus = SystemInfo.batteryStatus;

#if UNITY_EDITOR
        if (isDebug)
        {
            batteryLevel = debugBatteryLevel;
            batteryStatus = debugBatteryStatus;
        }
#endif

        if (Mathf.Approximately(batteryLevel, BatteryLevel) && batteryStatus == BatteryStatus)
            return;

        BatteryLevel = batteryLevel;
        BatteryStatus = batteryStatus;

        Debug.Log($"BatteryProvider battery changed: {BatteryLevel}, {BatteryStatus}");
        onBatteryChanged?.Invoke(BatteryLevel, BatteryStatus);
    }
}