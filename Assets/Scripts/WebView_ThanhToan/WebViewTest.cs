using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WebViewTest : MonoBehaviour
{
    [SerializeField] private UniWebView webViewPrefab;
    private UniWebView webViewInstance;
    private WebViewNavigation _navigation;

    private ScreenOrientation previousOrientation;

    [Header("Default URL")]
    // [SerializeField] private string defaultUrl = SecurityConfig.UrlWeb + "/en";
    [SerializeField] private string defaultUrl;

    private static string pendingUrl = "";
    private static string storeTitleCourse = "";
    private static string currentOrderId = "";
    private static bool isPaymentFinished = false;

    private static string currentCourseId = "";
    private static string currentCourseSeo = "";
    private static string currentCourseName = "";

    public static string CurrentOrderId => currentOrderId;
    public static bool IsPaymentFinished => isPaymentFinished;
    public static string StoreTitleCourse => storeTitleCourse;

    public static string CurrentCourseId => currentCourseId;
    public static string CurrentCourseSeo => currentCourseSeo;
    public static string CurrentCourseName => currentCourseName;

    private const string PlayerPrefsOrderIdKey = "PAYMENT_ORDER_ID";
    private const string PlayerPrefsFinishedKey = "PAYMENT_FINISHED";

    [Header("Auto Find Bank Transfer Link")]
    [SerializeField] private bool autoFindBankTransferLink = true;
    [SerializeField] private float startPollingDelay = 1.0f;
    [SerializeField] private float linkPollInterval = 0.6f;
    [SerializeField] private float jsTimeout = 2.0f;
    [SerializeField] private float minSecondsBetweenAutoLoads = 1.5f;
    [SerializeField] private int maxAutoNavigateAttempts = 3;
    [SerializeField] private bool debugLogs = true;

    [Header("Auto Open Bank App")]
    [SerializeField] private bool autoOpenBankAppLink = true;
    [SerializeField] private float minSecondsBetweenBankAppOpen = 2.0f;
    [SerializeField] private int maxAutoOpenBankAppAttempts = 2;

    private Coroutine bankTransferPollRoutine;
    private bool isNavigatingToBankTransfer = false;
    private float lastAutoLoadTime = -999f;
    private int autoNavigateAttempts = 0;
    private string lastAutoLoadedLink = "";

    private float lastBankAppOpenTime = -999f;
    private int autoOpenBankAppAttempts = 0;
    private string lastOpenedBankAppLink = "";

    private static readonly HashSet<string> BankingSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "tcb",
        "techcombank",
        "vcb",
        "vietcombank",
        "vietcombankmobile",
        "bidv",
        "bidvsmartbanking",
        "bidvapp",
        "mbbank",
        "mb",
        "mbbankpay",
        "acb",
        "acbapp",
        "acbbiz",
        "vietinbank",
        "vietinbankipay",
        "vietinbankmobile",
        "icb",
        "vpbank",
        "vpbankneo",
        "tpbank",
        "tpbankmobile",
        "tpb-pay",
        "hdbank",
        "dihdbank",
        "shb",
        "shbmobile",
        "shbvn",
        "ocb",
        "ocbomni",
        "msb",
        "msbmbanking",
        "msbmbank",
        "msbmobile",
        "seabank",
        "seabankconnect",
        "agribank",
        "agribankemobile",
        "vba",
        "sacombank",
        "sacombankmobile",
        "sacombankpay",
        "vietbank",
        "vietbankdigital",
        "vib",
        "myvib",
        "vib-2",
        "lpbank",
        "lpb",
        "kienlongbank",
        "klb",
        "pvcombank",
        "pvcb",
        "cakebyvpbank",
        "cake",
        "timo",
        "sgbmobile",
        "ncbizimobile",
        "vabmobilebanking",
        "newomni-app",
        "acbone",
        "lv24h",
        "seamobile",
        "zalo"
    };

    private void Awake()
    {
        _navigation = GetComponentInChildren<WebViewNavigation>();
        previousOrientation = Screen.orientation;
        Screen.orientation = ScreenOrientation.Portrait;

        if (string.IsNullOrWhiteSpace(defaultUrl))
            defaultUrl = SecurityConfig.UrlWeb + "/en";

        string targetUrl = string.IsNullOrWhiteSpace(pendingUrl) ? defaultUrl : pendingUrl;
        StartCoroutine(CreateWebView(targetUrl));

        pendingUrl = "";

        if (_navigation)
        {
            _navigation.OnExitClicked += ExitView;
            _navigation.OnReloadClicked += ReloadView;
            _navigation.OnLeftNaviClicked += GoBackward;
            _navigation.OnRightNaviClicked += GoForward;
        }
    }

    private void OnDestroy()
    {
        if (_navigation)
        {
            _navigation.OnExitClicked -= ExitView;
            _navigation.OnReloadClicked -= ReloadView;
            _navigation.OnLeftNaviClicked -= GoBackward;
            _navigation.OnRightNaviClicked -= GoForward;
        }

        if (bankTransferPollRoutine != null)
        {
            StopCoroutine(bankTransferPollRoutine);
            bankTransferPollRoutine = null;
        }

        if (webViewInstance != null)
        {
            webViewInstance.OnPageFinished -= OnWebPageFinished;
            webViewInstance.Hide();
            Destroy(webViewInstance.gameObject);
            webViewInstance = null;
        }
    }

    private void GoBackward()
    {
        if (webViewInstance != null && webViewInstance.CanGoBack)
        {
            webViewInstance.GoBack();
        }
    }

    private void GoForward()
    {
        if (webViewInstance != null && webViewInstance.CanGoForward)
        {
            webViewInstance.GoForward();
        }
    }

    private void ReloadView()
    {
        if (webViewInstance != null)
        {
            if (debugLogs) Debug.Log("[WebView] Manual reload clicked.");
            webViewInstance.Reload();
        }
    }

    private void ExitView()
    {
        Screen.orientation = previousOrientation;
        SceneManager.UnloadSceneAsync("WebView_Mobile");
    }

    private IEnumerator CreateWebView(string url)
    {
        webViewInstance = Instantiate(webViewPrefab, transform);
        webViewInstance.gameObject.SetActive(true);

        webViewInstance.OnPageFinished += OnWebPageFinished;

        LoadingUI.Show();
        yield return new WaitForSeconds(1f);
        LoadingUI.Hide();

        webViewInstance.Load(url);
        webViewInstance.Show();

        if (debugLogs) Debug.Log("[WebViewTest] Loaded URL: " + url);
    }

    private void OnWebPageFinished(UniWebView view, int statusCode, string currentUrl)
    {
        if (debugLogs)
            Debug.Log("[WebView] OnPageFinished | statusCode: " + statusCode + " | url: " + currentUrl);

        HandleWebViewUrl(currentUrl);

        if (string.IsNullOrWhiteSpace(currentUrl))
            return;

        // Nếu URL hiện tại tự nó đã là deeplink ngân hàng / intent
        if (TryHandleExternalBankLink(currentUrl))
        {
            return;
        }

        if (IsBankTransferUrl(currentUrl))
        {
            if (debugLogs) Debug.Log("[WebView] Already in bank transfer page. Stop auto-find transfer page, continue polling external bank app link.");
        }

        if (bankTransferPollRoutine == null)
        {
            bankTransferPollRoutine = StartCoroutine(PollPageRoutine());
        }
    }

    private IEnumerator PollPageRoutine()
    {
        yield return new WaitForSeconds(startPollingDelay);

        while (webViewInstance != null)
        {
            string currentUrl = webViewInstance.Url;

            if (!string.IsNullOrWhiteSpace(currentUrl))
            {
                HandleWebViewUrl(currentUrl);

                if (TryHandleExternalBankLink(currentUrl))
                {
                    yield return new WaitForSeconds(linkPollInterval);
                    continue;
                }
            }

            // 1) Tìm link bank transfer từ DOM
            if (autoFindBankTransferLink &&
                !isNavigatingToBankTransfer &&
                Time.unscaledTime - lastAutoLoadTime >= minSecondsBetweenAutoLoads)
            {
                if (string.IsNullOrWhiteSpace(currentUrl) || !IsBankTransferUrl(currentUrl))
                {
                    string foundTransferLink = "";
                    yield return EvaluateJsForString(BuildFindBankTransferJs(), result => foundTransferLink = result);

                    foundTransferLink = NormalizeJsResult(foundTransferLink);

                    if (debugLogs && !string.IsNullOrEmpty(foundTransferLink))
                        Debug.Log("[WebView] Found bank transfer link from DOM: " + foundTransferLink);

                    if (!string.IsNullOrEmpty(foundTransferLink) && IsBankTransferUrl(foundTransferLink))
                    {
                        if (string.Equals(foundTransferLink, lastAutoLoadedLink, StringComparison.OrdinalIgnoreCase))
                        {
                            if (debugLogs)
                                Debug.Log("[WebView] Found same link as last auto-loaded. Skip duplicate navigation.");
                        }
                        else
                        {
                            isNavigatingToBankTransfer = true;
                            autoNavigateAttempts++;
                            lastAutoLoadTime = Time.unscaledTime;
                            lastAutoLoadedLink = foundTransferLink;

                            if (debugLogs)
                                Debug.Log("[WebView] Auto loading bank transfer page: " + foundTransferLink);

                            webViewInstance.Load(foundTransferLink);

                            yield return new WaitForSeconds(1.0f);

                            isNavigatingToBankTransfer = false;
                        }
                    }
                }
            }

            // 2) Tìm deeplink app ngân hàng từ DOM
            if (autoOpenBankAppLink &&
                Time.unscaledTime - lastBankAppOpenTime >= minSecondsBetweenBankAppOpen &&
                autoOpenBankAppAttempts < maxAutoOpenBankAppAttempts)
            {
                string foundExternalLink = "";
                yield return EvaluateJsForString(BuildFindBankAppDeepLinkJs(), result => foundExternalLink = result);

                foundExternalLink = NormalizeJsResult(foundExternalLink);

                if (debugLogs && !string.IsNullOrEmpty(foundExternalLink))
                    Debug.Log("[WebView] Found bank app deeplink from DOM: " + foundExternalLink);

                if (!string.IsNullOrEmpty(foundExternalLink))
                {
                    TryHandleExternalBankLink(foundExternalLink);
                }
            }

            yield return new WaitForSeconds(linkPollInterval);
        }

        bankTransferPollRoutine = null;
    }

    private IEnumerator EvaluateJsForString(string js, Action<string> onDone)
    {
        bool finished = false;
        string result = "";

        webViewInstance.EvaluateJavaScript(js, payload =>
        {
            finished = true;
            result = payload.data;
        });

        float timeoutAt = Time.unscaledTime + jsTimeout;
        while (!finished && Time.unscaledTime < timeoutAt)
        {
            yield return null;
        }

        if (!finished)
        {
            if (debugLogs) Debug.LogWarning("[WebView] JS polling timeout.");
            onDone?.Invoke("");
            yield break;
        }

        onDone?.Invoke(result);
    }

    private static string BuildFindBankTransferJs()
    {
        return @"
(function() {
    try {
        function normalizeHref(href) {
            if (!href) return '';
            try {
                return new URL(href, window.location.href).href;
            } catch (e) {
                return href;
            }
        }

        var direct = document.querySelector(""a[href*='payment/bank-transfer/']"");
        if (direct) {
            var href = direct.getAttribute('href') || direct.href || '';
            href = normalizeHref(href);
            if (href.indexOf('/payment/bank-transfer/') >= 0) return href;
        }

        var allLinks = document.querySelectorAll(""a[href]"");
        for (var i = 0; i < allLinks.length; i++) {
            var href = allLinks[i].getAttribute('href') || allLinks[i].href || '';
            href = normalizeHref(href);
            if (href.indexOf('/payment/bank-transfer/') >= 0) {
                return href;
            }
        }

        var buttons = document.querySelectorAll(""button, a, div[role='button'], span[role='button']"");
        for (var j = 0; j < buttons.length; j++) {
            var el = buttons[j];
            var text = ((el.innerText || el.textContent || '') + '').toLowerCase().trim();
            if (
                text.indexOf('bank transfer') >= 0 ||
                text.indexOf('chuyển khoản') >= 0 ||
                text.indexOf('chuyen khoan') >= 0
            ) {
                var href2 = el.getAttribute('href') || '';
                href2 = normalizeHref(href2);
                if (href2.indexOf('/payment/bank-transfer/') >= 0) {
                    return href2;
                }
            }
        }

        return '';
    } catch (e) {
        return '';
    }
})();";
    }

    private static string BuildFindBankAppDeepLinkJs()
    {
        return @"
(function() {
    try {
        function getNormalized(href) {
            if (!href) return '';
            href = (href + '').trim();
            return href;
        }

        function isBankScheme(href) {
            if (!href) return false;
            var lower = href.toLowerCase();

            if (lower.indexOf('intent://') === 0) return true;

            var schemes = [
                'tcb','techcombank','vcb','vietcombank','vietcombankmobile','bidv','bidvsmartbanking','bidvapp',
                'mbbank','mb','mbbankpay','acb','acbapp','acbbiz','vietinbank','vietinbankipay','vietinbankmobile',
                'icb','vpbank','vpbankneo','tpbank','tpbankmobile','tpb-pay','hdbank','dihdbank','shb','shbmobile',
                'shbvn','ocb','ocbomni','msb','msbmbanking','msbmbank','msbmobile','seabank','seabankconnect',
                'agribank','agribankemobile','vba','sacombank','sacombankmobile','sacombankpay','vietbank',
                'vietbankdigital','vib','myvib','vib-2','lpbank','lpb','kienlongbank','klb','pvcombank','pvcb',
                'cakebyvpbank','cake','timo','sgbmobile','ncbizimobile','vabmobilebanking','newomni-app',
                'acbone','lv24h','seamobile','zalo'
            ];

            for (var i = 0; i < schemes.length; i++) {
                if (lower.indexOf(schemes[i] + '://') === 0) return true;
            }

            return false;
        }

        var all = document.querySelectorAll('[href], [data-href], [data-url], [onclick]');
        for (var i = 0; i < all.length; i++) {
            var el = all[i];

            var href = getNormalized(el.getAttribute('href'));
            if (isBankScheme(href)) return href;

            var dataHref = getNormalized(el.getAttribute('data-href'));
            if (isBankScheme(dataHref)) return dataHref;

            var dataUrl = getNormalized(el.getAttribute('data-url'));
            if (isBankScheme(dataUrl)) return dataUrl;

            var onclick = getNormalized(el.getAttribute('onclick'));
            if (onclick) {
                var match = onclick.match(/([a-zA-Z0-9\-]+:\/\/[^'"")\s]+)/);
                if (match && match.length > 1 && isBankScheme(match[1])) {
                    return match[1];
                }

                var intentMatch = onclick.match(/(intent:\/\/[^'"")\s]+)/i);
                if (intentMatch && intentMatch.length > 1) {
                    return intentMatch[1];
                }
            }
        }

        return '';
    } catch (e) {
        return '';
    }
})();";
    }

    private static string NormalizeJsResult(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";

        value = value.Trim();

        if (value.Length >= 2 && value[0] == '"' && value[value.Length - 1] == '"')
        {
            value = value.Substring(1, value.Length - 2);
        }

        value = value.Replace("\\/", "/");
        value = value.Replace("\\u002F", "/");
        value = value.Replace("\\\"", "\"");
        value = value.Trim();

        return value;
    }

    private static bool IsBankTransferUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        return url.IndexOf("/payment/bank-transfer/", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private bool TryHandleExternalBankLink(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

#if UNITY_IOS && !UNITY_EDITOR
    if (IsIntentUrl(url))
    {
        if (debugLogs)
            Debug.Log("[WebView] iOS ignores intent:// URL: " + url);
        return false;
    }
#endif

        if (!IsBankAppDeepLink(url) && !IsIntentUrl(url))
            return false;

        if (string.Equals(url, lastOpenedBankAppLink, StringComparison.OrdinalIgnoreCase) &&
            Time.unscaledTime - lastBankAppOpenTime < minSecondsBetweenBankAppOpen)
        {
            if (debugLogs)
                Debug.Log("[WebView] Same bank deeplink was opened recently. Skip duplicate open.");
            return true;
        }

        bool opened = OpenExternalUrl(url);
        if (!opened)
        {
            if (debugLogs)
                Debug.LogWarning("[WebView] Failed to open external bank app: " + url);
            return false;
        }

        lastOpenedBankAppLink = url;
        lastBankAppOpenTime = Time.unscaledTime;
        autoOpenBankAppAttempts++;

        if (debugLogs)
            Debug.Log("[WebView] Opened external bank app: " + url);

        return true;
    }

    private bool IsBankAppDeepLink(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        int schemeIndex = url.IndexOf("://", StringComparison.Ordinal);
        if (schemeIndex <= 0)
            return false;

        string scheme = url.Substring(0, schemeIndex).Trim();
        if (string.IsNullOrEmpty(scheme))
            return false;

        return BankingSchemes.Contains(scheme);
    }

    private static bool IsIntentUrl(string url)
    {
        return !string.IsNullOrWhiteSpace(url) &&
               url.StartsWith("intent://", StringComparison.OrdinalIgnoreCase);
    }

    private bool OpenExternalUrl(string url)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var uriClass = new AndroidJavaClass("android.net.Uri"))
            using (var intentClass = new AndroidJavaClass("android.content.Intent"))
            {
                AndroidJavaObject intent = null;

                if (IsIntentUrl(url))
                {
                    int uriIntentScheme = intentClass.GetStatic<int>("URI_INTENT_SCHEME");
                    intent = intentClass.CallStatic<AndroidJavaObject>("parseUri", url, uriIntentScheme);
                }
                else
                {
                    string actionView = intentClass.GetStatic<string>("ACTION_VIEW");
                    AndroidJavaObject uri = uriClass.CallStatic<AndroidJavaObject>("parse", url);
                    intent = new AndroidJavaObject("android.content.Intent", actionView, uri);
                }

                if (intent == null)
                    return false;

                using (var packageManager = currentActivity.Call<AndroidJavaObject>("getPackageManager"))
                {
                    AndroidJavaObject resolved = intent.Call<AndroidJavaObject>("resolveActivity", packageManager);
                    if (resolved == null)
                    {
                        if (debugLogs)
                            Debug.LogWarning("[WebView] No app can handle this external URL: " + url);
                        return false;
                    }
                }

                currentActivity.Call("startActivity", intent);
                return true;
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[WebView] OpenExternalUrl exception: " + ex.Message);
            return false;
        }
#elif UNITY_IOS && !UNITY_EDITOR
    try
    {
        Application.OpenURL(url);
        return true;
    }
    catch (Exception ex)
    {
        Debug.LogWarning("[WebView] iOS OpenExternalUrl exception: " + ex.Message);
        return false;
    }
#else
    if (debugLogs) Debug.Log("[WebView] OpenExternalUrl (editor simulated): " + url);
    return false;
#endif
    }

    private void HandleWebViewUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        if (debugLogs) Debug.Log("[WebView] Current URL: " + url);

        TryExtractOrderId(url);
        TryMarkPaymentFinished(url);
    }

    private void TryExtractOrderId(string url)
    {
        const string marker = "/payment/bank-transfer/";

        int index = url.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return;

        string orderId = url.Substring(index + marker.Length);

        int queryIndex = orderId.IndexOf('?');
        if (queryIndex >= 0)
            orderId = orderId.Substring(0, queryIndex);

        int hashIndex = orderId.IndexOf('#');
        if (hashIndex >= 0)
            orderId = orderId.Substring(0, hashIndex);

        orderId = orderId.Trim('/').Trim();

        if (string.IsNullOrEmpty(orderId))
            return;

        if (currentOrderId == orderId)
            return;

        currentOrderId = orderId;

        PlayerPrefs.SetString(PlayerPrefsOrderIdKey, currentOrderId);
        PlayerPrefs.Save();

        if (debugLogs) Debug.Log("[WebView] Extracted orderId: " + currentOrderId);
    }

    private void TryMarkPaymentFinished(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        bool isOrderPage =
            url.Contains("/en/order?", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("/vi/order?", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("/order?", StringComparison.OrdinalIgnoreCase);

        if (!isOrderPage)
            return;

        if (!string.IsNullOrEmpty(currentOrderId) &&
            !url.Contains("orderId=" + currentOrderId, StringComparison.OrdinalIgnoreCase))
            return;

        if (isPaymentFinished)
            return;

        isPaymentFinished = true;

        PlayerPrefs.SetInt(PlayerPrefsFinishedKey, 1);
        PlayerPrefs.Save();

        if (debugLogs)
            Debug.Log("[WebView] Payment finished. Matched final order page. orderId: " + currentOrderId);

        ExitView();
    }

    public static void LoadWebView()
    {
        pendingUrl = "";
        storeTitleCourse = "";
        ResetPaymentState(true);

        if (!SceneManager.GetSceneByName("WebView_Mobile").isLoaded)
        {
            SceneManager.LoadScene("WebView_Mobile", LoadSceneMode.Additive);
        }
    }

    public static void LoadWebView(string url, string title)
    {
        pendingUrl = url;
        storeTitleCourse = title;
        ResetPaymentState(false);

        if (!SceneManager.GetSceneByName("WebView_Mobile").isLoaded)
        {
            SceneManager.LoadScene("WebView_Mobile", LoadSceneMode.Additive);
        }
    }
    public static void SetCourseContext(string courseId, string courseSeo, string courseName = "")
    {
        currentCourseId = courseId ?? "";
        currentCourseSeo = courseSeo ?? "";
        currentCourseName = courseName ?? "";
    }

    private static void ResetPaymentState(bool clearCourseContext = false)
    {
        currentOrderId = "";
        isPaymentFinished = false;

        if (clearCourseContext)
        {
            currentCourseId = "";
            currentCourseSeo = "";
            currentCourseName = "";
            storeTitleCourse = "";
        }

        PlayerPrefs.DeleteKey(PlayerPrefsOrderIdKey);
        PlayerPrefs.DeleteKey(PlayerPrefsFinishedKey);
        PlayerPrefs.Save();
    }

    public static string GetSavedOrderId()
    {
        return PlayerPrefs.GetString(PlayerPrefsOrderIdKey, "");
    }

    public static bool GetSavedPaymentFinished()
    {
        return PlayerPrefs.GetInt(PlayerPrefsFinishedKey, 0) == 1;
    }

    private void Update()
    {
        UpdateButtonState();
    }

    private void UpdateButtonState()
    {
        if (_navigation == null || webViewInstance == null)
            return;

        _navigation.SetNavigationState(webViewInstance.CanGoBack, webViewInstance.CanGoForward);
    }

    public static void ClearPaymentState()
    {
        pendingUrl = "";
        storeTitleCourse = "";
        currentOrderId = "";
        isPaymentFinished = false;

        currentCourseId = "";
        currentCourseSeo = "";
        currentCourseName = "";

        PlayerPrefs.DeleteKey(PlayerPrefsOrderIdKey);
        PlayerPrefs.DeleteKey(PlayerPrefsFinishedKey);
        PlayerPrefs.Save();
    }
}