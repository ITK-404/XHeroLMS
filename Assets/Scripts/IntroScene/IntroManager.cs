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

    [Header("UI References")]
    public Image progressRing;
    public Slider sliderUI;
    public TMP_Text textLoading;

    [Header("Progress Behavior")]
    public float visualLerpSpeed = 8f;
    public float warmupMinProgress = 0.01f;

    [Header("Fail UI (Optional)")]
    public CanvasGroup failCanvasGroup;
    public TMP_Text failText;
    public Button retryButton;

    // ---- internal ----
    private AddressablesPreload _preload;

    private bool videoStarted;
    private bool videoFailed;
    private string videoFailReason = "";

    private float currentVisual;
    private float targetVisual;

    private bool hasFatalFail;
    private Coroutine progressRoutine;

    public void SetExternalPreload(AddressablesPreload preload)
    {
        _preload = preload;
    }

    private void Awake()
    {
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

        ForceProgress(0f);
    }

    private void Start()
    {
        if (_preload == null) _preload = AddressablesPreload.Instance;

        // Video
        if (videoPlayer == null)
        {
            Debug.LogWarning("[Intro] VideoPlayer not assigned -> skip video.");
            videoFailed = true;
        }
        else
        {
            SetupVideoPlayerForFreezeLastFrame(videoPlayer);

            videoPlayer.errorReceived += OnVideoError;
            videoPlayer.prepareCompleted += OnVideoPrepared;
            videoPlayer.loopPointReached += OnVideoEndReached;

            StartCoroutine(CoPrepareAndPlayVideo_Robust());
            StartCoroutine(CoVideoStartTimeoutCheck());
        }

        progressRoutine = StartCoroutine(CoUpdateProgressUI());
    }

    public void OnAboutToEnterMain()
    {
        // hook (optional): disable buttons, fade out UI, v.v.
    }

    public void ForceProgress(float t01)
    {
        SetProgressInstant(Mathf.Clamp01(t01));
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

        videoStarted = false;
        videoFailed = false;
        videoFailReason = "";

        if (videoPlayer != null)
        {
            try { videoPlayer.Stop(); } catch { }
            StartCoroutine(CoPrepareAndPlayVideo_Robust());
            StartCoroutine(CoVideoStartTimeoutCheck());
        }
    }

    // ========================= VIDEO =========================

    private void SetupVideoPlayerForFreezeLastFrame(VideoPlayer vp)
    {
        vp.playOnAwake = false;
        vp.isLooping = false;
        vp.waitForFirstFrame = true;
        vp.skipOnDrop = true;
        vp.sendFrameReadyEvents = true;
    }

