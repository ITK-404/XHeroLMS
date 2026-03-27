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

        if (webViewInstance != null)
        {
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
        _navigation.SetTitle(storeTitleCourse);
        
        webViewInstance = Instantiate(webViewPrefab, transform);
        webViewInstance.gameObject.SetActive(true);

        webViewInstance.OnPageFinished += OnWebPageFinished;
        
        LoadingUI.Show();
        yield return new WaitForSeconds(1f);
        LoadingUI.Hide();

        webViewInstance.Load(url);
        webViewInstance.Show();

        Debug.Log("[WebViewTest] Loaded URL: " + url);
    }

    private void OnWebPageFinished(UniWebView view, int statusCode, string currentUrl)
    {
        HandleWebViewUrl(currentUrl);
    }

    private void HandleWebViewUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;

        Debug.Log("[WebView] Current URL: " + url);

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

        Debug.Log("[WebView] Extracted orderId: " + currentOrderId);
    }

    private void TryMarkPaymentFinished(string url)
    {
        const string finishMarker = "/don-hang";

        if (!url.Contains(finishMarker, StringComparison.OrdinalIgnoreCase))
            return;

        if (isPaymentFinished)
            return;

        isPaymentFinished = true;

        PlayerPrefs.SetInt(PlayerPrefsFinishedKey, 1);
        PlayerPrefs.Save();

        Debug.Log("[WebView] Payment flow finished. Ready to check order detail.");
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
        
        Debug.Log($"WebView Update Title {title}");
        
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