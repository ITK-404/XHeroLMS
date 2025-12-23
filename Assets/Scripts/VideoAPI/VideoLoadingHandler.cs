using System.Collections;
using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class VideoLoadingHandler : MonoBehaviour
{
    [Tooltip("Ngưỡng coi là lag/stall nếu frame không đổi trong (giây) khi đang phát")]
    public float stallThreshold = 0.35f;

    [Header("Retry when network lost")]
    [Tooltip("Mỗi bao lâu retry (giây)")]
    public float retryIntervalSeconds = 10f;

    [Tooltip("Tối đa số lần retry")]
    public int maxRetryCount = 5;

    private VideoPlayer _vp;
    private bool _waitingFirstFrame;
    private bool _isPreparing;

    private long _lastFrame = -1;
    private float _stallTimer;
    private Coroutine _prepareRoutine;
    private bool _cancelled;

    // ===== Retry state =====
    private string _lastUrl;
    private bool _lastAutoplay = true;
    private int _retryCount;
    private Coroutine _retryRoutine;
    private bool _popupShown;

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
            _waitingFirstFrame = true;
            LoadingUI.Show(
                timeoutSeconds: 60f,
                timeoutMessage: "Không thể tải nội dung.\nVui lòng kiểm tra kết nối mạng hoặc thử lại.",
                timeoutHeader:  "Lỗi Mạng"
            );
        }
    }

    void Update()
    {
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

        if (shouldShow) LoadingUI.Show(
            timeoutSeconds: 60f,
            timeoutMessage: "Không thể tải nội dung.\nVui lòng kiểm tra kết nối mạng hoặc thử lại.",
            timeoutHeader:  "Lỗi Mạng"
        );
        else LoadingUI.Hide();
    }

    public void LoadVideo(string url, bool autoplay = true)
    {
        if (string.IsNullOrEmpty(url))
        {
            Debug.LogWarning("[VideoLoadingHandler] URL rỗng -> không loading.");
            LoadingUI.Hide();
            return;
        }

        // lưu để retry
        _lastUrl = url;
        _lastAutoplay = autoplay;
        _retryCount = 0;
        _popupShown = false;

        StopRetryRoutineIfAny();

        _cancelled = false;
        _vp.url = url;

        _waitingFirstFrame = true;
        _isPreparing = true;
        _stallTimer = 0f;
        _lastFrame = -1;

        LoadingUI.Show(
            timeoutSeconds: 60f,
            timeoutMessage: "Không thể tải nội dung.\nVui lòng kiểm tra kết nối mạng hoặc thử lại.",
            timeoutHeader: "Lỗi Mạng"
        );

        _vp.Prepare();
        _prepareRoutine = StartCoroutine(PrepareTimeout(10f, autoplay));
    }

    public void Seek(double time)
    {
        if (!_vp.isPrepared) return;

        time = Mathf.Clamp((float)time, 0, (float)_vp.length);
        _waitingFirstFrame = true;
        _stallTimer = 0f;
        _lastFrame  = -1;

        LoadingUI.Show(
            timeoutSeconds: 60f,
            timeoutMessage: "Không thể tải nội dung.\nVui lòng kiểm tra kết nối mạng hoặc thử lại.",
            timeoutHeader:  "Lỗi Mạng"
        );

        _vp.time = time;
        _vp.Play();
    }

    private void OnPrepared(VideoPlayer source)
    {
        _isPreparing = false;
    }

    private void OnStarted(VideoPlayer source)
    {
        // Giữ loading tới khi có frame thật
    }

    private void OnFrameReady(VideoPlayer source, long frameIdx)
    {
        // Có frame rồi => coi như phục hồi thành công => dừng retry
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

        LoadingUI.Show(
            timeoutSeconds: 60f,
            timeoutMessage: "Không thể tải nội dung.\nVui lòng kiểm tra kết nối mạng hoặc thử lại.",
            timeoutHeader:  "Lỗi Mạng"
        );
    }

    private void OnError(VideoPlayer source, string message)
    {
        Debug.LogError($"[VideoLoadingHandler] Video error: {message}");

        // Đang cancel thì bỏ qua
        if (_cancelled) return;

        // Nếu không có URL để retry thì popup luôn
        if (string.IsNullOrEmpty(_lastUrl))
        {
            ShowNetworkErrorPopupOnce();
            return;
        }

        // Bắt đầu retry thay vì popup ngay
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

            // Hiển thị loading trong lúc retry
            LoadingUI.Show(
                timeoutSeconds: 60f,
                timeoutMessage: "Không thể tải nội dung.\nVui lòng kiểm tra kết nối mạng hoặc thử lại.",
                timeoutHeader:  "Lỗi Mạng"
            );

            Debug.Log($"[VideoLoadingHandler] Retry {_retryCount}/{maxRetryCount} after {retryIntervalSeconds}s...");

            // đợi 10s
            float t = 0f;
            while (!_cancelled && t < retryIntervalSeconds)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            if (_cancelled) yield break;

            // thử load lại
            TryReloadInternal();

            // Chờ một chút để nó kịp Prepare/Play.
            // Nếu thành công, OnFrameReady sẽ StopRetryRoutineIfAny().
            float waitAfterTry = 2f;
            float w = 0f;
            while (!_cancelled && w < waitAfterTry)
            {
                // nếu đã có frame / đang play => coi như OK (OnFrameReady thường sẽ chạy)
                if (_vp != null && _vp.isPrepared && _vp.texture != null)
                {
                    // nếu autoplay thì play, còn không thì chỉ prepare xong
                    if (_lastAutoplay && !_vp.isPlaying) _vp.Play();
                    // để OnFrameReady dọn trạng thái; nhưng ta cũng có thể break
                    break;
                }

                w += Time.unscaledDeltaTime;
                yield return null;
            }

            // nếu routine bị stop bởi OnFrameReady thì _retryRoutine sẽ null (vì StopRetryRoutineIfAny() set null)
            if (_retryRoutine == null) yield break;
        }

        // hết retry mà chưa thành công => popup
        _retryRoutine = null;
        ShowNetworkErrorPopupOnce();
    }

    private void TryReloadInternal()
    {
        if (_vp == null) return;

        // Dừng prepare timeout cũ
        if (_prepareRoutine != null)
        {
            StopCoroutine(_prepareRoutine);
            _prepareRoutine = null;
        }

        _vp.Stop(); // reset state
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

        LoadingUI.ShowErrorPopup(
            "Không thể tải nội dung.\nVui lòng kiểm tra kết nối mạng hoặc thử lại.",
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
            _vp.Stop();

        _waitingFirstFrame = false;
        _isPreparing = false;
        _stallTimer = 0f;
        _lastFrame = -1;

        LoadingUI.Hide();

        this.enabled = false;
    }
}
