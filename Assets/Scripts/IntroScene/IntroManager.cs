using UnityEngine;
using UnityEngine.Video;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using System.IO;
using UnityEngine.Networking;

[DefaultExecutionOrder(-12000)]
public class IntroManager : MonoBehaviour
{
    [Header("Intro Video")]
    public VideoPlayer videoPlayer;
    public string streamingAssetsVideoName = "myintro.mp4";
    public float videoStartTimeout = 6f;
    public bool skipVideoIfFail = true;

    public bool forceRefreshPersistentVideo = true;

    // Nếu file persistent nhỏ hơn ngưỡng này thì coi như lỗi và copy lại.
    public long minValidVideoBytes = 1024 * 50; // 50KB

    public RawImage videoRawImage;

    public RenderTexture videoRenderTexture;
    public bool keepFinalVideoFrameUntilSceneExit = true;
    public Color fallbackLoadingCoverColor = new Color(0.96f, 0.98f, 1f, 1f);
    [SerializeField] private bool useRuntimeVideoOverlay = true;
    [SerializeField] private int runtimeVideoTextureWidth = 1920;
    [SerializeField] private int runtimeVideoTextureHeight = 1080;

    [Header("UI References")]
    public Image progressRing;
    public Slider sliderUI;
    public TMP_Text textLoading;

    [Header("Progress Behavior")]
    public float warmupMinProgress = 0.01f;

    [Header("Intro Sync (6s default UX)")]
    [Tooltip("Thời gian tối thiểu chạy intro (thường = thời lượng video bạn muốn).")]
    [SerializeField] private float introDurationSec = 6f;

    [Header("Fail UI (Optional)")]
    public CanvasGroup failCanvasGroup;
    public TMP_Text failText;
    public Button retryButton;

    // ---- internal ----
    private AddressablesPreload _preload;

    private bool videoStarted;
    private bool videoFailed;
    private bool videoFrameVisible;
    
    private string videoFailReason = "";
    private Coroutine videoFrameWaitRoutine;
    private Coroutine freezeAtIntroDurationRoutine;
    private Image fallbackLoadingCover;
    private RenderTexture runtimeVideoRenderTexture;
    private RawImage runtimeVideoRawImage;
    private AspectRatioFitter runtimeVideoAspectFitter;

    private float currentVisual;
    private float targetVisual;

    private bool hasFatalFail;
    private Coroutine progressRoutine;

    private float monotonicTarget01;
    // Sync
    private float introStartRealtime;
    private float videoPlayRealtime;
    private bool videoEnded;

    // Finish gate
    private bool visualReached100;
    private bool finishRequestedByBootFlow;

    private int lastPreloadPhaseId = -1;

    public void SetExternalPreload(AddressablesPreload preload)
    {
        _preload = preload;
    }

private void Awake()
{
    forceRefreshPersistentVideo = true;
    NormalizeFallbackCoverColor();
    EnsureVideoOverlaySurface();
    PrepareVideoSurfaceForPlayback();

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

    visualReached100 = false;
    ForceProgress(0f);
}

    private void Start()
    {
        if (_preload == null)
            _preload = AddressablesPreload.Instance;

        introStartRealtime = Time.realtimeSinceStartup;
        videoPlayRealtime = 0f;
        videoEnded = false;
        visualReached100 = false;

        if (videoPlayer == null)
        {
            Debug.LogWarning("[Intro] VideoPlayer not assigned -> skip video.");
            videoFailed = true;
            videoEnded = true;
        }
        else
        {
            SetupVideoPlayer(videoPlayer);

            videoPlayer.errorReceived += OnVideoError;
            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.loopPointReached += OnVideoEndReached;
            videoPlayer.frameReady += OnVideoFrameReady;

            StartCoroutine(CoPrepareAndPlayVideo_Robust());
            StartCoroutine(CoVideoStartTimeoutCheck());
        }

        progressRoutine = StartCoroutine(CoUpdateProgressUI());
    }

    public void OnAboutToEnterMain()
    {
        EnsureLoadingCoverVisible();
    }

