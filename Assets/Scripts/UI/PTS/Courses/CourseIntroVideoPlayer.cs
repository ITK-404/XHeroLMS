using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.Video;
using Object = UnityEngine.Object;

public class CourseIntroVideoPlayer : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RawImage targetRawImage;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [Header("Proxy")]
    [SerializeField] private LocalProxyAutoBoot proxyBoot;

    // Banner
    private Texture fallbackBannerTexture;
    private bool loadBannerOnEnable = true;
    private int bannerResizeMaxSize = 512;
    private int bannerRequestTimeout = 8;
    private bool preferBannerOverImage = true;
    private bool resetWhenCourseChanges = true;

    private VideoPlayer _videoPlayer;
    private RenderTexture _renderTexture;

    private string _currentUrl;
    private bool _isPrepared;
    private bool _didSetup;
    private bool _hasStartedVideo;
    private bool _isPreparing;

    private Coroutine _loadBannerCoroutine;
    private UnityWebRequest _activeBannerRequest;

    private Texture2D _runtimeBannerTexture;
    private Texture _currentBannerTexture;
    private string _currentBannerUrl;

    private string _observedCourseId;

    private void Awake()
    {
        ResolveReferences(forceSetup: true);
        _observedCourseId = CourseDetailStaticStore.CurrentCourseId;
        ShowFallbackBannerOnly();
    }

    private void OnEnable()
    {
        CourseDetailStaticStore.OnChanged += HandleCourseStoreChanged;

        if (loadBannerOnEnable)
            RefreshBannerFromStore();
    }

    private void OnDisable()
    {
        CourseDetailStaticStore.OnChanged -= HandleCourseStoreChanged;

        StopPlayback();
        ReleaseRenderTexture();
        CancelBannerLoad();
        ShowCurrentBannerOrFallback();
    }

    private void OnDestroy()
    {
        CourseDetailStaticStore.OnChanged -= HandleCourseStoreChanged;

        if (_videoPlayer != null)
        {
            _videoPlayer.errorReceived -= OnVideoError;
            _videoPlayer.prepareCompleted -= OnPrepared;
            _videoPlayer.loopPointReached -= OnLoopPointReached;
        }

        CancelBannerLoad();
        ReleaseRenderTexture();
        ReleaseRuntimeBannerTexture();
    }

    public VideoPlayer GetVideoPlayer()
    {
        ResolveReferences(forceSetup: false);
        return _videoPlayer;
    }

    public AudioSource GetAudioSource()
    {
        ResolveReferences(forceSetup: false);
        return audioSource;
    }

    public bool HasStartedVideo() => _hasStartedVideo;
    public bool IsPrepared() => _videoPlayer != null && _videoPlayer.isPrepared;
    public bool IsPreparing() => _isPreparing;

    private void HandleCourseStoreChanged()
    {
        string newCourseId = CourseDetailStaticStore.CurrentCourseId;

        bool courseChanged = !string.Equals(_observedCourseId, newCourseId, System.StringComparison.Ordinal);

        _observedCourseId = newCourseId;

        if (courseChanged && resetWhenCourseChanges)
        {
            ResetForNewCourse();
            return;
        }

        if (!_hasStartedVideo && loadBannerOnEnable)
            RefreshBannerFromStore();
    }

    public void ResetForNewCourse()
    {
        CancelBannerLoad();
        StopPlaybackInternal(clearSource: true);
        ReleaseRenderTexture();
        ReleaseRuntimeBannerTexture();

        _currentUrl = null;
        _currentBannerUrl = null;
        _currentBannerTexture = fallbackBannerTexture;

        _isPrepared = false;
        _isPreparing = false;
        _hasStartedVideo = false;

        if (targetRawImage != null)
            targetRawImage.texture = _currentBannerTexture != null ? _currentBannerTexture : fallbackBannerTexture;

        if (loadBannerOnEnable)
            RefreshBannerFromStore();
    }

    public void RefreshBannerFromStore()
    {
        ResolveReferences(forceSetup: false);

        string bannerUrl = GetBannerUrlFromStore();

        if (string.IsNullOrWhiteSpace(bannerUrl))
        {
            CancelBannerLoad();
            ReleaseRuntimeBannerTexture();
            _currentBannerUrl = null;
            _currentBannerTexture = fallbackBannerTexture;
            ShowCurrentBannerOrFallback();
            return;
        }

        if (_currentBannerUrl == bannerUrl && _currentBannerTexture != null)
        {
            ShowCurrentBannerOrFallback();
            return;
        }

        CancelBannerLoad();
        ReleaseRuntimeBannerTexture();

        _currentBannerUrl = bannerUrl;
        _currentBannerTexture = fallbackBannerTexture;
        ShowCurrentBannerOrFallback();

        _loadBannerCoroutine = StartCoroutine(LoadBannerRoutine(bannerUrl));
    }

    public void SetFallbackBannerTexture(Texture texture)
    {
        fallbackBannerTexture = texture;

        if (!_hasStartedVideo && _runtimeBannerTexture == null)
        {
            _currentBannerTexture = fallbackBannerTexture;
            ShowCurrentBannerOrFallback();
        }
    }

    public void ShowFallbackBannerOnly()
    {
        _currentBannerTexture = _runtimeBannerTexture != null ? _runtimeBannerTexture : fallbackBannerTexture;
        ShowCurrentBannerOrFallback();
    }

    public void StartPlayFromCurrentSource(RawImage rawImage = null)
    {
        if (rawImage != null)
            targetRawImage = rawImage;

        ResolveReferences(forceSetup: false);

        if (_videoPlayer == null)
        {
            Debug.LogError("[CourseIntroVideoPlayer] Không có VideoPlayer để phát.");
            return;
        }

        if (_hasStartedVideo)
        {
            if (_videoPlayer.isPrepared)
            {
                Resume();
            }
            else if (!_isPreparing)
            {
                _isPreparing = true;
                _videoPlayer.Prepare();
            }

            return;
        }

        _hasStartedVideo = true;

        if (_videoPlayer.source == VideoSource.VideoClip && _videoPlayer.clip != null)
        {
            StopPlaybackInternal(clearSource: false);

            _currentUrl = null;
            _isPrepared = false;
            _isPreparing = true;

            EnsureRenderTexture();

            _videoPlayer.targetTexture = _renderTexture;
            if (targetRawImage != null)
                targetRawImage.texture = _renderTexture;

            _videoPlayer.Prepare();
            return;
        }

        if (_videoPlayer.source == VideoSource.Url && !string.IsNullOrWhiteSpace(_videoPlayer.url))
        {
            PlayResolvedUrl(_videoPlayer.url);
            return;
        }

        string introUrl = CourseDetailStaticStore.GetVideoIntro();
        if (string.IsNullOrWhiteSpace(introUrl))
        {
            _hasStartedVideo = false;
            
            return;
        }

        PlayResolvedUrl(introUrl);
    }

    public void PlayFromStore(RawImage rawImage = null)
    {
        if (rawImage != null)
            targetRawImage = rawImage;

        string introUrl = CourseDetailStaticStore.GetVideoIntro();

        if (string.IsNullOrWhiteSpace(introUrl))
        {
            return;
        }

        PlayUrl(introUrl, targetRawImage);
    }

    public void PlayUrl(string url, RawImage rawImage = null)
    {
        if (rawImage != null)
            targetRawImage = rawImage;

        ResolveReferences(forceSetup: false);

        if (_videoPlayer == null)
        {
            Debug.LogError("[CourseIntroVideoPlayer] Không có VideoPlayer để phát.");
            return;
        }

        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        _hasStartedVideo = true;
        PlayResolvedUrl(url);
    }

    private void ResolveReferences(bool forceSetup)
    {
        if (targetRawImage == null)
        {
            targetRawImage = GetComponent<RawImage>();

            if (targetRawImage == null)
                targetRawImage = GetComponentInChildren<RawImage>(true);

            if (targetRawImage == null)
                targetRawImage = GetComponentInParent<RawImage>(true);
        }

        if (_videoPlayer == null)
        {
            _videoPlayer = GetComponent<VideoPlayer>();

            if (_videoPlayer == null && targetRawImage != null)
                _videoPlayer = targetRawImage.GetComponent<VideoPlayer>();

            if (_videoPlayer == null)
                _videoPlayer = GetComponentInChildren<VideoPlayer>(true);

            if (_videoPlayer == null)
                _videoPlayer = GetComponentInParent<VideoPlayer>(true);
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
                audioSource = GetComponentInChildren<AudioSource>(true);

            if (audioSource == null)
                audioSource = GetComponentInParent<AudioSource>(true);
        }

        if (proxyBoot == null)
            proxyBoot = FindAnyObjectByType<LocalProxyAutoBoot>();

        if (_videoPlayer == null)
        {
            Debug.LogError("[CourseIntroVideoPlayer] Không tìm thấy VideoPlayer trên object hiện tại / parent / children.");
            return;
        }

        if (forceSetup || !_didSetup)
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
            _videoPlayer.EnableAudioTrack(0, true);
            _videoPlayer.SetTargetAudioSource(0, audioSource);
            audioSource.playOnAwake = false;
        }

        _videoPlayer.errorReceived -= OnVideoError;
        _videoPlayer.prepareCompleted -= OnPrepared;
        _videoPlayer.loopPointReached -= OnLoopPointReached;

        _videoPlayer.errorReceived += OnVideoError;
        _videoPlayer.prepareCompleted += OnPrepared;
        _videoPlayer.loopPointReached += OnLoopPointReached;
    }

    private string GetBannerUrlFromStore()
    {
        string bannerUrl = null;
        string imageUrl = null;

        var detail = CourseDetailStaticStore.CurrentDetail;
        var flow = CourseDetailStaticStore.GetCourseFlow();

        if (detail != null)
        {
            if (detail.banner != null && detail.banner.Length > 0)
                bannerUrl = detail.banner[0];

            imageUrl = detail.image;
        }

        if (flow != null)
        {
            if (string.IsNullOrWhiteSpace(bannerUrl) &&
                flow.banner != null &&
                flow.banner.Count > 0)
            {
                bannerUrl = flow.banner[0];
            }

            if (string.IsNullOrWhiteSpace(imageUrl))
                imageUrl = flow.image;
        }

        if (preferBannerOverImage)
            return !string.IsNullOrWhiteSpace(bannerUrl) ? bannerUrl : imageUrl;

        return !string.IsNullOrWhiteSpace(imageUrl) ? imageUrl : bannerUrl;
    }

    private IEnumerator LoadBannerRoutine(string url)
    {
        using (var req = UnityWebRequestTexture.GetTexture(url, true))
        {
            _activeBannerRequest = req;
            req.timeout = bannerRequestTimeout;

            yield return req.SendWebRequest();

            if (_activeBannerRequest != req)
                yield break;

            _activeBannerRequest = null;
            _loadBannerCoroutine = null;

#if UNITY_2020_2_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                yield break;
            }

            Texture2D downloaded = DownloadHandlerTexture.GetContent(req);
            if (downloaded == null)
                yield break;

            downloaded.name = "CourseIntroBanner_Downloaded";

            Texture2D resized = ResizeTexture(downloaded, bannerResizeMaxSize);
            if (resized != downloaded)
                Object.Destroy(downloaded);

            if (resized == null)
                yield break;

            ReleaseRuntimeBannerTexture();

            _runtimeBannerTexture = resized;
            _runtimeBannerTexture.name = "CourseIntroBanner_Runtime";
            _currentBannerTexture = _runtimeBannerTexture;

            if (!_hasStartedVideo && !_isPreparing && targetRawImage != null)
                targetRawImage.texture = _currentBannerTexture;
        }
    }

    private Texture2D ResizeTexture(Texture2D source, int maxSize)
    {
        if (source == null)
            return null;

        if (maxSize <= 0)
            return source;

        int srcW = source.width;
        int srcH = source.height;

        if (srcW <= maxSize && srcH <= maxSize)
            return source;

        float ratio = srcW >= srcH
            ? (float)maxSize / srcW
            : (float)maxSize / srcH;

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

    private void PlayResolvedUrl(string url)
    {
        if (_videoPlayer == null)
        {
            Debug.LogError("[CourseIntroVideoPlayer] _videoPlayer is null.");
            return;
        }

        StopPlaybackInternal(clearSource: false);

        string finalUrl = BuildPlayableUrl(url);
        _currentUrl = finalUrl;
        _isPrepared = false;
        _isPreparing = true;

        EnsureRenderTexture();

        _videoPlayer.source = VideoSource.Url;
        _videoPlayer.clip = null;
        _videoPlayer.url = finalUrl;
        _videoPlayer.targetTexture = _renderTexture;

        if (targetRawImage != null)
            targetRawImage.texture = _renderTexture;

        _videoPlayer.Prepare();
    }

    private string BuildPlayableUrl(string rawUrl)
    {
        string result = rawUrl;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (proxyBoot == null)
            proxyBoot = FindAnyObjectByType<LocalProxyAutoBoot>();

        if (proxyBoot != null)
            result = proxyBoot.GetPlayableUrl(rawUrl);
#endif

        return result;
    }

    private void EnsureRenderTexture()
    {
        int width = Mathf.Max(Screen.width, 1280);
        int height = Mathf.Max(Screen.height, 720);

        bool needCreate = _renderTexture == null ||
                          _renderTexture.width != width ||
                          _renderTexture.height != height;

        if (!needCreate)
            return;

        ReleaseRenderTexture();

        _renderTexture = new RenderTexture(width, height, 0, RenderTextureFormat.ARGB32)
        {
            name = "CourseIntroVideo_RT"
        };
        _renderTexture.Create();

        if (_hasStartedVideo && targetRawImage != null)
            targetRawImage.texture = _renderTexture;
    }

    private void OnPrepared(VideoPlayer vp)
    {
        _isPrepared = true;
        _isPreparing = false;

        if (targetRawImage != null && vp.targetTexture != null)
            targetRawImage.texture = vp.targetTexture;

        vp.Play();

        if (audioSource != null && !audioSource.isPlaying)
            audioSource.Play();
    }

    private void OnLoopPointReached(VideoPlayer vp)
    {
        Debug.Log("[CourseIntroVideoPlayer] Video finished.");
    }

    private void OnVideoError(VideoPlayer vp, string message)
    {
        _isPreparing = false;

        string sourceInfo = vp.source == VideoSource.VideoClip
            ? (vp.clip != null ? vp.clip.name : "<null clip>")
            : _currentUrl;

        Debug.LogError($"[CourseIntroVideoPlayer] Video error: {message} | source={sourceInfo}");
    }

    public void Pause()
    {
        if (_videoPlayer != null && _videoPlayer.isPlaying)
            _videoPlayer.Pause();

        if (audioSource != null && audioSource.isPlaying)
            audioSource.Pause();
    }

    public void Resume()
    {
        if (_videoPlayer != null && _videoPlayer.isPrepared)
            _videoPlayer.Play();

        if (audioSource != null)
            audioSource.UnPause();
    }

    public void StopPlayback()
    {
        StopPlaybackInternal(clearSource: false);
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
    }

    private void ShowCurrentBannerOrFallback()
    {
        if (targetRawImage == null)
            return;

        targetRawImage.texture = _currentBannerTexture != null ? _currentBannerTexture : fallbackBannerTexture;
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

    private void ReleaseRuntimeBannerTexture()
    {
        if (_runtimeBannerTexture != null)
        {
            if (_currentBannerTexture == _runtimeBannerTexture)
                _currentBannerTexture = null;

            if (targetRawImage != null && targetRawImage.texture == _runtimeBannerTexture && !_hasStartedVideo)
                targetRawImage.texture = fallbackBannerTexture;

            Object.Destroy(_runtimeBannerTexture);
            _runtimeBannerTexture = null;
        }
    }

    private void ReleaseRenderTexture()
    {
        if (_renderTexture != null)
        {
            if (_videoPlayer != null && _videoPlayer.targetTexture == _renderTexture)
                _videoPlayer.targetTexture = null;

            if (targetRawImage != null && targetRawImage.texture == _renderTexture)
                targetRawImage.texture = null;

            _renderTexture.Release();
            Destroy(_renderTexture);
            _renderTexture = null;
        }
    }
}