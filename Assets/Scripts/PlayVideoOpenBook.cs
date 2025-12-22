using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class PlayVideoOpenBook : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public AudioSource audioSource;
    public AutomaticTextPreview automaticTextPreview;

    private void Awake()
    {
        string url = "file://" + Application.streamingAssetsPath + "/" + "SACH LAT V2_nosound.mp4";
#if !UNITY_EDITOR && UNITY_ANDROID
             url = Application.streamingAssetsPath + "/" + "SACH LAT V2_nosound.mp4";
#endif
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = url;
    }

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
        
        // Start creating text (if assigned)
        if (automaticTextPreview != null)
        {
            automaticTextPreview.CreateText();
            automaticTextPreview.StartTimer();
        }

        // Wait until both conditions are met:
        // - the video finished playing, AND
        // - the configured timer has elapsed
        float stopTime = 28f; // Giây thứ 28

        while (true)
        {
            bool videoReachedTarget = (videoPlayer != null) && (videoPlayer.time >= stopTime);

            if (videoReachedTarget && videoPlayer.isPlaying)
            {
                videoPlayer.Pause();
            }

            bool timerCompleted = true;
            if (automaticTextPreview != null)
                timerCompleted = automaticTextPreview.IsTimerCompleted();

            if (videoReachedTarget && timerCompleted)
                break;

            yield return null;
        }

        // Stop everything
        if (videoPlayer != null)
            videoPlayer.Stop();
        if (audioSource != null)
            audioSource.Stop();
        if (automaticTextPreview != null)
            automaticTextPreview.StopText();

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
        automaticTextPreview.StopText();
    }

    public void Show()
    {
        
    }
    
}