    public void ForceProgress(float t01)
    {
        t01 = Mathf.Clamp01(t01);

        // Không cho progress tụt xuống.
        if (t01 < monotonicTarget01)
            t01 = monotonicTarget01;

        monotonicTarget01 = t01;
        currentVisual = Mathf.Max(currentVisual, t01);
        targetVisual = Mathf.Max(targetVisual, t01);

        if (currentVisual >= 1f)
        {
            currentVisual = 1f;
            targetVisual = 1f;
            monotonicTarget01 = 1f;
            visualReached100 = true;
        }

        SetProgressInstant(currentVisual);
    }

    public void ForceProgressNoDecrease(float t01)
    {
        ForceProgress(t01);
    }

    public void ShowFatalFail(string msg)
    {
        hasFatalFail = true;
        Debug.LogError(msg);

        if (progressRoutine != null)
        {
            StopCoroutine(progressRoutine);
            progressRoutine = null;
        }

        if (failText != null)
        {
            string extra = string.IsNullOrEmpty(videoFailReason) ? "" : ("\n\n" + videoFailReason);
            failText.text = msg + extra;
        }

        if (failCanvasGroup != null)
        {
            failCanvasGroup.alpha = 1f;
            failCanvasGroup.blocksRaycasts = true;
            failCanvasGroup.interactable = true;
        }
    }

    public void ClearFatalFailAfterNetworkRestored()
    {
        HideFail();
    }

    private void HideFail()
    {
        hasFatalFail = false;

        if (failCanvasGroup != null)
        {
            failCanvasGroup.alpha = 0f;
            failCanvasGroup.blocksRaycasts = false;
            failCanvasGroup.interactable = false;
        }

        if (progressRoutine == null)
            progressRoutine = StartCoroutine(CoUpdateProgressUI());
    }

    private void OnRetryClicked()
    {
        HideFail();

        if (_preload != null)
            _preload.RequestRetry();

        introStartRealtime = Time.realtimeSinceStartup;
        videoPlayRealtime = 0f;
        videoEnded = false;
        monotonicTarget01 = 0f;
        currentVisual = 0f;
        targetVisual = 0f;
        visualReached100 = false;

        ForceProgress(0f);

        videoStarted = false;
        videoFailed = false;
        videoFrameVisible = false;
        videoFailReason = "";
        PrepareVideoSurfaceForPlayback();

        if (videoFrameWaitRoutine != null)
        {
            StopCoroutine(videoFrameWaitRoutine);
            videoFrameWaitRoutine = null;
        }

        if (freezeAtIntroDurationRoutine != null)
        {
            StopCoroutine(freezeAtIntroDurationRoutine);
            freezeAtIntroDurationRoutine = null;
        }

        finishRequestedByBootFlow = false;

        if (videoPlayer != null)
        {
            try
            {
                videoPlayer.Stop();
            }
            catch { }

            StartCoroutine(CoPrepareAndPlayVideo_Robust());
            StartCoroutine(CoVideoStartTimeoutCheck());
        }
    }

    // ========================= VIDEO =========================

    private void SetupVideoPlayer(VideoPlayer vp)
    {
        vp.playOnAwake = false;
        vp.isLooping = false;
        vp.waitForFirstFrame = true;
        vp.skipOnDrop = true;
        vp.sendFrameReadyEvents = true;

        // Nếu bạn dùng RawImage + RenderTexture thì code tự gán.
        if (videoRenderTexture != null)
        {
            vp.renderMode = VideoRenderMode.RenderTexture;
            vp.targetTexture = videoRenderTexture;

            if (videoRawImage != null)
                videoRawImage.texture = videoRenderTexture;

            PrepareVideoSurfaceForPlayback();

            Debug.Log("[Intro] Video display mode = RenderTexture");
        }
        else if (videoRawImage != null)
        {
            Debug.LogWarning("[Intro] videoRawImage assigned but videoRenderTexture is null. RawImage sẽ không hiện video nếu VideoPlayer chưa có targetTexture.");
        }
    }