private IEnumerator CoPrepareAndPlayVideo_Robust()
{
    string persistentPath = Path.Combine(Application.persistentDataPath, streamingAssetsVideoName);

    bool needCopy = true;
    try
    {
        if (File.Exists(persistentPath))
        {
            var fi = new FileInfo(persistentPath);
            needCopy = fi.Length <= 0;
        }
    }
    catch { needCopy = true; }

    if (needCopy)
    {
        string src = Path.Combine(Application.streamingAssetsPath, streamingAssetsVideoName);

            bool srcLooksLikeUrl = src.Contains("://") || src.Contains("jar:");

        if (!srcLooksLikeUrl && File.Exists(src))
        {
            // iOS/Editor/Standalone (local file)
            try
            {
                File.Copy(src, persistentPath, true);
            }
            catch (System.Exception e)
            {
                FailVideo("[Intro] File.Copy StreamingAssets -> persistent failed: " + e);
                yield break;
            }
        }
        else
        {
            // Android (jar) hoặc trường hợp src là URL
            // Ensure proper URL for local paths
            string srcUrl = src;
            if (!srcUrl.Contains("://") && !srcUrl.Contains("jar:"))
                srcUrl = new System.Uri(srcUrl).AbsoluteUri; // => file://...

            using (UnityWebRequest req = UnityWebRequest.Get(src))
            {
                yield return req.SendWebRequest();
                bool ok = req.result == UnityWebRequest.Result.Success;
                if (!ok)
                {
                    FailVideo($"[Intro] Cannot read video from StreamingAssets.\n{req.error}\nURL={src}");
                    yield break;
                }

                try { File.WriteAllBytes(persistentPath, req.downloadHandler.data); }
                catch (System.Exception e)
                {
                    FailVideo("[Intro] Write persistent video failed: " + e);
                    yield break;
                }
            }
        }
    }

    string url = new System.Uri(persistentPath).AbsoluteUri;
    Debug.Log($"[Intro] Video url = {url}");

    videoPlayer.source = VideoSource.Url;
    videoPlayer.url = url;

    videoPlayer.Prepare();
        yield return null;
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        if (videoFailed) return;

        Debug.Log("[Intro] Video prepared -> Play()");
        videoPlayer.Play();
        videoStarted = true;
    }

    private void OnVideoEndReached(VideoPlayer vp)
    {
        Debug.Log("[Intro] Video reached end -> Freeze last frame.");
        FreezeAtLastFrame(vp);
    }

    private void FreezeAtLastFrame(VideoPlayer vp)
    {
        try
        {
            vp.Pause();
            long last = (vp.frameCount > 0) ? (long)vp.frameCount - 1 : vp.frame;
            if (last < 0) last = 0;
            vp.frame = last;
            vp.StepForward();
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
        float t = 0f;
        while (t < videoStartTimeout && !videoStarted && !videoFailed)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!videoStarted && !videoFailed)
            FailVideo($"[Intro] Video start timeout ({videoStartTimeout:0.##}s). isPrepared={videoPlayer != null && videoPlayer.isPrepared}");
    }

    private void FailVideo(string reason)
    {
        Debug.LogWarning(reason);
        videoFailed = true;
        videoFailReason = reason;

        if (!skipVideoIfFail)
            ShowFatalFail(reason);
    }

    // ========================= PROGRESS =========================

    private IEnumerator CoUpdateProgressUI()
    {
        while (!hasFatalFail)
        {
            UpdateProgressFromPreload();
            yield return null;
        }
    }

    private void UpdateProgressFromPreload()
    {
        float p = 0f;

        if (_preload == null)
        {
            p = warmupMinProgress;
        }
        else
        {
            if (_preload.HasFailed) p = 1f; // hoặc 0f tùy bạn, ở đây cho UI không “kẹt”
            else if (_preload.IsReady) p = 1f;
            else p = Mathf.Clamp01(_preload.DownloadPercent01);

            if (p <= 0f && warmupMinProgress > 0f) p = warmupMinProgress;
        }

        targetVisual = Mathf.Clamp01(p);

        currentVisual = Mathf.Lerp(
            currentVisual,
            targetVisual,
            1f - Mathf.Exp(-visualLerpSpeed * Time.unscaledDeltaTime)
        );

        SetProgressInstant(currentVisual);
    }

    private void SetProgressInstant(float t01)
    {
        t01 = Mathf.Clamp01(t01);

        if (textLoading != null)
        {
            textLoading.text = GetStageText(t01);
        }

        if (progressRing != null) progressRing.fillAmount = t01;
        if (sliderUI != null) sliderUI.value = t01;
    }

    private string GetStageText(float t01)
    {
        if (_preload == null) return "Khởi động";

        switch (_preload.Stage)
        {
            case AddressablesPreload.PreloadStage.Initialize:   return "Khởi tạo dữ liệu";
            case AddressablesPreload.PreloadStage.CheckCatalog: return "Kiểm tra phiên bản";
            case AddressablesPreload.PreloadStage.UpdateCatalog:return "Cập nhật nội dung";
            case AddressablesPreload.PreloadStage.GetSize:      return "Chuẩn bị tải";
            case AddressablesPreload.PreloadStage.Download:
                return $"Đang tải tài nguyên ({Mathf.FloorToInt(t01 * 100f)}%)";
            case AddressablesPreload.PreloadStage.Done:         return "Hoàn tất";
            case AddressablesPreload.PreloadStage.Failed:       return "Lỗi tải dữ liệu";
            case AddressablesPreload.PreloadStage.Probe:        return "Kiểm tra kết nối";
            case AddressablesPreload.PreloadStage.ClearCache:   return "Dọn cache";
            default: return "Đang xử lý";
        }
    }

    private void OnDestroy()
    {
        if (retryButton != null)
            retryButton.onClick.RemoveListener(OnRetryClicked);

        if (videoPlayer != null)
        {
            videoPlayer.errorReceived -= OnVideoError;
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.loopPointReached -= OnVideoEndReached;
        }
    }
}
