using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class LoadingUI
{
    private const string DEFAULT_IMG1_PATH   = "IMG_XHeroLMS/Img1";
    private const string DEFAULT_IMG2_PATH   = "IMG_XHeroLMS/Img2";
    private const string DEFAULT_POPUP_PATH  = "Login_Popup/Failed Login Popup UI Variant";
    private const string DEFAULT_POPUP_Update  = "Login_Popup/Warning_Update_Popup";
    private const string DEFAULT_PREFAB_PATH = "Loading_UI/Loading_UI";

    private const int LOADING_SORTING_ORDER = 32760;
    private const int POPUP_BLOCKER_SORTING_ORDER = 32766;
    private const int POPUP_CONTENT_SORTING_ORDER = 32767;

    private static Sprite _cachedCenter;
    private static Sprite _cachedSatellite;

    private static GameObject _loadingRoot;
    private static Canvas _canvas;
    private static RingFaderOverlay _overlay;

    private static LoadingUICoroutineHost _host;
    private static Coroutine _timeoutRoutine;
    private static string _timeoutMessage;
    private static string _timeoutHeader;

    private const string INLINE_LOADING_NAME = "~InlineImageLoading";
    private static Sprite _inlineLoadingSprite;



    // =========================================================

    public static void Show(
        float timeoutSeconds = 0f,
        string timeoutMessage = "Hệ thống đang xử lý quá lâu.\nVui lòng kiểm tra kết nối mạng hoặc thử lại sau.",
        string timeoutHeader  = "HẾT THỜI GIAN CHỜ")
    {
        try
        {
            InternalShowFromPrefab();

            if (_timeoutRoutine == null && timeoutSeconds > 0f)
                StartTimeout(timeoutSeconds, timeoutMessage, timeoutHeader);
        }
        catch (System.Exception ex)
        {
            Debug.LogError("LoadingUI.Show() ERROR: " + ex.Message);
            Debug.LogException(ex);
            ShowErrorPopup("Có lỗi khi khởi tạo LoadingUI.\n" + ex.Message, "Lỗi hệ thống");
            Hide();
        }
    }

    public static void Hide()
    {
        // hide prefab
        if (_loadingRoot != null)
            _loadingRoot.SetActive(false);

        // stop timeout
        if (_host != null && _timeoutRoutine != null)
        {
            _host.StopCoroutine(_timeoutRoutine);
            _timeoutRoutine = null;
        }

    }

    public static GameObject ShowInside(RectTransform parent)
    {
        if (parent == null)
            return null;

        Transform old = parent.Find(INLINE_LOADING_NAME);
        GameObject root = old != null ? old.gameObject : CreateInlineLoading(parent);
        if (root == null)
            return null;

        root.transform.SetAsLastSibling();
        root.SetActive(true);
        return root;
    }

    public static void HideInside(GameObject handle)
    {
        if (handle != null)
            handle.SetActive(false);
    }

    private static GameObject CreateInlineLoading(RectTransform parent)
    {
        var root = new GameObject(INLINE_LOADING_NAME, typeof(RectTransform), typeof(CanvasGroup));
        var rect = root.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        var group = root.GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
        var dimRect = dim.GetComponent<RectTransform>();
        dimRect.SetParent(rect, false);
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = Vector2.zero;
        dimRect.offsetMax = Vector2.zero;
        dimRect.localScale = Vector3.one;

        var dimImage = dim.GetComponent<Image>();
        dimImage.color = new Color(0.96f, 0.88f, 0.68f, 0.28f);
        dimImage.raycastTarget = false;

        var spinner = new GameObject("Spinner", typeof(RectTransform), typeof(Image), typeof(InlineImageLoadingSpinner));
        var spinnerRect = spinner.GetComponent<RectTransform>();
        spinnerRect.SetParent(rect, false);
        spinnerRect.anchorMin = new Vector2(0.5f, 0.5f);
        spinnerRect.anchorMax = new Vector2(0.5f, 0.5f);
        spinnerRect.pivot = new Vector2(0.5f, 0.5f);
        spinnerRect.sizeDelta = new Vector2(42f, 42f);
        spinnerRect.anchoredPosition = Vector2.zero;
        spinnerRect.localScale = Vector3.one;

        var spinnerImage = spinner.GetComponent<Image>();
        spinnerImage.sprite = GetInlineLoadingSprite();
        spinnerImage.color = Color.white;
        spinnerImage.raycastTarget = false;

        return root;
    }

    private static Sprite GetInlineLoadingSprite()
    {
        if (_inlineLoadingSprite != null)
            return _inlineLoadingSprite;

        const int size = 64;
        const float radius = 22f;
        const float thickness = 5f;
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        Color clear = new Color(1f, 1f, 1f, 0f);
        Color gold = new Color(1f, 0.72f, 0.18f, 1f);

        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.hideFlags = HideFlags.HideAndDontSave;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 p = new Vector2(x, y);
                float dist = Vector2.Distance(p, center);
                float edgeAlpha = Mathf.Clamp01(1f - Mathf.Abs(dist - radius) / thickness);

                if (edgeAlpha <= 0f)
                {
                    tex.SetPixel(x, y, clear);
                    continue;
                }

                float angle = Mathf.Atan2(p.y - center.y, p.x - center.x) * Mathf.Rad2Deg;
                if (angle < 0f)
                    angle += 360f;

                float arcAlpha = angle <= 300f ? Mathf.Lerp(0.35f, 1f, angle / 300f) : 0f;
                tex.SetPixel(x, y, new Color(gold.r, gold.g, gold.b, edgeAlpha * arcAlpha));
            }
        }

        tex.Apply(false, true);
        _inlineLoadingSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        _inlineLoadingSprite.hideFlags = HideFlags.HideAndDontSave;
        return _inlineLoadingSprite;
    }

    private sealed class InlineImageLoadingSpinner : MonoBehaviour
    {
        private void Update()
        {
            transform.Rotate(0f, 0f, -260f * Time.unscaledDeltaTime);
        }
    }

    public static void Destroy()
    {
        if (_loadingRoot != null)
        {
            Object.Destroy(_loadingRoot);
            _loadingRoot = null;
        }

        _canvas = null;
        _overlay = null;

        if (_host != null && _timeoutRoutine != null)
        {
            _host.StopCoroutine(_timeoutRoutine);
            _timeoutRoutine = null;
        }

    }

    // =========================================================
    // INTERNAL BUILD
    // =========================================================
    private static void InternalShowFromPrefab()
    {
        if (_loadingRoot != null)
        {
            _loadingRoot.SetActive(true);
            if (_overlay != null) _overlay.Resume();
            return;
        }

        // load prefab
        GameObject prefab = Resources.Load<GameObject>(DEFAULT_PREFAB_PATH);
        if (prefab == null)
        {
            Debug.LogError("Không tìm thấy LoadingUI prefab: " + DEFAULT_PREFAB_PATH);
            ShowErrorPopup("Không tìm thấy prefab Loading_UI/Loading_UI.\nVui lòng kiểm tra Resources.", "Lỗi giao diện");
            return;
        }

        _loadingRoot = Object.Instantiate(prefab);
        _loadingRoot.name = "~LoadingUI_Prefab";
        Object.DontDestroyOnLoad(_loadingRoot);

        _canvas = _loadingRoot.GetComponentInChildren<Canvas>(true);
        _overlay = _loadingRoot.GetComponentInChildren<RingFaderOverlay>(true);

if (_canvas != null)
{
    _canvas.overrideSorting = true;
    _canvas.sortingOrder = LOADING_SORTING_ORDER;
}

        _loadingRoot.SetActive(true);

        if (_overlay != null)
        {
            if (_cachedCenter == null)    _cachedCenter    = Resources.Load<Sprite>(DEFAULT_IMG1_PATH);
            if (_cachedSatellite == null) _cachedSatellite = Resources.Load<Sprite>(DEFAULT_IMG2_PATH);

            if (_overlay.centerSprite == null)    _overlay.centerSprite    = _cachedCenter;
            if (_overlay.satelliteSprite == null) _overlay.satelliteSprite = _cachedSatellite;

            if (_overlay.centerSprite == null || _overlay.satelliteSprite == null)
            {
                ShowErrorPopup("Thiếu resource IMG_XHeroLMS/Img1 hoặc Img2.\nKhông thể chạy loading animation.", "Lỗi giao diện");
                Hide();
                return;
            }

            _overlay.BuildAndPlay();
        }
    }

    // =========================================================
    // TIMEOUT
    // =========================================================
    private static void StartTimeout(float timeoutSeconds, string message, string header)
    {
        if (timeoutSeconds <= 0f) return;

        var host = EnsureHost();
        _timeoutMessage = message;
        _timeoutHeader  = header;

        _timeoutRoutine = host.StartCoroutine(TimeoutRoutine(timeoutSeconds));
    }

    private static IEnumerator TimeoutRoutine(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);

        if (_loadingRoot != null && _loadingRoot.activeSelf)
        {
            Hide();

            if (!string.IsNullOrEmpty(_timeoutMessage))
                ShowErrorPopup(_timeoutMessage, _timeoutHeader);
        }

        _timeoutRoutine = null;
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

    var popupRoot = new GameObject("~LoadingErrorCanvas");
    Object.DontDestroyOnLoad(popupRoot);

    // BLOCKER: phủ toàn màn hình, cao hơn mọi UI thường.
    CreateTouchBlocker(
        popupRoot.transform,
        "~PopupTouchBlocker",
        POPUP_BLOCKER_SORTING_ORDER
    );

    // POPUP CANVAS: cao hơn blocker đúng +1.
    var popupCanvasGO = new GameObject("~PopupContentCanvas",
        typeof(Canvas),
        typeof(UnityEngine.UI.CanvasScaler),
        typeof(UnityEngine.UI.GraphicRaycaster));

    popupCanvasGO.transform.SetParent(popupRoot.transform, false);

    var canvas = popupCanvasGO.GetComponent<Canvas>();
    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    canvas.overrideSorting = true;
    canvas.sortingOrder = POPUP_CONTENT_SORTING_ORDER;

    var scaler = popupCanvasGO.GetComponent<UnityEngine.UI.CanvasScaler>();
    scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
    scaler.referenceResolution = new Vector2(1920, 1080);

    GameObject popup = Object.Instantiate(prefab, popupCanvasGO.transform);
    popup.transform.SetAsLastSibling();

    var ui = popup.GetComponent<LoginPopupUI>();

    if (ui == null)
    {
        Debug.LogError("Prefab popup không chứa LoginPopupUI!");
        Object.Destroy(popupRoot);
        return;
    }

    var headerTMP = popup.GetComponentInChildren<TMPro.TMP_Text>(true);
    if (headerTMP != null)
    {
        headerTMP.enableAutoSizing = false;
        headerTMP.fontSize = 28;
    }

    UnityAction combined = () =>
    {
        onReturn?.Invoke();
        Hide();
        Object.Destroy(popupRoot);
    };

    ui.Init(header, message, combined);
}

