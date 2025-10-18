using UnityEngine;

public class ToggleMiniMap : ToggleBase
{
    [SerializeField] private GameObject minimapContainer;
    [SerializeField] private bool defaultValue = true;

    protected override void Awake()
    {
        base.Awake();
        toggle.SetIsOnWithoutNotify(defaultValue);
        minimapContainer.SetActive(defaultValue);
    }
    protected override void OnValueChanged(bool value)
    {
        base.OnValueChanged(value);
        minimapContainer.SetActive(value);
        Debug.Log("Set change value: " + value);
    }
}