    private IEnumerator CoPrepareAndPlayVideo_Robust()
    {
        if (videoPlayer == null)
            yield break;

        videoStarted = false;
        videoFailed = false;
        videoFrameVisible = false;
        videoEnded = false;
        videoFailReason = "";
        PrepareVideoSurfaceForPlayback();

        string persistentPath = Path.Combine(Application.persistentDataPath, streamingAssetsVideoName);

        Debug.Log("[Intro] persistent video path = " + persistentPath);

        bool needCopy = forceRefreshPersistentVideo;

        if (!needCopy)
        {
            try
            {
                if (!File.Exists(persistentPath))
                {
                    needCopy = true;
                    Debug.Log("[Intro] Persistent video not found -> copy needed.");
                }
                else
                {
                    FileInfo fi = new FileInfo(persistentPath);

                    if (fi.Length < minValidVideoBytes)
                    {
                        needCopy = true;
                        Debug.LogWarning($"[Intro] Persistent video too small ({fi.Length} bytes) -> copy needed.");
                    }
                    else
                    {
                        needCopy = false;
                        Debug.Log($"[Intro] Persistent video exists ({fi.Length} bytes) -> reuse.");
                    }
                }
            }
            catch (System.Exception e)
            {
                needCopy = true;
                Debug.LogWarning("[Intro] Check persistent video failed -> copy needed. " + e);
            }
        }

        if (needCopy)
        {
            try
            {
                if (File.Exists(persistentPath))
                {
                    File.Delete(persistentPath);
                    Debug.Log("[Intro] Deleted old persistent video.");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Intro] Delete old persistent video failed: " + e);
            }

            string src = Path.Combine(Application.streamingAssetsPath, streamingAssetsVideoName);
            bool srcLooksLikeUrl = src.Contains("://") || src.Contains("jar:");

            Debug.Log("[Intro] StreamingAssets video src = " + src);

            if (!srcLooksLikeUrl && File.Exists(src))
            {
                // Editor / Standalone / iOS có thể đọc trực tiếp file path.
                try
                {
                    File.Copy(src, persistentPath, true);

                    FileInfo copied = new FileInfo(persistentPath);
                    Debug.Log($"[Intro] Copied video to persistent. Size={copied.Length} bytes");
                }
                catch (System.Exception e)
                {
                    FailVideo("[Intro] File.Copy StreamingAssets -> persistent failed: " + e);
                    yield break;
                }
            }
            else
            {
                // Android StreamingAssets nằm trong jar nên phải đọc qua UnityWebRequest.
                string srcUrl = src;

                if (!srcUrl.Contains("://") && !srcUrl.Contains("jar:"))
                    srcUrl = new System.Uri(srcUrl).AbsoluteUri;

                Debug.Log("[Intro] Read video via UnityWebRequest: " + srcUrl);

                using (UnityWebRequest req = UnityWebRequest.Get(srcUrl))
                {
                    yield return req.SendWebRequest();

                    bool ok = req.result == UnityWebRequest.Result.Success;

                    if (!ok)
                    {
                        FailVideo($"[Intro] Cannot read video from StreamingAssets.\nError={req.error}\nURL={srcUrl}");
                        yield break;
                    }

                    byte[] data = req.downloadHandler.data;

                    if (data == null || data.Length < minValidVideoBytes)
                    {
                        FailVideo($"[Intro] Loaded video data invalid. Bytes={(data == null ? 0 : data.Length)} URL={srcUrl}");
                        yield break;
                    }

                    try
                    {
                        File.WriteAllBytes(persistentPath, data);

                        FileInfo copied = new FileInfo(persistentPath);
                        Debug.Log($"[Intro] Wrote persistent video. Size={copied.Length} bytes");
                    }
                    catch (System.Exception e)
                    {
                        FailVideo("[Intro] Write persistent video failed: " + e);
                        yield break;
                    }
                }
            }
        }

        if (!File.Exists(persistentPath))
        {
            FailVideo("[Intro] Persistent video missing after copy: " + persistentPath);
            yield break;
        }

        try
        {
            FileInfo fi = new FileInfo(persistentPath);

            if (fi.Length < minValidVideoBytes)
            {
                FailVideo($"[Intro] Persistent video invalid after copy. Size={fi.Length} bytes");
                yield break;
            }
        }
        catch (System.Exception e)
        {
            FailVideo("[Intro] Validate persistent video failed: " + e);
            yield break;
        }

        string url = new System.Uri(persistentPath).AbsoluteUri;
        Debug.Log($"[Intro] Video url = {url}");

        videoPlayer.Stop();
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = url;

        videoPlayer.Prepare();

        float prepareTimeout = Mathf.Max(1f, videoStartTimeout);
        float elapsed = 0f;
        bool warnedSlowPrepare = false;

        while (!videoPlayer.isPrepared && !videoFailed)
        {
            elapsed += Time.unscaledDeltaTime;

            if (!warnedSlowPrepare && elapsed >= prepareTimeout)
            {
                warnedSlowPrepare = true;
                Debug.LogWarning($"[Intro] Video prepare is slower than {prepareTimeout:0.##}s. Keep waiting for the intro video instead of skipping it. URL={url}");
            }

            yield return null;
        }

        if (videoFailed)
            yield break;


        // Phòng khi prepareCompleted event không bắn đúng trên một số device.
        OnVideoPrepared(videoPlayer);
    }

private void OnVideoPrepared(VideoPlayer vp)
{
    if (videoFailed || videoStarted)
        return;

    Debug.Log($"[Intro] Video prepared -> Play(). length={vp.length:0.###}, frameCount={vp.frameCount}, width={vp.width}, height={vp.height}");

    try
    {
        PrepareVideoSurfaceForPlayback();
        ApplyVideoAspectFromPlayer(vp);

        vp.Play();
        videoPlayRealtime = Time.realtimeSinceStartup;
        videoStarted = true;

        if (videoFrameWaitRoutine != null)
            StopCoroutine(videoFrameWaitRoutine);

        videoFrameWaitRoutine = StartCoroutine(CoWaitForFirstVideoFrame(vp));

        if (freezeAtIntroDurationRoutine != null)
            StopCoroutine(freezeAtIntroDurationRoutine);

        freezeAtIntroDurationRoutine = StartCoroutine(CoFreezeVideoAtIntroDuration(vp));
    }
    catch (System.Exception e)
    {
        FailVideo("[Intro] Video Play failed: " + e);
    }
}

