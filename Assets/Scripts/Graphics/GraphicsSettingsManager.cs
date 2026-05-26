using System;
using UnityEngine;

public class GraphicsSettingsManager : MonoBehaviour
{
    public static GraphicsSettingsManager Instance;
    [SerializeField] private GraphicsPresetSO[] presets; // 0=Low, 1=Medium, 2=High, 3=Ultra
    private int currentIndex = 1;

    // LOAD SAVE

    private const string GRAPHICS_SAVE_KEY = "graphics_preset_select";

    private void Awake()
    {
        Instance = this;

        Debug.Log($"[GraphicsSettingsManager] Loaded");

        if (presets == null || presets.Length == 0)
        {
            Debug.LogError("[GraphicsSettingsManager] presets graphics is not load right !!!!");
        }

        Load();
        Debug.Log($"[GraphicsSettingsManager] current preset index after load: "+currentIndex);
        ApplyPresetIndex(currentIndex);
    }

    private void OnDestroy()
    {
        Save();
    }

    private void Save()
    {
        PlayerPrefs.SetInt(GRAPHICS_SAVE_KEY, currentIndex);
    }

    private void Load()
    {
        GraphicsPreset preset;

        if (PlayerPrefs.HasKey(GRAPHICS_SAVE_KEY))
        {
            int saved = PlayerPrefs.GetInt(GRAPHICS_SAVE_KEY);
            preset = (GraphicsPreset)Mathf.Clamp(saved, 0, (int)GraphicsPreset.Ultra);
        }
        else
        {
            // auto detect
            preset = SystemInfo.systemMemorySize switch
            {
                <= 1024 => GraphicsPreset.Low,
                <= 2048 => GraphicsPreset.Medium,
                <= 4096 => GraphicsPreset.High,
                _       => GraphicsPreset.Ultra,
            };
        }

        currentIndex = (int)preset;
    }


    public void ApplyPresetIndex(int index)
    {
        Debug.Log($"[GraphicsSettingsManager] Apply graphics index {index}");
        currentIndex = index;
        ApplyPreset((GraphicsPreset)index);
    }

    public void ApplyPreset(GraphicsPreset preset)
    {
        var currentConfig = presets[(int)preset].config;
        GraphicsApplier.Apply(currentConfig);
    }

    public int GetActiveIndex()
    {
        return currentIndex;
    }
}
