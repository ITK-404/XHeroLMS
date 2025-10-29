using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class PlayVideoOpenBook : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public AudioSource audioSource;

    public IEnumerator PlayCoroutine()
    {
        // dừng phát nếu đang phát
        videoPlayer.Stop();
        audioSource.Stop();

        // tua cả hai về 0
        videoPlayer.time = 0;
        audioSource.time = 0;
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared)
            yield return null;

        videoPlayer.Play();
        audioSource.Play();
        
        while (videoPlayer.isPlaying)
            yield return null;
    }
    [ContextMenu("Play Test")]
    public void PlayTest()
    {
        StartCoroutine(PlayCoroutine());
    }

    public void Stop()
    {
        videoPlayer.Stop();
        audioSource.Stop();
    }

    public void Show()
    {
        
    }
    
}