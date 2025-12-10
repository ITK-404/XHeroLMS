using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class VideoLoadingHandler : MonoBehaviour
{
    [Tooltip("Ngưỡng coi là lag/stall nếu frame không đổi trong (giây) khi đang phát")]
    public float stallThreshold = 0.35f;

    private VideoPlayer _vp;
    private bool _waitingFirstFrame;
    private bool _isPreparing;

    private long _lastFrame = -1;
    private float _stallTimer;
    private Coroutine _prepareRoutine;
    private bool _cancelled;

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
        // Nếu có clip/url sẵn từ Inspector -> show ngay
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
        // Nếu chưa có clip hoặc url => KHÔNG loading
        if (_vp.clip == null && string.IsNullOrEmpty(_vp.url))
        {
            LoadingUI.Hide();
            return;
        }

        //Stall detector
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

        // Điều kiện hiển/ẩn overlay
        bool noTexture = !_vp.isPrepared || _vp.texture == null;
        bool shouldShow = noTexture || _waitingFirstFrame || stalled;

        if (shouldShow) LoadingUI.Show(
                timeoutSeconds: 60f,
                timeoutMessage: "Không thể tải nội dung.\nVui lòng kiểm tra kết nối mạng hoặc thử lại.",
                timeoutHeader:  "Lỗi Mạng"
            );
        else            LoadingUI.Hide();
    }

    public void LoadVideo(string url, bool autoplay = true)
    {
        if (string.IsNullOrEmpty(url))
        {
            Debug.LogWarning("[VideoLoadingHandler] URL rỗng -> không loading.");
            LoadingUI.Hide();
            return;
        }

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

        // LƯU lại coroutine để còn Stop
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
        _waitingFirstFrame = false;
        _lastFrame = frameIdx;
        _stallTimer = 0f;
        LoadingUI.Hide();
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
    Debug.LogError($"[VideoLoadingHandler] Video error: {message}, url={source.url}");
    _waitingFirstFrame = false;

    // Chỉ test trên device cho đỡ ồn log Editor
#if UNITY_ANDROID && !UNITY_EDITOR
    if (!string.IsNullOrEmpty(source.url))
        StartCoroutine(TestVideoUrlRoutine(source.url));
#endif

    LoadingUI.ShowErrorPopup(
        "Không thể tải nội dung.\nVui lòng kiểm tra kết nối mạng hoặc thử lại.",
        "Lỗi Mạng",
        onReturn: () => { CancelLoadingAndStop(); }
    );
}

private IEnumerator TestVideoUrlRoutine(string url)
{
    using (var req = UnityWebRequest.Head(url))
    {
        req.redirectLimit = 8;
        req.timeout = 15;

        yield return req.SendWebRequest();

        Debug.Log("[TestVideoUrl]Result   = " + req.result);
        Debug.Log("[TestVideoUrl]Error    = " + req.error);
        Debug.Log("[TestVideoUrl]Code     = " + req.responseCode);
        Debug.Log("[TestVideoUrl]Type     = " + req.GetResponseHeader("Content-Type"));
        Debug.Log("[TestVideoUrl]Location = " + req.GetResponseHeader("Location"));
    }
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
    public void CancelLoadingAndStop()
    {
        _cancelled = true;

        // Dừng coroutine chuẩn bị nếu còn
        if (_prepareRoutine != null)
        {
            StopCoroutine(_prepareRoutine);
            _prepareRoutine = null;
        }

        // Dừng video
        if (_vp != null)
            _vp.Stop();

        // Reset state + tắt loading
        _waitingFirstFrame = false;
        _isPreparing = false;
        _stallTimer = 0f;
        _lastFrame = -1;

        LoadingUI.Hide();

        // Optional: tắt luôn handler để nó không chạy Update nữa
        this.enabled = false;
    }
}

public class PreviewCou
{
    
}
