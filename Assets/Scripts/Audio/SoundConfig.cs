using System;
using UnityEngine;
using Random = UnityEngine.Random;
[CreateAssetMenu(fileName = "SFX_Config_",menuName ="Sound Config")]
public class SoundConfig : ScriptableObject
{
    [SerializeField] protected AudioClip audioClip;
    public AudioClip AudioClip
    {
        get => GetAudioClip();
    }

    [Range(0, 1)] public float volume = 1;
    [Range(0,1)]public float spatialBlend = 0.5f;
    [Range(-3, 3)] public float pitch = 0.5f;

    [Header("Random setting")]
    public bool isRandomPitch = false;
    [SerializeField] private float minRandomPitch = 0.5f;
    [SerializeField] private float maxRandomPitch = 1f;
    public bool loop = false;

    public virtual AudioClip GetAudioClip()
    {
        return audioClip;
    }

    public float GetRandomPitch()
    {
        return Random.Range(minRandomPitch, maxRandomPitch);
    }
}