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

    public float extraEndDelay = 2f;

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
            yield break;

        isPlaying = true;

        // reset trước khi play (KHÔNG đụng isPlaying)
        ResetForNewPlay();

        // ---- Prepare + Play video ----
        if (videoPlayer != null)
        {
            videoPlayer.time = 0;
            videoPlayer.Prepare();
            while (!videoPlayer.isPrepared)
                yield return null;

            videoPlayer.Play();
        }

        // ---- Play extra audio (nếu có) ----
        if (audioSource != null)
        {
            audioSource.time = 0;
            audioSource.loop = false;
            audioSource.Play();
        }

        // ---- Text + Course audio (AutomaticTextPreview) ----
        if (automaticTextPreview != null)
            automaticTextPreview.PlayTextAndSpeak(fullText);

        // ---- Nếu có stopTime: chỉ PAUSE video tại mốc, KHÔNG stop mô tả ----
        if (stopTime > 0f && videoPlayer != null)
        {
            while (isPlaying)
            {
                if (videoPlayer.time >= stopTime)
                {
                    if (videoPlayer.isPlaying)
                        videoPlayer.Pause(); // chỉ pause video
                    break;
                }
                yield return null;
            }
        }

        // ---- Chờ mô tả chạy xong + audio mô tả chạy xong ----
        if (automaticTextPreview != null)
        {
            // chờ spawn text xong (AutomaticTextPreview đặt isShowTextDone = true ở cuối coroutine)
            yield return new WaitUntil(() => !isPlaying || !automaticTextPreview.IsPlaying());

            // chờ audio mô tả xong (nếu có clip)
            yield return new WaitUntil(() =>
                !isPlaying ||
                automaticTextPreview.audioSource == null ||
                !automaticTextPreview.audioSource.isPlaying
            );
        }

        // ---- Đợi thêm +2s tránh cắt đột ngột ----
        if (isPlaying && extraEndDelay > 0f)
            yield return new WaitForSecondsRealtime(extraEndDelay);

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
