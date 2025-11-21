using UnityEngine;
[CreateAssetMenu(fileName = "SFX_Config_Multi_", menuName = "Multiply Sound Config")]
public class MultiplySoundConfig : SoundConfig
{
    [SerializeField] private SoundConfig[] soundConfigs;

    public override AudioClip GetAudioClip()
    {
        return GetRandomSound();
    }
    private AudioClip GetRandomSound()
    {
        return soundConfigs[Random.Range(0, soundConfigs.Length)].AudioClip;
    }

}