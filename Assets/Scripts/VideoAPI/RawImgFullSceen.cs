using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RawImgFullSceen : MonoBehaviour
{
    public TextMeshProUGUI textTime;
    public Slider sliderDuration;
    public Slider sliderVolume;
    
    VideoPlayerControllerPro videoPlayerControllerPro;

    void Start()
    {
        videoPlayerControllerPro = FindFirstObjectByType<VideoPlayerControllerPro>(FindObjectsInactive.Include);

        videoPlayerControllerPro.sliderDuration = sliderDuration;
        videoPlayerControllerPro.sliderVolume = sliderVolume;
        videoPlayerControllerPro.textTime = textTime;

        WireUpUI();
    }

    private void Update()
    {
        if (!videoPlayerControllerPro._isScrubbingByUI && sliderDuration && videoPlayerControllerPro.videoPlayer.isPrepared && videoPlayerControllerPro.videoPlayer.length > 0.0001f)
        {
            float v = Mathf.Clamp01((float)(videoPlayerControllerPro.videoPlayer.time / videoPlayerControllerPro.videoPlayer.length));
            sliderDuration.SetValueWithoutNotify(v);
        }

        if (videoPlayerControllerPro != null && textTime != null)
        {
            if (videoPlayerControllerPro.videoPlayer.isPrepared && videoPlayerControllerPro.videoPlayer.length > 0.0001f)
                textTime.text = $"{FormatTime(videoPlayerControllerPro.videoPlayer.time)} / {FormatTime(videoPlayerControllerPro.videoPlayer.length)}";
            else
                textTime.text = "00:00 / 00:00";
        }
    }
    string FormatTime(double seconds)
    {
        if (double.IsNaN(seconds) || double.IsInfinity(seconds)) return "00:00";
        int s = Mathf.Max(0, (int)Math.Round(seconds));
        int h = s / 3600; s %= 3600;
        int m = s / 60; s %= 60;
        return h > 0 ? $"{h:00}:{m:00}:{s:00}" : $"{m:00}:{s:00}";
    }
    
    void WireUpUI()
    {
        if (sliderVolume)
        {
            sliderVolume.minValue = 0f;
            sliderVolume.maxValue = 1f;
            sliderVolume.wholeNumbers = false;
            sliderVolume.onValueChanged.AddListener(videoPlayerControllerPro.OnVolumeSliderChanged);
        }

        if (sliderDuration)
        {
            sliderDuration.minValue = 0f;
            sliderDuration.maxValue = 1f;
            sliderDuration.wholeNumbers = false;

            sliderDuration.onValueChanged.AddListener(videoPlayerControllerPro.OnDurationSliderChangedContinuous);

            var et = sliderDuration.GetComponent<EventTrigger>();
            if (!et) et = sliderDuration.gameObject.AddComponent<EventTrigger>();
            videoPlayerControllerPro.AddPointerEntry(et, EventTriggerType.PointerDown, videoPlayerControllerPro.OnDurationPointerDown);
            videoPlayerControllerPro.AddPointerEntry(et, EventTriggerType.PointerUp,   videoPlayerControllerPro.OnDurationPointerUp);
            videoPlayerControllerPro.AddPointerEntry(et, EventTriggerType.Drag,        videoPlayerControllerPro.OnDurationPointerDrag);
        }
    }
}
