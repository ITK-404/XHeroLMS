using System;
using UnityEngine;

public class BatteryWarningUIHandler : MonoBehaviour
{
    [SerializeField] private BatteryWarningHandler batteryWarningHandler;
    [SerializeField] private ToggleSwitch toggle;

    private void Awake() => batteryWarningHandler = GameInitializer.Instance.BatteryWarningHandler;
    
    private void Start() => SyncToggleUI();

    private void OnEnable()
    {
        toggle.onToggleOn.AddListener(OnToggleOn);
        toggle.onToggleOff.AddListener(OnToggleOff);

        SyncToggleUI();
    }

    private void OnDisable()
    {
        toggle.onToggleOn.RemoveListener(OnToggleOn);
        toggle.onToggleOff.RemoveListener(OnToggleOff);
    }

    public void OnToggleOn() => batteryWarningHandler.SetEnabled(true);
    public void OnToggleOff() => batteryWarningHandler.SetEnabled(false);

    private void SyncToggleUI()
    {
        toggle?.ToggleByGroupManager(batteryWarningHandler.IsEnabled);
    }
}