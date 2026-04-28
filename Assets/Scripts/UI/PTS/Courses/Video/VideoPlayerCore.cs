using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Video;
using Object = UnityEngine.Object;

public class VideoPlayerCore : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [Header("Proxy")]
    [SerializeField] private LocalProxyAutoBoot proxyBoot;

    // Events
    public event Action<RenderTexture> OnTextureReady;
    public event Action<VideoPlayerModel> OnStateChanged;
    public event Action<Texture> OnBannerLoaded;
    public event Action OnVideoFinished;
    public event Action<string> OnError;

    // Config
    private int _bannerResizeMaxSize = 512;
    private int _bannerRequestTimeout = 8;

    // Internal
    [SerializeField] private VideoPlayer _videoPlayer;
    [SerializeField] private RenderTexture _renderTexture;
    [SerializeField] private VideoPlayerModel _model = new VideoPlayerModel();

    private bool _didSetup;
    private bool _isPreparing;

    private Coroutine _loadBannerCoroutine;
    private UnityWebRequest _activeBannerRequest;
    private Texture2D _runtimeBannerTexture;

    // ─────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────

    public VideoPlayerModel GetCurrentModel() => _model;

    public void LoadAndPlay(string videoUrl, string bannerUrl)
    {
        ResolveReferences();

        // Load banner trước nếu có
        if (!string.IsNullOrWhiteSpace(bannerUrl))
            StartLoadBanner(bannerUrl);

        // Load video
        if (string.IsNullOrWhiteSpace(videoUrl))
        {
            Debug.LogWarning("[VideoPlayerCore] videoUrl is empty.");
            return;
        }

        StopPlaybackInternal(clearSource: false);

        string finalUrl = BuildPlayableUrl(videoUrl);

        _model.VideoUrl = finalUrl;
        _model.IsPrepared = false;
        _model.IsPlaying = false;
        _isPreparing = true;

        EnsureRenderTexture();

        _videoPlayer.source = VideoSource.Url;
        _videoPlayer.clip = null;
        _videoPlayer.url = finalUrl;
        _videoPlayer.targetTexture = _renderTexture;

        _videoPlayer.Prepare();
    }

    public void Pause()
    {
        if (_videoPlayer != null && _videoPlayer.isPlaying)
            _videoPlayer.Pause();

        if (audioSource != null && audioSource.isPlaying)
            audioSource.Pause();

        _model.IsPlaying = false;
        OnStateChanged?.Invoke(_model);
    }
    [ContextMenu("Resume")]
    public void Resume()
    {
        if (_videoPlayer != null && _videoPlayer.isPrepared)
            _videoPlayer.Play();

        if (audioSource != null)
            audioSource.UnPause();

        _model.IsPlaying = _videoPlayer != null && _videoPlayer.isPlaying;
        OnStateChanged?.Invoke(_model);
    }

    public void Seek(float normalizedTime)
    {
        if (_videoPlayer == null || !_videoPlayer.isPrepared) return;

        double targetTime = normalizedTime * _videoPlayer.length;
        _videoPlayer.time = targetTime;

        _model.CurrentTime = (float)targetTime;
        OnStateChanged?.Invoke(_model);
    }

    public void SetVolume(float volume)
    {
        if (audioSource == null) return;

        audioSource.volume = Mathf.Clamp01(volume);
        _model.Volume = audioSource.volume;
        OnStateChanged?.Invoke(_model);
    }

    public void Stop()
    {
        StopPlaybackInternal(clearSource: true);

        _model.IsPlaying = false;
        _model.IsPrepared = false;
        _model.CurrentTime = 0f;

        OnStateChanged?.Invoke(_model);
    }

    // ─────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────

    private void Awake()
    {
        ResolveReferences();
    }

    private void Update()
    {
        if (_videoPlayer == null || !_videoPlayer.isPrepared) return;

        _model.CurrentTime = (float)_videoPlayer.time;
        _model.IsPlaying = _videoPlayer.isPlaying;

        OnStateChanged?.Invoke(_model);
    }

    private void OnDestroy()
    {
        if (_videoPlayer != null)
        {
            _videoPlayer.prepareCompleted -= OnPrepared;
            _videoPlayer.loopPointReached -= OnLoopPointReached;
            _videoPlayer.errorReceived -= OnVideoError;
        }

        CancelBannerLoad();
        ReleaseRenderTexture();
        ReleaseRuntimeBannerTexture();
    }

    // ─────────────────────────────────────────
    // Setup
    // ─────────────────────────────────────────

    private void ResolveReferences()
    {
        if (_videoPlayer == null)
            _videoPlayer = GetComponent<VideoPlayer>() ?? gameObject.AddComponent<VideoPlayer>();

        if (audioSource == null)
            audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();

        if (proxyBoot == null)
            proxyBoot = FindAnyObjectByType<LocalProxyAutoBoot>();

        if (!_didSetup)
        {
            SetupVideoPlayer();
            _didSetup = true;
        }
    }

    private void SetupVideoPlayer()
    {
        if (_videoPlayer == null) return;

        _videoPlayer.playOnAwake = false;
        _videoPlayer.waitForFirstFrame = true;
        _videoPlayer.skipOnDrop = true;
        _videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        _videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            _videoPlayer.EnableAudioTrack(0, true);
            _videoPlayer.SetTargetAudioSource(0, audioSource);
        }

        _videoPlayer.prepareCompleted -= OnPrepared;
        _videoPlayer.loopPointReached -= OnLoopPointReached;
        _videoPlayer.errorReceived -= OnVideoError;

        _videoPlayer.prepareCompleted += OnPrepared;
        _videoPlayer.loopPointReached += OnLoopPointReached;
        _videoPlayer.errorReceived += OnVideoError;
    }

    // ─────────────────────────────────────────
    // Video Callbacks
    // ─────────────────────────────────────────

    private void OnPrepared(VideoPlayer vp)
    {
        _isPreparing = false;

        _model.IsPrepared = true;
        _model.Duration = (float)vp.length;

        vp.Play();

        if (audioSource != null && !audioSource.isPlaying)
            audioSource.Play();

        _model.IsPlaying = true;

        OnTextureReady?.Invoke(_renderTexture);
        OnStateChanged?.Invoke(_model);
    }

    private void OnLoopPointReached(VideoPlayer vp)
    {
        _model.IsPlaying = false;
        _model.CurrentTime = 0f;

        OnVideoFinished?.Invoke();
        OnStateChanged?.Invoke(_model);
    }

    private void OnVideoError(VideoPlayer vp, string message)
    {
        _isPreparing = false;
        _model.IsPlaying = false;

        Debug.LogError($"[VideoPlayerCore] Error: {message}");
        OnError?.Invoke(message);
        OnStateChanged?.Invoke(_model);
    }

    // ─────────────────────────────────────────
    // Banner
    // ─────────────────────────────────────────

    private void StartLoadBanner(string url)
    {
        CancelBannerLoad();
        _loadBannerCoroutine = StartCoroutine(LoadBannerRoutine(url));
    }

    private IEnumerator LoadBannerRoutine(string url)
    {
        using (var req = UnityWebRequestTexture.GetTexture(url, true))
        {
            _activeBannerRequest = req;
            req.timeout = _bannerRequestTimeout;

            yield return req.SendWebRequest();

            if (_activeBannerRequest != req) yield break;

            _activeBannerRequest = null;
            _loadBannerCoroutine = null;

#if UNITY_2020_2_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogWarning($"[VideoPlayerCore] Banner load failed: {req.error}");
                yield break;
            }

            Texture2D downloaded = DownloadHandlerTexture.GetContent(req);
            if (downloaded == null) yield break;

            Texture2D resized = ResizeTexture(downloaded, _bannerResizeMaxSize);
            if (resized != downloaded) Object.Destroy(downloaded);
            if (resized == null) yield break;

            ReleaseRuntimeBannerTexture();
            _runtimeBannerTexture = resized;
            _runtimeBannerTexture.name = "VideoCore_Banner";

            OnBannerLoaded?.Invoke(_runtimeBannerTexture);
        }
    }

    // ─────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────

    private string BuildPlayableUrl(string rawUrl)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (proxyBoot == null)
            proxyBoot = FindAnyObjectByType<LocalProxyAutoBoot>();

        if (proxyBoot != null)
            return proxyBoot.GetPlayableUrl(rawUrl);
