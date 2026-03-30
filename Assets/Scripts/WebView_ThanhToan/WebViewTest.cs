using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WebViewTest : MonoBehaviour
{
    [SerializeField] private UniWebView webViewPrefab;
    private UniWebView webViewInstance;
    private WebViewNavigation _navigation;

    private ScreenOrientation previousOrientation;

    [Header("Default URL")]
    [SerializeField] private string defaultUrl = SecurityConfig.UrlWeb + "/en";

    private static string pendingUrl = "";
    private static string storeTitleCourse = "";
    private static string currentOrderId = "";
    private static bool isPaymentFinished = false;

    public static string CurrentOrderId => currentOrderId;
    public static bool IsPaymentFinished => isPaymentFinished;
    public static string StoreTitleCourse => storeTitleCourse;

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

    private Coroutine bankTransferPollRoutine;
    private bool isNavigatingToBankTransfer = false;
    private float lastAutoLoadTime = -999f;
    private int autoNavigateAttempts = 0;
    private string lastAutoLoadedLink = "";

    private void Awake()
    {
        _navigation = GetComponentInChildren<WebViewNavigation>();
        previousOrientation = Screen.orientation;
        Screen.orientation = ScreenOrientation.Portrait;

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

        if (IsBankTransferUrl(currentUrl))
        {
            if (debugLogs) Debug.Log("[WebView] Already in bank transfer page. Stop auto-find.");
            return;
        }

        if (autoFindBankTransferLink && bankTransferPollRoutine == null)
        {
            bankTransferPollRoutine = StartCoroutine(PollBankTransferLinkRoutine());
        }
    }

    private IEnumerator PollBankTransferLinkRoutine()
    {
        yield return new WaitForSeconds(startPollingDelay);

        while (webViewInstance != null)
        {
            if (!autoFindBankTransferLink)
            {
                yield return new WaitForSeconds(linkPollInterval);
                continue;
            }

            if (isNavigatingToBankTransfer)
            {
                yield return new WaitForSeconds(linkPollInterval);
                continue;
            }

            // if (autoNavigateAttempts >= maxAutoNavigateAttempts)
            // {
            //     if (debugLogs)
            //         Debug.LogWarning("[WebView] Reached max auto navigate attempts. Stop polling.");
            //     break;
            // }

            if (Time.unscaledTime - lastAutoLoadTime < minSecondsBetweenAutoLoads)
            {
                yield return new WaitForSeconds(linkPollInterval);
                continue;
            }

            string currentUrl = webViewInstance.Url;
            if (!string.IsNullOrWhiteSpace(currentUrl) && IsBankTransferUrl(currentUrl))
            {
                if (debugLogs) Debug.Log("[WebView] Already at bank transfer URL while polling. Stop polling.");
                break;
            }

            bool finished = false;
            string foundLink = "";

            string js = @"
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

            webViewInstance.EvaluateJavaScript(js, payload =>
            {
                finished = true;
                foundLink = payload.data;
            });

            float timeoutAt = Time.unscaledTime + jsTimeout;
            while (!finished && Time.unscaledTime < timeoutAt)
            {
                yield return null;
            }

            if (!finished)
            {
                if (debugLogs) Debug.LogWarning("[WebView] JS polling timeout.");
                yield return new WaitForSeconds(linkPollInterval);
                continue;
            }

            foundLink = NormalizeJsResult(foundLink);

            if (debugLogs && !string.IsNullOrEmpty(foundLink))
            {
                Debug.Log("[WebView] Found bank transfer link from DOM: " + foundLink);
            }

            if (!string.IsNullOrEmpty(foundLink) && IsBankTransferUrl(foundLink))
            {
                if (string.Equals(foundLink, lastAutoLoadedLink, StringComparison.OrdinalIgnoreCase))
                {
                    if (debugLogs)
                        Debug.Log("[WebView] Found same link as last auto-loaded. Skip duplicate navigation.");
                }
                else
                {
                    isNavigatingToBankTransfer = true;
                    autoNavigateAttempts++;
                    lastAutoLoadTime = Time.unscaledTime;
                    lastAutoLoadedLink = foundLink;

                    if (debugLogs)
                        Debug.Log("[WebView] Auto loading bank transfer page: " + foundLink);

                    webViewInstance.Load(foundLink);

                    yield return new WaitForSeconds(1.0f);

                    isNavigatingToBankTransfer = false;
                }
            }

            yield return new WaitForSeconds(linkPollInterval);
        }

        bankTransferPollRoutine = null;
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
        ResetPaymentState();

        if (!SceneManager.GetSceneByName("WebView_Mobile").isLoaded)
        {
            SceneManager.LoadScene("WebView_Mobile", LoadSceneMode.Additive);
        }
    }

    public static void LoadWebView(string url, string title)
    {
        pendingUrl = url;
        storeTitleCourse = title;
        ResetPaymentState();

        if (!SceneManager.GetSceneByName("WebView_Mobile").isLoaded)
        {
            SceneManager.LoadScene("WebView_Mobile", LoadSceneMode.Additive);
        }
    }

    private static void ResetPaymentState()
    {
        currentOrderId = "";
        isPaymentFinished = false;

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

        PlayerPrefs.DeleteKey(PlayerPrefsOrderIdKey);
        PlayerPrefs.DeleteKey(PlayerPrefsFinishedKey);
        PlayerPrefs.Save();
    }
}