    private IEnumerator CoWaitForFirstVideoFrame(VideoPlayer vp)
    {
        float t = 0f;
        float timeout = Mathf.Max(1f, videoStartTimeout);
        bool warnedSlowFirstFrame = false;

        while (!videoFailed && !videoFrameVisible)
        {
            if (HasVisibleVideoFrame(vp))
            {
                ShowVideoSurface();
                videoFrameWaitRoutine = null;
                yield break;
            }

            t += Time.unscaledDeltaTime;

            if (!warnedSlowFirstFrame && t >= timeout)
            {
                warnedSlowFirstFrame = true;
                Debug.LogWarning($"[Intro] Video first frame is slower than {timeout:0.##}s. Keep waiting instead of hiding the intro video. url={(vp != null ? vp.url : "NULL")}");
            }

            yield return null;
        }

        videoFrameWaitRoutine = null;
    }

    private IEnumerator CoFreezeVideoAtIntroDuration(VideoPlayer vp)
    {
        while (!videoFailed && !videoFrameVisible)
            yield return null;

        float holdStart = videoPlayRealtime > 0f ? videoPlayRealtime : Time.realtimeSinceStartup;

        while (!videoFailed && !videoEnded &&
               Time.realtimeSinceStartup - holdStart < Mathf.Max(0.01f, introDurationSec))
        {
            yield return null;
        }

        if (!videoFailed && !videoEnded)
        {
            Debug.Log("[Intro] Intro duration reached -> hold video frame until world load is ready.");
            videoEnded = true;
            FreezeAtLastFrame(vp);
            KeepVideoSurfaceVisible();
        }

        freezeAtIntroDurationRoutine = null;
    }

