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
    public string xheroScheme = "xhero";
    public string xheroAndroidHost = "com.xhero_app";
    public string xheroPath = "/";

    public string codeParamName = "authLMSCode";
    public string timestampParamName = "timestamp";

    [Header("Extra param (required by XHero)")]
    public string functionParamName = "function";
    public string functionValue = "auth-for-lms";

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

    private void OnEnable()
    {
        TrySubscribeFirebase();
    }

    private void OnDisable()
    {
        // DeepLink có thể làm UI/panel disable giữa chừng -> nếu đang chạy flow thì KHÔNG unsubscribe
        if (_isRunning && !_loggedIn) return;

        StopFlow();
        UnsubscribeFirebase();
    }

    private void Update()
    {
        if (!_subscribedFirebase)
            TrySubscribeFirebase();
    }

    // ===================== PUBLIC =====================
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

    // ===================== SUBSCRIBE FIREBASE =====================
    private void TrySubscribeFirebase()
    {
        if (_subscribedFirebase) return;

        var fb = EnsureFirebase();

        // đảm bảo không subscribe trùng
        fb.OnAccessTokenReceived -= OnFirebaseAccessToken;
        fb.OnAccessTokenReceived += OnFirebaseAccessToken;

        _subscribedFirebase = true;
        Debug.Log("[LmsDeepLinkAuthUI] Subscribed FirebaseLoginQrPerCode.OnAccessTokenReceived");
    }

    private void UnsubscribeFirebase()
    {
        if (!_subscribedFirebase) return;

        if (FirebaseLoginQrPerCode.Instance != null)
            FirebaseLoginQrPerCode.Instance.OnAccessTokenReceived -= OnFirebaseAccessToken;

        _subscribedFirebase = false;
        Debug.Log("[LmsDeepLinkAuthUI] Unsubscribed FirebaseLoginQrPerCode.OnAccessTokenReceived");
    }

    // ===================== FIREBASE CALLBACK =====================
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

    // ===================== MAIN FLOW =====================
    private IEnumerator CoLoginFlow()
    {
        _isRunning = true;

        if (LmsStore.Instance == null || string.IsNullOrEmpty(LmsStore.Instance.baseUrl))
        {
            _isRunning = false;
            LoadingUI.Hide();
            LoginController.ShowWarning("Thiếu cấu hình baseUrl. Vui lòng kiểm tra LmsStore.");
            yield break;
        }

        // gọi API xin code
        string url = $"{LmsStore.Instance.baseUrl}{path}?step=1&platform={platform}";
        Debug.Log("[LmsDeepLinkAuthUI] step=1 URL = " + url);

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
                Debug.LogError("[LmsDeepLinkAuthUI] step=1 Request error: " + req.error + " | body=" + (req.downloadHandler != null ? req.downloadHandler.text : "<null>"));
                LoginController.ShowWarning("Không thể tạo mã đăng nhập. Vui lòng kiểm tra mạng và thử lại.");
                yield break;
            }

            LmsAuthResponse resp;
            try { resp = JsonUtility.FromJson<LmsAuthResponse>(req.downloadHandler.text); }
            catch (Exception e)
            {
                _isRunning = false;
                LoadingUI.Hide();
                Debug.LogError("[LmsDeepLinkAuthUI] Parse JSON fail: " + e + " | raw=" + req.downloadHandler.text);
                LoginController.ShowWarning("Dữ liệu trả về không hợp lệ.");
                yield break;
            }

            if (resp == null || resp.data == null || string.IsNullOrEmpty(resp.data.code))
            {
                _isRunning = false;
                LoadingUI.Hide();
                Debug.LogError("[LmsDeepLinkAuthUI] Missing data.code | raw=" + req.downloadHandler.text);
                LoginController.ShowWarning("Không lấy được mã đăng nhập.");
                yield break;
            }

            _currentCode = resp.data.code;
            _currentTimestamp = resp.data.timestamp;

            Debug.Log("[LmsDeepLinkAuthUI] step=1 OK => code=" + _currentCode + " | timestamp=" + _currentTimestamp);

            if (LmsStore.Instance != null)
            {
                LmsStore.Instance.lastLoginQrCode = _currentCode;
                LmsStore.Instance.lastLoginQrTimestamp = _currentTimestamp;
            }
        }

        // BÂY GIỜ mới listen firebase theo code
        EnsureFirebase().StartListen(_currentCode);

        // mở XHero trên CHÍNH thiết bị hiện tại
        OpenXHeroDeepLink(_currentCode, _currentTimestamp);

        // đợi token về (timeout)
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

    // ===================== OPEN XHERO =====================
    private void OpenXHeroDeepLink(string code, string timestamp)
    {
        string codeEnc = UnityWebRequest.EscapeURL(code ?? "");
        string tsEnc = UnityWebRequest.EscapeURL(timestamp ?? "");
        string fnEnc = UnityWebRequest.EscapeURL(functionValue ?? "");

        string host = xheroAndroidHost;

        string p = string.IsNullOrEmpty(xheroPath) ? "/" : xheroPath;
        if (!p.StartsWith("/")) p = "/" + p;

        string deepLinkUrl =
            $"{xheroScheme}://{host}{p}" +
            $"?{codeParamName}={codeEnc}" +
            $"&{timestampParamName}={tsEnc}" +
            $"&{functionParamName}={fnEnc}";

        Debug.Log("[LmsDeepLinkAuthUI] Open deep link: " + deepLinkUrl);
        Application.OpenURL(deepLinkUrl);
    }

    // ===================== JSON =====================
    [Serializable] private class LmsAuthResponse { public bool status; public LmsAuthData data; }
    [Serializable] private class LmsAuthData { public string code; public string timestamp; }

    private FirebaseLoginQrPerCode EnsureFirebase()
    {
        if (FirebaseLoginQrPerCode.Instance != null)
            return FirebaseLoginQrPerCode.Instance;

        var found = FindAnyObjectByType<FirebaseLoginQrPerCode>();
        if (found != null) return found;

        var go = new GameObject(nameof(FirebaseLoginQrPerCode));
        var inst = go.AddComponent<FirebaseLoginQrPerCode>();
        DontDestroyOnLoad(go);
        Debug.LogWarning("[LmsDeepLinkAuthUI] Created FirebaseLoginQrPerCode runtime.");
        return inst;
    }
}
