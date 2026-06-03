using UnityEngine;

public class SettingViewUI : UIView
{
    [SerializeField] private UIView settingView;
    protected override void Awake()
    {
        base.Awake();

        GameInitializer.Instance.BatteryWarningHandler.onBatteryLow.AddListener(RefreshUI);
    }

    private void OnDestroy()
    {
        GameInitializer.Instance.BatteryWarningHandler.onBatteryLow.RemoveListener(RefreshUI);
    }

    private void RefreshUI()
    {
        // reshowing ui
        settingView.Show();
    }
}