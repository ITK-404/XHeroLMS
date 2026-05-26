using System;
using UnityEngine;

public class ToggleSwitchAudioHandle : MonoBehaviour
{
    [SerializeField] private ToggleSwitch toggleSwitch;

    private void OnValidate()
    {
        if (toggleSwitch == null)
        {
            toggleSwitch = GetComponent<ToggleSwitch>();
        }
    }

    private void Awake()
    {
        if (toggleSwitch != null)
        {
            toggleSwitch.onToggleOn.AddListener(OnToggleOn);
            toggleSwitch.onToggleOff.AddListener(OnToggleOff);
        }
    }

    private void OnDestroy()
    {
        if (toggleSwitch != null)
        {
            toggleSwitch.onToggleOn.RemoveListener(OnToggleOn);
            toggleSwitch.onToggleOff.RemoveListener(OnToggleOff);
        }
    }

    private void OnEnable()
    {
        UpdateToggleVisual();
    }

    private void OnToggleOn()
    {
        AudioManager.Instance.Resume();
        UpdateToggleVisual();
    }
    
    private void OnToggleOff()
    {
        AudioManager.Instance.Pause();
        UpdateToggleVisual();
    }

    private void UpdateToggleVisual()
    {
        if (!AudioManager.Instance)
        {
            return;
        }
        bool isOn = AudioManager.Instance.settings.MusicVolume >= 0.5f;
        toggleSwitch.ToggleByGroupManager(isOn);
    }
}