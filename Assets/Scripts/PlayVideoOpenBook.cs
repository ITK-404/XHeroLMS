using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class PlayVideoOpenBook : MonoBehaviour
{
    [Header("Video")]
    public VideoPlayer videoPlayer;
    public AudioSource audioSource;

    [Header("Text + TTS")]
    public AutomaticTextPreview automaticTextPreview;

    [Header("Config")]
    public float stopTime = 28f;

    private bool isPlaying = false;

    private void Awake()
    {
        string url = "file://" + Application.streamingAssetsPath + "/SACH LAT V2_nosound.mp4";
#if !UNITY_EDITOR && UNITY_ANDROID
        url = Application.streamingAssetsPath + "/SACH LAT V2_nosound.mp4";
#endif
        if (videoPlayer != null)
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = url;
        }
    }

    public IEnumerator PlayCoroutine(string fullText)
    {
        if (isPlaying)
            yield break; // đang chạy thì không cho chạy lại

        isPlaying = true;

        // reset trước khi play (KHÔNG đụng isPlaying)
        ResetForNewPlay();

        // ---- Prepare video ----
        if (videoPlayer != null)
        {
            videoPlayer.time = 0;
            videoPlayer.Prepare();
            while (!videoPlayer.isPrepared)
                yield return null;

            videoPlayer.Play();
        }

        if (audioSource != null)
        {
            audioSource.time = 0;
            audioSource.Play();
        }

        // ---- Text + TTS ----
        if (automaticTextPreview != null)
            automaticTextPreview.PlayTextAndSpeak(fullText);

        // ---- Wait until reach stopTime ----
        while (true)
        {
            if (videoPlayer != null && videoPlayer.time >= stopTime)
            {
                if (videoPlayer.isPlaying)
                    videoPlayer.Pause();
                break;
            }
            yield return null;
        }

        // ---- Finish ----
        StopInternal();
    }

    public void Stop()
    {
        if (!isPlaying)
            return;

        StopInternal();
    }

    public bool IsPlayingVideo()
    {
        return isPlaying;
    }

    private void ResetForNewPlay()
    {
        if (videoPlayer != null)
            videoPlayer.Stop();

        if (audioSource != null)
            audioSource.Stop();

        if (automaticTextPreview != null)
            automaticTextPreview.ResetRuntimeState(stopAudio: true);

    }

    private void StopInternal()
    {
        if (videoPlayer != null)
            videoPlayer.Stop();

        if (audioSource != null)
            audioSource.Stop();

        if (automaticTextPreview != null)
            automaticTextPreview.ResetRuntimeState(stopAudio: true);

        isPlaying = false;
    }
}
