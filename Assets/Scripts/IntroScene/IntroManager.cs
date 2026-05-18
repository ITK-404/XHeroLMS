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

    [Header("UI References")]
    public Image progressRing;
    public Slider sliderUI;
    public TMP_Text textLoading;

    [Header("Progress Behavior")]
    public float visualLerpSpeed = 8f;
    public float warmupMinProgress = 0.01f;

    // Khi preload đã xong, progress sẽ chạy từ vị trí hiện tại lên 100. Tăng số này nếu muốn chạy nhanh hơn.
    float finishLerpSpeed = 2.5f;

    // Khi progress gần 100% tới ngưỡng này thì snap lên 100 để tránh kẹt 99%.
    [Range(0.95f, 0.9999f)]
    float finishSnapThreshold = 0.995f;

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

    private float monotonicTarget01;
    private bool sawAnyFailure;

    // Sync
    private float introStartRealtime;
    private bool videoEnded;

    // Finish gate
    private bool visualReached100;
    private bool finishRequestedByBootFlow;

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

        visualReached100 = false;
        ForceProgress(0f);
    }

    private void Start()
    {
        if (_preload == null)
            _preload = AddressablesPreload.Instance;

        introStartRealtime = Time.realtimeSinceStartup;
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

            StartCoroutine(CoPrepareAndPlayVideo_Robust());
            StartCoroutine(CoVideoStartTimeoutCheck());
        }

        progressRoutine = StartCoroutine(CoUpdateProgressUI());
    }

    public void OnAboutToEnterMain()
    {
        // hook optional
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
        videoEnded = false;
        sawAnyFailure = false;

        monotonicTarget01 = 0f;
        currentVisual = 0f;
        targetVisual = 0f;
        visualReached100 = false;

        ForceProgress(0f);

        videoStarted = false;
        videoFailed = false;
        videoFailReason = "";

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
        vp.sendFrameReadyEvents = false;

        // Nếu bạn dùng RawImage + RenderTexture thì code tự gán.
        if (videoRenderTexture != null)
        {
            vp.renderMode = VideoRenderMode.RenderTexture;
            vp.targetTexture = videoRenderTexture;

            if (videoRawImage != null)
            {
                videoRawImage.texture = videoRenderTexture;
                videoRawImage.enabled = true;
            }

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
        videoEnded = false;
        videoFailReason = "";

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

        while (!videoPlayer.isPrepared && !videoFailed && elapsed < prepareTimeout)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (videoFailed)
            yield break;

        if (!videoPlayer.isPrepared)
        {
            FailVideo($"[Intro] Video prepare timeout ({prepareTimeout:0.##}s). URL={url}");
            yield break;
        }

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
            vp.Play();
            videoStarted = true;
        }
        catch (System.Exception e)
        {
            FailVideo("[Intro] Video Play failed: " + e);
        }
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
        {
            bool isPrepared = videoPlayer != null && videoPlayer.isPrepared;
            string url = videoPlayer != null ? videoPlayer.url : "NULL";
            FailVideo($"[Intro] Video start timeout ({videoStartTimeout:0.##}s). isPrepared={isPrepared}, url={url}");
        }
    }

    private void FailVideo(string reason)
    {
        Debug.LogWarning(reason);

        videoFailed = true;
        sawAnyFailure = true;
        videoFailReason = reason;

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
            // UpdateProgressFromPreload();
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
        float time01 = GetIntroTime01();

        // Không có preload -> chạy theo thời gian cho mượt.
        if (_preload == null)
        {
            SetTargetMonotonic(Mathf.Max(warmupMinProgress, time01));
            ApplyVisual();
            return;
        }

bool ready = _preload.IsReady;

if (ready || finishRequestedByBootFlow)
{
    SetTargetMonotonic(1f);
    ApplyVisual();
    return;
}

        float data01 = Mathf.Clamp01(_preload.DownloadPercent01);

        if (data01 <= 0f && warmupMinProgress > 0f)
            data01 = warmupMinProgress;

        if (time01 < 1f && !videoEnded)
        {
            // Trong introDurationSec đầu: chạy theo time nhưng không vượt cap.
            float videoDriven = Mathf.Min(time01 * capWhenVideoDoneButNotReady, capWhenVideoDoneButNotReady);
            SetTargetMonotonic(Mathf.Max(data01, videoDriven));
        }
        else
        {
            // Sau introDurationSec: map data 0..1 -> cap..1.
            float mapped = Mathf.Lerp(capWhenVideoDoneButNotReady, 1f, data01);
            SetTargetMonotonic(mapped);
        }

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

        float speed = targetVisual >= 1f ? finishLerpSpeed : visualLerpSpeed;

        currentVisual = Mathf.Lerp(
            currentVisual,
            targetVisual,
            1f - Mathf.Exp(-speed * Time.unscaledDeltaTime)
        );

        // Không cho lố quá 100.
        currentVisual = Mathf.Clamp01(currentVisual);

        // Khi đang finish, gần 100 thì snap để không kẹt 99%.
        if (targetVisual >= 1f && currentVisual >= finishSnapThreshold)
        {
            currentVisual = 1f;
            visualReached100 = true;
        }

        SetProgressInstant(currentVisual);
    }

    private string GetStageText(float t01)
    {
        if (_preload != null && _preload.IsReady && (GetIntroTime01() >= 1f || videoEnded))
            return "Hoàn tất";

        return $"Đang tải tài nguyên ({Mathf.FloorToInt(t01 * 100f)}%)";
    }

    private void SetProgressInstant(float t01)
    {
        t01 = Mathf.Clamp01(t01);

        if (textLoading != null)
            textLoading.text = GetStageText(t01);

        if (progressRing != null)
            progressRing.fillAmount = t01;

        if (sliderUI != null)
            sliderUI.value = t01;
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

    public bool CanEnterMain
    {
        get
        {
            bool minTimePassed = GetIntroTime01() >= 1f;

            if (!minTimePassed)
                return false;

            // Chặn vào Main cho tới khi UI progress thật sự lên 100%.
            if (!visualReached100)
                return false;

            if (videoFailed && skipVideoIfFail)
                return true;

            return videoEnded;
        }
    }
    public void RequestFinishTo100()
    {
        finishRequestedByBootFlow = true;
    }
    public void SetBootProgress01(float p01, bool allowComplete = false)
    {
        p01 = Mathf.Clamp01(p01);

        // Nếu chưa cho complete thật, không cho thanh lên 100.
        if (!allowComplete)
            p01 = Mathf.Min(p01, 0.99f);

        SetTargetMonotonic(p01);
    }
}