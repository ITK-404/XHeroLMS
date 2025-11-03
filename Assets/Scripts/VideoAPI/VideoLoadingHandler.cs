using System.Collections;
using UnityEngine;
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
            LoadingUI.Show();
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

        if (shouldShow) LoadingUI.Show();
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

        _vp.url = url;

        _waitingFirstFrame = true;
        _isPreparing = true;
        _stallTimer = 0f;
        _lastFrame = -1;

        LoadingUI.Show();
        _vp.Prepare();

        StartCoroutine(PrepareTimeout(10f, autoplay));
    }

    public void Seek(double time)
    {
        if (!_vp.isPrepared) return;

        time = Mathf.Clamp((float)time, 0, (float)_vp.length);
        _waitingFirstFrame = true;
        _stallTimer = 0f;
        _lastFrame  = -1;

        LoadingUI.Show();
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
        _waitingFirstFrame = true;
        _stallTimer = 0f;
        _lastFrame  = -1;
        LoadingUI.Show();
    }

    private void OnError(VideoPlayer source, string message)
    {
        Debug.LogError($"[VideoLoadingHandler] Video error: {message}");
        _waitingFirstFrame = false;
        LoadingUI.Hide();
    }

    private IEnumerator PrepareTimeout(float seconds, bool autoplay)
    {
        float t = 0f;
        while (_isPreparing && !_vp.isPrepared && t < seconds)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        if (_vp.isPrepared && autoplay)
            _vp.Play();
    }
}

public class PreviewCou
{
    
}
