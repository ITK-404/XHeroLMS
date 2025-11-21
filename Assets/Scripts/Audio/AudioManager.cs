using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;

    public static AudioManager Instance
    {
        get
        {
            if (instance != null) return instance;

            var prefab = Resources.Load<AudioManager>("Manager/AudioManager");
            if (prefab == null)
            {
                Debug.LogError("AudioManager prefab not found at Resources/Manager/AudioManager");
                return null;
            }

            var element = Instantiate(prefab);
            instance = element;
            DontDestroyOnLoad(element.gameObject);
            return instance;
        }
    }
    
    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;

    [Header("Audio Mixers")]
    [SerializeField] private AudioMixer mainMixer;

    [SerializeField] private AudioMixerGroup musicMixerGroup;
    [SerializeField] private AudioMixerGroup sfxMixerGroup;
    [SerializeField] private SoundBuilder soundBuilderPrefab;

    public AudioSettingsManager settings;
    protected virtual void Awake()
    {
        ObjectPoolManager.GetObjectPool(soundBuilderPrefab, 15);
        settings = GetComponent<AudioSettingsManager>();
    }

    public void PlayMusic(SoundConfig soundConfig)
    {
        if (soundConfig == null)
        {
            Debug.Log("Sound Config is null music");
            StopMusic(0.2f);
            return;
        }
       
        if (musicSource.isPlaying)
        {
            StopMusic(0.2f, () =>
            {
                musicSource.volume = soundConfig.volume;
                musicSource.clip = soundConfig.AudioClip;
                musicSource.Play();
            });
            return;
        }

        musicSource.volume = soundConfig.volume;
        musicSource.clip = soundConfig.AudioClip;
        musicSource.Play();
    }

    public void PlayMusic(AudioClip bgmClip)
    {
        musicSource.clip = bgmClip;
        musicSource.Play();
    }

    public void StopMusic(float duration, Action callback = null)
    {
        musicSource.DOFade(0, duration).SetUpdate(true).OnComplete(() =>
        {
            musicSource.DOKill(); 
            musicSource.Stop();
            callback?.Invoke();
        });
    }

    public void Resume()
    {
        settings.SetMusicVolume(1);
    }

    public void Pause()
    {
        settings.SetMusicVolume(0);
    }

    public SoundBuilder CreateSound(SoundConfig soundConfig, bool timeAffect = true)
    {
        if (!soundConfig) return null;
        //Debug.Log("Create Sound Builder with: " + soundConfig.name);
        var soundBuilder = ObjectPoolManager.GetObject<SoundBuilder>(soundBuilderPrefab);
        soundBuilder.Init(soundConfig);
        soundBuilder.SetAudioMixer(sfxMixerGroup);
        return soundBuilder;
    }

    public SoundBuilder CreateSound(AudioClip clip)
    {
        var soundBuilder = ObjectPoolManager.GetObject<SoundBuilder>(soundBuilderPrefab);
        soundBuilder.Init(clip);
        soundBuilder.SetAudioMixer(sfxMixerGroup);
        return soundBuilder;
    }
}