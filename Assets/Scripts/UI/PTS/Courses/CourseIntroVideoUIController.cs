using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;
using UnityEngine.EventSystems;

public class CourseIntroVideoUIController : MonoBehaviour
{
    [Header("Target Player")]
    [SerializeField] private CourseIntroVideoPlayer introPlayer;

    [Header("Buttons")]
    [SerializeField] private Button btnPlayPause;
    [SerializeField] private Button btnVolume;
    [SerializeField] private Button btnFullscreen;
    [SerializeField] private Button buyBtn;
    [Header("Sliders")]
    [SerializeField] private Slider sliderTime;
    [SerializeField] private Slider sliderVolume;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI txtTimeline;

    [Header("Icons")]
    [SerializeField] private Image imgPlayPause;
    [SerializeField] private Sprite iconPlay;
    [SerializeField] private Sprite iconPause;

    [SerializeField] private Image imgVolume;
    [SerializeField] private Sprite iconVolumeOn;
    [SerializeField] private Sprite iconVolumeOff;

    [Header("Config")]
    [SerializeField] private bool autoFindPlayer = true;
    [SerializeField] private bool autoInitVolume = true;

    private VideoPlayer _videoPlayer;
    private AudioSource _audioSource;

    private bool _isDraggingTimeSlider;
    private bool _isDraggingVolumeSlider;

    private float _lastVolumeBeforeMute = 1f;
    private bool _isMuted;

    private void Awake()
    {
        ResolveReferences();
        WireUI();
        SyncInitialUI();

        if (introPlayer != null)
        {
            introPlayer.RefreshBannerFromStore();
        }
    }

    private void OnEnable()
    {
        BindVideoEvents();
        RefreshAllUI();

        if (introPlayer != null)
        {
            introPlayer.RefreshBannerFromStore();
        }
        
        if(buyBtn != null) buyBtn.onClick.AddListener(OnClickPlayPause);
    }

    private void OnDisable()
    {
        UnbindVideoEvents();
        if(buyBtn != null) buyBtn.onClick.RemoveListener(OnClickPlayPause);
    }

    private void Update()
    {
        if (_videoPlayer == null)
            return;

        UpdateTimelineUI();
        UpdatePlayPauseIcon();
    }

    private void ResolveReferences()
    {
        if (introPlayer == null && autoFindPlayer)
            introPlayer = FindAnyObjectByType<CourseIntroVideoPlayer>();

        if (introPlayer == null)
        {
            Debug.LogError("[CourseIntroVideoUIController] Không tìm thấy CourseIntroVideoPlayer.");
            return;
        }

        _videoPlayer = introPlayer.GetVideoPlayer();
        _audioSource = introPlayer.GetAudioSource();

        if (_videoPlayer == null)
            Debug.LogError("[CourseIntroVideoUIController] Không lấy được VideoPlayer từ CourseIntroVideoPlayer.");
    }

    private RawImage FindBannerRawImage()
    {
        if (introPlayer == null)
            return null;

        var raw = introPlayer.GetComponent<RawImage>();
        if (raw != null) return raw;

        raw = introPlayer.GetComponentInChildren<RawImage>(true);
        if (raw != null) return raw;

        raw = introPlayer.GetComponentInParent<RawImage>(true);
        return raw;
    }

