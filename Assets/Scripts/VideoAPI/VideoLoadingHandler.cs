using System.Collections;
using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class VideoLoadingHandler : MonoBehaviour
{
    [Header("Stall Detection")]
    [Tooltip("Thời gian đứng frame bao lâu mới coi là stall. 0.35 quá nhạy, dễ làm UI nhấp nháy/lag.")]
    public float stallThreshold = 2.0f;

    [Tooltip("Có hiện loading khi video bị stall không.")]
    public bool showLoadingWhenStalled = true;

    [Header("Retry when network lost")]
    [Tooltip("Tắt mặc định để tránh retry đụng với CourseListView/proxy fallback.")]
    public bool enableRetryOnError = false;

    public float retryIntervalSeconds = 10f;
    public int maxRetryCount = 3;

    [Header("Loading UI")]
    [Tooltip("Delay nhẹ trước khi hiện loading để tránh nhấp nháy khi buffer rất ngắn.")]
    public float showLoadingDelay = 0.15f;

    [Tooltip("Bật log debug. Không nên bật trên Android release.")]
    public bool debugLog = false;

    private VideoPlayer _vp;

    private bool _waitingFirstFrame;
    private bool _isPreparing;

    private long _lastFrame = -1;
    private float _stallTimer;
    private float _loadingCandidateTimer;

    private Coroutine _prepareRoutine;
    private Coroutine _retryRoutine;

    private bool _cancelled;
    private bool _suppressLoadingUI;

    private string _lastUrl;
    private bool _lastAutoplay = true;
    private int _retryCount;
    private bool _popupShown;

    private bool _loadingVisible;
    private string _lastLoadingReason;

    private bool _eventsBound;

    private const string NetworkErrorMessage =
        "Không thể tải nội dung.\nVui lòng kiểm tra kết nối mạng hoặc\n thử lại sau.";

    private const string NetworkErrorHeader = "Lỗi Mạng";

    void Awake()
    {
        _vp = GetComponent<VideoPlayer>();

        if (_vp != null)
        {
            _vp.playOnAwake = false;
            _vp.waitForFirstFrame = false;
            _vp.skipOnDrop = true;
            _vp.sendFrameReadyEvents = false;
        }

        BindEvents();
    }

    void OnEnable()
    {
        BindEvents();
    }

    void OnDisable()
    {
        HideLoadingIfVisible();
        StopPrepareRoutineIfAny();
        StopRetryRoutineIfAny();
        UnbindEvents();
    }

    void OnDestroy()
    {
        HideLoadingIfVisible();
        StopPrepareRoutineIfAny();
        StopRetryRoutineIfAny();
        UnbindEvents();
    }

    void Start()
    {
        if (_vp == null)
            return;

        if (_vp.clip != null || !string.IsNullOrEmpty(_vp.url))
        {
            _suppressLoadingUI = false;
            _waitingFirstFrame = true;

            if (!string.IsNullOrEmpty(_vp.url))
                _lastUrl = _vp.url;

            MarkLoadingCandidate("Start has video source");
        }
    }

    void Update()
    {
        if (_vp == null)
            return;

        TryAutoResumeFromExternalChange();

        if (_suppressLoadingUI)
        {
            SetLoadingVisible(false, "suppressed");
            return;
        }

        if (_vp.clip == null && string.IsNullOrEmpty(_vp.url))
        {
            ResetRuntimeState();
            SetLoadingVisible(false, "no source");
            return;
        }

        bool playing = _vp.isPrepared && _vp.isPlaying;
        bool stalled = false;

        if (playing)
        {
            long currentFrame = _vp.frame;

            if (currentFrame >= 0 && currentFrame == _lastFrame)
            {
                _stallTimer += Time.unscaledDeltaTime;

                if (_stallTimer >= stallThreshold)
                    stalled = true;
            }
            else
            {
                _stallTimer = 0f;
                _lastFrame = currentFrame;
            }
        }
        else
        {
            _stallTimer = 0f;
            _lastFrame = -1;
        }

        bool noTexture = !_vp.isPrepared || _vp.texture == null;
        bool shouldShow =
            _isPreparing ||
            _waitingFirstFrame ||
            noTexture ||
            (showLoadingWhenStalled && stalled);

        if (shouldShow)
        {
            string reason = GetLoadingReason(noTexture, stalled);
            MarkLoadingCandidate(reason);
        }
        else
        {
            _loadingCandidateTimer = 0f;
            SetLoadingVisible(false, "video ready");
        }
    }

    private string GetLoadingReason(bool noTexture, bool stalled)
    {
        if (_isPreparing)
            return "preparing";

        if (_waitingFirstFrame)
            return "waiting first frame";

        if (noTexture)
            return "no texture";

        if (stalled)
            return "stalled";

        return "unknown";
    }

    private void MarkLoadingCandidate(string reason)
    {
        _loadingCandidateTimer += Time.unscaledDeltaTime;

        if (_loadingCandidateTimer < showLoadingDelay)
            return;

        SetLoadingVisible(true, reason);
    }

    private void SetLoadingVisible(bool visible, string reason)
    {
        if (_loadingVisible == visible && _lastLoadingReason == reason)
            return;

        _loadingVisible = visible;
        _lastLoadingReason = reason;

        if (visible)
        {
            Log($"[VideoLoadingHandler] Show loading: {reason}");

            LoadingUI.Show(
                60f,
                NetworkErrorMessage,
                NetworkErrorHeader
            );
        }
        else
        {
            Log($"[VideoLoadingHandler] Hide loading: {reason}");
            LoadingUI.Hide();
        }
    }

    private void HideLoadingIfVisible()
    {
        if (!_loadingVisible)
            return;

        _loadingVisible = false;
        _lastLoadingReason = null;
        LoadingUI.Hide();
    }

    private void TryAutoResumeFromExternalChange()
    {
        if (_vp == null)
            return;

        string currentUrl = _vp.url;

        bool hasUrl = !string.IsNullOrEmpty(currentUrl);
        bool urlChanged = hasUrl && currentUrl != _lastUrl;

        if (urlChanged)
        {
            Log("[VideoLoadingHandler] External URL changed.");

            _lastUrl = currentUrl;
            _cancelled = false;
            _popupShown = false;
            _suppressLoadingUI = false;

            _waitingFirstFrame = true;
            _isPreparing = !_vp.isPrepared;

            _stallTimer = 0f;
            _lastFrame = -1;
            _loadingCandidateTimer = 0f;

            if (_vp != null)
                _vp.sendFrameReadyEvents = true;
        }

        if (_suppressLoadingUI)
        {
            bool isActuallyRunning =
                _vp.isPrepared ||
                _vp.isPlaying ||
                _vp.texture != null;

            if (isActuallyRunning)
            {
                _suppressLoadingUI = false;
                _cancelled = false;
                _popupShown = false;

                _waitingFirstFrame = _vp.texture == null;
                _stallTimer = 0f;
                _lastFrame = -1;
                _loadingCandidateTimer = 0f;
            }
        }
    }

    public void LoadVideo(string url, bool autoplay = true)
    {
        if (_vp == null)
            return;

        if (string.IsNullOrEmpty(url))
        {
            Debug.LogWarning("[VideoLoadingHandler] URL rỗng -> không loading.");
            HideLoadingIfVisible();
            return;
        }

        StopPrepareRoutineIfAny();
        StopRetryRoutineIfAny();

        _suppressLoadingUI = false;
        _cancelled = false;
        _popupShown = false;

        _lastUrl = url;
        _lastAutoplay = autoplay;
        _retryCount = 0;

        _waitingFirstFrame = true;
        _isPreparing = true;
        _stallTimer = 0f;
        _lastFrame = -1;
        _loadingCandidateTimer = 0f;

        _vp.sendFrameReadyEvents = true;

        _vp.Stop();
        _vp.clip = null;
        _vp.source = VideoSource.Url;
        _vp.url = url;

        SetLoadingVisible(true, "LoadVideo");

        _vp.Prepare();
        _prepareRoutine = StartCoroutine(PrepareTimeout(10f, autoplay));
    }

    public void Seek(double time)
    {
        if (_vp == null)
            return;

        if (!_vp.isPrepared)
            return;

        _suppressLoadingUI = false;
        _cancelled = false;

        time = Mathf.Clamp((float)time, 0, (float)_vp.length);

        _waitingFirstFrame = true;
        _stallTimer = 0f;
        _lastFrame = -1;
        _loadingCandidateTimer = 0f;

        _vp.sendFrameReadyEvents = true;

        SetLoadingVisible(true, "seek");

        _vp.time = time;
        _vp.Play();
    }

    private void OnPrepared(VideoPlayer source)
    {
        _isPreparing = false;

        if (source != null)
        {
            source.sendFrameReadyEvents = true;
        }

        Log("[VideoLoadingHandler] Prepared.");
    }

    private void OnStarted(VideoPlayer source)
    {
        if (source == null)
            return;

        if (source.texture == null)
            _waitingFirstFrame = true;
    }

    private void OnFrameReady(VideoPlayer source, long frameIdx)
    {
        _waitingFirstFrame = false;
        _lastFrame = frameIdx;
        _stallTimer = 0f;
        _loadingCandidateTimer = 0f;

        SetLoadingVisible(false, "first frame ready");

        StopRetryRoutineIfAny();

        _retryCount = 0;
        _popupShown = false;

        if (source != null)
            source.sendFrameReadyEvents = false;
    }

    private void OnSeekCompleted(VideoPlayer source)
    {
        if (source == null)
            return;

        _waitingFirstFrame = source.texture == null;
        _stallTimer = 0f;
        _lastFrame = -1;

        source.sendFrameReadyEvents = true;
    }

    private void OnLoopEnd(VideoPlayer source)
    {
        if (source == null)
            return;

        if (!source.isLooping)
        {
            _waitingFirstFrame = false;
            SetLoadingVisible(false, "loop end");
            return;
        }

        _waitingFirstFrame = true;
        _stallTimer = 0f;
        _lastFrame = -1;
        _loadingCandidateTimer = 0f;

        source.sendFrameReadyEvents = true;

        MarkLoadingCandidate("loop restart");
    }

    private void OnError(VideoPlayer source, string message)
    {
        Debug.LogError($"[VideoLoadingHandler] Video error: {message}");

        if (_cancelled)
            return;

        _waitingFirstFrame = true;
        _isPreparing = false;
        _loadingCandidateTimer = 0f;

        if (_vp != null && !string.IsNullOrEmpty(_vp.url))
            _lastUrl = _vp.url;

        if (!enableRetryOnError)
        {
            // Không tự reload để tránh đụng với CourseListView/proxy fallback.
            // Chỉ hiện popup sau lỗi thật.
            ShowNetworkErrorPopupOnce();
            return;
        }

        if (string.IsNullOrEmpty(_lastUrl))
        {
            ShowNetworkErrorPopupOnce();
            return;
        }

        if (_retryRoutine == null)
            _retryRoutine = StartCoroutine(RetryLoadRoutine());
    }

    private IEnumerator RetryLoadRoutine()
    {
        while (!_cancelled && _retryCount < maxRetryCount)
        {
            _retryCount++;

            SetLoadingVisible(true, "retry wait");

            float t = 0f;

            while (!_cancelled && t < retryIntervalSeconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }

            if (_cancelled)
                yield break;

            TryReloadInternal();

            float waitAfterTry = 2f;
            float w = 0f;

            while (!_cancelled && w < waitAfterTry)
            {
                if (_vp != null && _vp.isPrepared && _vp.texture != null)
                {
                    if (_lastAutoplay && !_vp.isPlaying)
                        _vp.Play();

                    _retryRoutine = null;
                    yield break;
                }

                w += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        _retryRoutine = null;
        ShowNetworkErrorPopupOnce();
    }

    private void TryReloadInternal()
    {
        if (_vp == null)
            return;

        if (string.IsNullOrEmpty(_lastUrl))
            return;

        StopPrepareRoutineIfAny();

        _vp.Stop();
        _vp.clip = null;
        _vp.source = VideoSource.Url;
        _vp.url = _lastUrl;
        _vp.sendFrameReadyEvents = true;

        _waitingFirstFrame = true;
        _isPreparing = true;
        _stallTimer = 0f;
        _lastFrame = -1;
        _loadingCandidateTimer = 0f;

        SetLoadingVisible(true, "retry prepare");

        _vp.Prepare();
        _prepareRoutine = StartCoroutine(PrepareTimeout(10f, _lastAutoplay));
    }

    private IEnumerator PrepareTimeout(float seconds, bool autoplay)
    {
        float t = 0f;

        while (!_cancelled && _isPreparing && _vp != null && !_vp.isPrepared && t < seconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        _prepareRoutine = null;

        if (_cancelled || _vp == null)
            yield break;

        if (_vp.isPrepared)
        {
            _isPreparing = false;

            if (autoplay && !_vp.isPlaying)
                _vp.Play();

            yield break;
        }

        _isPreparing = false;

        if (enableRetryOnError && _retryRoutine == null)
            _retryRoutine = StartCoroutine(RetryLoadRoutine());
    }

    private void StopPrepareRoutineIfAny()
    {
        if (_prepareRoutine == null)
            return;

        StopCoroutine(_prepareRoutine);
        _prepareRoutine = null;
    }

    private void StopRetryRoutineIfAny()
    {
        if (_retryRoutine == null)
            return;

        StopCoroutine(_retryRoutine);
        _retryRoutine = null;
    }

    private void ShowNetworkErrorPopupOnce()
    {
        if (_popupShown)
            return;

        _popupShown = true;
        _waitingFirstFrame = false;
        _isPreparing = false;

        _suppressLoadingUI = true;
        HideLoadingIfVisible();

        LoadingUI.ShowErrorPopup(
            NetworkErrorMessage,
            NetworkErrorHeader,
            onReturn: () =>
            {
                CancelLoadingAndStop();
            }
        );
    }

    public void CancelLoadingAndStop()
    {
        _cancelled = true;

        StopRetryRoutineIfAny();
        StopPrepareRoutineIfAny();

        if (_vp != null)
        {
            _vp.sendFrameReadyEvents = false;
            _vp.Stop();
            _vp.url = "";
            _vp.clip = null;
        }

        ResetRuntimeState();

        _suppressLoadingUI = true;

        HideLoadingIfVisible();
    }

    private void ResetRuntimeState()
    {
        _waitingFirstFrame = false;
        _isPreparing = false;
        _stallTimer = 0f;
        _lastFrame = -1;
        _loadingCandidateTimer = 0f;
    }

    private void BindEvents()
    {
        if (_eventsBound || _vp == null)
            return;

        _vp.prepareCompleted += OnPrepared;
        _vp.loopPointReached += OnLoopEnd;
        _vp.errorReceived += OnError;
        _vp.started += OnStarted;
        _vp.frameReady += OnFrameReady;
        _vp.seekCompleted += OnSeekCompleted;

        _eventsBound = true;
    }

    private void UnbindEvents()
    {
        if (!_eventsBound || _vp == null)
            return;

        _vp.prepareCompleted -= OnPrepared;
        _vp.loopPointReached -= OnLoopEnd;
        _vp.errorReceived -= OnError;
        _vp.started -= OnStarted;
        _vp.frameReady -= OnFrameReady;
        _vp.seekCompleted -= OnSeekCompleted;

        _eventsBound = false;
    }

    private void Log(string message)
    {
        if (debugLog)
            Debug.Log(message, gameObject);
    }
}