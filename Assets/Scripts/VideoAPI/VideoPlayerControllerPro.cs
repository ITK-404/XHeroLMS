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
    public VideoPlayer videoPlayer;          // Kéo VideoPlayer vào đây
    public AudioSource audioSource;          // Optional (giúp chỉnh volume mượt hơn)

    [Header("Seek & Speed")]
    public double seekStepSeconds = 5.0;     // tua ±5s
    [Range(0.25f, 3f)] public float playbackSpeed = 1.0f;
    public float speedStep = 0.1f;           // , . để tăng/giảm tốc

    [Header("Volume")]
    [Range(0f, 1f)] public float volume = 1.0f;
    public float volumeStep = 0.05f;         // ↑/↓ ±5%
    public bool startMuted = false;

    [Header("Quality (Like YouTube)")]
    public QualityOption[] qualities;        // cấu hình nhiều chất lượng
    public int defaultQualityIndex = 0;      // chất lượng khi start

    [Serializable]
    public class QualityOption
    {
        public string label = "1080p";
        public SourceType sourceType = SourceType.Url;
        public string url;                   // dùng khi SourceType.Url
        public VideoClip clip;               // dùng khi SourceType.Clip
    }
    public enum SourceType { Url, Clip }

    // ==== UI BINDINGS (Canvas) ====
    [Header("UI Bindings (Canvas)")]
    public Button btnPlayPause;
    public Button btnBackward;
    public Button btnForward;
    public Button btnVolume;           // Mute/Unmute
    public Slider sliderVolume;        // 0..1 (không thay đổi trạng thái mute)
    public Slider sliderDuration;      // 0..1 (scrub timeline)
    public TextMeshProUGUI textTime;   // "mm:ss / mm:ss"
    public Button btnSetting;          // Toggle Settings Panel
    public GameObject settingsPanel;   // Panel Setting (optional)
    public Button btnMaxMin;           // Fullscreen/Minimize
    [Tooltip("Quad/Transform đang hiển thị video để scale fullscreen/mini.")]
    public Transform videoQuad;
    public Vector3 fullscreenScale = new Vector3(2f, 2f, 1f);
    // NEW ===== Fullscreen to Camera =====
    [Header("Fullscreen (3D to Camera)")]
    public Camera playerCamera;              // <-- Kéo Camera player vào đây
    public float fullscreenDistance = 0.5f;  // khoảng cách trước near plane (m)
    public bool alwaysFaceCamera = true;     // quay mặt theo camera khi fullscreen

    [Header("Events")]
    public UnityEvent<bool> OnPlayStateChanged;
    public UnityEvent<bool> OnFullscreenChanged;

    // state gốc để restore
    Transform _origParent;
    Vector3 _origPos;
    Quaternion _origRot;
    Vector3 _origScale;
    [Header("Menu Auto Hide")]
    public GameObject panelMenu;       // Panel control chính (hàng nút/slider)
    public bool autoHideMenu = true;
    public float autoHideSeconds = 5f;

    // ==== INTERNAL ====
    int _currentQualityIndex = -1;
    bool _preparedOnce = false;
    bool _isSwitchingQuality = false;
    bool _wasPlayingBeforeSwitch = false;
    double _savedTimeOnSwitch = 0.0;
    bool _muted;

    // fullscreen state
    Vector3 _origVideoQuadScale;
    bool _isFullscreen;

    // scrub state
    bool _isScrubbingByUI; // đang kéo sliderDuration → không auto cập nhật slider

    // inactivity detect
    float _lastInteractTime;
    Vector3 _lastMousePos;

    LearnUI learnUI;

    void Reset()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        audioSource = GetComponent<AudioSource>();
    }

    void Awake()
    {
        learnUI = FindAnyObjectByType<LearnUI>();
        if (!videoPlayer) videoPlayer = GetComponent<VideoPlayer>();

        if (audioSource)
        {
            audioSource.playOnAwake = false;
            audioSource.volume = startMuted ? 0f : volume;
        }

        _muted = startMuted;
        ApplyVolume();
        ApplyPlaybackSpeed();

        // Video events
        if (videoPlayer)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.errorReceived += OnVideoError;
            videoPlayer.prepareCompleted += OnVideoPrepared;
        }

        // Cache scale gốc cho fullscreen toggle
        if (videoQuad) _origVideoQuadScale = videoQuad.localScale;

        // Bind UI events
        WireUpUI();

        // Menu initial
        _lastMousePos = Input.mousePosition;
        _lastInteractTime = Time.time;
        if (panelMenu) panelMenu.SetActive(true);
    }

