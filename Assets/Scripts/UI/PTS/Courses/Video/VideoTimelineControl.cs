using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VideoTimelineControl : MonoBehaviour
{
    [SerializeField] private Slider sliderTime;
    [SerializeField] private TextMeshProUGUI txtTimeline;

    public event Action<float> OnSeekRequested;

    private bool _isDragging;

    private void OnEnable()
    {
        if (sliderTime != null)
            sliderTime.onValueChanged.AddListener(OnSliderChanged);
    }

    private void OnDisable()
    {
        if (sliderTime != null)
            sliderTime.onValueChanged.RemoveListener(OnSliderChanged);
    }

    // ── Nhận data từ View ──────────────────────

    public void UpdateState(float currentTime, float duration)
    {
        if (sliderTime != null && !_isDragging)
            sliderTime.SetValueWithoutNotify(duration > 0f ? currentTime / duration : 0f);

        if (txtTimeline != null)
            txtTimeline.text = $"{FormatTime(currentTime)} / {FormatTime(duration)}";
    }

    // ── Gọi từ Inspector (Event Trigger) ───────

    public void OnPointerDown() => _isDragging = true;

    public void OnPointerUp()
    {
        _isDragging = false;
        OnSeekRequested?.Invoke(sliderTime.value);
    }

    // ── Internal ───────────────────────────────

    private void OnSliderChanged(float value)
    {
        // Chỉ cập nhật text khi đang kéo, seek thực sự ở OnPointerUp
        if (!_isDragging) return;

        if (txtTimeline != null)
            txtTimeline.text = $"{FormatTime(value)} / ...";
    }

    private string FormatTime(float seconds)
    {
        if (float.IsNaN(seconds) || seconds < 0) seconds = 0;
        int total   = Mathf.FloorToInt(seconds);
        int hours   = total / 3600;
        int minutes = (total % 3600) / 60;
        int secs    = total % 60;
        return hours > 0
            ? $"{hours:00}:{minutes:00}:{secs:00}"
            : $"{minutes:00}:{secs:00}";
    }
}