#endif
        return rawUrl;
    }

    private void EnsureRenderTexture()
    {
        // int width = Mathf.Max(Screen.width, 1920);
        // int height = Mathf.Max(Screen.height, 1080);
        int width = 1920;
        int height = 1080;

        bool needCreate = _renderTexture == null
                          || _renderTexture.width != width
                          || _renderTexture.height != height;

        if (!needCreate) return;

        ReleaseRenderTexture();

        _renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
        {
            name = "VideoCore_RT"
        };
        _renderTexture.Create();
    }

    private void StopPlaybackInternal(bool clearSource)
    {
        if (_videoPlayer != null)
        {
            try { _videoPlayer.Stop(); } catch { }

            if (clearSource)
            {
                _videoPlayer.clip = null;
                _videoPlayer.url = string.Empty;
            }
        }

        if (audioSource != null)
        {
            try { audioSource.Stop(); } catch { }
        }

        _isPreparing = false;
    }

    private Texture2D ResizeTexture(Texture2D source, int maxSize)
    {
        if (source == null || maxSize <= 0) return source;

        int srcW = source.width;
        int srcH = source.height;

        if (srcW <= maxSize && srcH <= maxSize) return source;

        float ratio = srcW >= srcH ? (float)maxSize / srcW : (float)maxSize / srcH;
        int dstW = Mathf.Max(2, Mathf.RoundToInt(srcW * ratio));
        int dstH = Mathf.Max(2, Mathf.RoundToInt(srcH * ratio));

        RenderTexture rt = RenderTexture.GetTemporary(dstW, dstH, 0, RenderTextureFormat.ARGB32);
        RenderTexture prev = RenderTexture.active;

        Graphics.Blit(source, rt);
        RenderTexture.active = rt;

        Texture2D result = new Texture2D(dstW, dstH, TextureFormat.RGBA32, false);
        result.ReadPixels(new Rect(0, 0, dstW, dstH), 0, 0);
        result.Apply(false, false);

        RenderTexture.active = prev;
        RenderTexture.ReleaseTemporary(rt);

        return result;
    }

    private void CancelBannerLoad()
    {
        if (_loadBannerCoroutine != null)
        {
            StopCoroutine(_loadBannerCoroutine);
            _loadBannerCoroutine = null;
        }

        if (_activeBannerRequest != null)
        {
            try { _activeBannerRequest.Abort(); } catch { }
            _activeBannerRequest.Dispose();
            _activeBannerRequest = null;
        }
    }

    private void ReleaseRenderTexture()
    {
        if (_renderTexture == null) return;

        if (_videoPlayer != null && _videoPlayer.targetTexture == _renderTexture)
            _videoPlayer.targetTexture = null;

        _renderTexture.Release();
        Destroy(_renderTexture);
        _renderTexture = null;
    }

    private void ReleaseRuntimeBannerTexture()
    {
        if (_runtimeBannerTexture == null) return;

        Object.Destroy(_runtimeBannerTexture);
        _runtimeBannerTexture = null;
    }
    
    public RenderTexture GetRenderTexture() => _renderTexture;

}