    private bool HasVisibleVideoFrame(VideoPlayer vp)
    {
        if (vp == null || !vp.isPrepared)
            return false;

        if (!UsesCameraVideoSurface(vp) && vp.texture == null && videoRenderTexture == null)
            return false;

        return vp.frame >= 0 || vp.time > 0.0;
    }

    private void ApplyVideoAspectFromPlayer(VideoPlayer vp)
    {
        if (vp == null || videoRawImage == null)
            return;

        AspectRatioFitter fitter = runtimeVideoAspectFitter != null
            ? runtimeVideoAspectFitter
            : videoRawImage.GetComponent<AspectRatioFitter>();

        if (fitter == null || vp.width <= 0 || vp.height <= 0)
            return;

        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = (float)vp.width / vp.height;
    }

    private void OnVideoFrameReady(VideoPlayer vp, long frameIdx)
    {
        if (videoFailed || videoFrameVisible)
            return;

        ShowVideoSurface();
    }

    private bool EnsureVideoOverlaySurface()
    {
        if (!useRuntimeVideoOverlay)
            return videoRawImage != null && videoRenderTexture != null;

        if (videoPlayer == null && videoRenderTexture == null)
            return false;

        if (videoRenderTexture == null)
        {
            int width = Mathf.Max(64, runtimeVideoTextureWidth > 0 ? runtimeVideoTextureWidth : Screen.width);
            int height = Mathf.Max(64, runtimeVideoTextureHeight > 0 ? runtimeVideoTextureHeight : Screen.height);

            runtimeVideoRenderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
            {
                name = "[Intro Runtime Video RT]",
                useMipMap = false,
                autoGenerateMips = false
            };

            runtimeVideoRenderTexture.Create();
            videoRenderTexture = runtimeVideoRenderTexture;
        }

        if (videoRawImage == null)
        {
            Canvas canvas = FindIntroCanvas();

            if (canvas == null)
                return false;

            GameObject surfaceObject = new GameObject("[Intro Runtime Video Surface]", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
            surfaceObject.transform.SetParent(canvas.transform, false);
            surfaceObject.transform.SetAsFirstSibling();

            RectTransform rect = surfaceObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            runtimeVideoRawImage = surfaceObject.GetComponent<RawImage>();
            runtimeVideoRawImage.raycastTarget = false;
            runtimeVideoRawImage.color = Color.white;
            runtimeVideoRawImage.texture = videoRenderTexture;

            runtimeVideoAspectFitter = surfaceObject.GetComponent<AspectRatioFitter>();
            runtimeVideoAspectFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            runtimeVideoAspectFitter.aspectRatio = videoRenderTexture != null && videoRenderTexture.height > 0
                ? (float)videoRenderTexture.width / videoRenderTexture.height
                : 16f / 9f;

            videoRawImage = runtimeVideoRawImage;
        }

        if (videoRawImage != null && videoRenderTexture != null)
        {
            videoRawImage.texture = videoRenderTexture;
            videoRawImage.transform.SetAsFirstSibling();
            EnsureParentsActive(videoRawImage.transform);
            return true;
        }

        return false;
    }

    private bool PrepareVideoSurfaceForPlayback()
    {
        if (UsesCameraVideoSurface(videoPlayer))
        {
            DisableFallbackLoadingCover();
            return true;
        }

        EnsureVideoOverlaySurface();

        if (videoRawImage == null)
            return false;

        bool hasOverlaySurface = EnsureVideoOverlaySurface();

        if (hasOverlaySurface && videoRenderTexture != null)
            videoRawImage.texture = videoRenderTexture;
        else if (videoPlayer != null && videoPlayer.texture != null)
            videoRawImage.texture = videoPlayer.texture;

        EnsureParentsActive(videoRawImage.transform);
        videoRawImage.transform.SetAsFirstSibling();
        bool hasVideoSurface = videoRawImage.texture != null;
        videoRawImage.enabled = hasVideoSurface;

        if (hasVideoSurface)
            DisableFallbackLoadingCover();

        return hasVideoSurface;
    }

    private void ShowVideoSurface()
    {
        if (videoFrameVisible)
            return;

        bool surfaceVisible = videoRawImage == null;

        if (videoRawImage != null)
        {
            if (videoRenderTexture != null)
                videoRawImage.texture = videoRenderTexture;
            else if (videoPlayer != null && videoPlayer.texture != null)
                videoRawImage.texture = videoPlayer.texture;

            videoRawImage.enabled = videoRawImage.texture != null;
            surfaceVisible = videoRawImage.enabled;
        }

        if (!surfaceVisible)
            return;

        DisableFallbackLoadingCover();
        videoFrameVisible = true;
        Debug.Log("[Intro] Video first frame visible.");
    }

    private void OnVideoEndReached(VideoPlayer vp)
    {
        Debug.Log("[Intro] Video reached end -> Freeze last frame.");
        videoEnded = true;
        FreezeAtLastFrame(vp);
    }

    private void FreezeAtLastFrame(VideoPlayer vp)
    {
        try
        {
            if (vp == null)
                return;

            vp.Pause();

            if (vp.frameCount > 0)
            {
                long last = (long)vp.frameCount - 1;
                if (last < 0) last = 0;
                vp.frame = last;
            }

            KeepVideoSurfaceVisible();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[Intro] Freeze last frame failed: " + e);
        }
    }

    private void OnVideoError(VideoPlayer vp, string message)
    {
        FailVideo("[Intro] Video error: " + message);
    }

    private IEnumerator CoVideoStartTimeoutCheck()
    {
        while (!videoStarted && !videoFailed)
            yield return null;

        if (videoFailed)
            yield break;

        float t = 0f;

        while (t < videoStartTimeout && !videoFrameVisible && !videoFailed)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!videoFrameVisible && !videoFailed)
        {
            bool isPrepared = videoPlayer != null && videoPlayer.isPrepared;
            string url = videoPlayer != null ? videoPlayer.url : "NULL";
            Debug.LogWarning($"[Intro] Video frame is slower than {videoStartTimeout:0.##}s. Keep waiting; do not skip or cover the intro video. isPrepared={isPrepared}, url={url}");
        }
    }

