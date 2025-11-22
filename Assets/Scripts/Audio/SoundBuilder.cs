using DG.Tweening;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

public class SoundBuilder : PoolableObject
{
    private float volume = 1f;
    public float pitch = 1;
    private Vector3? position = null;
    private Transform target = null;
    private float spatialBlend = 0f;
    private float minDistance = 1f;
    private float maxDistance = 20f;
    private AudioRolloffMode rolloffMode = AudioRolloffMode.Linear;
    private AudioSource source;
    private float delay = 0;
    private void Awake()
    {
        source = GetComponent<AudioSource>();
    }

    public void Init(SoundConfig soundConfig)
    {
        if (soundConfig == null)
        {
            Debug.LogWarning("This config is null");
            return;
        }
        ResetAudioSource();
        source.clip = soundConfig.AudioClip;
        this.volume = soundConfig.volume;
        this.pitch = soundConfig.isRandomPitch ? soundConfig.GetRandomPitch() : soundConfig.pitch;
        this.spatialBlend = soundConfig.spatialBlend;
        this.delay = 0;
    }

    private void ResetAudioSource()
    {
        source.Stop();
        source.volume = 1;
        source.pitch = 1;

        source.spatialBlend = 0f; // 0 = 2D, 1 = 3D
        source.loop = false;
        source.playOnAwake = false;

        source.time = 0f; // đảm bảo luôn phát từ đầu
        delay = 0;
    }

    public void Init(AudioClip audioClip)
    {
        source.clip = audioClip;
    }

    public SoundBuilder SetVolume(float volume)
    {
        this.volume = volume;
        return this;
    }

    public SoundBuilder SetPitch(float pitch)
    {
        this.pitch = pitch;
        return this;
    }

    public SoundBuilder SetRandomPitch(float min,float max)
    {
        this.pitch = UnityEngine.Random.Range(min,max);
        return this;
    }

    public SoundBuilder SetPosition(Vector3 position)
    {
        this.position = position;
        this.spatialBlend = 1f;
        return this;
    }

    public SoundBuilder SetTarget(Transform target)
    {
        this.target = target;
        this.spatialBlend = 1f;
        return this;
    }

    public SoundBuilder SetSpatialBlend(float blend)
    {
        this.spatialBlend = Mathf.Clamp01(blend);
        return this;
    }

    public SoundBuilder SetRolloff(float min, float max, AudioRolloffMode mode = AudioRolloffMode.Linear)
    {
        this.minDistance = min;
        this.maxDistance = max;
        this.rolloffMode = mode;
        return this;
    }

    public AudioSource Play()
    {
        if (source.clip == null)
        {
            return null;
        }
        
        source.pitch = pitch;
        source.volume = volume;
        source.spatialBlend = spatialBlend;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.rolloffMode = rolloffMode;

        if (target != null)
        {
            source.transform.SetParent(target);
            source.transform.localPosition = Vector3.zero;
        }
        else if (position.HasValue)
        {
            source.transform.position = position.Value;
        }
        Debug.Log($"Sound Manager play {source.clip.name}");
        source.PlayDelayed(delay);
        StartCoroutine(ReturnToPool());
        return source;
    }

    private IEnumerator ReturnToPool()
    {
        yield return new WaitWhile(() => source.isPlaying);

        RecoverSelf();
    }

    public void SetAudioMixer(AudioMixerGroup audioMixerGroup)
    {
        source.outputAudioMixerGroup = audioMixerGroup;
    }

    public SoundBuilder With2DPreset()
    {
        source.spatialBlend = 0;
        return this;
    }

    public SoundBuilder With3DPreset()
    {
        source.spatialBlend = 0;
        return this; 
    }

    public SoundBuilder SetDelay(float delay)
    {
        this.delay = delay;
        return this;
    }

}