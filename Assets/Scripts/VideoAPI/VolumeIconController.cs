using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

/// <summary>
/// Quản lý icon âm lượng độc lập:
/// - Thay icon theo mức volume (0 -> mute, <=low -> low, <=mid -> mid, >mid -> high)
/// - Toggle mute bằng nút (btnVolume)
/// - Kéo sliderVolume sẽ auto-unmute nếu > 0
/// Gợi ý: Bắt sự kiện OnVolumeChanged / OnMutedChanged để set vào AudioSource/VideoPlayer bên ngoài.
/// </summary>
[DisallowMultipleComponent]
public class VolumeIconController : MonoBehaviour
{
    [Header("UI")]
    public Button btnVolume;          // Nút toggle mute
    public Slider sliderVolume;       // 0..1
    public Image  imgVolumeIcon;      // Ảnh hiển thị icon

    [Header("Sprites")]
    public Sprite spriteMute;         // volume == 0 (hoặc đang mute)
    public Sprite spriteLow;          // 0 < v <= lowThreshold
    public Sprite spriteMid;          // lowThreshold < v <= midThreshold
    public Sprite spriteHigh;         // midThreshold < v <= 1

    [Header("Thresholds")]
    [Range(0f, 1f)] public float lowThreshold = 0.25f;
    [Range(0f, 1f)] public float midThreshold = 0.75f;

    [Header("Behavior")]
    [Tooltip("Khi bấm nút mute, nếu đang unmute sẽ lưu lại mức volume hiện tại để khôi phục khi unmute.")]
    public bool restorePreviousOnUnmute = true;
    [Tooltip("Mức tối thiểu khi unmute nếu mức cũ quá nhỏ (tránh unmute nhưng vẫn gần 0).")]
    [Range(0f, 1f)] public float minimumRestoreVolume = 0.05f;

    [Header("Events")]
    public UnityEvent<float> OnVolumeChanged;  // bắn ra giá trị 0..1 sau mỗi thay đổi slider hoặc mute
    public UnityEvent<bool>  OnMutedChanged;   // bắn ra trạng thái mute

    // State
    [SerializeField, Range(0f, 1f)] private float _volume = 1f;
    [SerializeField] private bool _muted = false;
    private float _lastNonZeroVolume = 1f;
    const float EPS = 0.0001f;

    void Reset()
    {
        sliderVolume = GetComponentInChildren<Slider>();
        imgVolumeIcon = GetComponentInChildren<Image>();
        btnVolume = GetComponentInChildren<Button>();
    }

    void Awake()
    {
        if (sliderVolume)
        {
            sliderVolume.minValue = 0f;
            sliderVolume.maxValue = 1f;
            sliderVolume.wholeNumbers = false;
            sliderVolume.SetValueWithoutNotify(_volume);
            sliderVolume.onValueChanged.AddListener(SliderChanged);
        }

        if (btnVolume)
            btnVolume.onClick.AddListener(ToggleMute);

        // Đồng bộ icon lần đầu
        UpdateIcon();
        // Phát sự kiện initial cho chắc (tùy nhu cầu, có thể bỏ)
        OnVolumeChanged?.Invoke(CurrentVolume);
        OnMutedChanged?.Invoke(_muted);
    }

    // ========== Public API ==========
    /// <summary> Volume hiện tại có tính đến mute (nếu muted -> 0). </summary>
    public float CurrentVolume => _muted ? 0f : _volume;

    /// <summary> Set volume tuyệt đối (0..1). Nếu v>0 và đang mute -> tự unmute. </summary>
    public void SetVolume(float v, bool updateSlider = true)
    {
        _volume = Mathf.Clamp01(v);
        if (_volume > EPS && _muted)
        {
            _muted = false;
            OnMutedChanged?.Invoke(_muted);
        }
        if (_volume > EPS) _lastNonZeroVolume = _volume;

        if (updateSlider && sliderVolume)
            sliderVolume.SetValueWithoutNotify(_volume);

        UpdateIcon();
        OnVolumeChanged?.Invoke(CurrentVolume);
    }

    /// <summary> Bật/tắt mute. </summary>
    public void SetMuted(bool muted)
    {
        if (_muted == muted) return;
        _muted = muted;

        if (_muted)
        {
            // Lưu mức cũ để khôi phục
            if (_volume > EPS) _lastNonZeroVolume = _volume;
            // Đưa slider về 0 để phản ánh mute
            if (sliderVolume) sliderVolume.SetValueWithoutNotify(0f);
        }
        else
        {
            // Khôi phục mức cũ (tối thiểu minimumRestoreVolume)
            float restore = restorePreviousOnUnmute
                ? Mathf.Max(_lastNonZeroVolume, minimumRestoreVolume)
                : _volume; // hoặc giữ như cũ nếu không muốn restore
            _volume = Mathf.Clamp01(restore);
            if (sliderVolume) sliderVolume.SetValueWithoutNotify(_volume);
        }

        UpdateIcon();
        OnMutedChanged?.Invoke(_muted);
        OnVolumeChanged?.Invoke(CurrentVolume);
    }

    /// <summary> Toggle mute. </summary>
    public void ToggleMute()
    {
        SetMuted(!_muted);
        FindAnyObjectByType<VolumeIconController>()?.SetMuted(_muted);

    }

    // ========== Handlers ==========
    private void SliderChanged(float v)
    {
        // Kéo slider > 0 => auto unmute
        if (_muted && v > EPS)
        {
            _muted = false;
            OnMutedChanged?.Invoke(_muted);
        }

        _volume = Mathf.Clamp01(v);
        if (_volume > EPS) _lastNonZeroVolume = _volume;

        UpdateIcon();
        OnVolumeChanged?.Invoke(CurrentVolume);
    }

    // ========== Icon Logic ==========
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
