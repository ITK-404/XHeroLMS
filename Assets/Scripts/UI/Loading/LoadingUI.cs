using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public static class LoadingUI
{
    private const string DEFAULT_IMG1_PATH   = "IMG_XHeroLMS/Img1";
    private const string DEFAULT_IMG2_PATH   = "IMG_XHeroLMS/Img2";
    private const string DEFAULT_POPUP_PATH  = "Login_Popup/Failed Login Popup UI Variant";
    private const string DEFAULT_POPUP_Update  = "Login_Popup/Failed Login Popup UI Variant";
    private const string DEFAULT_PREFAB_PATH = "Loading_UI/Loading_UI";

    private static Sprite _cachedCenter;
    private static Sprite _cachedSatellite;

    private static GameObject _loadingRoot;
    private static Canvas _canvas;
    private static RingFaderOverlay _overlay;

    private static LoadingUICoroutineHost _host;
    private static Coroutine _timeoutRoutine;
    private static string _timeoutMessage;
    private static string _timeoutHeader;

    // ===================== TAP TO CANCEL =====================
    /// <summary>
    /// Mặc định bật để hạn chế khó chịu khi lag.
    /// Chạm/click bất kỳ -> Hide().
    /// </summary>
    public static bool tapToCancel = true;

    /// <summary>
    /// Đợi X giây sau khi Show() rồi mới cho phép tap để cancel,
    /// tránh trường hợp cú click mở loading bị tính luôn và tắt ngay.
    /// </summary>
    public static float tapToCancelDelay = 0.15f;

    private static Coroutine _tapCancelRoutine;

    // =========================================================

    public static void Show(
        float timeoutSeconds = 0f,
        string timeoutMessage = "Hệ thống đang xử lý quá lâu.\nVui lòng kiểm tra kết nối mạng hoặc thử lại sau.",
        string timeoutHeader  = "HẾT THỜI GIAN CHỜ")
    {
        try
        {
            InternalShowFromPrefab();

            // NEW: Tap/click để cancel (không cần sửa các chỗ gọi Show())
            StartTapToCancelWatcher();

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

        // stop tap-to-cancel watcher
        if (_host != null && _tapCancelRoutine != null)
        {
            _host.StopCoroutine(_tapCancelRoutine);
            _tapCancelRoutine = null;
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

        if (_host != null && _tapCancelRoutine != null)
        {
            _host.StopCoroutine(_tapCancelRoutine);
            _tapCancelRoutine = null;
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
            _canvas.sortingOrder = 32760;

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
    // TAP TO CANCEL WATCHER
    // =========================================================
    private static void StartTapToCancelWatcher()
    {
        if (!tapToCancel) return;

        var host = EnsureHost();

        // nếu gọi Show() nhiều lần, chỉ cần 1 watcher
        if (_tapCancelRoutine != null) return;

        _tapCancelRoutine = host.StartCoroutine(TapToCancelRoutine());
    }

    private static IEnumerator TapToCancelRoutine()
    {
        // Delay nhỏ để tránh click mở loading bị "ăn" luôn và tắt ngay
        if (tapToCancelDelay > 0f)
            yield return new WaitForSecondsRealtime(tapToCancelDelay);
        else
            yield return null;

        while (_loadingRoot != null && _loadingRoot.activeSelf)
        {
            if (tapToCancel && AnyUserInputDown())
            {
                Hide();
                break;
            }

            yield return null;
        }

        _tapCancelRoutine = null;
    }

    private static bool AnyUserInputDown()
    {
        // PC / mouse
        if (Input.GetMouseButtonDown(0)) return true;

        // Mobile touch
        if (Input.touchCount > 0)
        {
            var t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began) return true;
        }

        // Optional: phím bất kỳ
        if (Input.anyKeyDown) return true;

        return false;
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

    var popupCanvasGO = new GameObject("~LoadingErrorCanvas",
        typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));

    var canvas = popupCanvasGO.GetComponent<Canvas>();
    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    canvas.sortingOrder = 32761;

    var scaler = popupCanvasGO.GetComponent<UnityEngine.UI.CanvasScaler>();
    scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
    scaler.referenceResolution = new Vector2(1920, 1080);

    // CHẶN TOUCH XUYÊN QUA POPUP
    CreateTouchBlocker(popupCanvasGO.transform, "~PopupTouchBlocker");

    GameObject popup = Object.Instantiate(prefab, popupCanvasGO.transform);
    popup.transform.SetAsLastSibling();

    var ui = popup.GetComponent<LoginPopupUI>();

    if (ui == null)
    {
        Debug.LogError("Prefab popup không chứa LoginPopupUI!");
        Object.Destroy(popupCanvasGO);
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
        Object.Destroy(popupCanvasGO);
    };

    ui.Init(header, message, combined);
}

public static void ShowUpdatePopup(string message,
                                 UnityAction onReturn = null)
{
    GameObject prefab = Resources.Load<GameObject>(DEFAULT_POPUP_PATH);
    if (prefab == null)
    {
        Debug.LogError("Không tìm thấy prefab: " + DEFAULT_POPUP_PATH);
        return;
    }

    var popupCanvasGO = new GameObject("~LoadingUpdateCanvas",
        typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));

    var canvas = popupCanvasGO.GetComponent<Canvas>();
    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
    canvas.sortingOrder = 32761;

    var scaler = popupCanvasGO.GetComponent<UnityEngine.UI.CanvasScaler>();
    scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
    scaler.referenceResolution = new Vector2(1920, 1080);

    // CHẶN TOUCH XUYÊN QUA POPUP
    CreateTouchBlocker(popupCanvasGO.transform, "~PopupTouchBlocker");

    GameObject popup = Object.Instantiate(prefab, popupCanvasGO.transform);
    popup.transform.SetAsLastSibling();

    var ui = popup.GetComponent<UpdatePopupUI>();

    if (ui == null)
    {
        Debug.LogError("Prefab popup không chứa UpdatePopupUI!");
        Object.Destroy(popupCanvasGO);
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
        Object.Destroy(popupCanvasGO);
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
private static GameObject CreateTouchBlocker(Transform parent, string name)
{
    var blocker = new GameObject(
        name,
        typeof(RectTransform),
        typeof(UnityEngine.UI.Image)
    );

    blocker.transform.SetParent(parent, false);

    var rect = blocker.GetComponent<RectTransform>();
    rect.anchorMin = Vector2.zero;
    rect.anchorMax = Vector2.one;
    rect.offsetMin = Vector2.zero;
    rect.offsetMax = Vector2.zero;
    rect.localScale = Vector3.one;

    var img = blocker.GetComponent<UnityEngine.UI.Image>();

    img.color = new Color(0f, 0f, 0f, 0.001f);
    img.raycastTarget = true;

    // Đặt dưới popup, nhưng trên toàn bộ UI phía sau.
    blocker.transform.SetAsFirstSibling();

    return blocker;
}
}
