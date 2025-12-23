using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas), typeof(CanvasGroup), typeof(GraphicRaycaster))]
public class VideoPlayerQuadUI : MonoBehaviour
{
    [Header("Link tới controller (trên Quad)")]
    public VideoPlayerControllerPro controller;

    [Header("Auto-wire theo tên nếu để trống")]
    public Slider progress;      // tên chứa "Progress"
    public Text timeLabel;       // "Time"
    public Button btnPlayPause;  // "PlayPause"
    public Button btnBack5;      // "Back5"
    public Button btnFwd5;       // "Fwd5"
    public Button btnMute;       // "Mute"
    public Slider volSlider;     // "Volume"

    [Header("UI auto hide")]
    public float autoHideAfter = 5f;

    CanvasGroup _group;
    bool _dragging;
    float _lastInputTime;
    bool _visible = true;

    void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        if (!_group) _group = gameObject.AddComponent<CanvasGroup>();
        _group.alpha = 1; _group.blocksRaycasts = true;

        // Tự tìm control theo tên nếu bạn không kéo tay
        progress    = progress    ?? FindInChildren<Slider>("Progress");
        timeLabel   = timeLabel   ?? FindInChildren<Text>("Time");
        btnPlayPause= btnPlayPause?? FindInChildren<Button>("PlayPause");
        btnBack5    = btnBack5    ?? FindInChildren<Button>("Back5");
        btnFwd5     = btnFwd5     ?? FindInChildren<Button>("Fwd5");
        // btnMute     = btnMute     ?? FindInChildren<Button>("Mute");
        volSlider   = volSlider   ?? FindInChildren<Slider>("Volume");

        // Bind events
        if (btnPlayPause) btnPlayPause.onClick.AddListener(()=> { controller.TogglePlayPause(); Touch(); });

        if (progress)
            progress.onValueChanged.AddListener(v =>
            {
                if (Input.GetMouseButton(0)) { _dragging = true; Touch(); }
            });

if (volSlider)
{
    volSlider.minValue = 0f;
    volSlider.maxValue = 1f;

    // set value theo controller/system ngay khi awake
    float sys = SystemVolumeBridge.GetNormalized();
    volSlider.SetValueWithoutNotify(sys);
}

        FitUnderQuad();
        Touch();
    }

    void Update()
    {
        var vp = controller ? controller.videoPlayer : null;
        if (vp && vp.isPrepared)
        {
            if (!_dragging && progress)
                progress.SetValueWithoutNotify((float)(vp.time / vp.length));

            if (timeLabel)
                timeLabel.text = $"{Fmt(vp.time)} / {Fmt(vp.length)}";
        }

        // auto-hide
        if (AnyUserInput()) Touch();
        if (_visible && Time.time - _lastInputTime > autoHideAfter) HideUI();
    }

    // Đặt canvas nằm sát dưới Quad (theo local)
    public void FitUnderQuad(float yOffset = -0.05f, float zLift = -0.001f)
    {
        if (!controller || !controller.videoPlayer) return;
        // Canvas là con của Quad -> đặt thanh dưới mép quad
        var t = transform;
        t.localPosition = new Vector3(0, yOffset, zLift);
        t.localRotation = Quaternion.identity;

        // scale ngang theo chiều rộng quad
        var quad = controller.videoPlayer.transform;
        // giả định Quad scale X = chiều rộng, Y = chiều cao
        float w = Mathf.Max(0.001f, quad.localScale.x);
        t.localScale = new Vector3(w, w, 1); // đơn giản: scale theo bề rộng
    }

    void ShowUI(){ _group.alpha = 1; _group.blocksRaycasts = true; _visible = true; }
    void HideUI(){ _group.alpha = 0; _group.blocksRaycasts = false; _visible = false; }
    void Touch(){ _lastInputTime = Time.time; if(!_visible) ShowUI(); }

    bool AnyUserInput() =>
        Input.anyKey || Mathf.Abs(Input.GetAxis("Mouse X"))>0 || Mathf.Abs(Input.GetAxis("Mouse Y"))>0;

    string Fmt(double s)
    {
        if (double.IsNaN(s)) return "--:--";
        int x = Mathf.Max(0, (int)Math.Round(s));
        int h = x / 3600; x %= 3600;
        int m = x / 60; x %= 60;
        return h>0 ? $"{h:00}:{m:00}:{x:00}" : $"{m:00}:{x:00}";
    }

    T FindInChildren<T>(string nameContains) where T:Component
    {
        foreach (var tr in GetComponentsInChildren<Transform>(true))
            if (tr.name.IndexOf(nameContains, StringComparison.OrdinalIgnoreCase) >= 0)
            { var c = tr.GetComponent<T>(); if (c) return c; }
        return null;
    }
}
