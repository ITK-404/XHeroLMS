using UnityEngine;
using UnityEngine.UI;

public class AudioSliderController : MonoBehaviour
{
    public enum SliderType { Music, SFX }
    public SliderType sliderType;

    private Slider slider;
    private AudioSettingsManager settings;
    private void Start()
    {
        slider = GetComponent<Slider>();
        settings = AudioManager.Instance.settings;
        if (settings == null) return;

        // Gán giá trị ban đầu từ AudioSettingsManager
        if (sliderType == SliderType.Music)
            slider.value = settings.MusicVolume;
        else
            slider.value = settings.SFXVolume;

        // Lắng nghe thay đổi slider
        slider.onValueChanged.AddListener(OnSliderValueChanged);
    }

    private void OnEnable()
    {
        if (settings == null) return;

        if (sliderType == SliderType.Music)
            slider.value = settings.MusicVolume;
        else
            slider.value = settings.SFXVolume;
    }

    private void OnSliderValueChanged(float value)
    {
        if (AudioManager.Instance.settings == null) return;

        if (sliderType == SliderType.Music)
            settings.SetMusicVolume(value);
        else
            settings.SetSFXVolume(value);
    }
}