public static void ShowUpdatePopup(string message,
                                   UnityAction onReturn = null)
{
    GameObject prefab = Resources.Load<GameObject>(DEFAULT_POPUP_Update);
    if (prefab == null)
    {
        Debug.LogError("Không tìm thấy prefab: " + DEFAULT_POPUP_Update);
        return;
    }

    var popupRoot = new GameObject("~LoadingUpdateCanvas");
    Object.DontDestroyOnLoad(popupRoot);

    // BLOCKER: phủ toàn màn hình.
    CreateTouchBlocker(
        popupRoot.transform,
        "~PopupTouchBlocker",
        POPUP_BLOCKER_SORTING_ORDER
    );

    // POPUP CANVAS: cao hơn blocker +1.
    var popupCanvasGO = new GameObject("~PopupContentCanvas",
        typeof(Canvas),
        typeof(UnityEngine.UI.CanvasScaler),
        typeof(UnityEngine.UI.GraphicRaycaster));

    popupCanvasGO.transform.SetParent(popupRoot.transform, false);

    var canvas = popupCanvasGO.GetComponent<Canvas>();
    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    canvas.overrideSorting = true;
    canvas.sortingOrder = POPUP_CONTENT_SORTING_ORDER;

    var scaler = popupCanvasGO.GetComponent<UnityEngine.UI.CanvasScaler>();
    scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
    scaler.referenceResolution = new Vector2(1920, 1080);

    GameObject popup = Object.Instantiate(prefab, popupCanvasGO.transform);
    popup.transform.SetAsLastSibling();

    var ui = popup.GetComponentInChildren<UpdatePopupUI>(true);

    if (ui == null)
    {
        Debug.LogError("Prefab popup không chứa UpdatePopupUI: " + DEFAULT_POPUP_Update);
        Object.Destroy(popupRoot);
        return;
    }

    var headerTMP = popup.GetComponentInChildren<TMPro.TMP_Text>(true);
    if (headerTMP != null)
    {
        headerTMP.enableAutoSizing = false;
        headerTMP.fontSize = 28;
    }

    UnityAction combined = () =>
    {
        onReturn?.Invoke();
        Hide();
        Object.Destroy(popupRoot);
    };

    ui.Init(message, combined);
}

    private static LoadingUICoroutineHost EnsureHost()
    {
        if (_host != null) return _host;

        var go = new GameObject("~LoadingUICoroutineHost");
        Object.DontDestroyOnLoad(go);
        _host = go.AddComponent<LoadingUICoroutineHost>();
        return _host;
    }