    private void FailVideo(string reason)
    {
        if (videoFailed)
            return;

        Debug.LogWarning(reason);

        videoFailed = true;
        videoFailReason = reason;

        EnsureLoadingCoverVisible();

        // Nếu fail video mà vẫn cho qua, coi như video ended để flow không bị giữ.
        if (skipVideoIfFail)
            videoEnded = true;

        if (!skipVideoIfFail)
            ShowFatalFail(reason);
    }

    // ========================= PROGRESS =========================

private IEnumerator CoUpdateProgressUI()
{
    while (!hasFatalFail)
    {
        UpdateProgressFromPreload();
        ApplyVisual();
        yield return null;
    }
}

    private float GetIntroTime01()
    {
        float t = Time.realtimeSinceStartup - introStartRealtime;
        return Mathf.Clamp01(t / Mathf.Max(0.01f, introDurationSec));
    }

private void UpdateProgressFromPreload()
{
    if (_preload == null)
    {
        SetTargetMonotonic(warmupMinProgress);
        ApplyVisual();
        return;
    }

    if (_preload.LoadingPhaseId != lastPreloadPhaseId)
    {
        lastPreloadPhaseId = _preload.LoadingPhaseId;
        ResetVisualProgressForNewPhase(Mathf.Max(warmupMinProgress, _preload.DownloadPercent01));
    }

    bool preloadDataDone =
        _preload.Stage == AddressablesPreload.PreloadStage.Done &&
        Mathf.Clamp01(_preload.DownloadPercent01) >= 1f &&
        !_preload.IsPreparingKey;

    if (preloadDataDone || finishRequestedByBootFlow)
    {
        SetTargetMonotonic(1f);
        ApplyVisual();
        return;
    }

    float data01 = Mathf.Clamp01(_preload.DownloadPercent01);

    if (data01 <= 0f && warmupMinProgress > 0f)
        data01 = warmupMinProgress;

    // Không map theo video/time nữa.
    // Thanh progress phải đồng bộ với % thật của phase hiện tại.
    SetTargetMonotonic(data01);
    ApplyVisual();
}

