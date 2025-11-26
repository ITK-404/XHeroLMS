using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;

public class AudioZoneElement : MonoBehaviour
{
    private float volume = 1;
    [SerializeField] private float fadeInDuration = 0.1f;
    [SerializeField] private float fadeOutDuration = 0.1f;
    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void FadeIn()
    {
        if(audioSource == null)
        {
            Debug.Log("Audio is null");
            return;
        }
        audioSource.DOKill();
        audioSource.Play();
        audioSource.DOFade(volume, fadeInDuration);
    }

    public void FadeOut()
    {
        if (audioSource == null)
        {
            Debug.Log("Audio is null");
            return;
        }
        audioSource.DOKill();
        audioSource.DOFade(0, fadeOutDuration).OnComplete(PauseAudio);
    }

    private void PauseAudio()
    {
        audioSource.Pause();
    }
}