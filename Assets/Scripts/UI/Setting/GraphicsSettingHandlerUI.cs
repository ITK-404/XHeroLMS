using System;
using UnityEngine;

public class GraphicsSettingHandlerUI : MonoBehaviour
{
    [SerializeField] private ToggleSwitchGroupManager toggleManager;
    // dùng để chặn logic xảy ra khi update ui
    [SerializeField] private bool isEnable = false;

    [SerializeField] private UIView parent;

    private void Start()
    {
        if (parent)
        {
            parent.OnViewOpened += OnViewOpened;
            parent.OnViewClosed += OnViewClosed;
        }

        GraphicsSettingsManager.Instance.OnSettingIndexChanged += UpdateVisual;
        UpdateVisual();
    }

    private void OnDestroy()
    {
        if (parent != null)
        {
            parent.OnViewOpened -= OnViewOpened;
            parent.OnViewClosed -= OnViewClosed;
        }
        GraphicsSettingsManager.Instance.OnSettingIndexChanged -= UpdateVisual;

    }

    private void OnViewOpened()
    {
        UpdateVisual();
        isEnable = true;
    }
    
    private void OnViewClosed()
    {
        isEnable = false;
    }
    
    private void OnValidate()
    {
        if (toggleManager == null)
        {
            toggleManager = GetComponent<ToggleSwitchGroupManager>();
        }

        if (parent == null)
        {
            parent = GetComponentInParent<UIView>();
        }
    }
    
    public void OnSelectToggle(int index)
    {
        // this treat like user input ?
        // if (isEnable == false) return;
        Debug.Log($"GraphicsSettingHandlerUI: On Toggle Setting active by user");
        GraphicsSettingsManager.Instance.ApplyPresetIndex(index);

        DisableBatteryCheck(index);
        // GameInitializer.Instance.BatteryWarningHandler.DisableByUser();
    }

    private void DisableBatteryCheck(int checkIndex)
    {
        var batteryWarn = GameInitializer.Instance.BatteryWarningHandler; 
        if (batteryWarn.IsBatteryLowEnough() && checkIndex > 0)
        { 
            batteryWarn.DisableByUser();
        }
    }
    
    
    private void Toggle(int index)
    {
        var toggleSwitches = toggleManager.ToggleSwitches;
        toggleManager.ToggleGroup(toggleSwitches[index]); 
    }

    private void UpdateVisual()
    {
        var index = GraphicsSettingsManager.Instance.GetActiveIndex();
        var toggleSwitches = toggleManager.ToggleSwitches;
        // toggleManager.ToggleGroup(toggleSwitches[index]); 

        Debug.Log($"GraphicsSettingHandlerUI Update Visual with active index {index}");

        Toggle(index);
    }
    
}