    private void WireUI()
    {
        if (btnPlayPause != null)
        {
            btnPlayPause.onClick.RemoveListener(OnClickPlayPause);
            btnPlayPause.onClick.AddListener(OnClickPlayPause);
        }

        if (btnVolume != null)
        {
            btnVolume.onClick.RemoveListener(OnClickVolume);
            btnVolume.onClick.AddListener(OnClickVolume);
        }

        if (btnFullscreen != null)
        {
            btnFullscreen.onClick.RemoveListener(OnClickFullscreen);
            btnFullscreen.onClick.AddListener(OnClickFullscreen);
        }

        if (sliderTime != null)
        {
            sliderTime.minValue = 0f;
            sliderTime.maxValue = 1f;
            sliderTime.wholeNumbers = false;
            sliderTime.SetValueWithoutNotify(0f);

            sliderTime.onValueChanged.RemoveListener(OnTimeSliderChanged);
            sliderTime.onValueChanged.AddListener(OnTimeSliderChanged);

            AddSliderEvents(sliderTime, OnTimeSliderPointerDown, OnTimeSliderPointerUp);
        }

        if (sliderVolume != null)
        {
            sliderVolume.minValue = 0f;
            sliderVolume.maxValue = 1f;
            sliderVolume.wholeNumbers = false;

            sliderVolume.onValueChanged.RemoveListener(OnVolumeSliderChanged);
            sliderVolume.onValueChanged.AddListener(OnVolumeSliderChanged);

            AddSliderEvents(sliderVolume, OnVolumeSliderPointerDown, OnVolumeSliderPointerUp);
        }
    }

    private void BindVideoEvents()
    {
        if (_videoPlayer == null) return;

        _videoPlayer.prepareCompleted -= OnVideoPrepared;
        _videoPlayer.loopPointReached -= OnVideoFinished;

        _videoPlayer.prepareCompleted += OnVideoPrepared;
        _videoPlayer.loopPointReached += OnVideoFinished;
    }

    private void UnbindVideoEvents()
    {
        if (_videoPlayer == null) return;

        _videoPlayer.prepareCompleted -= OnVideoPrepared;
        _videoPlayer.loopPointReached -= OnVideoFinished;
    }

    private void SyncInitialUI()
    {
        if (_audioSource != null)
        {
            _lastVolumeBeforeMute = Mathf.Clamp01(_audioSource.volume);
            _isMuted = _audioSource.volume <= 0.0001f;
        }
        else
        {
            _lastVolumeBeforeMute = 1f;
            _isMuted = false;
        }

        if (sliderVolume != null && autoInitVolume)
        {
            float v = _audioSource != null ? _audioSource.volume : 1f;
            sliderVolume.SetValueWithoutNotify(v);
        }

        if (sliderTime != null)
            sliderTime.SetValueWithoutNotify(0f);

        if (txtTimeline != null)
            txtTimeline.text = "00:00 / 00:00";

        RefreshAllUI();
    }

    private void RefreshAllUI()
    {
        UpdatePlayPauseIcon();
        UpdateVolumeIcon();
        UpdateTimelineUI();
    }

    private void UpdatePlayPauseIcon()
    {
        if (imgPlayPause == null || introPlayer == null)
            return;

        bool isPlaying = _videoPlayer != null && _videoPlayer.isPlaying;
        imgPlayPause.sprite = isPlaying ? iconPause : iconPlay;
    }

    private void UpdateVolumeIcon()
    {
        if (imgVolume == null)
            return;

        imgVolume.sprite = _isMuted ? iconVolumeOff : iconVolumeOn;
    }

    private void UpdateTimelineUI()
    {
        if (_videoPlayer == null || !_videoPlayer.isPrepared)
        {
            if (!_isDraggingTimeSlider && sliderTime != null)
                sliderTime.SetValueWithoutNotify(0f);

            if (txtTimeline != null)
                txtTimeline.text = "00:00 / 00:00";
            return;
        }

        double current = _videoPlayer.time;
        double length = _videoPlayer.length;

        if (sliderTime != null && !_isDraggingTimeSlider)
        {
            float normalized = 0f;
            if (length > 0.0001)
                normalized = Mathf.Clamp01((float)(current / length));

            sliderTime.SetValueWithoutNotify(normalized);
        }

        if (txtTimeline != null)
            txtTimeline.text = $"{FormatTime(current)} / {FormatTime(length)}";
    }

    private void OnClickPlayPause()
    {
        if (introPlayer == null)
            return;

        if (_videoPlayer == null)
        {
            ResolveReferences();
            if (_videoPlayer == null) return;
        }

        if (!_videoPlayer.isPrepared)
        {
            if (!introPlayer.HasStartedVideo())
            {

                introPlayer.StartPlayFromCurrentSource();
                UpdatePlayPauseIcon();
                return;
            }
            return;
        }

        if (_videoPlayer.isPlaying)
            introPlayer.Pause();
        else
            introPlayer.Resume();

        UpdatePlayPauseIcon();
    }