void Start()
{
    // (giữ nguyên phần chuẩn bị video)

    // === Liên kết VolumeIconController ===
    var volumeUI = FindAnyObjectByType<VolumeIconController>();
    if (volumeUI)
    {
        // Khi UI thay đổi → cập nhật vào VideoPlayer
        volumeUI.OnVolumeChanged.AddListener((v) =>
        {
            volume = v;
            ApplyVolume();
        });

        volumeUI.OnMutedChanged.AddListener((muted) =>
        {
            _muted = muted;
            ApplyVolume();
        });

        // Khi VideoPlayer thay đổi volume bằng phím tắt → cập nhật lại UI
        // (ví dụ ↑/↓ phím mũi tên)
        volumeUI.SetVolume(volume, updateSlider: true);
        volumeUI.SetMuted(_muted);
    }

    // === Chất lượng video ===
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

        // ======= Interaction detect (để auto-hide menu) =======
        if (Input.anyKeyDown) RegisterInteraction();
        // click chuột / giữ chuột
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0)) RegisterInteraction();
        // di chuyển chuột
        if ((Input.mousePresent) && (Input.mousePosition != _lastMousePos))
        {
            RegisterInteraction();
            _lastMousePos = Input.mousePosition;
        }
        // chạm màn hình
        if (Input.touchCount > 0) RegisterInteraction();

        // auto hide sau N giây không tương tác
        if (autoHideMenu && panelMenu)
        {
            if (panelMenu.activeSelf && (Time.time - _lastInteractTime) > autoHideSeconds)
                panelMenu.SetActive(false);
        }

        // ======= Hotkeys =======
        if (Input.GetKeyDown(KeyCode.Space))       { RegisterInteraction(); TogglePlayPause(); }
        if (Input.GetKeyDown(KeyCode.LeftArrow))   { RegisterInteraction(); SeekRelative(-seekStepSeconds); }
        if (Input.GetKeyDown(KeyCode.RightArrow))  { RegisterInteraction(); SeekRelative(+seekStepSeconds); }
        if (Input.GetKeyDown(KeyCode.UpArrow))     { RegisterInteraction(); ChangeVolume(+volumeStep); }
        if (Input.GetKeyDown(KeyCode.DownArrow))   { RegisterInteraction(); ChangeVolume(-volumeStep); }
        if (Input.GetKeyDown(KeyCode.Comma))       { RegisterInteraction(); ChangeSpeed(-speedStep); }
        if (Input.GetKeyDown(KeyCode.Period))      { RegisterInteraction(); ChangeSpeed(+speedStep); }
        if (Input.GetKeyDown(KeyCode.M))           { RegisterInteraction(); ToggleMute(); }
        if (Input.GetKeyDown(KeyCode.Q))           { RegisterInteraction(); CycleQuality(+1); }

        for (int i = 0; i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                RegisterInteraction();
                int pick = i; // 0-based for Alpha1
                if (qualities != null && pick < qualities.Length)
                    SwitchQualityKeepTime(pick);
                break;
            }
        }

        // Auto update slider nếu không kéo
        if (!_isScrubbingByUI && sliderDuration && videoPlayer.isPrepared && videoPlayer.length > 0.0001f)
        {
            float v = Mathf.Clamp01((float)(videoPlayer.time / videoPlayer.length));
            sliderDuration.SetValueWithoutNotify(v);
        }

        // Cập nhật text thời gian
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
        }
    }

    // ===== UI Wiring =====
    void WireUpUI()
    {
        if (btnPlayPause) btnPlayPause.onClick.AddListener(() => { RegisterInteraction(); TogglePlayPause(); });
        if (btnBackward)  btnBackward.onClick.AddListener(() => { RegisterInteraction(); SeekRelative(-seekStepSeconds); });
        if (btnForward)   btnForward.onClick.AddListener(() => { RegisterInteraction(); SeekRelative(+seekStepSeconds); });
        if (btnVolume)    btnVolume.onClick.AddListener(() => { RegisterInteraction(); ToggleMute(); });
        if (btnMaxMin)    btnMaxMin.onClick.AddListener(() => { RegisterInteraction(); ToggleFullscreen(); });
        if (btnSetting)   btnSetting.onClick.AddListener(() => { RegisterInteraction(); ToggleSettingsPanel(); });

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

            // ❶ Cho phép click/drag là tua ngay
            sliderDuration.onValueChanged.AddListener(OnDurationSliderChangedContinuous);

            // ❷ Gắn EventTrigger để click/drag trên track (không chỉ handle)
            var et = sliderDuration.GetComponent<EventTrigger>();
            if (!et) et = sliderDuration.gameObject.AddComponent<EventTrigger>();
            AddPointerEntry(et, EventTriggerType.PointerDown, OnDurationPointerDown);
            AddPointerEntry(et, EventTriggerType.PointerUp,   OnDurationPointerUp);
            AddPointerEntry(et, EventTriggerType.Drag,        OnDurationPointerDrag);
        }
    }

    // helper thêm entry cho EventTrigger
    void AddPointerEntry(EventTrigger et, EventTriggerType type, UnityEngine.Events.UnityAction<BaseEventData> cb)
    {
        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(cb);
        et.triggers.Add(entry);
    }

    void SyncUIFromState(bool initial = false)
    {
        if (sliderVolume)
            sliderVolume.SetValueWithoutNotify(volume);

        if (settingsPanel && initial)
            settingsPanel.SetActive(false);
    }

    // ===== Controls (public) =====
    public void TogglePlayPause()
    {
        if (!videoPlayer) return;
        if (!videoPlayer.isPrepared)
        {
            PrepareIfNeeded(autoPlay: true);
            return;
        }

        if (videoPlayer.isPlaying)
        {
            videoPlayer.Pause();
            OnPlayStateChanged?.Invoke(false);
        }
        else
        {
            videoPlayer.Play();
            OnPlayStateChanged?.Invoke(true);
        }
    }

    public void SeekRelative(double deltaSeconds)
    {
        if (!videoPlayer || !videoPlayer.isPrepared) return;
        double t = Mathf.Clamp((float)(videoPlayer.time + deltaSeconds), 0f, (float)videoPlayer.length);
        SetTimeSafely(t);
    }

    public void ChangeVolume(float delta)
    {
        // SetVolumeAbsolute(volume + delta, syncSlider: true);
        SetVolumeAbsolute(volume + delta, syncSlider: true);
        FindAnyObjectByType<VolumeIconController>()?.SetVolume(volume, updateSlider: true);

    }

    public void ToggleMute()
    {
        _muted = !_muted;
        ApplyVolume();
    }

    public void ChangeSpeed(float delta)
    {
        playbackSpeed = Mathf.Clamp(playbackSpeed + delta, 0.25f, 3f);
        ApplyPlaybackSpeed();
    }

    public void ToggleSettingsPanel()
    {
        if (!settingsPanel) return;
        settingsPanel.SetActive(!settingsPanel.activeSelf);
    }

    public void ToggleFullscreen()
    {
        if (_isFullscreen) ExitFullscreen3D();
        else EnterFullscreen3D();
    }

    void EnterFullscreen3D()
    {
        if (!videoQuad || !playerCamera) return;

        // Lưu trạng thái gốc để restore
        _origParent = videoQuad.parent;
        _origPos = videoQuad.position;
        _origRot = videoQuad.rotation;
        _origScale = videoQuad.localScale;

        // Re-parent vào camera để luôn bám theo (tùy bạn)
        videoQuad.SetParent(playerCamera.transform, worldPositionStays: true);

        // Đặt vị trí/rotation trước camera
        float z = playerCamera.nearClipPlane + Mathf.Max(0.001f, fullscreenDistance);
        videoQuad.position = playerCamera.transform.position + playerCamera.transform.forward * z;
        if (alwaysFaceCamera) videoQuad.rotation = playerCamera.transform.rotation;

        // Fit kích thước
        FitQuadToCameraFrustum(playerCamera, videoQuad, fullscreenDistance);

        // Đánh dấu
        _isFullscreen = true;
        OnFullscreenChanged?.Invoke(true);
        learnUI?.Hide();
    }

    public void ExitFullscreen3D()
    {
        if (!videoQuad) return;

        // Trả về trạng thái gốc
        videoQuad.SetParent(_origParent, worldPositionStays: true);
        videoQuad.position = _origPos;
        videoQuad.rotation = _origRot;
        videoQuad.localScale = _origScale;

        _isFullscreen = false;
        
        if(!PlayerChairManager.IsStantUp)
            learnUI?.Show();
    }

    // Volume slider: kéo > 0 sẽ tự unmute
    void OnVolumeSliderChanged(float v)
    {
        RegisterInteraction();
        if (_muted && v > 0f) _muted = false; // auto-unmute khi kéo lên
        SetVolumeAbsolute(v, syncSlider: false);
    }

    // Click/drag sliderDuration: tua ngay
    void OnDurationSliderChangedContinuous(float vNorm)
    {
        RegisterInteraction();
        if (!videoPlayer || !videoPlayer.isPrepared || videoPlayer.length <= 0) return;
        double t = Mathf.Clamp01(vNorm) * videoPlayer.length;
        SetTimeSafely(t);
    }

    // PointerDown: bật cờ và set value ngay tại vị trí click
    public void OnDurationPointerDown(BaseEventData e)
    {
        RegisterInteraction();
        _isScrubbingByUI = true;
        var ped = e as PointerEventData;
        if (sliderDuration && ped != null)
            SetSliderValueFromPointer(sliderDuration, ped);
    }

    // Drag: liên tục cập nhật value theo vị trí con trỏ (kể cả ngoài handle)
    public void OnDurationPointerDrag(BaseEventData e)
    {
        RegisterInteraction();
        var ped = e as PointerEventData;
        if (sliderDuration && ped != null)
            SetSliderValueFromPointer(sliderDuration, ped);
    }

    // PointerUp: tắt cờ và đảm bảo đặt time đúng vị trí
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

        // Dùng chính RectTransform của Slider (track cố định), KHÔNG dùng fillRect/handle
        RectTransform rt = s.GetComponent<RectTransform>();
        if (rt == null) return;

        // Lấy local point trong rect của slider
        Vector2 local;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, ped.position, ped.pressEventCamera, out local))
            return;

        // Tính normalized theo chiều của slider
        float t;

        if (s.direction == Slider.Direction.LeftToRight || s.direction == Slider.Direction.RightToLeft)
        {
            // Horizontal
            float xMin = rt.rect.xMin;
            float xMax = rt.rect.xMax;
            t = Mathf.InverseLerp(xMin, xMax, local.x);
            if (s.direction == Slider.Direction.RightToLeft) t = 1f - t;
        }
        else
        {
            // Vertical
            float yMin = rt.rect.yMin;
            float yMax = rt.rect.yMax;
            t = Mathf.InverseLerp(yMin, yMax, local.y);
            if (s.direction == Slider.Direction.TopToBottom) t = 1f - t;
        }

        t = Mathf.Clamp01(t);

        s.SetValueWithoutNotify(t);
        OnDurationSliderChangedContinuous(t);
    }

    // ===== Volume/Speed Apply =====
    void SetVolumeAbsolute(float v, bool syncSlider)
    {
        volume = Mathf.Clamp01(v);
        ApplyVolume();
        if (syncSlider && sliderVolume)
            sliderVolume.SetValueWithoutNotify(volume);
    }

    void ApplyVolume()
    {
        float vol = _muted ? 0f : volume;
        if (audioSource) audioSource.volume = vol;

        // VideoPlayer direct audio (nếu không dùng AudioSource)
        if (videoPlayer && videoPlayer.audioOutputMode == VideoAudioOutputMode.Direct)
        {
            try { videoPlayer.SetDirectAudioVolume(0, vol); } catch { /* ignore */ }
        }
    }

    void ApplyPlaybackSpeed()
    {
        if (!videoPlayer) return;
        videoPlayer.playbackSpeed = playbackSpeed;
        if (audioSource) audioSource.pitch = playbackSpeed; // lưu ý: pitch ≠ time-stretch
    }

    // ===== Quality Handling =====
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

        // Prepare then restore time
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared)
            yield return null;

        _preparedOnce = true;

        // Một số nguồn cần 1 frame mới set time chuẩn
        yield return null;

        SetTimeSafely(_savedTimeOnSwitch);

        if (_wasPlayingBeforeSwitch) videoPlayer.Play();

        _isSwitchingQuality = false;
    }

    // ===== Prepare & Time Helpers =====
    void PrepareIfNeeded(bool autoPlay)
    {
        if (!videoPlayer) return;

        if (!videoPlayer.isPrepared)
        {
            StartCoroutine(PrepareAndMaybePlay(autoPlay));
        }
        else
        {
            if (autoPlay) videoPlayer.Play();
        }
    }

    IEnumerator PrepareAndMaybePlay(bool autoPlay)
    {
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared) yield return null;
        _preparedOnce = true;
        if (autoPlay) videoPlayer.Play();
    }

    void SetTimeSafely(double t)
    {
        if (!videoPlayer) return;

        // Nếu có frameRate hợp lệ thì set frame để chính xác hơn
        if (videoPlayer.frameRate > 0.01f && videoPlayer.frameCount > 0)
        {
            long frame = (long)Mathf.Clamp((float)(t * videoPlayer.frameRate), 0, (float)(videoPlayer.frameCount - 1));
            try { videoPlayer.frame = frame; }
            catch { videoPlayer.time = t; } // fallback
        }
        else
        {
            videoPlayer.time = t;
        }
    }

    void OnVideoPrepared(VideoPlayer vp)
    {
        ApplyPlaybackSpeed();
        ApplyVolume();

        // nếu đang fullscreen, refit lại cho đúng aspect thật của video
        if (_isFullscreen && playerCamera && videoQuad)
            FitQuadToCameraFrustum(playerCamera, videoQuad, fullscreenDistance);
    }

    void OnVideoError(VideoPlayer vp, string msg)
    {
        Debug.LogError("[VideoPlayer] Error: " + msg);
    }

    // ===== Utils =====
    string FormatTime(double seconds)
    {
        if (double.IsNaN(seconds) || double.IsInfinity(seconds)) return "00:00";
        int s = Mathf.Max(0, (int)Math.Round(seconds));
        int h = s / 3600; s %= 3600;
        int m = s / 60; s %= 60;
        return h > 0 ? $"{h:00}:{m:00}:{s:00}" : $"{m:00}:{s:00}";
    }

    // ===== Menu Auto Hide Helpers =====
    void RegisterInteraction()
    {
        _lastInteractTime = Time.time;
        if (panelMenu && !panelMenu.activeSelf)
            panelMenu.SetActive(true);
    }
    float GetVideoAspect()
    {
        // Ưu tiên số liệu từ VideoPlayer (nếu đã prepared)
        try
        {
            if (videoPlayer && videoPlayer.width > 0 && videoPlayer.height > 0)
                return (float)videoPlayer.width / Mathf.Max(1, (float)videoPlayer.height);
        }
        catch { /* ignore */ }

        // fallback 16:9
        return 16f / 9f;
    }
    
    void FitQuadToCameraFrustum(Camera cam, Transform quad, float distanceFromNear)
    {
        if (!cam || !quad) return;

        // Z target (khoảng cách từ camera)
        float z = cam.nearClipPlane + Mathf.Max(0.001f, distanceFromNear);

        // Tính kích thước frustum tại khoảng cách z
        float frustumHeight = 2f * z * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        float frustumWidth = frustumHeight * cam.aspect;

        // Tỉ lệ video
        float videoAspect = GetVideoAspect();

        // Fit giữ nguyên tỉ lệ: chọn bề nào chạm trước, bề kia letterbox/pillarbox
        float targetW, targetH;
        float frustumAspect = frustumWidth / frustumHeight;

        if (frustumAspect >= videoAspect)
        {
            // cao bằng frustum, rộng theo aspect video
            targetH = frustumHeight;
            targetW = targetH * videoAspect;
        }
        else
        {
            // rộng bằng frustum, cao theo aspect video
            targetW = frustumWidth;
            targetH = targetW / videoAspect;
        }

        // Unity Quad mặc định 1x1 (localScale.x/y chính là kích thước)
        quad.localScale = new Vector3(targetW, targetH, 1f);
    }
}