    private void SetTargetMonotonic(float newTarget01)
    {
        newTarget01 = Mathf.Clamp01(newTarget01);

        if (newTarget01 > monotonicTarget01)
            monotonicTarget01 = newTarget01;
    }

    private void ApplyVisual()
    {
        EnsureLoadingCoverVisible();

        targetVisual = monotonicTarget01;
        currentVisual = Mathf.Clamp01(targetVisual);
        visualReached100 = currentVisual >= 1f;

        SetProgressInstant(currentVisual);
    }

private string GetStageText(float t01)
{
    if (_preload != null && !string.IsNullOrEmpty(_preload.LoadingText))
        return _preload.LoadingText;

    return $"Đang tải tài nguyên ({Mathf.FloorToInt(t01 * 100f)}%)";
}

    private void SetProgressInstant(float t01)
    {
        t01 = Mathf.Clamp01(t01);

        EnsureLoadingCoverVisible();
        EnsureAssignedProgressUiVisible();

        if (textLoading != null)
            textLoading.text = GetStageText(t01);

        if (progressRing != null)
            progressRing.fillAmount = t01;

        if (sliderUI != null)
            sliderUI.value = t01;
    }

    private void EnsureAssignedProgressUiVisible()
    {
        EnsureLoadingCoverVisible();
        EnsureUiBehaviourVisible(textLoading);
        EnsureUiBehaviourVisible(progressRing);
        EnsureUiBehaviourVisible(sliderUI);
    }

    private void EnsureLoadingCoverVisible()
    {
        if (!videoFailed && PrepareVideoSurfaceForPlayback())
            return;

        if (keepFinalVideoFrameUntilSceneExit && !videoFailed && videoFrameVisible)
        {
            KeepVideoSurfaceVisible();
            return;
        }

        EnsureFallbackLoadingCover();
    }

    private void KeepVideoSurfaceVisible()
    {
        if (UsesCameraVideoSurface(videoPlayer))
        {
            DisableFallbackLoadingCover();
            return;
        }

        if (videoRawImage == null)
        {
            EnsureFallbackLoadingCover();
            return;
        }

        if (videoRenderTexture != null)
            videoRawImage.texture = videoRenderTexture;
        else if (videoPlayer != null && videoPlayer.texture != null)
            videoRawImage.texture = videoPlayer.texture;

        if (videoRawImage.texture == null)
        {
            EnsureFallbackLoadingCover();
            return;
        }

        EnsureParentsActive(videoRawImage.transform);
        videoRawImage.transform.SetAsFirstSibling();
        videoRawImage.enabled = true;
        DisableFallbackLoadingCover();
    }

    private void DisableFallbackLoadingCover()
    {
        if (fallbackLoadingCover != null)
            fallbackLoadingCover.enabled = false;
    }

    private bool UsesCameraVideoSurface(VideoPlayer vp)
    {
        if (vp == null)
            return false;

        return vp.renderMode == VideoRenderMode.CameraFarPlane ||
               vp.renderMode == VideoRenderMode.CameraNearPlane;
    }

    private void EnsureFallbackLoadingCover()
    {
        NormalizeFallbackCoverColor();

        if (fallbackLoadingCover == null)
        {
            Canvas canvas = FindIntroCanvas();

            if (canvas == null)
                return;

            GameObject coverObject = new GameObject("[Intro Loading Cover]", typeof(RectTransform), typeof(Image));
            coverObject.transform.SetParent(canvas.transform, false);
            coverObject.transform.SetAsFirstSibling();

            RectTransform rect = coverObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            fallbackLoadingCover = coverObject.GetComponent<Image>();
            fallbackLoadingCover.raycastTarget = false;
        }

        fallbackLoadingCover.color = fallbackLoadingCoverColor;
        fallbackLoadingCover.enabled = true;
        EnsureParentsActive(fallbackLoadingCover.transform);
        fallbackLoadingCover.transform.SetAsFirstSibling();
    }

