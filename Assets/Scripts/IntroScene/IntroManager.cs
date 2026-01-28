using UnityEngine;
using UnityEngine.Video;
using System.Collections;
using DG.Tweening;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif

public class IntroManager : MonoBehaviour
{
    [Header("Intro Config")]
    public VideoPlayer videoPlayer;

    [Header("Next Scene")]
    [Tooltip("Nếu bật, Intro sẽ load scene bằng Addressables (scene remote / local group).")]
    public bool nextSceneIsAddressable = true;

    [Tooltip("Nếu nextSceneIsAddressable=false thì dùng tên scene trong Build Settings.")]
    public string nextBuiltInSceneName = "New Scene";

    [Tooltip("Nếu nextSceneIsAddressable=true thì dùng Addressables key (vd: Cloud_NewScene(Main)).")]
    public string nextAddressableSceneKey = "Cloud_NewScene(Main)";

    [Header("UI References")]
    public Image progressRing;
    public Slider sliderUI;
    public TMP_Text textLoading;

    [Header("Loading Text Animation")]
    public float dotSpeed = 0.5f;
    public string baseText = "Đang tải";

    [Header("Progress Behavior")]
    public float headroom = 0.02f;
    public float visualLerpSpeed = 8f;

    [Header("Intro Minimum Time")]
    public float minIntroSeconds = 0f;

    [Header("Intro Video Safety")]
    public float videoStartTimeout = 5f;
    public string streamingAssetsVideoName = "myintro.mp4";

    [Header("Activation Behavior")]
    public bool activateImmediatelyWhenReady = true;
    public float finishTo100Duration = 0.25f;

    [Header("Addressables Gate")]
    [Tooltip("Nếu Addressables chưa tải xong thì progress chỉ lên tối đa 99% và đứng chờ.")]
    public bool gateProgressByAddressables = true;

    [Header("Optional UI Root")]
    public CanvasGroup loaderCanvasGroup;

    [Header("Fail UI (Optional)")]
    [Tooltip("Nếu gán, sẽ show khi preload Addressables fail.")]
    public CanvasGroup failCanvasGroup;
    public TMP_Text failText;
    public Button retryButton;

    [Header("Failsafe (Anti-stuck)")]
    [Tooltip("Tránh kẹt vô hạn nếu preload scene không bao giờ ready. 0 = disable.")]
    public float preloadSceneMaxWaitSeconds = 60f;

    // ---------------- internal ----------------
    private AsyncOperation preloadBuiltInSceneOp;

#if ADDRESSABLES
    private AsyncOperationHandle<SceneInstance>? preloadAddrSceneHandle;
#endif

    private bool videoStarted = false;
    private bool videoEnded = false;
    private float introStartTime;

    private float dotTimer;
    private int dotCount;

    private float currentVisual;
    private float targetVisual;

    private bool activationRoutineStarted = false;

    private bool forcedFallbackToBuiltIn = false;
    private bool AddressablesEnabled =>
#if ADDRESSABLES
        true;
#else
        false;
#endif

    private void Awake()
    {
        if (loaderCanvasGroup != null) loaderCanvasGroup.DOFade(0, 0);
        if (failCanvasGroup != null)
        {
            failCanvasGroup.alpha = 0f;
            failCanvasGroup.blocksRaycasts = false;
            failCanvasGroup.interactable = false;
        }

        if (retryButton != null)
        {
            retryButton.onClick.RemoveAllListeners();
            retryButton.onClick.AddListener(OnRetryClicked);
        }

        SetProgressInstant(0f);
    }

    private void Start()
    {
        introStartTime = Time.unscaledTime;

        Debug.Log($"[Intro] nextSceneIsAddressable={nextSceneIsAddressable}, ADDRESSABLES_DEFINE={(AddressablesEnabled ? "ON" : "OFF")}");

        // Anti-stuck: Nếu user bật Addressables nhưng project chưa enable -> fallback built-in
        if (nextSceneIsAddressable && !AddressablesEnabled)
        {
            Debug.LogWarning("[Intro] nextSceneIsAddressable=true nhưng ADDRESSABLES define OFF / thiếu package -> fallback built-in scene để tránh kẹt.");
            nextSceneIsAddressable = false;
            forcedFallbackToBuiltIn = true;
        }

        BeginPreloadNextScene();

        if (videoPlayer == null)
        {
            Debug.LogError("[Intro] VideoPlayer chưa được gán! Skip intro.");
            videoEnded = true;
            RequestFinishIntroAndActivate();
        }
        else
        {
            videoPlayer.prepareCompleted += OnFadeLoader;
            StartCoroutine(CoPrepareAndPlayVideo());
            StartCoroutine(CoVideoStartTimeoutCheck());
        }

        StartCoroutine(CoUpdateProgressUI());
    }