    private void OnClickVolume()
    {
        if (_audioSource == null)
            return;

        if (_isMuted)
        {
            float restoreVolume = _lastVolumeBeforeMute > 0.0001f ? _lastVolumeBeforeMute : 1f;
            _audioSource.volume = restoreVolume;
            _isMuted = false;

            if (sliderVolume != null)
                sliderVolume.SetValueWithoutNotify(restoreVolume);
        }
        else
        {
            _lastVolumeBeforeMute = Mathf.Clamp01(_audioSource.volume);
            _audioSource.volume = 0f;
            _isMuted = true;

            if (sliderVolume != null)
                sliderVolume.SetValueWithoutNotify(0f);
        }

        UpdateVolumeIcon();
    }

    private void OnClickFullscreen()
    {
        Debug.Log("[CourseIntroVideoUIController] Đã nhấn fullscreen.");
    }

    private void OnTimeSliderChanged(float value)
    {
        if (!_isDraggingTimeSlider || _videoPlayer == null || !_videoPlayer.isPrepared)
            return;

        double length = _videoPlayer.length;
        if (length <= 0.0001)
            return;

        double targetTime = value * length;
        _videoPlayer.time = targetTime;

        if (txtTimeline != null)
            txtTimeline.text = $"{FormatTime(targetTime)} / {FormatTime(length)}";
    }

    private void OnVolumeSliderChanged(float value)
    {
        if (_audioSource == null)
            return;

        _audioSource.volume = Mathf.Clamp01(value);

        _isMuted = _audioSource.volume <= 0.0001f;

        if (!_isMuted)
            _lastVolumeBeforeMute = _audioSource.volume;

        UpdateVolumeIcon();
    }

    private void OnTimeSliderPointerDown(BaseEventData _)
    {
        _isDraggingTimeSlider = true;
    }

    private void OnTimeSliderPointerUp(BaseEventData _)
    {
        _isDraggingTimeSlider = false;

        if (_videoPlayer == null || !_videoPlayer.isPrepared || sliderTime == null)
            return;

        double length = _videoPlayer.length;
        if (length <= 0.0001)
            return;

        double targetTime = sliderTime.value * length;
        _videoPlayer.time = targetTime;
    }

    private void OnVolumeSliderPointerDown(BaseEventData _)
    {
        _isDraggingVolumeSlider = true;
    }

    private void OnVolumeSliderPointerUp(BaseEventData _)
    {
        _isDraggingVolumeSlider = false;
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        RefreshAllUI();
    }

    private void OnVideoFinished(VideoPlayer vp)
    {
        UpdatePlayPauseIcon();
    }

    private void AddSliderEvents(Slider slider, Action<BaseEventData> onDown, Action<BaseEventData> onUp)
    {
        if (slider == null) return;

        var trigger = slider.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = slider.gameObject.AddComponent<EventTrigger>();

        AddEventTrigger(trigger, EventTriggerType.PointerDown, onDown);
        AddEventTrigger(trigger, EventTriggerType.PointerUp, onUp);
    }

    private void AddEventTrigger(EventTrigger trigger, EventTriggerType type, Action<BaseEventData> action)
    {
        if (trigger == null || action == null) return;

        var entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(data => action.Invoke(data));
        trigger.triggers.Add(entry);
    }

    private string FormatTime(double seconds)
    {
        if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0)
            seconds = 0;

        int totalSeconds = Mathf.FloorToInt((float)seconds);
        int hours = totalSeconds / 3600;
        int minutes = (totalSeconds % 3600) / 60;
        int secs = totalSeconds % 60;

        return hours > 0
            ? $"{hours:00}:{minutes:00}:{secs:00}"
            : $"{minutes:00}:{secs:00}";
    }
}