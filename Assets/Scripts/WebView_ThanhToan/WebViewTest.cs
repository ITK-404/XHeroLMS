using System.Collections;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.SceneManagement;

public class WebViewTest : MonoBehaviour
{
    [SerializeField] private UniWebView webViewPrefab;
    private UniWebView webViewInstance;
    private WebViewNavigation _navigation;

    private ScreenOrientation previousOrientation;
    private void Awake()
    {
        previousOrientation = Screen.orientation;
        Screen.orientation = ScreenOrientation.Portrait;
        StartCoroutine(CreateWebView());

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
        if (webViewInstance && webViewInstance.CanGoBack)
        {
            webViewInstance.GoBack();
        }
    }

    private void GoForward()
    {
        if (webViewInstance && webViewInstance.CanGoForward)
        {
            webViewInstance.GoForward();
        }
    }

    private void ReloadView()
    {
        if (webViewInstance)
        {
            webViewInstance.Reload();
        }
    }

    private void ExitView()
    {
        Screen.orientation = previousOrientation;
        SceneManager.UnloadSceneAsync("WebView_Mobile");
    }

    private IEnumerator CreateWebView()
    {
        webViewInstance = Instantiate(webViewPrefab, transform);
        webViewInstance.gameObject.SetActive(true);
        LoadingUI.Show();
        yield return new WaitForSeconds(1f);
        LoadingUI.Hide();

        webViewInstance.Load("https://daotao.phongthuydainam.vn/en");
        webViewInstance.Show();
    }

    public static void LoadWebView()
    {
        if (!SceneManager.GetSceneByName("WebView_Mobile").isLoaded)
        {
            SceneManager.LoadScene("WebView_Mobile", LoadSceneMode.Additive);
        }
    }
}

public class LoadToWebViewTest : MonoBehaviour
{
}