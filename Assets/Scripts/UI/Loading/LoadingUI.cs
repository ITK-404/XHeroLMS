using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class LoadingUI
{
    private const string DEFAULT_IMG1_PATH = "IMG_XHeroLMS/Img1";
    private const string DEFAULT_IMG2_PATH = "IMG_XHeroLMS/Img2";
    private const string DEFAULT_POPUP_PATH = "Login_Popup/Failed Login Popup UI Variant";

    private static Sprite _cachedCenter;
    private static Sprite _cachedSatellite;

    private static Canvas _canvas;
    private static GameObject _panel;
    private static RingFaderOverlay _overlay;

    private static LoadingUICoroutineHost _host;
    private static Coroutine _timeoutRoutine;
    private static string _timeoutMessage;
    private static string _timeoutHeader;

public static void Show(float timeoutSeconds = 0f,
                        string timeoutMessage = "Hệ thống đang xử lý quá lâu.\nVui lòng kiểm tra kết nối mạng hoặc thử lại sau.",
                        string timeoutHeader  = "Timeout")
{
    try
    {
        InternalShow();

        // CHỈ start timeout nếu chưa có
        if (_timeoutRoutine == null && timeoutSeconds > 0f)
        {
            StartTimeout(timeoutSeconds, timeoutMessage, timeoutHeader);
        }
    }
    catch (System.Exception ex)
    {
        Debug.LogError("LoadingUI.Show() ERROR: " + ex.Message);
        Debug.LogException(ex);
        ShowErrorPopup("Có lỗi khi khởi tạo LoadingUI.\n" + ex.Message, "Lỗi hệ thống");
        Hide();
    }
}

private static void StartTimeout(float timeoutSeconds, string message, string header)
{
    if (timeoutSeconds <= 0f) return;

    var host = EnsureHost();
    _timeoutMessage = message;
    _timeoutHeader  = header;

    // KHÔNG cần stop cũ nữa vì đã check _timeoutRoutine == null ở Show()
    _timeoutRoutine = host.StartCoroutine(TimeoutRoutine(timeoutSeconds));
}

private static IEnumerator TimeoutRoutine(float seconds)
{
    Debug.Log($"[LoadingUI] TimeoutRoutine started: {seconds}s (realtime)");
    yield return new WaitForSecondsRealtime(seconds);

    if (_canvas != null && _canvas.gameObject.activeSelf)
    {
        Debug.LogWarning("[LoadingUI] Timeout, auto hide + show popup.");

        Hide();

        if (!string.IsNullOrEmpty(_timeoutMessage))
            ShowErrorPopup(_timeoutMessage, _timeoutHeader);
    }

    _timeoutRoutine = null;
}

    public static void Hide()
    {
        if (_canvas != null)
            _canvas.gameObject.SetActive(false);

        if (_host != null && _timeoutRoutine != null)
        {
            _host.StopCoroutine(_timeoutRoutine);
            _timeoutRoutine = null;
        }
    }


    public static void Destroy()
    {
        if (_overlay != null)
        {
            Object.Destroy(_overlay.gameObject);
            _overlay = null;
        }
        if (_panel != null)
        {
            Object.Destroy(_panel);
            _panel = null;
        }
        if (_canvas != null)
        {
            Object.Destroy(_canvas.gameObject);
            _canvas = null;
        }
    }

    // =============================
    // INTERNAL BUILD
    // =============================

    private static void InternalShow()
{
    try
    {
        InternalShowUnsafe();
    }
    catch (System.Exception ex)
        {
        // ShowErrorPopup("Không thể hiển thị LoadingUI.\n" + ex.Message);
        ShowErrorPopup("Thiếu resource IMG_XHeroLMS/Img1 hoặc Img2.\nKhông thể tạo loading animation.", "Lỗi giao diện");
        Hide();
    }
}

    private static void InternalShowUnsafe()
    {
        // Canvas đã có thì bật lại
        if (_canvas != null)
        {
            _canvas.gameObject.SetActive(true);
            if (_overlay != null) _overlay.Resume();
            return;
        }

        _canvas = EnsureOverlayCanvas();
        EnsureEventSystem();

        // ========= Panel nền đen =========
        _panel = new GameObject("~LoadingPanel",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));

        var panelRT = _panel.GetComponent<RectTransform>();
        var panelImg = _panel.GetComponent<Image>();

        panelRT.SetParent(_canvas.transform, false);
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;

        panelImg.color = new Color(0, 0, 0, 240f / 255f);
        panelImg.raycastTarget = true;

        // ========= Overlay =========
        var go = new GameObject("~RingFaderOverlay",
            typeof(RectTransform), typeof(RingFaderOverlay));

        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(panelRT, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;

        _overlay = go.GetComponent<RingFaderOverlay>();

        // Load sprite
        if (_cachedCenter == null)
            _cachedCenter = Resources.Load<Sprite>(DEFAULT_IMG1_PATH);
        if (_cachedSatellite == null)
            _cachedSatellite = Resources.Load<Sprite>(DEFAULT_IMG2_PATH);

        if (_cachedCenter == null || _cachedSatellite == null)
        {
            ShowErrorPopup("Thiếu resource IMG_XHeroLMS/Img1 hoặc Img2.\nKhông thể tạo loading animation.");
            Hide();
            return;
        }

        // Setup overlay
        _overlay.centerSprite = _cachedCenter;
        _overlay.satelliteSprite = _cachedSatellite;
        _overlay.satelliteCount = 16;
        _overlay.radius = 140;
        _overlay.faceInward = false;
        _overlay.cycleSeconds = 1.2f;
        _overlay.minAlpha = 0.15f;
        _overlay.maxAlpha = 1f;
        _overlay.phaseStep = 1f / 16f;

        _overlay.BuildAndPlay();

        Object.DontDestroyOnLoad(_canvas.gameObject);
    }
    
public static void ShowErrorPopup(string message,
                                  string header = "Lỗi hệ thống",
                                  UnityAction onReturn = null)
{
    GameObject prefab = Resources.Load<GameObject>(DEFAULT_POPUP_PATH);
    if (prefab == null)
    {
        Debug.LogError("Không tìm thấy prefab: " + DEFAULT_POPUP_PATH);
        return;
    }

    // Canvas riêng cho popup
    var popupCanvasGO = new GameObject("~LoadingErrorCanvas",
        typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

    var canvas = popupCanvasGO.GetComponent<Canvas>();
    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    canvas.sortingOrder = 32761;

    var scaler = popupCanvasGO.GetComponent<CanvasScaler>();
    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
    scaler.referenceResolution = new Vector2(1920, 1080);

    // Instantiate popup
    GameObject popup = Object.Instantiate(prefab, popupCanvasGO.transform);
    var ui = popup.GetComponent<LoginPopupUI>();

    if (ui == null)
    {
        Debug.LogError("Prefab popup không chứa LoginPopupUI!");
        return;
    }

    // Gộp callback hủy + đóng UI
    UnityAction combined = () =>
    {
        // 1. Cho caller hủy API / video / coroutine
        onReturn?.Invoke();

        // 2. Tắt loading + popup canvas
        Hide();
        Object.Destroy(popupCanvasGO);
    };

    ui.Init(header, message, combined);
}
    
    private static Canvas EnsureOverlayCanvas()
    {
        var existing = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

        foreach (var c in existing)
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay &&
                c.name == "~LoadingCanvas")
                return c;
        }

        var goCanvas = new GameObject("~LoadingCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        var canvas = goCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760;

        var scaler = goCanvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        return canvas;
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule));

            Object.DontDestroyOnLoad(es);
        }
    }
    private static LoadingUICoroutineHost EnsureHost()
    {
        if (_host != null) return _host;

        var go = new GameObject("~LoadingUICoroutineHost");
        Object.DontDestroyOnLoad(go);
        _host = go.AddComponent<LoadingUICoroutineHost>();
        return _host;
    }
}