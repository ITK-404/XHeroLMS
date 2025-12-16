using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;

public class LmsDeepLinkAuthUI : MonoBehaviour
{
    [Header("API config")]
    public string path = "/auth-for-lms/request";
    public string platform = "pc";
    public float requestTimeout = 10f;

    [Header("XHero deeplink (MATCH AndroidManifest of XHero)")]
    [Tooltip("Scheme của XHero")]
    public string xheroScheme = "xhero";

    [Tooltip("Android host đúng theo manifest XHero: com.xhero_app")]
    public string xheroAndroidHost = "com.xhero_app";

    [Tooltip("Path (optional). manifest dùng pathPrefix=\"/\" nên để \"/\" là ok.")]
    public string xheroPath = "/";

    [Tooltip("Query param name theo XHero team")]
    public string codeParamName = "authLMSCode";

    [Tooltip("Query param name theo XHero team")]
    public string timestampParamName = "timestamp";

    [Header("Flow")]
    public float waitTokenTimeoutSeconds = 25f;
    public float antiSpamSeconds = 3f;

    [Serializable] public class StringEvent : UnityEvent<string> { }
    public StringEvent onLoginSuccess;

    private bool _subscribedFirebase = false;
    private bool _isRunning = false;
    private bool _loggedIn = false;
    private float _antiSpamUntil = 0f;

    private Coroutine _flowCo;
    private string _currentCode;
    private string _currentTimestamp;

    private void OnEnable() => TrySubscribeFirebase();

    private void OnDisable()
    {
        StopFlow();
        UnsubscribeFirebase();
    }

    // ===== PUBLIC =====
    public void StartDeepLinkLogin()
    {
        if (Time.unscaledTime < _antiSpamUntil)
        {
            LoadingUI.Hide();
            LoginController.ShowWarning("Bạn thao tác quá nhanh. Vui lòng thử lại sau.");
            return;
        }
        _antiSpamUntil = Time.unscaledTime + antiSpamSeconds;

        if (_isRunning) return;

        _loggedIn = false;
        _currentCode = null;
        _currentTimestamp = null;

        TrySubscribeFirebase();

        StopFlow();
        _flowCo = StartCoroutine(CoLoginFlow());
    }

    private void StopFlow()
    {
        _isRunning = false;
        if (_flowCo != null)
        {
            StopCoroutine(_flowCo);
            _flowCo = null;
        }
    }

    // ===== SUBSCRIBE FIREBASE =====
    private void TrySubscribeFirebase()
    {
        if (_subscribedFirebase) return;

        if (FirebaseLoginQrPerCode.Instance != null)
        {
            FirebaseLoginQrPerCode.Instance.OnAccessTokenReceived -= OnFirebaseAccessToken;
            FirebaseLoginQrPerCode.Instance.OnAccessTokenReceived += OnFirebaseAccessToken;
            _subscribedFirebase = true;
            Debug.Log("[LmsDeepLinkAuthUI] Subscribed FirebaseLoginQrPerCode.OnAccessTokenReceived");
        }
        else
        {
            Debug.LogWarning("[LmsDeepLinkAuthUI] FirebaseLoginQrPerCode.Instance is NULL");
        }
    }

    private void UnsubscribeFirebase()
    {
        if (!_subscribedFirebase) return;

        if (FirebaseLoginQrPerCode.Instance != null)
            FirebaseLoginQrPerCode.Instance.OnAccessTokenReceived -= OnFirebaseAccessToken;

        _subscribedFirebase = false;
    }

    // ===== FIREBASE CALLBACK =====
    private void OnFirebaseAccessToken(string accessToken)
    {
        Debug.Log("[LmsDeepLinkAuthUI] OnFirebaseAccessToken: " + accessToken);

        if (string.IsNullOrEmpty(accessToken)) return;
        if (_loggedIn) return;

        _loggedIn = true;
        _isRunning = false;

        LoadingUI.Hide();

        LoginController.LoginWithQrToken(accessToken);
        onLoginSuccess?.Invoke(accessToken);

        if (LmsStore.Instance != null)
            LmsStore.Instance.ClearQrLoginCache();

        StopFlow();
    }

