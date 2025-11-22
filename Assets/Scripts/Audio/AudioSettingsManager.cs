using UnityEngine;
using UnityEngine.Audio;

public class AudioSettingsManager : MonoBehaviour
{

    [Header("Audio Mixer")]
    public AudioMixer audioMixer;

    public const string MusicKey = "MusicVolume";
    public const string SFXKey = "SFXVolume";
    
    // Giá trị volume (0–1)

    private void Awake()
    {
        LoadSettings();
    }

    public float MusicVolume { get; private set; }
    public float SFXVolume { get; private set; }

    public void SetMusicVolume(float value)
    {
        MusicVolume = Mathf.Clamp01(value);
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(MusicVolume <= 0 ? 0.0001f : MusicVolume) * 20);
    }

    public void SetSFXVolume(float value)
    {
        SFXVolume = Mathf.Clamp01(value);
        audioMixer.SetFloat("SFXVolume", Mathf.Log10(SFXVolume <= 0 ? 0.0001f : SFXVolume) * 20);
    }

    public void LoadSettings()
    {
        MusicVolume = PlayerPrefs.GetFloat(MusicKey, 0.75f);
        SFXVolume = PlayerPrefs.GetFloat(SFXKey, 0.75f);

        SetMusicVolume(MusicVolume);
        SetSFXVolume(SFXVolume);
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetFloat(MusicKey, MusicVolume);
        PlayerPrefs.SetFloat(SFXKey, SFXVolume);
        PlayerPrefs.Save();
    }

    private void OnApplicationQuit()
    {
        SaveSettings();
    }
}