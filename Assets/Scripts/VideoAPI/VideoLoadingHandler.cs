using System.Collections;
using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class VideoLoadingHandler : MonoBehaviour
{
    public float stallThreshold = 0.35f;

    [Header("Retry when network lost")]
    public float retryIntervalSeconds = 10f;
    public int maxRetryCount = 5;

    private VideoPlayer _vp;
    private bool _waitingFirstFrame;
    private bool _isPreparing;

    private long _lastFrame = -1;
    private float _stallTimer;
    private Coroutine _prepareRoutine;
    private bool _cancelled;

    private string _lastUrl;
    private bool _lastAutoplay = true;
    private int _retryCount;
    private Coroutine _retryRoutine;
    private bool _popupShown;

    // chặn Update spam LoadingUI sau khi user cancel/popup
    private bool _suppressLoadingUI;

    void Awake()
    {
        _vp = GetComponent<VideoPlayer>();
        _vp.playOnAwake = false;
        _vp.sendFrameReadyEvents = true;

        _vp.prepareCompleted += OnPrepared;
        _vp.loopPointReached += OnLoopEnd;
        _vp.errorReceived    += OnError;
        _vp.started          += OnStarted;
        _vp.frameReady       += OnFrameReady;
        _vp.seekCompleted    += OnSeekCompleted;
    }

    void OnDestroy()
    {
        if (_vp == null) return;
        _vp.prepareCompleted -= OnPrepared;
        _vp.loopPointReached -= OnLoopEnd;
        _vp.errorReceived    -= OnError;
        _vp.started          -= OnStarted;
        _vp.frameReady       -= OnFrameReady;
        _vp.seekCompleted    -= OnSeekCompleted;
    }

    void Start()
    {
        if (_vp.clip != null || !string.IsNullOrEmpty(_vp.url))
        {
            _suppressLoadingUI = false;
            _waitingFirstFrame = true;
            LoadingUI.Show(60f,
                "Không thể tải nội dung.\nVui lòng kiểm tra kết nối mạng hoặc\n thử lại sau.",
                "Lỗi Mạng"
            );
        }
    }

    void Update()
    {
        TryAutoResumeFromExternalChange();

        if (_suppressLoadingUI)
        {
            LoadingUI.Hide();
            return;
        }

        if (_vp.clip == null && string.IsNullOrEmpty(_vp.url))
        {
            LoadingUI.Hide();
            return;
        }

        bool playing = _vp.isPrepared && _vp.isPlaying;
        bool stalled = false;

        if (playing)
        {
            if (_vp.frame == _lastFrame)
            {
                _stallTimer += Time.unscaledDeltaTime;
                if (_stallTimer >= stallThreshold) stalled = true;
            }
            else
            {
                _stallTimer = 0f;
                _lastFrame = _vp.frame;
            }
        }
        else
        {
            _stallTimer = 0f;
            _lastFrame  = -1;
        }

        bool noTexture  = !_vp.isPrepared || _vp.texture == null;
        bool shouldShow = noTexture || _waitingFirstFrame || stalled;

        if (shouldShow)
        {
            LoadingUI.Show(60f,
                "Không thể tải nội dung.\nVui lòng kiểm tra kết nối mạng hoặc\n thử lại sau.",
                "Lỗi Mạng"
            );
        }
        else
        {
            LoadingUI.Hide();
        }
    }

    private void TryAutoResumeFromExternalChange()
    {
        if (_vp == null) return;

        // nếu đang suppress mà thấy có dấu hiệu phiên mới => bật lại
        if (_suppressLoadingUI)
        {
            bool hasNewUrl = !string.IsNullOrEmpty(_vp.url) && _vp.url != _lastUrl;
            bool isActuallyRunning = _vp.isPrepared || _vp.isPlaying || _vp.texture != null;

            if (hasNewUrl || isActuallyRunning)
            {
                _suppressLoadingUI = false;
                _cancelled = false;
                _popupShown = false;

                _waitingFirstFrame = true;
                _stallTimer = 0f;
                _lastFrame = -1;

                // cập nhật lastUrl để retry đúng URL mới
                if (!string.IsNullOrEmpty(_vp.url))
                    _lastUrl = _vp.url;
            }
        }
    }

    public void LoadVideo(string url, bool autoplay = true)
    {
        _suppressLoadingUI = false;
        _cancelled = false;

        if (string.IsNullOrEmpty(url))
        {
            Debug.LogWarning("[VideoLoadingHandler] URL rỗng -> không loading.");
            LoadingUI.Hide();
            return;
        }

        _lastUrl = url;
        _lastAutoplay = autoplay;
        _retryCount = 0;
        _popupShown = false;

        StopRetryRoutineIfAny();

        if (_prepareRoutine != null)
        {
            StopCoroutine(_prepareRoutine);
            _prepareRoutine = null;
        }

        _waitingFirstFrame = true;
        _isPreparing = true;
        _stallTimer = 0f;
        _lastFrame = -1;

        _vp.Stop();
        _vp.clip = null;
        _vp.url  = url;

        LoadingUI.Show(60f,
            "Không thể tải nội dung.\nVui lòng kiểm tra kết nối mạng hoặc\n thử lại sau.",
            "Lỗi Mạng"
        );

        _vp.Prepare();
        _prepareRoutine = StartCoroutine(PrepareTimeout(10f, autoplay));
    }

    public void Seek(double time)
    {
        if (!_vp.isPrepared) return;

        _suppressLoadingUI = false;

        time = Mathf.Clamp((float)time, 0, (float)_vp.length);
        _waitingFirstFrame = true;
        _stallTimer = 0f;
        _lastFrame  = -1;

        LoadingUI.Show(60f,
            "Không thể tải nội dung.\nVui lòng kiểm tra kết nối mạng hoặc\n thử lại sau.",
            "Lỗi Mạng"
        );

        _vp.time = time;
        _vp.Play();
    }

    private void OnPrepared(VideoPlayer source) => _isPreparing = false;

    private void OnStarted(VideoPlayer source)
    {
        // giữ loading tới khi có frame thật
    }

    private void OnFrameReady(VideoPlayer source, long frameIdx)
    {
        _waitingFirstFrame = false;
        _lastFrame = frameIdx;
        _stallTimer = 0f;
        LoadingUI.Hide();

        StopRetryRoutineIfAny();
        _retryCount = 0;
        _popupShown = false;
    }

    private void OnSeekCompleted(VideoPlayer source) { }

    private void OnLoopEnd(VideoPlayer source)
    {
        if (!source.isLooping)
        {
            _waitingFirstFrame = false;
            LoadingUI.Hide();
            return;
        }

        _waitingFirstFrame = true;
        _stallTimer = 0f;
        _lastFrame  = -1;

        LoadingUI.Show(60f,
            "Không thể tải nội dung.\nVui lòng kiểm tra kết nối mạng hoặc\n thử lại sau.",
            "Lỗi Mạng"
        );
    }

    private void OnError(VideoPlayer source, string message)
    {
        Debug.LogError($"[VideoLoadingHandler] Video error: {message}");

        if (_cancelled) return;

        // nếu url hiện tại khác lastUrl thì cập nhật để retry đúng
        if (!string.IsNullOrEmpty(_vp.url))
            _lastUrl = _vp.url;

        if (string.IsNullOrEmpty(_lastUrl))
        {
            ShowNetworkErrorPopupOnce();
            return;
        }

        _waitingFirstFrame = true;
        _isPreparing = false;

        if (_retryRoutine == null)
            _retryRoutine = StartCoroutine(RetryLoadRoutine());
    }

    private IEnumerator RetryLoadRoutine()
    {
        while (!_cancelled && _retryCount < maxRetryCount)
        {
            _retryCount++;

            LoadingUI.Show(60f,
                "Không thể tải nội dung.\nVui lòng kiểm tra kết nối mạng hoặc\n thử lại sau.",
                "Lỗi Mạng"
            );

            float t = 0f;
            while (!_cancelled && t < retryIntervalSeconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            if (_cancelled) yield break;

            TryReloadInternal();

            float waitAfterTry = 2f;
            float w = 0f;
            while (!_cancelled && w < waitAfterTry)
            {
                if (_vp != null && _vp.isPrepared && _vp.texture != null)
                {
                    if (_lastAutoplay && !_vp.isPlaying) _vp.Play();
                    break;
                }

                w += Time.unscaledDeltaTime;
                yield return null;
            }

            if (_retryRoutine == null) yield break;
        }

        _retryRoutine = null;
        ShowNetworkErrorPopupOnce();
    }

    private void TryReloadInternal()
    {
        if (_vp == null) return;

        if (_prepareRoutine != null)
        {
            StopCoroutine(_prepareRoutine);
            _prepareRoutine = null;
        }

        _vp.Stop();
        _vp.clip = null;
        _vp.url = _lastUrl;

        _waitingFirstFrame = true;
        _isPreparing = true;
        _stallTimer = 0f;
        _lastFrame = -1;

        _vp.Prepare();
        _prepareRoutine = StartCoroutine(PrepareTimeout(10f, _lastAutoplay));
    }

    private IEnumerator PrepareTimeout(float seconds, bool autoplay)
    {
        float t = 0f;
        while (!_cancelled && _isPreparing && !_vp.isPrepared && t < seconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        _prepareRoutine = null;

        if (!_cancelled && _vp.isPrepared && autoplay)
            _vp.Play();
    }

    private void StopRetryRoutineIfAny()
    {
        if (_retryRoutine != null)
        {
            StopCoroutine(_retryRoutine);
            _retryRoutine = null;
        }
    }

    private void ShowNetworkErrorPopupOnce()
    {
        if (_popupShown) return;
        _popupShown = true;

        _waitingFirstFrame = false;

        // popup hiện => chặn Update show loading
        _suppressLoadingUI = true;
        LoadingUI.Hide();

        LoadingUI.ShowErrorPopup(
            "Không thể tải nội dung.\nVui lòng kiểm tra kết nối mạng hoặc\n thử lại sau.",
            "Lỗi Mạng",
            onReturn: () => { CancelLoadingAndStop(); }
        );
    }

    public void CancelLoadingAndStop()
    {
        _cancelled = true;

        StopRetryRoutineIfAny();

        if (_prepareRoutine != null)
        {
            StopCoroutine(_prepareRoutine);
            _prepareRoutine = null;
        }

        if (_vp != null)
        {
            _vp.Stop();
            _vp.url = "";
            _vp.clip = null;
        }

        _waitingFirstFrame = false;
        _isPreparing = false;
        _stallTimer = 0f;
        _lastFrame = -1;

        // chặn Update spam show loading
        _suppressLoadingUI = true;

        LoadingUI.Hide();
    }
}