    private void NormalizeFallbackCoverColor()
    {
        if (fallbackLoadingCoverColor.a <= 0.001f)
            fallbackLoadingCoverColor = new Color(0.96f, 0.98f, 1f, 1f);

        if (fallbackLoadingCoverColor.r < 0.08f &&
            fallbackLoadingCoverColor.g < 0.08f &&
            fallbackLoadingCoverColor.b < 0.08f)
        {
            fallbackLoadingCoverColor = new Color(0.96f, 0.98f, 1f, 1f);
        }
    }

    private Canvas FindIntroCanvas()
    {
        if (textLoading != null)
            return textLoading.GetComponentInParent<Canvas>(true);

        if (sliderUI != null)
            return sliderUI.GetComponentInParent<Canvas>(true);

        if (progressRing != null)
            return progressRing.GetComponentInParent<Canvas>(true);

        if (videoRawImage != null)
            return videoRawImage.GetComponentInParent<Canvas>(true);

        return GetComponentInChildren<Canvas>(true);
    }

    private void EnsureUiBehaviourVisible(Behaviour ui)
    {
        if (ui == null)
            return;

        ui.enabled = true;
        EnsureParentsActive(ui.transform);
    }

    private void EnsureParentsActive(Transform target)
    {
        Transform current = target;

        while (current != null)
        {
            if (!current.gameObject.activeSelf)
                current.gameObject.SetActive(true);

            CanvasGroup canvasGroup = current.GetComponent<CanvasGroup>();
            if (canvasGroup != null && canvasGroup.alpha <= 0.001f)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }

            current = current.parent;
        }
    }

    private void OnDestroy()
    {
        if (videoFrameWaitRoutine != null)
        {
            StopCoroutine(videoFrameWaitRoutine);
            videoFrameWaitRoutine = null;
        }

        if (freezeAtIntroDurationRoutine != null)
        {
            StopCoroutine(freezeAtIntroDurationRoutine);
            freezeAtIntroDurationRoutine = null;
        }

        if (retryButton != null)
            retryButton.onClick.RemoveListener(OnRetryClicked);

        if (videoPlayer != null)
        {
            videoPlayer.errorReceived -= OnVideoError;
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.loopPointReached -= OnVideoEndReached;
            videoPlayer.frameReady -= OnVideoFrameReady;
        }

        if (runtimeVideoRenderTexture != null)
        {
            runtimeVideoRenderTexture.Release();
            Destroy(runtimeVideoRenderTexture);
            runtimeVideoRenderTexture = null;
        }
    }

public bool CanEnterMain
{
    get
    {
        bool minTimePassed = GetIntroTime01() >= 1f;

        if (!minTimePassed)
            return false;

        // Quan trọng:
        // Không cho vào Main nếu AddressablesPreload chưa download + giải nén xong.
        if (_preload != null && !_preload.IsReady)
            return false;

        if (_preload != null && _preload.IsPreparingKey)
            return false;

        // Chặn vào Main cho tới khi UI progress thật sự lên 100%.
        if (!visualReached100)
            return false;

        if (!videoFailed && !videoFrameVisible)
            return false;

        if (videoFailed && skipVideoIfFail)
            return true;

        return videoEnded;
    }
}
public void RequestFinishTo100()
{
    // Chỉ cho finish khi preload thật sự xong.
    if (_preload != null && (!_preload.IsReady || _preload.IsPreparingKey))
        return;

    finishRequestedByBootFlow = true;
}
    public void SetBootProgress01(float p01, bool allowComplete = false)
    {
        p01 = Mathf.Clamp01(p01);

        if (allowComplete && p01 >= 1f)
            finishRequestedByBootFlow = true;

        SetTargetMonotonic(p01);
    }
private void ResetVisualProgressForNewPhase(float start01)
{
    start01 = Mathf.Clamp01(start01);

    monotonicTarget01 = start01;
    currentVisual = start01;
    targetVisual = start01;
    visualReached100 = start01 >= 1f;

    SetProgressInstant(start01);
}
}
