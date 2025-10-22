using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class LoadingUI
{
    private const string DEFAULT_IMG1_PATH = "IMG_XHeroLMS/Img1";
    private const string DEFAULT_IMG2_PATH = "IMG_XHeroLMS/Img2";

    private static RingFaderOverlay _overlay;

    private static Canvas _canvas;       
    private static GameObject _panel;

    private static Sprite _cachedCenter;
    private static Sprite _cachedSatellite;

    /// <summary>Hiện overlay loading với nền đen mờ và cấu hình mặc định.</summary>
    public static void Show()
    {
        // Nếu đã tạo rồi: chỉ bật lại root Canvas
        if (_canvas != null)
        {
            _canvas.gameObject.SetActive(true);
            if (_overlay != null) _overlay.Resume();
            return;
        }
        
        _canvas = EnsureOverlayCanvas();      
        EnsureEventSystem();
        
        _panel = new GameObject("~LoadingPanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)); // <--- lưu lại
        var panelRT = _panel.GetComponent<RectTransform>();
        var panelImg = _panel.GetComponent<Image>();
        panelRT.SetParent(_canvas.transform, false);
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;
        panelImg.color = new Color(0f, 0f, 0f, 240f / 255f);
        panelImg.raycastTarget = true;
        
        var go = new GameObject("~RingFaderOverlay", typeof(RectTransform), typeof(RingFaderOverlay));
        var rt = go.GetComponent<RectTransform>();
        rt.SetParent(panelRT, false);
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = Vector2.zero;

        _overlay = go.GetComponent<RingFaderOverlay>();

        if (_cachedCenter == null) _cachedCenter = Resources.Load<Sprite>(DEFAULT_IMG1_PATH);
        if (_cachedSatellite == null) _cachedSatellite = Resources.Load<Sprite>(DEFAULT_IMG2_PATH);

        _overlay.centerSprite = _cachedCenter;
        _overlay.satelliteSprite = _cachedSatellite;
        
        _overlay.satelliteCount = 16;
        _overlay.radius = 140f;
        _overlay.faceInward = false;
        _overlay.cycleSeconds = 1.2f;
        _overlay.minAlpha = 0.15f;
        _overlay.maxAlpha = 1f;
        _overlay.phaseStep = 1f / 16f;

        _overlay.BuildAndPlay();

        Object.DontDestroyOnLoad(_canvas.gameObject);
    }

    /// <summary>Ẩn overlay (panel vẫn tồn tại để bật lại nhanh).</summary>
    public static void Hide()
    {
        // if (_overlay != null) _overlay.gameObject.SetActive(false);
        if (_canvas != null)
            _canvas.gameObject.SetActive(false);
    }

    /// <summary>Huỷ hoàn toàn overlay + panel + canvas.</summary>
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
    
    private static Canvas EnsureOverlayCanvas()
    {
        var existing = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (var c in existing)
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay && c.name == "~LoadingCanvas")
                return c;
        }

        var goCanvas = new GameObject("~LoadingCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
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
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
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
        // Khi canvas/GO được bật lại, nếu đã build trước đó thì khởi động lại fades
        if (_built)
        {
            StartFades();
        }
        else
        {
            // phòng trường hợp lần đầu bật mà chưa build
            if (centerSprite || satelliteSprite) BuildAndPlay();
        }
    }

    void OnDisable() => StopFades();

    public void Resume()                     // <--- NEW
    {
        if (!_built || _cgs.Count == 0 || transform.childCount == 0)
            Rebuild();                       // nếu chưa có child/CG thì rebuild
        StartFades();
        _built = true;
    }

    public void Rebuild()
    {
        // clear
        var trash = new List<GameObject>();
        for (int i = 0; i < transform.childCount; i++)
            trash.Add(transform.GetChild(i).gameObject);
        foreach (var t in trash) Destroy(t);

        _cgs.Clear();

        // center
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

        // satellites
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
