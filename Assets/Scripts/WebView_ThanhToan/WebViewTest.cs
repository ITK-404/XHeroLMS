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
    [SerializeField] private string defaultUrl = "https://daotao.phongthuydainam.vn/en";

    private static string pendingUrl = "";

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
        webViewInstance = Instantiate(webViewPrefab, transform);
        webViewInstance.gameObject.SetActive(true);

        LoadingUI.Show();
        yield return new WaitForSeconds(1f);
        LoadingUI.Hide();

        webViewInstance.Load(url);
        webViewInstance.Show();

        Debug.Log("[WebViewTest] Loaded URL: " + url);
    }

    public static void LoadWebView()
    {
        pendingUrl = "";

        if (!SceneManager.GetSceneByName("WebView_Mobile").isLoaded)
        {
            SceneManager.LoadScene("WebView_Mobile", LoadSceneMode.Additive);
        }
    }

    public static void LoadWebView(string url)
    {
        pendingUrl = url;

        if (!SceneManager.GetSceneByName("WebView_Mobile").isLoaded)
        {
            SceneManager.LoadScene("WebView_Mobile", LoadSceneMode.Additive);
        }
    }

    private void Update()
    {
        UpdateButtonState();
    }

    private void UpdateButtonState()
    {
        _navigation.SetNavigationState(webViewInstance.CanGoBack,webViewInstance.CanGoForward);
    }
}