using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class CourseIntroVideoPlayer : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private RawImage targetRawImage;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    [Header("Proxy")]
    [SerializeField] private LocalProxyAutoBoot proxyBoot;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;
    [SerializeField] private bool autoPlayOnEnable = false;

    private VideoPlayer _videoPlayer;
    private RenderTexture _renderTexture;
    private string _currentUrl;
    private bool _isPrepared;
    private bool _didSetup;

    private void Awake()
    {
        ResolveReferences(forceSetup: true);
    }

    private void OnEnable()
    {
        if (autoPlayOnEnable)
            PlayAuto(targetRawImage);
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

    private void ResolveReferences(bool forceSetup)
    {
        // 1) targetRawImage tự tìm nếu chưa gán
        if (targetRawImage == null)
        {
            targetRawImage = GetComponent<RawImage>();

            if (targetRawImage == null)
                targetRawImage = GetComponentInChildren<RawImage>(true);

            if (targetRawImage == null)
                targetRawImage = GetComponentInParent<RawImage>(true);
        }

        // 2) VideoPlayer tự tìm trên cùng object trước
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

        // 3) AudioSource tự tìm
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();

            if (audioSource == null)
                audioSource = GetComponentInChildren<AudioSource>(true);

            if (audioSource == null)
                audioSource = GetComponentInParent<AudioSource>(true);
        }

        // 4) Proxy tự tìm
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

    public void PlayAuto(RawImage rawImage = null)
    {
        if (rawImage != null)
            targetRawImage = rawImage;

        ResolveReferences(forceSetup: false);

        if (_videoPlayer == null)
        {
            Debug.LogError("[CourseIntroVideoPlayer] Không có VideoPlayer để phát.");
            return;
        }

        EnsureRenderTexture();

        if (_videoPlayer.source == VideoSource.VideoClip && _videoPlayer.clip != null)
        {
            if (debugLog)
                Debug.Log("[CourseIntroVideoPlayer] Ưu tiên VideoClip đang gán sẵn trên VideoPlayer.");

            StopPlaybackInternal(clearSource: false);

            _currentUrl = null;
            _isPrepared = false;

            _videoPlayer.targetTexture = _renderTexture;
            if (targetRawImage != null)
                targetRawImage.texture = _renderTexture;

            _videoPlayer.Prepare();
            return;
        }

        if (_videoPlayer.source == VideoSource.Url && !string.IsNullOrWhiteSpace(_videoPlayer.url))
        {
            if (debugLog)
                Debug.Log($"[CourseIntroVideoPlayer] Ưu tiên URL đang có sẵn trên VideoPlayer: {_videoPlayer.url}");

            PlayResolvedUrl(_videoPlayer.url);
            return;
        }

        string introUrl = CourseDetailStaticStore.GetVideoIntro();
        if (string.IsNullOrWhiteSpace(introUrl))
        {
            if (debugLog)
                Debug.LogWarning("[CourseIntroVideoPlayer] CourseDetailStaticStore không có videoIntro.");
            return;
        }

        if (debugLog)
            Debug.Log($"[CourseIntroVideoPlayer] URL rỗng -> fallback sang detail: {introUrl}");

        PlayResolvedUrl(introUrl);
    }

    public void PlayFromStore(RawImage rawImage = null)
    {
        if (rawImage != null)
            targetRawImage = rawImage;

        string introUrl = CourseDetailStaticStore.GetVideoIntro();

        if (string.IsNullOrWhiteSpace(introUrl))
        {
            if (debugLog)
                Debug.LogWarning("[CourseIntroVideoPlayer] CourseDetailStaticStore không có videoIntro.");
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
            if (debugLog)
                Debug.LogWarning("[CourseIntroVideoPlayer] url null/empty.");
            return;
        }

        PlayResolvedUrl(url);
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

        EnsureRenderTexture();

        _videoPlayer.source = VideoSource.Url;
        _videoPlayer.clip = null;
        _videoPlayer.url = finalUrl;
        _videoPlayer.targetTexture = _renderTexture;

        if (targetRawImage != null)
            targetRawImage.texture = _renderTexture;

        if (debugLog)
        {
            Debug.Log($"[CourseIntroVideoPlayer] Prepare video: {finalUrl}");
#if UNITY_ANDROID && !UNITY_EDITOR
            Debug.Log("[CourseIntroVideoPlayer] Android mode: dùng LocalProxy nếu có.");
#else
            Debug.Log("[CourseIntroVideoPlayer] Non-Android mode: có thể dùng direct url.");
#endif
        }

        _videoPlayer.Prepare();
    }

    private string BuildPlayableUrl(string rawUrl)
    {
        string result = rawUrl;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (proxyBoot == null)
            proxyBoot = FindAnyObjectByType<LocalProxyAutoBoot>();

        if (proxyBoot != null)
        {
            result = proxyBoot.GetPlayableUrl(rawUrl);

            if (debugLog)
                Debug.Log($"[CourseIntroVideoPlayer] Proxy URL: {result}");
        }
        else
        {
            if (debugLog)
                Debug.LogWarning("[CourseIntroVideoPlayer] Không tìm thấy LocalProxyAutoBoot, fallback direct url.");
        }
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

        if (targetRawImage != null)
            targetRawImage.texture = _renderTexture;
    }

    private void OnPrepared(VideoPlayer vp)
    {
        _isPrepared = true;

        if (debugLog)
        {
            string sourceInfo = vp.source == VideoSource.VideoClip
                ? (vp.clip != null ? vp.clip.name : "<null clip>")
                : vp.url;

            Debug.Log(
                $"[CourseIntroVideoPlayer] Prepared. " +
                $"width={vp.width}, height={vp.height}, length={vp.length:F2}, source={sourceInfo}"
            );
        }

        if (targetRawImage != null && vp.targetTexture != null)
            targetRawImage.texture = vp.targetTexture;

        vp.Play();

        if (audioSource != null && !audioSource.isPlaying)
            audioSource.Play();
    }

    private void OnLoopPointReached(VideoPlayer vp)
    {
        if (debugLog)
            Debug.Log("[CourseIntroVideoPlayer] Video finished.");
    }

    private void OnVideoError(VideoPlayer vp, string message)
    {
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
        if (_videoPlayer != null && _isPrepared)
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

    private void OnDisable()
    {
        StopPlayback();
    }

    private void OnDestroy()
    {
        if (_videoPlayer != null)
        {
            _videoPlayer.errorReceived -= OnVideoError;
            _videoPlayer.prepareCompleted -= OnPrepared;
            _videoPlayer.loopPointReached -= OnLoopPointReached;
        }

        ReleaseRenderTexture();
    }
}