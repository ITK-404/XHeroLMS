using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class VolumeIconController : MonoBehaviour
{
    [Header("UI")]
    public Button btnVolume;          // nếu bạn muốn nút này toggle mute
    public Slider sliderVolume;       // 0..1 (UI)
    public Image imgVolumeIcon;       // icon

    [Header("Sprites")]
    public Sprite spriteMute;         // volume == 0 (hoặc đang mute)
    public Sprite spriteLow;          // 0 < v <= lowThreshold
    public Sprite spriteMid;          // lowThreshold < v <= midThreshold
    public Sprite spriteHigh;         // midThreshold < v <= 1

    [Header("Thresholds")]
    [Range(0f, 1f)] public float lowThreshold = 0.25f;
    [Range(0f, 1f)] public float midThreshold = 0.75f;

    [Header("Behavior")]
    // TẮT mặc định: btnVolume sẽ KHÔNG toggle mute nữa. Tránh xung đột với VideoPlayerControllerPro (btnVolume chỉ show/hide slider).
    public bool enableMuteToggleButton = false;

    // Khi đang mute, kéo slider > 0 thì tự unmute.
    public bool autoUnmuteOnSlider = true;

    // Khi bấm mute, nếu đang unmute sẽ lưu lại mức volume hiện tại để khôi phục khi unmute.
    public bool restorePreviousOnUnmute = true;

    // Mức tối thiểu khi unmute nếu mức cũ quá nhỏ (tránh unmute nhưng vẫn gần 0).
    [Range(0f, 1f)] public float minimumRestoreVolume = 0.05f;

    [Header("Events")]
    public UnityEvent<float> OnVolumeChanged;  // giá trị 0..1 (đã tính mute => muted -> 0)
    public UnityEvent<bool> OnMutedChanged;

    // State
    [SerializeField, Range(0f, 1f)] private float _volume = 1f;  // volume gốc (không tính mute)
    [SerializeField] private bool _muted = false;
    private float _lastNonZeroVolume = 1f;

    const float EPS = 0.0001f;
    bool _ignoreSliderCallback;

    void Reset()
    {
        sliderVolume = GetComponentInChildren<Slider>(true);
        imgVolumeIcon = GetComponentInChildren<Image>(true);
        btnVolume = GetComponentInChildren<Button>(true);
    }

    void Awake()
    {
        if (sliderVolume)
        {
            sliderVolume.minValue = 0f;
            sliderVolume.maxValue = 1f;
            sliderVolume.wholeNumbers = false;

            _ignoreSliderCallback = true;
            sliderVolume.SetValueWithoutNotify(_volume);
            _ignoreSliderCallback = false;

            sliderVolume.onValueChanged.AddListener(SliderChanged);
        }

        // FIX: mặc định KHÔNG add listener toggle mute vào btnVolume
        if (btnVolume && enableMuteToggleButton)
            btnVolume.onClick.AddListener(ToggleMute);

        UpdateIcon();

        // optional: bắn initial events
        OnVolumeChanged?.Invoke(CurrentVolume);
        OnMutedChanged?.Invoke(_muted);
    }

    void OnDestroy()
    {
        if (sliderVolume)
            sliderVolume.onValueChanged.RemoveListener(SliderChanged);

        if (btnVolume && enableMuteToggleButton)
            btnVolume.onClick.RemoveListener(ToggleMute);
    }

    // ===== Public API =====
    /// <summary> Volume hiện tại có tính đến mute (muted -> 0). </summary>
    public float CurrentVolume => _muted ? 0f : _volume;

    public bool IsMuted => _muted;
    
    public void SetVolume(float v, bool updateSlider = true)
    {
        _volume = Mathf.Clamp01(v);

        if (_volume > EPS)
            _lastNonZeroVolume = _volume;

        if (autoUnmuteOnSlider && _muted && _volume > EPS)
        {
            _muted = false;
            OnMutedChanged?.Invoke(_muted);
        }

        if (updateSlider && sliderVolume)
        {
            _ignoreSliderCallback = true;
            sliderVolume.SetValueWithoutNotify(_volume);
            _ignoreSliderCallback = false;
        }

        UpdateIcon();
        OnVolumeChanged?.Invoke(CurrentVolume);
    }

    public void SetMuted(bool muted, bool updateSlider = true)
    {
        if (_muted == muted) return;

        _muted = muted;

        if (_muted)
        {
            if (_volume > EPS) _lastNonZeroVolume = _volume;

            if (updateSlider && sliderVolume)
            {
                // phản ánh mute (slider 0)
                _ignoreSliderCallback = true;
                sliderVolume.SetValueWithoutNotify(0f);
                _ignoreSliderCallback = false;
            }
        }
        else
        {
            if (restorePreviousOnUnmute)
                _volume = Mathf.Clamp01(Mathf.Max(_lastNonZeroVolume, minimumRestoreVolume));

            if (updateSlider && sliderVolume)
            {
                _ignoreSliderCallback = true;
                sliderVolume.SetValueWithoutNotify(_volume);
                _ignoreSliderCallback = false;
            }
        }

        UpdateIcon();
        OnMutedChanged?.Invoke(_muted);
        OnVolumeChanged?.Invoke(CurrentVolume);
    }

    public void ToggleMute()
    {
        SetMuted(!_muted, updateSlider: true);
    }

    // ===== Internal =====
    private void SliderChanged(float v)
    {
        if (_ignoreSliderCallback) return;

        _volume = Mathf.Clamp01(v);

        if (_volume > EPS)
            _lastNonZeroVolume = _volume;

        // kéo slider > 0 thì auto-unmute (nếu bật)
        if (autoUnmuteOnSlider && _muted && _volume > EPS)
        {
            _muted = false;
            OnMutedChanged?.Invoke(_muted);
        }

        UpdateIcon();
        OnVolumeChanged?.Invoke(CurrentVolume);
    }

    private void UpdateIcon()
    {
        if (!imgVolumeIcon) return;

        float v = _muted ? 0f : _volume;

        if (v <= EPS)
        {
            if (spriteMute) imgVolumeIcon.sprite = spriteMute;
            return;
        }

        if (v <= Mathf.Max(lowThreshold, EPS))
        {
            if (spriteLow) imgVolumeIcon.sprite = spriteLow;
            return;
        }

        if (v <= Mathf.Max(midThreshold, lowThreshold))
        {
            if (spriteMid) imgVolumeIcon.sprite = spriteMid;
            return;
        }

        if (spriteHigh) imgVolumeIcon.sprite = spriteHigh;
    }
}
