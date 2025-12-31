using System.Collections;
using UnityEngine;
using UnityEngine.Video;

public class PlayVideoOpenBook : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public AudioSource audioSource;

    [Header("Text + TTS")]
    public AutomaticTextPreview automaticTextPreview;

    private bool isPlaying = false;

    [Header("Config")]
    public float stopTime = 28f;

    private void Awake()
    {
        string url = "file://" + Application.streamingAssetsPath + "/" + "SACH LAT V2_nosound.mp4";
#if !UNITY_EDITOR && UNITY_ANDROID
        url = Application.streamingAssetsPath + "/" + "SACH LAT V2_nosound.mp4";
#endif
        if (videoPlayer != null)
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = url;
        }
    }

    public IEnumerator PlayCoroutine(string fullText)
    {
        isPlaying = true;

        // reset sạch trước khi play
        Stop();

        if (videoPlayer != null) videoPlayer.time = 0;
        if (audioSource != null) audioSource.time = 0;

        if (videoPlayer != null)
        {
            videoPlayer.Prepare();
            while (!videoPlayer.isPrepared)
                yield return null;

            videoPlayer.Play();
        }

        if (audioSource != null)
            audioSource.Play();

        if (automaticTextPreview != null)
            automaticTextPreview.PlayTextAndSpeak(fullText);

        while (true)
        {
            bool reached = (videoPlayer != null) && (videoPlayer.time >= stopTime);

            if (reached && videoPlayer.isPlaying)
                videoPlayer.Pause();

            if (reached) break;
            yield return null;
        }

        // DONE -> reset sạch sau khi chạy xong
        Stop();
        isPlaying = false;
    }

    public void Stop()
    {
        if (videoPlayer != null) videoPlayer.Stop();
        if (audioSource != null) audioSource.Stop();

        if (automaticTextPreview != null)
        {
            automaticTextPreview.ResetRuntimeState(stopTTS: true);
        }

        isPlaying = false;
    }

    public bool IsPlayingVideo() => isPlaying;
}
