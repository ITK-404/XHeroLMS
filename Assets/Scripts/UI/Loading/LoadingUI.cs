using System.Collections;
using UnityEngine;
using UnityEngine.Events;

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
