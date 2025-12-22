using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class VideoPlayerControllerPro : MonoBehaviour
{
    [Header("Required")]
    public VideoPlayer videoPlayer;
    public AudioSource audioSource;

    [Header("Seek & Speed")]
    public double seekStepSeconds = 5.0;
    [Range(0.25f, 3f)] public float playbackSpeed = 1.0f;
    public float speedStep = 0.1f;

    [Header("Volume")]
    [Range(0f, 1f)] public float volume = 1.0f;
    public float volumeStep = 0.05f;
    public bool startMuted = false;

    [Header("Quality (Like YouTube)")]
    public QualityOption[] qualities;
    public int defaultQualityIndex = 0;

    [Serializable]
    public class QualityOption
    {
        public string label = "1080p";
        public SourceType sourceType = SourceType.Url;
        public string url;
        public VideoClip clip;
    }
    public enum SourceType { Url, Clip }

    [Header("UI Bindings (Canvas)")]
    public Button btnPlayPause;
    public Button btnVolume;
    public Slider sliderVolume;
    public Slider sliderDuration;
    public TextMeshProUGUI textTime;

    [Tooltip("Quad/Transform hiển thị video ở chế độ thường (3D).")]
    public Transform videoQuad;

    [Header("Fullscreen (UI RawImage)")]
    public Canvas fullscreenCanvas;          // có thể để trống, script sẽ tự tạo
    public RawImage fullscreenRawImage;      // có thể để trống, script sẽ tự tạo
    public bool useAspectFitter = true;

    [Header("RenderTexture (optional fixed size)")]
    public bool useFixedRT = true;           // BẬT: luôn dùng RT cố định
    public int fixedRTWidth = 3840;        // 4K
    public int fixedRTHeight = 2160;
    public RenderTextureFormat rtFormat = RenderTextureFormat.ARGB32;
    public int rtDepth = 0;
    public int rtAntiAliasing = 1;

    [Header("Events")]
    public UnityEvent<bool> OnPlayStateChanged;
    public UnityEvent<bool> OnFullscreenChanged;

    [Header("Menu Auto Hide")]
    public GameObject panelMenu;
    public bool autoHideMenu = true;
    public float autoHideSeconds = 5f;

    [Header("Watch Video Container")]
    public VideoContainer defaultContainer;
    public VideoContainer secondContainer;
    public VideoContainer fullScreenContainer;

    // ==== INTERNAL ====
    int _currentQualityIndex = -1;
    bool _isSwitchingQuality;
    bool _wasPlayingBeforeSwitch;
    double _savedTimeOnSwitch;
    bool _muted;

    bool _isFullscreen;
    public bool _isScrubbingByUI;
    float _lastInteractTime;
    Vector3 _lastMousePos;

    RenderTexture _rt;
    Renderer _quadRenderer;

    public Predicate<double> GetSkipVideoDuration;

    [Header("Volume Popup")]
    public bool hideVolumeSliderOnOutsideClick = true;
    public bool startVolumeSliderHidden = true;

    RectTransform _rtSliderVol;
    RectTransform _rtBtnVol;

    void Awake()
    {
        if (!videoPlayer) videoPlayer = GetComponent<VideoPlayer>();
        if (videoQuad) _quadRenderer = videoQuad.GetComponent<Renderer>();

        // if (audioSource)
        // {
        //     audioSource.playOnAwake = false;
        //     // audioSource.volume = startMuted ? 0f : volume;
        // }
        // _muted = startMuted;

        if (videoPlayer)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.skipOnDrop = true;

            videoPlayer.errorReceived += OnVideoError;
            videoPlayer.prepareCompleted += OnVideoPrepared;
        }

        EnsureFullscreenUI();

        // Nếu chọn dùng RT cố định, tạo & bind ngay từ đầu
        if (useFixedRT) EnsureFixedRT();

        WireUpUI();

        _lastMousePos = Input.mousePosition;
        _lastInteractTime = Time.time;
        if (panelMenu) panelMenu.SetActive(true);

        ApplyVolume();
        ApplyPlaybackSpeed();

        if (defaultContainer) defaultContainer.Hide();
        if (secondContainer) secondContainer.Hide();
        if (fullScreenContainer) fullScreenContainer.Hide();
    }

    void Start()
    {
        // var volumeUI = FindAnyObjectByType<VolumeIconController>();
        // if (volumeUI)
        // {
        //     volumeUI.OnVolumeChanged.AddListener((v) => { volume = v; ApplyVolume(); });
        //     // volumeUI.OnMutedChanged.AddListener((m) => { _muted = m; ApplyVolume(); });
        //     volumeUI.SetVolume(volume, updateSlider: true);
        //     // volumeUI.SetMuted(_muted);
        // }

        if (qualities != null && qualities.Length > 0)
        {
            int idx = Mathf.Clamp(defaultQualityIndex, 0, qualities.Length - 1);
            StartCoroutine(SwitchQualityAndRestore(idx, 0.0, autoPlay: true));
        }
        else
        {
            PrepareIfNeeded(autoPlay: false);
        }

        SyncUIFromState(initial: true);
    }

    void Update()
    {
        if (!videoPlayer) return;

        // Giữ binding luôn đúng
        if (useFixedRT)
        {
            if (videoPlayer.targetTexture != _rt) EnsureFixedRT();
        }
        else
        {
            RebindIfNeeded();
        }

        // Interaction detect
        if (Input.anyKeyDown) RegisterInteraction();
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) RegisterInteraction();
        if ((Input.mousePresent) && (Input.mousePosition != _lastMousePos))
        { _lastMousePos = Input.mousePosition; RegisterInteraction(); }
        if (Input.touchCount > 0) RegisterInteraction();

        // auto hide
        if (autoHideMenu && panelMenu)
            if (panelMenu.activeSelf && (Time.time - _lastInteractTime) > autoHideSeconds)
                panelMenu.SetActive(false);

        // Hotkeys (PC)
        if (Input.GetKeyDown(KeyCode.Space)) { RegisterInteraction(); TogglePlayPause(); }
        if (Input.GetKeyDown(KeyCode.UpArrow)) { RegisterInteraction(); ChangeVolume(+volumeStep); }
        if (Input.GetKeyDown(KeyCode.DownArrow)) { RegisterInteraction(); ChangeVolume(-volumeStep); }
        if (Input.GetKeyDown(KeyCode.Comma)) { RegisterInteraction(); ChangeSpeed(-speedStep); }
        if (Input.GetKeyDown(KeyCode.Period)) { RegisterInteraction(); ChangeSpeed(+speedStep); }
        if (Input.GetKeyDown(KeyCode.Q)) { RegisterInteraction(); CycleQuality(+1); }

        for (int i = 0; i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                RegisterInteraction();
                int pick = i;
                if (qualities != null && pick < qualities.Length)
                    SwitchQualityKeepTime(pick);
                break;
            }
        }

        // timeline / time text
        if (!_isScrubbingByUI && sliderDuration && videoPlayer.isPrepared && videoPlayer.length > 0.0001f)
        {
            float v = Mathf.Clamp01((float)(videoPlayer.time / videoPlayer.length));
            sliderDuration.SetValueWithoutNotify(v);
        }

        if (textTime)
        {
            if (videoPlayer.isPrepared && videoPlayer.length > 0.0001f)
                textTime.text = $"{FormatTime(videoPlayer.time)} / {FormatTime(videoPlayer.length)}";
            else
                textTime.text = "00:00 / 00:00";
        }
    }

    void OnDestroy()
    {
        if (videoPlayer)
        {
            videoPlayer.errorReceived -= OnVideoError;
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            if (_rt && videoPlayer.targetTexture == _rt) videoPlayer.targetTexture = null;
        }
        if (_rt) { _rt.Release(); Destroy(_rt); }
    }

    // ---- UI wiring ----
    void WireUpUI()
    {
        if (btnPlayPause) btnPlayPause.onClick.AddListener(() => { RegisterInteraction(); TogglePlayPause(); });
        if (btnVolume)
        {
            btnVolume.onClick.AddListener(() =>
            {
                RegisterInteraction();

                if (!sliderVolume) return;

                bool next = !sliderVolume.gameObject.activeSelf;

                if (next)
                    sliderVolume.SetValueWithoutNotify(volume); // SYNC TRƯỚC KHI SHOW

                SetVolumeSliderVisible(next);
            });
        }

        if (sliderVolume)
        {
            sliderVolume.minValue = 0f;
            sliderVolume.maxValue = 1f;
            sliderVolume.wholeNumbers = false;
            sliderVolume.onValueChanged.AddListener(OnVolumeSliderChanged);
        }

        if (sliderDuration)
        {
            sliderDuration.minValue = 0f;
            sliderDuration.maxValue = 1f;
            sliderDuration.wholeNumbers = false;

            sliderDuration.onValueChanged.AddListener(OnDurationSliderChangedContinuous);

            var et = sliderDuration.GetComponent<EventTrigger>();
            if (!et) et = sliderDuration.gameObject.AddComponent<EventTrigger>();
            AddPointerEntry(et, EventTriggerType.PointerDown, OnDurationPointerDown);
            AddPointerEntry(et, EventTriggerType.PointerUp, OnDurationPointerUp);
            AddPointerEntry(et, EventTriggerType.Drag, OnDurationPointerDrag);
        }

        // cache rects để check click ngoài vùng
        if (sliderVolume) _rtSliderVol = sliderVolume.GetComponent<RectTransform>();
        if (btnVolume)    _rtBtnVol    = btnVolume.GetComponent<RectTransform>();

        if (sliderVolume && startVolumeSliderHidden)
            sliderVolume.gameObject.SetActive(false);

    }

    public void AddPointerEntry(EventTrigger et, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> cb)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(cb);
        et.triggers.Add(entry);
    }

    void SyncUIFromState(bool initial = false)
    {
        if (sliderVolume) sliderVolume.SetValueWithoutNotify(volume);
    }

    // ---- Controls ----
    public void TogglePlayPause()
    {
        if (!videoPlayer) return;
        if (!videoPlayer.isPrepared)
        {
            PrepareIfNeeded(autoPlay: true);
            return;
        }

        if (videoPlayer.isPlaying)
        { videoPlayer.Pause(); OnPlayStateChanged?.Invoke(false); }
        else
        { videoPlayer.Play(); OnPlayStateChanged?.Invoke(true); }
    }

    public void TryToPauseVideo()
    {
        if (videoPlayer.isPlaying)
        { videoPlayer.Pause(); OnPlayStateChanged?.Invoke(false); }
    }

    public void ChangeVolume(float delta)
    {
        SetVolumeAbsolute(volume + delta, syncSlider: true);
        FindAnyObjectByType<VolumeIconController>()?.SetVolume(volume, updateSlider: true);
    }

    public void ChangeSpeed(float delta)
    {
        playbackSpeed = Mathf.Clamp(playbackSpeed + delta, 0.25f, 3f);
        ApplyPlaybackSpeed();
    }

    [ContextMenu("EnterFullscreenUI")]
    public void EnterFullscreenUI()
    {
        if (videoQuad) fullscreenCanvas.gameObject.SetActive(true);
        if (fullscreenCanvas) fullscreenCanvas.enabled = true;

        if (useFixedRT) EnsureFixedRT();
        else RebindIfNeeded(force: true);

        _isFullscreen = true;
        OnFullscreenChanged?.Invoke(true);
    }

    public void EnterDefaultMode()
    {
        defaultContainer.Show();
        secondContainer.Hide();
        fullScreenContainer.Hide();
    }

    public void EnterSecondMode()
    {
        defaultContainer.Hide();
        secondContainer.Show();
        fullScreenContainer.Hide();
    }

    public void EnterFullScreenMode()
    {
        defaultContainer.Hide();
        secondContainer.Hide();
        fullScreenContainer.Show();
    }

    [ContextMenu("ExitFullscreenUI")]
    public void ExitFullscreenUI()
    {
        if (videoQuad) fullscreenCanvas.gameObject.SetActive(false);

        if (videoQuad) videoQuad.gameObject.SetActive(true);
        if (fullscreenCanvas) fullscreenCanvas.enabled = false;

        _isFullscreen = false;
        OnFullscreenChanged?.Invoke(false);

        defaultContainer.Hide();
        secondContainer.Hide();
        fullScreenContainer.Hide();
    }

    // ---- Volume/Speed ----
    void SetVolumeAbsolute(float v, bool syncSlider)
    {
        volume = Mathf.Clamp01(v);
        ApplyVolume();
        if (syncSlider && sliderVolume) sliderVolume.SetValueWithoutNotify(volume);
    }

    void ApplyVolume()
    {
        float vol = _muted ? 0f : volume;
        if (audioSource) audioSource.volume = vol;

        if (videoPlayer && videoPlayer.audioOutputMode == VideoAudioOutputMode.Direct)
        {
            try { videoPlayer.SetDirectAudioVolume(0, vol); } catch { }
        }
    }

    void ApplyPlaybackSpeed()
    {
        if (!videoPlayer) return;
        videoPlayer.playbackSpeed = playbackSpeed;
        if (audioSource) audioSource.pitch = playbackSpeed;
    }

    // ---- Quality ----
    public void CycleQuality(int direction)
    {
        if (qualities == null || qualities.Length == 0) return;
        int next = (_currentQualityIndex + direction + qualities.Length) % qualities.Length;
        SwitchQualityKeepTime(next);
    }

    public void SwitchQualityKeepTime(int index)
    {
        if (!videoPlayer || qualities == null || index < 0 || index >= qualities.Length) return;

        double curTime = videoPlayer.isPrepared ? videoPlayer.time : 0.0;
        bool wasPlaying = videoPlayer.isPlaying;
        StartCoroutine(SwitchQualityAndRestore(index, curTime, wasPlaying));
    }

    IEnumerator SwitchQualityAndRestore(int index, double timeToRestore, bool autoPlay)
    {
        if (_isSwitchingQuality) yield break;
        _isSwitchingQuality = true;

        _savedTimeOnSwitch = timeToRestore;
        _wasPlayingBeforeSwitch = autoPlay;

        var q = qualities[index];

        videoPlayer.Stop();
        videoPlayer.source = (q.sourceType == SourceType.Url) ? VideoSource.Url : VideoSource.VideoClip;
        if (q.sourceType == SourceType.Url) videoPlayer.url = q.url;
        else videoPlayer.clip = q.clip;

        _currentQualityIndex = index;

        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared) yield return null;
        yield return null; // chờ 1 frame

        SetTimeSafely(_savedTimeOnSwitch);

        if (_wasPlayingBeforeSwitch) videoPlayer.Play();

        _isSwitchingQuality = false;
    }

    // ---- Prepare & time ----
    void PrepareIfNeeded(bool autoPlay)
    {
        if (!videoPlayer) return;

        if (!videoPlayer.isPrepared)
            StartCoroutine(PrepareAndMaybePlay(autoPlay));
        else if (autoPlay)
            videoPlayer.Play();
    }

    IEnumerator PrepareAndMaybePlay(bool autoPlay)
    {
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared) yield return null;
        if (autoPlay) videoPlayer.Play();
    }

    void SetTimeSafely(double t)
    {
        if (!videoPlayer) return;
        if (GetSkipVideoDuration != null && GetSkipVideoDuration(t) == false)
        {
            Debug.Log("Cảnh báo, không thể skip video tới thời gian: " + t);
            return;
        }

        if (videoPlayer.frameRate > 0.01f && videoPlayer.frameCount > 0)
        {
            long frame = (long)Mathf.Clamp((float)(t * videoPlayer.frameRate), 0, (float)(videoPlayer.frameCount - 1));
            try { videoPlayer.frame = frame; }
            catch { videoPlayer.time = t; }
        }
        else
        {
            videoPlayer.time = t;
        }
    }

    // ---- Prepare callback ----
    void OnVideoPrepared(VideoPlayer vp)
    {
        if (useFixedRT)
        {
            EnsureFixedRT();  // luôn giữ RT cố định
        }
        else
        {
            if (vp.targetTexture == null)
            {
                int w = Mathf.Clamp((int)(vp.width > 0 ? vp.width : 1920), 16, 8192);
                int h = Mathf.Clamp((int)(vp.height > 0 ? vp.height : 1080), 16, 8192);

                if (_rt == null || _rt.width != w || _rt.height != h)
                {
                    if (_rt) { _rt.Release(); Destroy(_rt); }
                    _rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32)
                    {
                        antiAliasing = 1,
                        useDynamicScale = false
                    };
                    _rt.Create();
                    ClearRT(Color.black);
                }
                vp.targetTexture = _rt;
            }
            else
            {
                _rt = vp.targetTexture;
            }

            RebindIfNeeded(force: true);
        }

        ApplyPlaybackSpeed();
        ApplyVolume();
    }

    void OnVideoError(VideoPlayer vp, string msg)
    {
        Debug.LogError("[VideoPlayer] Error: " + msg);

        // clear RT về đen để không bị rác lốm đốm
        ClearRT(Color.black);

        LoadingUI.Hide();
    }

    // ---- Utils ----
    string FormatTime(double seconds)
    {
        if (double.IsNaN(seconds) || double.IsInfinity(seconds)) return "00:00";
        int s = Mathf.Max(0, (int)Math.Round(seconds));
        int h = s / 3600; s %= 3600;
        int m = s / 60; s %= 60;
        return h > 0 ? $"{h:00}:{m:00}:{s:00}" : $"{m:00}:{s:00}";
    }

    public void RegisterInteraction()
    {
        _lastInteractTime = Time.time;
        if (panelMenu && !panelMenu.activeSelf) panelMenu.SetActive(true);
    }

    // ---- Slider handlers ----
    public void OnVolumeSliderChanged(float v)
    {
        RegisterInteraction();
        if (_muted && v > 0f) _muted = false;
        SetVolumeAbsolute(v, syncSlider: false);
    }

    public void OnDurationSliderChangedContinuous(float vNorm)
    {
        RegisterInteraction();
        if (!videoPlayer || !videoPlayer.isPrepared || videoPlayer.length <= 0) return;
        double t = Mathf.Clamp01(vNorm) * videoPlayer.length;
        SetTimeSafely(t);
    }

    public void OnDurationPointerDown(BaseEventData e)
    {
        RegisterInteraction();
        _isScrubbingByUI = true;
        var ped = e as PointerEventData;
        if (sliderDuration && ped != null)
            SetSliderValueFromPointer(sliderDuration, ped);
    }

    public void OnDurationPointerDrag(BaseEventData e)
    {
        RegisterInteraction();
        var ped = e as PointerEventData;
        if (sliderDuration && ped != null)
            SetSliderValueFromPointer(sliderDuration, ped);
    }

    public void OnDurationPointerUp(BaseEventData _)
    {
        RegisterInteraction();
        _isScrubbingByUI = false;
        if (sliderDuration && videoPlayer && videoPlayer.isPrepared && videoPlayer.length > 0)
        {
            double t = sliderDuration.value * videoPlayer.length;
            SetTimeSafely(t);
        }
    }

    void SetSliderValueFromPointer(Slider s, PointerEventData ped)
    {
        if (s == null) return;

        RectTransform rt = s.GetComponent<RectTransform>();
        if (rt == null) return;

        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, ped.position, ped.pressEventCamera, out local))
            return;

        float t;
        if (s.direction == Slider.Direction.LeftToRight || s.direction == Slider.Direction.RightToLeft)
        {
            float xMin = rt.rect.xMin;
            float xMax = rt.rect.xMax;
            t = Mathf.InverseLerp(xMin, xMax, local.x);
            if (s.direction == Slider.Direction.RightToLeft) t = 1f - t;
        }
        else
        {
            float yMin = rt.rect.yMin;
            float yMax = rt.rect.yMax;
            t = Mathf.InverseLerp(yMin, yMax, local.y);
            if (s.direction == Slider.Direction.TopToBottom) t = 1f - t;
        }

        t = Mathf.Clamp01(t);
        s.SetValueWithoutNotify(t);
        OnDurationSliderChangedContinuous(t);
    }

    // ---- UI / RT ----
    void EnsureFullscreenUI()
    {
        if (!fullscreenCanvas)
        {
            var go = new GameObject("FullscreenCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            fullscreenCanvas = go.GetComponent<Canvas>();
            fullscreenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fullscreenCanvas.sortingOrder = 5000;
        }
        if (!fullscreenRawImage)
        {
            var riGO = new GameObject("RawImage", typeof(RectTransform), typeof(RawImage));
            fullscreenRawImage = riGO.GetComponent<RawImage>();
            fullscreenRawImage.raycastTarget = true;
            riGO.transform.SetParent(fullscreenCanvas.transform, false);

            var rt = riGO.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }
        if (useAspectFitter && !fullscreenRawImage.GetComponent<AspectRatioFitter>())
        {
            var fitter = fullscreenRawImage.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
            fitter.aspectRatio = 16f / 9f;
        }

        fullscreenCanvas.enabled = false;
        fullscreenRawImage.enabled = true;
    }

    void EnsureFixedRT()
    {
        if (!videoPlayer) return;

        int w = Mathf.Clamp(fixedRTWidth, 16, 8192);
        int h = Mathf.Clamp(fixedRTHeight, 16, 8192);

        if (_rt == null || _rt.width != w || _rt.height != h || _rt.format != rtFormat)
        {
            if (_rt) { _rt.Release(); Destroy(_rt); }
            _rt = new RenderTexture(w, h, rtDepth, rtFormat)
            {
                antiAliasing = Mathf.Max(1, rtAntiAliasing),
                useDynamicScale = false
            };
            _rt.Create();
            ClearRT(Color.black); // clear ngay khi tạo
        }

        videoPlayer.targetTexture = _rt;

        if (fullscreenRawImage && fullscreenRawImage.texture != _rt)
            fullscreenRawImage.texture = _rt;

        if (defaultContainer) defaultContainer.videoContainer.texture = _rt;
        if (secondContainer) secondContainer.videoContainer.texture = _rt;

        if (_quadRenderer && _quadRenderer.material && _quadRenderer.material.mainTexture != _rt)
            _quadRenderer.material.mainTexture = _rt;

        if (useAspectFitter && fullscreenRawImage)
        {
            var fitter = fullscreenRawImage.GetComponent<AspectRatioFitter>();
            if (fitter) fitter.aspectRatio = Mathf.Max(0.01f, (float)w / Mathf.Max(1, (float)h));
        }
    }

    void RebindIfNeeded(bool force = false)
    {
        if (!videoPlayer) return;

        if (videoPlayer.targetTexture == null || force)
        {
            if (videoPlayer.targetTexture == null)
            {
                int w = Mathf.Clamp((int)(videoPlayer.width > 0 ? videoPlayer.width : 1920), 16, 8192);
                int h = Mathf.Clamp((int)(videoPlayer.height > 0 ? videoPlayer.height : 1080), 16, 8192);

                if (_rt == null || _rt.width != w || _rt.height != h)
                {
                    if (_rt) { _rt.Release(); Destroy(_rt); }
                    _rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
                    _rt.Create();
                    ClearRT(Color.black);
                }
                videoPlayer.targetTexture = _rt;
            }
            else
            {
                _rt = videoPlayer.targetTexture;
            }
        }

        if (fullscreenRawImage && fullscreenRawImage.texture != videoPlayer.targetTexture && videoPlayer.targetTexture != null)
            fullscreenRawImage.texture = videoPlayer.targetTexture;

        if (_quadRenderer && _quadRenderer.material && _quadRenderer.material.mainTexture != videoPlayer.targetTexture && videoPlayer.targetTexture != null)
            _quadRenderer.material.mainTexture = videoPlayer.targetTexture;

        if (useAspectFitter && fullscreenRawImage)
        {
            var fitter = fullscreenRawImage.GetComponent<AspectRatioFitter>();
            if (fitter && videoPlayer.width > 0 && videoPlayer.height > 0)
                fitter.aspectRatio = Mathf.Max(0.01f, (float)videoPlayer.width / Mathf.Max(1, (float)videoPlayer.height));
        }
    }

    // Clear RT về 1 màu (dùng khi mới tạo hoặc khi lỗi video)
    void ClearRT(Color c)
    {
        if (_rt == null) return;
        var prev = RenderTexture.active;
        RenderTexture.active = _rt;
        GL.Clear(true, true, c);
        RenderTexture.active = prev;
    }

    void SetVolumeSliderVisible(bool visible)
    {
        if (!sliderVolume) return;
        sliderVolume.gameObject.SetActive(visible);
    }
}

public class VideoContainerManager : MonoBehaviour
{
    public RectTransform fullScreenRawImg;

    public VideoContainer defaultContainer;
    public VideoContainer secondContainer;
    public VideoContainer fullScreenContainer;
}
