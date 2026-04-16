using System;
using UnityEngine;
using UnityEngine.UI;

public class VideoVolumeControl : MonoBehaviour
{
    [SerializeField] private Slider sliderVolume;
    [SerializeField] private Image imgVolume;
    [SerializeField] private Sprite iconVolumeOn;
    [SerializeField] private Sprite iconVolumeOff;

    public event Action<float> OnVolumeChanged;

    private float _lastVolume = 1f;
    private bool _isMuted;

    private void OnEnable()
    {
        if (sliderVolume != null)
            sliderVolume.onValueChanged.AddListener(OnSliderChanged);
    }

    private void OnDisable()
    {
        if (sliderVolume != null)
            sliderVolume.onValueChanged.RemoveListener(OnSliderChanged);
    }

    // ── Nhận data từ View ──────────────────────

    public void UpdateState(float volume)
    {
        if (sliderVolume != null)
            sliderVolume.SetValueWithoutNotify(volume);

        UpdateIcon(volume);
    }

    // ── Gọi từ Inspector (nút mute) ────────────

    public void OnClickMute()
    {
        if (_isMuted)
        {
            float restore = _lastVolume > 0f ? _lastVolume : 1f;
            _isMuted = false;
            OnVolumeChanged?.Invoke(restore);

            if (sliderVolume != null)
                sliderVolume.SetValueWithoutNotify(restore);

            UpdateIcon(restore);
        }
        else
        {
            _lastVolume = sliderVolume != null ? sliderVolume.value : 1f;
            _isMuted = true;
            OnVolumeChanged?.Invoke(0f);

            if (sliderVolume != null)
                sliderVolume.SetValueWithoutNotify(0f);

            UpdateIcon(0f);
        }
    }

    // ── Internal ───────────────────────────────

    private void OnSliderChanged(float value)
    {
        if (!_isMuted && value > 0f)
            _lastVolume = value;

        _isMuted = value <= 0f;
        UpdateIcon(value);
        OnVolumeChanged?.Invoke(value);
    }

    private void UpdateIcon(float volume)
    {
        if (imgVolume == null) return;
        imgVolume.sprite = volume <= 0f ? iconVolumeOff : iconVolumeOn;
    }
}