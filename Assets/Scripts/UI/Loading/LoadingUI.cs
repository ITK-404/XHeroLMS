using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

    // =============================
    // PUBLIC API
    // =============================
    public static void Show()
    {
        try
        {
            InternalShow();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("LoadingUI.Show() ERROR: " + ex.Message);
            Debug.LogException(ex);
            // ShowErrorPopup("Có lỗi khi khởi tạo LoadingUI.\n" + ex.Message);
            ShowErrorPopup("Có lỗi khi khởi tạo LoadingUI.\n" + ex.Message, "Lỗi hệ thống");
            Hide();
        }
    }

    public static void Hide()
    {
        if (_canvas != null)
            _canvas.gameObject.SetActive(false);
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

    // =============================
    // POPUP ERROR
    // =============================
    // =============================
    // POPUP ERROR
    // =============================
    // Đổi từ private -> public và thêm header optional
    public static void ShowErrorPopup(string message, string header = "Lỗi hệ thống")
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

        ui.Init(header, message,
            () =>
            {
                Hide();                      // tắt loading overlay nếu đang bật
                Object.Destroy(popupCanvasGO);
            });
    }
    // =============================
    // HELPER
    // =============================
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
}
public class RingFaderOverlay : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite centerSprite;
    public Sprite satelliteSprite;

    [Header("Layout")]
    public Vector2 centerSize = new Vector2(160, 160);
    public int satelliteCount = 16;
    public float radius = 140f;
    public Vector2 satelliteSize = new Vector2(48, 48);
    public float startAngleDeg = 90f;
    public bool faceInward = false;

    [Header("Fade")]
    public float cycleSeconds = 1.2f;
    [Range(0f, 1f)] public float minAlpha = 0.15f;
    [Range(0f, 1f)] public float maxAlpha = 1f;
    [Range(0f, 1f)] public float phaseStep = 1f / 16f;

    private readonly List<CanvasGroup> _cgs = new();
    private readonly List<Coroutine> _running = new();
    private bool _built;

    Image _centerImage;

    public void BuildAndPlay()
    {
        StopFades();
        Rebuild();
        StartFades();
        _built = true;
    }

    void OnEnable()
    {
        if (_built)
        {
            StartFades();
        }
        else
        {
            if (centerSprite || satelliteSprite) BuildAndPlay();
        }
    }

    void OnDisable() => StopFades();

    public void Resume()
    {
        if (!_built || _cgs.Count == 0 || transform.childCount == 0)
            Rebuild();
        StartFades();
        _built = true;
    }

    public void Rebuild()
    {
        var trash = new List<GameObject>();
        for (int i = 0; i < transform.childCount; i++)
            trash.Add(transform.GetChild(i).gameObject);
        foreach (var t in trash) Destroy(t);

        _cgs.Clear();

        // Center icon
        if (centerSprite)
        {
            var go = new GameObject("center", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rt = go.GetComponent<RectTransform>();
            _centerImage = go.GetComponent<Image>();
            _centerImage.sprite = centerSprite;
            _centerImage.preserveAspect = true;

            rt.SetParent(transform, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = centerSize;
        }

        // Satellite dots
        if (satelliteSprite)
        {
            float step = 360f / Mathf.Max(1, satelliteCount);
            for (int i = 0; i < satelliteCount; i++)
            {
                var go = new GameObject($"satellite_{i}", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup));
                var rt = go.GetComponent<RectTransform>();
                var img = go.GetComponent<Image>();
                var cg = go.GetComponent<CanvasGroup>();

                go.transform.SetParent(transform, false);

                img.sprite = satelliteSprite;
                img.preserveAspect = true;
                rt.sizeDelta = satelliteSize;

                float angle = startAngleDeg + i * step;
                float rad = angle * Mathf.Deg2Rad;
                Vector2 pos = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = pos;

                if (faceInward)
                {
                    Vector2 dirToCenter = -pos.normalized;
                    float ang = Mathf.Atan2(dirToCenter.y, dirToCenter.x) * Mathf.Rad2Deg;
                    rt.localRotation = Quaternion.Euler(0, 0, ang + 90f);
                }
                else
                {
                    Vector2 lookOut = pos.normalized;
                    float ang = Mathf.Atan2(lookOut.y, lookOut.x) * Mathf.Rad2Deg;
                    rt.localRotation = Quaternion.Euler(0, 0, ang - 90f);
                }

                cg.alpha = minAlpha;
                _cgs.Add(cg);
            }
        }
    }

    public void StartFades()
    {
        StopFades();
        for (int i = 0; i < _cgs.Count; i++)
        {
            float phase = i * phaseStep;
            _running.Add(StartCoroutine(FadeLoop(_cgs[i], phase)));
        }
    }

    public void StopFades()
    {
        foreach (var c in _running) if (c != null) StopCoroutine(c);
        _running.Clear();
    }

    IEnumerator FadeLoop(CanvasGroup cg, float phase01)
    {
        float twoPi = Mathf.PI * 2f;
        while (true)
        {
            if (!cg) yield break;
            float t = (Time.time / cycleSeconds + phase01) % 1f;
            float s = (Mathf.Sin(t * twoPi) + 1f) * 0.5f;
            cg.alpha = Mathf.Lerp(minAlpha, maxAlpha, s);
            yield return null;
        }
    }
}
