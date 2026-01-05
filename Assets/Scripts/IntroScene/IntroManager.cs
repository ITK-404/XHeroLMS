using UnityEngine;
using UnityEngine.Video;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class IntroManager : MonoBehaviour
{
    [Header("Intro Config")]
    public VideoPlayer videoPlayer;
    public string nextSceneName = "New Scene";

    [Header("UI References")]
    public Image progressRing;     
    public Slider sliderUI;        
    public TMP_Text textLoading;   

    [Header("Loading Text Animation")]
    public float dotSpeed = 0.5f;
    public string baseText = "Đang tải";

    [Header("Progress Behavior")]
    public float headroom = 0.02f;         // giảm headroom, vì giờ đã sync theo video
    public float visualLerpSpeed = 8f;

    // Nếu cần đảm bảo intro hiện tối thiểu (giây). Để 0 nếu không cần
    public float minIntroSeconds = 0f;

    [Header("Intro Video Safety")]
    public float videoStartTimeout = 5f;
    public string streamingAssetsVideoName = "myintro.mp4";

    [Header("Activation Behavior")]
    public bool activateImmediatelyWhenReady = true;
    public float finishTo100Duration = 0.25f;

    private AsyncOperation preloadSceneOp;

    private bool videoStarted = false;
    private bool videoEnded = false;
    private bool introDoneRequested = false;
    private float introStartTime;

    private float dotTimer;
    private int dotCount;
    private float currentVisual;   // 0..1 hiển thị
    private float targetVisual;    // 0..1 mục tiêu hiển thị

    private bool activationRoutineStarted = false;

    private void Awake()
    {
        SetProgressInstant(0f);
    }

    private void Start()
    {
        introStartTime = Time.unscaledTime;

        BeginPreloadNextScene();

        if (videoPlayer == null)
        {
            Debug.LogError("VideoPlayer chưa được gán! Skip intro.");
            RequestFinishIntroAndActivate();
            return;
        }

        StartCoroutine(CoPrepareAndPlayVideo());
        StartCoroutine(CoVideoStartTimeoutCheck());
        StartCoroutine(CoUpdateProgressUI());
    }

    private void BeginPreloadNextScene()
    {
        if (preloadSceneOp != null) return;

        preloadSceneOp = SceneManager.LoadSceneAsync(nextSceneName);
        preloadSceneOp.allowSceneActivation = false;
    }

    private IEnumerator CoPrepareAndPlayVideo()
    {
        string videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, streamingAssetsVideoName);
        string videoUrl = "file://" + videoPath;

        if (!System.IO.File.Exists(videoPath) && Application.platform != RuntimePlatform.Android)
        {
            Debug.LogError($"Không tìm thấy video ở: {videoPath}. Skip intro.");
            RequestFinishIntroAndActivate();
            yield break;
        }

        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = videoUrl;

        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.loopPointReached += OnVideoEnd;

        videoPlayer.Prepare();
        yield return null;
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        videoPlayer.Play();
        videoStarted = true;
    }

    private void OnVideoEnd(VideoPlayer vp)
    {
        videoEnded = true;
        RequestFinishIntroAndActivate();
    }

    private IEnumerator CoVideoStartTimeoutCheck()
    {
        float timer = 0f;
        while (timer < videoStartTimeout && !videoStarted)
        {
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!videoStarted)
        {
            Debug.LogWarning("Video không chạy trong thời gian cho phép -> Skip intro.");
            videoEnded = true; // coi như xong để progress không bị kẹt do video=0
            RequestFinishIntroAndActivate();
        }
    }

    private void RequestFinishIntroAndActivate()
    {
        introDoneRequested = true;

        if (!activationRoutineStarted)
        {
            activationRoutineStarted = true;
            StartCoroutine(CoActivateWhenReady());
        }
    }

    private IEnumerator CoActivateWhenReady()
    {
        if (minIntroSeconds > 0f)
        {
            float elapsed = Time.unscaledTime - introStartTime;
            float remain = Mathf.Max(0f, minIntroSeconds - elapsed);
            if (remain > 0f) yield return new WaitForSecondsRealtime(remain);
        }

        // Đợi cả 2 điều kiện:
        // - video xong (hoặc bị skip)
        // - preload đạt 0.9 (ready activate)
        while (!IsVideoDone())
            yield return null;

        while (preloadSceneOp != null && preloadSceneOp.progress < 0.9f)
            yield return null;

        // Lúc này chắc chắn “không chờ lại” nữa: đủ cả video + scene
        if (activateImmediatelyWhenReady)
        {
            SetProgressInstant(1f);
            yield return null;

            if (preloadSceneOp != null) preloadSceneOp.allowSceneActivation = true;
            else SceneManager.LoadScene(nextSceneName);
            yield break;
        }

        yield return StartCoroutine(CoFinishTo100(finishTo100Duration));

        if (preloadSceneOp != null) preloadSceneOp.allowSceneActivation = true;
        else SceneManager.LoadScene(nextSceneName);
    }

    private IEnumerator CoUpdateProgressUI()
    {
        while (true)
        {
            UpdateDots();
            UpdateProgressSyncedVideoAndScene();
            yield return null;
        }
    }

    private void UpdateProgressSyncedVideoAndScene()
    {
        float scene01 = GetSceneProgress01(); // 0..1
        float video01 = GetVideoProgress01(); // 0..1

        float combined = Mathf.Min(scene01, video01);

        // headroom nhẹ để không bị “đơ” do float
        combined = Mathf.Clamp01(combined + headroom);

        targetVisual = combined;

        // mượt hóa hiển thị
        currentVisual = Mathf.Lerp(
            currentVisual,
            targetVisual,
            1f - Mathf.Exp(-visualLerpSpeed * Time.unscaledDeltaTime)
        );

        // Nếu video đã xong mà scene chưa xong, tránh “kẹt 99%” do rounding:
        // - không ép 0.99 nữa
        // - cứ để nó chạy theo scene01 thật
        SetProgressInstant(currentVisual);
    }

    private float GetSceneProgress01()
    {
        if (preloadSceneOp == null) return 0f;
        return Mathf.Clamp01(preloadSceneOp.progress / 0.9f); // 0..1
    }

    private float GetVideoProgress01()
    {
        // Nếu video đã xong hoặc bị skip -> 1
        if (IsVideoDone()) return 1f;

        if (videoPlayer == null) return 0f;
        if (!videoPlayer.isPrepared) return 0f;

        double len = videoPlayer.length; // seconds
        if (len <= 0.0001) return 0f;

        double t = videoPlayer.time; // seconds
        float v01 = (float)(t / len);
        return Mathf.Clamp01(v01);
    }

    private bool IsVideoDone()
    {
        // videoEnded được set khi loopPointReached hoặc timeout skip
        return videoEnded;
    }

    private void UpdateDots()
    {
        dotTimer += Time.unscaledDeltaTime;
        if (dotTimer >= dotSpeed)
        {
            dotTimer = 0f;
            dotCount = (dotCount + 1) % 4;
            SetProgressInstant(currentVisual);
        }
    }

    private IEnumerator CoFinishTo100(float dur)
    {
        float start = currentVisual;
        dur = Mathf.Max(0.01f, dur);

        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float s = Mathf.Clamp01(t / dur);
            float v = Mathf.Lerp(start, 1f, s);
            currentVisual = v;
            SetProgressInstant(v);
            yield return null;
        }

        SetProgressInstant(1f);
    }

    private void SetProgressInstant(float t01)
    {
        t01 = Mathf.Clamp01(t01);

        if (textLoading != null)
        {
            int percent = Mathf.FloorToInt(t01 * 100f); // floor để khỏi nhảy 100 sớm
            textLoading.text = $"{baseText} {percent}%{new string('.', dotCount)}";
        }

        if (progressRing != null) progressRing.fillAmount = t01;
        if (sliderUI != null) sliderUI.value = t01;
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.loopPointReached -= OnVideoEnd;
        }
    }
}