private static GameObject CreateTouchBlocker(Transform parent, string name, int sortingOrder)
{
    var blockerCanvasGO = new GameObject(name,
        typeof(Canvas),
        typeof(UnityEngine.UI.CanvasScaler),
        typeof(UnityEngine.UI.GraphicRaycaster));

    blockerCanvasGO.transform.SetParent(parent, false);

    var canvas = blockerCanvasGO.GetComponent<Canvas>();
    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    canvas.overrideSorting = true;
    canvas.sortingOrder = sortingOrder;

    var scaler = blockerCanvasGO.GetComponent<UnityEngine.UI.CanvasScaler>();
    scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
    scaler.referenceResolution = new Vector2(1920, 1080);

    var blocker = new GameObject(
        "BlockerImage",
        typeof(RectTransform),
        typeof(UnityEngine.UI.Image)
    );

    blocker.transform.SetParent(blockerCanvasGO.transform, false);

    var rect = blocker.GetComponent<RectTransform>();
    rect.anchorMin = Vector2.zero;
    rect.anchorMax = Vector2.one;
    rect.offsetMin = Vector2.zero;
    rect.offsetMax = Vector2.zero;
    rect.localScale = Vector3.one;

    var img = blocker.GetComponent<UnityEngine.UI.Image>();
    img.color = new Color(0f, 0f, 0f, 0.001f);
    img.raycastTarget = true;

    return blockerCanvasGO;
}
}