    // ===== MAIN FLOW =====
    private IEnumerator CoLoginFlow()
    {
        _isRunning = true;

        // 1) gọi API xin code
        string url = $"{LmsStore.Instance.baseUrl}{path}?step=1&platform={platform}";
        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = (int)requestTimeout;

#if UNITY_2020_2_OR_NEWER
            yield return req.SendWebRequest();
            bool hasError = req.result != UnityWebRequest.Result.Success;
#else
            yield return req.SendWebRequest();
            bool hasError = req.isNetworkError || req.isHttpError;
#endif

            if (hasError)
            {
                _isRunning = false;
                LoadingUI.Hide();
                Debug.LogError("[LmsDeepLinkAuthUI] Request error: " + req.error);
                LoginController.ShowWarning("Không thể tạo mã đăng nhập. Vui lòng kiểm tra mạng và thử lại.");
                yield break;
            }

            LmsAuthResponse resp;
            try { resp = JsonUtility.FromJson<LmsAuthResponse>(req.downloadHandler.text); }
            catch (Exception e)
            {
                _isRunning = false;
                LoadingUI.Hide();
                Debug.LogError("[LmsDeepLinkAuthUI] Parse JSON fail: " + e);
                LoginController.ShowWarning("Dữ liệu trả về không hợp lệ.");
                yield break;
            }

            if (resp == null || resp.data == null || string.IsNullOrEmpty(resp.data.code))
            {
                _isRunning = false;
                LoadingUI.Hide();
                Debug.LogError("[LmsDeepLinkAuthUI] Missing data.code");
                LoginController.ShowWarning("Không lấy được mã đăng nhập.");
                yield break;
            }

            _currentCode = resp.data.code;
            _currentTimestamp = resp.data.timestamp;

            if (LmsStore.Instance != null)
            {
                LmsStore.Instance.lastLoginQrCode = _currentCode;
                LmsStore.Instance.lastLoginQrTimestamp = _currentTimestamp;
            }
        }

        // 2) listen firebase theo code
        if (FirebaseLoginQrPerCode.Instance != null)
            FirebaseLoginQrPerCode.Instance.StartListen(_currentCode);

        // 3) mở XHero trên CHÍNH thiết bị hiện tại
        OpenXHeroDeepLink(_currentCode, _currentTimestamp);

        // 4) đợi token về (timeout)
        float t = waitTokenTimeoutSeconds;
        while (t > 0f && !_loggedIn)
        {
            t -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (!_loggedIn)
        {
            _isRunning = false;
            LoadingUI.Hide();
            LoginController.ShowWarning(
                "Chưa nhận được token từ XHero.\n" +
                "Hãy mở XHero và xác nhận đăng nhập."
            );
        }
    }

    // ===== OPEN XHERO =====
    private void OpenXHeroDeepLink(string code, string timestamp)
    {
        string codeEnc = UnityWebRequest.EscapeURL(code);
        string tsEnc   = UnityWebRequest.EscapeURL(timestamp);

        // Android host phải đúng manifest: com.xhero_app
        // iOS không dùng host theo bundleId; vẫn thử mở cùng format này để XHero parse thống nhất.
        string host = xheroAndroidHost;

        // đảm bảo path có "/" đầu
        string path = string.IsNullOrEmpty(xheroPath) ? "/" : xheroPath;
        if (!path.StartsWith("/")) path = "/" + path;

        string deepLinkUrl =
            $"{xheroScheme}://{host}{path}?{codeParamName}={codeEnc}&{timestampParamName}={tsEnc}";

        Debug.Log("[LmsDeepLinkAuthUI] Open deep link: " + deepLinkUrl);
        Application.OpenURL(deepLinkUrl);
    }

    // ===== JSON =====
    [Serializable] private class LmsAuthResponse { public bool status; public LmsAuthData data; }
    [Serializable] private class LmsAuthData { public string code; public string timestamp; }
}
