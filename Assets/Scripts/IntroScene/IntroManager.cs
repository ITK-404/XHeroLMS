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

    [Header("Intro Sync (6s default UX)")]
    [Tooltip("Thời gian tối thiểu chạy intro (thường = thời lượng video bạn muốn).")]
    [SerializeField] private float introDurationSec = 6f;

    [Tooltip("Nếu hết introDurationSec mà data chưa xong: UI sẽ đạt tới mốc này rồi tăng tiếp theo download.")]
    [SerializeField, Range(0.5f, 0.99f)] private float capWhenVideoDoneButNotReady = 0.85f;

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

    private float monotonicTarget01;   // mục tiêu UI chỉ tăng
    private bool sawAnyFailure;        // đã từng fail trong phiên này (giữ lại nếu bạn muốn dùng)

    // Sync
    private float introStartRealtime;
    private bool videoEnded;

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

        introStartRealtime = Time.realtimeSinceStartup;
        videoEnded = false;

        // Video
        if (videoPlayer == null)
        {
            Debug.LogWarning("[Intro] VideoPlayer not assigned -> skip video.");
            videoFailed = true;
            videoEnded = true; // coi như xong để UX không bị giữ
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
        // hook (optional)
    }

    public void ForceProgress(float t01)
    {
        t01 = Mathf.Clamp01(t01);
        monotonicTarget01 = t01;
        currentVisual = t01;
        targetVisual = t01;
        SetProgressInstant(t01);
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

        // reset sync/progress
        introStartRealtime = Time.realtimeSinceStartup;
        videoEnded = false;
        sawAnyFailure = false;
        ForceProgress(0f);

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
                // iOS/Editor/Standalone
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
                // Android jar hoặc src là URL
                string srcUrl = src;
                if (!srcUrl.Contains("://") && !srcUrl.Contains("jar:"))
                    srcUrl = new System.Uri(srcUrl).AbsoluteUri; // file://...

                using (UnityWebRequest req = UnityWebRequest.Get(srcUrl))
                {
                    yield return req.SendWebRequest();
                    bool ok = req.result == UnityWebRequest.Result.Success;
                    if (!ok)
                    {
                        FailVideo($"[Intro] Cannot read video from StreamingAssets.\n{req.error}\nURL={srcUrl}");
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
        videoEnded = true;
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

        // nếu fail video mà vẫn cho qua, coi như video ended để flow không bị giữ
        if (skipVideoIfFail) videoEnded = true;

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

    private float GetIntroTime01()
    {
        float t = Time.realtimeSinceStartup - introStartRealtime;
        return Mathf.Clamp01(t / Mathf.Max(0.01f, introDurationSec));
    }

    private void UpdateProgressFromPreload()
    {
        float time01 = GetIntroTime01();

        // no preload -> chạy theo time cho mượt
        if (_preload == null)
        {
            SetTargetMonotonic(Mathf.Max(warmupMinProgress, time01));
            ApplyVisual();
            return;
        }

        bool ready = _preload.IsReady;

        if (ready)
        {
            if (videoEnded)
            {
                SetTargetMonotonic(1f);
                ApplyVisual();
                return;
            }

            SetTargetMonotonic(Mathf.Max(warmupMinProgress, time01));
            ApplyVisual();
            return;
        }

        if (!ready)
        {
            float data01 = Mathf.Clamp01(_preload.DownloadPercent01);
            if (data01 <= 0f && warmupMinProgress > 0f) data01 = warmupMinProgress;

            if (time01 < 1f && !videoEnded)
            {
                // 6s đầu: chạy theo time nhưng không vượt cap
                float videoDriven = Mathf.Min(time01 * capWhenVideoDoneButNotReady, capWhenVideoDoneButNotReady);
                SetTargetMonotonic(Mathf.Max(data01, videoDriven));
            }
            else
            {
                // sau 6s: map data 0..1 -> cap..1
                float mapped = Mathf.Lerp(capWhenVideoDoneButNotReady, 1f, data01);
                SetTargetMonotonic(mapped);
            }

            ApplyVisual();
            return;
        }

        // Ready + (hết 6s hoặc video ended) -> 100%
        SetTargetMonotonic(1f);
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
        targetVisual = monotonicTarget01;

        currentVisual = Mathf.Lerp(
            currentVisual,
            targetVisual,
            1f - Mathf.Exp(-visualLerpSpeed * Time.unscaledDeltaTime)
        );

        // không tụt
        if (currentVisual < monotonicTarget01)
            currentVisual = monotonicTarget01;

        SetProgressInstant(currentVisual);
    }

    private string GetStageText(float t01)
    {
        // Chỉ hiện "Hoàn tất" khi đủ điều kiện: preload ready và đã hết intro 6s (hoặc video ended)
        if (_preload != null && _preload.IsReady && (GetIntroTime01() >= 1f || videoEnded))
            return "Hoàn tất";

        // Luôn hiển thị đang tải + %
        return $"Đang tải tài nguyên ({Mathf.FloorToInt(t01 * 100f)}%)";
    }

    private void SetProgressInstant(float t01)
    {
        t01 = Mathf.Clamp01(t01);

        if (textLoading != null)
            textLoading.text = GetStageText(t01);

        if (progressRing != null) progressRing.fillAmount = t01;
        if (sliderUI != null) sliderUI.value = t01;
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