    private void OnFadeLoader(VideoPlayer source)
    {
        if (loaderCanvasGroup != null) loaderCanvasGroup.DOFade(1, 0.1f);
    }

private void BeginPreloadNextScene()
{
    if (!nextSceneIsAddressable)
    {
        if (preloadBuiltInSceneOp != null) return;

        Debug.Log($"[Intro] Preload built-in scene: {nextBuiltInSceneName}");
        preloadBuiltInSceneOp = SceneManager.LoadSceneAsync(nextBuiltInSceneName);
        preloadBuiltInSceneOp.allowSceneActivation = false;
        return;
    }

    // Addressables scene: KHÔNG load scene ở đây nữa.
    // Đợi AddressablesPreload (download label) xong rồi mới LoadSceneAsync.
#if ADDRESSABLES
    preloadAddrSceneHandle = null;
#endif
}

    private IEnumerator CoPrepareAndPlayVideo()
    {
        string videoPath = System.IO.Path.Combine(Application.streamingAssetsPath, streamingAssetsVideoName);

        if (Application.platform == RuntimePlatform.Android)
        {
            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = System.IO.Path.Combine(Application.streamingAssetsPath, streamingAssetsVideoName);
        }
        else
        {
            if (!System.IO.File.Exists(videoPath))
            {
                Debug.LogError($"[Intro] Không tìm thấy video ở: {videoPath}. Skip intro.");
                videoEnded = true;
                RequestFinishIntroAndActivate();
                yield break;
            }

            videoPlayer.source = VideoSource.Url;
            videoPlayer.url = "file://" + videoPath;
        }

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
            Debug.LogWarning("[Intro] Video không chạy trong thời gian cho phép -> Skip intro.");
            videoEnded = true;
            RequestFinishIntroAndActivate();
        }
    }

    private void RequestFinishIntroAndActivate()
    {
        if (!activationRoutineStarted)
        {
            activationRoutineStarted = true;
            StartCoroutine(CoActivateWhenReady());
        }
    }

    private IEnumerator CoActivateWhenReady()
    {
        // minimum intro time
        if (minIntroSeconds > 0f)
        {
            float elapsed = Time.unscaledTime - introStartTime;
            float remain = Mathf.Max(0f, minIntroSeconds - elapsed);
            if (remain > 0f) yield return new WaitForSecondsRealtime(remain);
        }

        // wait video done (or skip)
        while (!IsVideoDone())
            yield return null;

        // wait preload scene ready (anti-stuck with timeout)
        float wait = 0f;
        while (!IsNextScenePreloadReady())
        {
            wait += Time.unscaledDeltaTime;

            if (preloadSceneMaxWaitSeconds > 0f && wait >= preloadSceneMaxWaitSeconds)
            {
                // fallback emergency
                Debug.LogError("[Intro] Preload scene wait timeout -> fallback direct load.");
                break;
            }

            yield return null;
        }

        // gate Addressables preload (nếu có)
        if (gateProgressByAddressables)
        {
            var a = AddressablesPreload.Instance;
            if (a != null)
            {
                while (!a.IsReady && !a.HasFailed)
                    yield return null;

                if (a.HasFailed)
                {
                    ShowFail(a.LastError);
                    yield break;
                }
            }
        }

        // OK: 100% + activate
        if (activateImmediatelyWhenReady)
        {
            SetProgressInstant(1f);
            yield return null;
            ActivateNextScene();
            yield break;
        }

        yield return StartCoroutine(CoFinishTo100(finishTo100Duration));
        ActivateNextScene();
    }

    private IEnumerator CoUpdateProgressUI()
    {
        while (true)
        {
            UpdateDots();
            UpdateProgressSynced();
            yield return null;
        }
    }

    private void UpdateProgressSynced()
    {
        float scene01 = GetNextSceneProgress01();
        float video01 = GetVideoProgress01();
        float addr01 = GetAddressablesProgress01();

        float combined = Mathf.Min(scene01, video01, addr01);
        combined = Mathf.Clamp01(combined + headroom);

        // gate 99% nếu Addressables chưa done
        if (gateProgressByAddressables && !IsAddressablesDone())
            combined = Mathf.Min(combined, 0.99f);

        targetVisual = combined;

        currentVisual = Mathf.Lerp(
            currentVisual,
            targetVisual,
            1f - Mathf.Exp(-visualLerpSpeed * Time.unscaledDeltaTime)
        );

        SetProgressInstant(currentVisual);
    }

    private float GetNextSceneProgress01()
    {
        if (!nextSceneIsAddressable)
        {
            if (preloadBuiltInSceneOp == null) return 0f;
            return Mathf.Clamp01(preloadBuiltInSceneOp.progress / 0.9f);
        }

#if ADDRESSABLES
        if (!preloadAddrSceneHandle.HasValue) return 0f;
        return Mathf.Clamp01(preloadAddrSceneHandle.Value.PercentComplete);
#else
        // Addressables chưa enable -> coi như không có progress, nhưng vì đã fallback nên không kẹt
        return 0f;
#endif
    }

    private bool IsNextScenePreloadReady()
    {
        if (!nextSceneIsAddressable)
        {
            if (preloadBuiltInSceneOp == null) return false;
            return preloadBuiltInSceneOp.progress >= 0.9f;
        }

#if ADDRESSABLES
        if (!preloadAddrSceneHandle.HasValue) return false;
        return preloadAddrSceneHandle.Value.IsDone;
#else
        return false;
#endif
    }

    private void ActivateNextScene()
    {
        if (!nextSceneIsAddressable)
        {
            // built-in activation
            if (preloadBuiltInSceneOp != null)
                preloadBuiltInSceneOp.allowSceneActivation = true;
            else
                SceneManager.LoadScene(nextBuiltInSceneName);

            return;
        }

#if ADDRESSABLES
        if (preloadAddrSceneHandle.HasValue)
        {
            var h = preloadAddrSceneHandle.Value;

            if (h.Status != AsyncOperationStatus.Succeeded)
            {
                ShowFail("[Intro] Preload Addressable scene failed:\n" + (h.OperationException?.ToString() ?? "Unknown"));
                return;
            }

            h.Result.ActivateAsync();
        }
        else
        {
            Addressables.LoadSceneAsync(nextAddressableSceneKey, LoadSceneMode.Single, activateOnLoad: true);
        }
#else
        Debug.LogError("[Intro] ADDRESSABLES not enabled -> fallback built-in direct load.");
        SceneManager.LoadScene(nextBuiltInSceneName);
#endif
    }

    private float GetVideoProgress01()
    {
        if (IsVideoDone()) return 1f;
        if (videoPlayer == null) return 0f;
        if (!videoPlayer.isPrepared) return 0f;

        double len = videoPlayer.length;
        if (len <= 0.0001) return 0f;

        double t = videoPlayer.time;
        return Mathf.Clamp01((float)(t / len));
    }

    private float GetAddressablesProgress01()
    {
        var a = AddressablesPreload.Instance;
        if (a == null) return 1f; // không có preload -> coi như xong
        return Mathf.Clamp01(a.DownloadPercent01);
    }

    private bool IsAddressablesDone()
    {
        var a = AddressablesPreload.Instance;
        if (a == null) return true;
        return a.IsReady;
    }

    private bool IsVideoDone() => videoEnded;

    private void UpdateDots()
    {
        dotTimer += Time.unscaledDeltaTime;
        if (dotTimer >= dotSpeed)
        {
            dotTimer = 0f;
            dotCount = (dotCount + 1) % 4;
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
            int percent = Mathf.FloorToInt(t01 * 100f);
            string extra = forcedFallbackToBuiltIn ? " (fallback)" : "";
            textLoading.text = $"{baseText}{extra} {percent}%{new string('.', dotCount)}";
        }

        if (progressRing != null) progressRing.fillAmount = t01;
        if (sliderUI != null) sliderUI.value = t01;
    }

    private void ShowFail(string msg)
    {
        Debug.LogError(msg);

        if (failText != null) failText.text = msg;

        if (failCanvasGroup != null)
        {
            failCanvasGroup.alpha = 1f;
            failCanvasGroup.blocksRaycasts = true;
            failCanvasGroup.interactable = true;
        }

        if (gateProgressByAddressables)
            SetProgressInstant(0.99f);
    }

    private void HideFail()
    {
        if (failCanvasGroup != null)
        {
            failCanvasGroup.alpha = 0f;
            failCanvasGroup.blocksRaycasts = false;
            failCanvasGroup.interactable = false;
        }
    }

    private void OnRetryClicked()
    {
        HideFail();

        var a = AddressablesPreload.Instance;
        if (a != null)
            a.RequestRetry();

        activationRoutineStarted = false;
        RequestFinishIntroAndActivate();
    }

    private void OnDestroy()
    {
        if (retryButton != null)
            retryButton.onClick.RemoveListener(OnRetryClicked);

        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.loopPointReached -= OnVideoEnd;
            videoPlayer.prepareCompleted -= OnFadeLoader;
        }
    }
}
