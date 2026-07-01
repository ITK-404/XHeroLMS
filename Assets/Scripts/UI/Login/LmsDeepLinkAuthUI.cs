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

    [Header("XHero deeplink")]
    public string xheroScheme = "xhero";

    [Tooltip("ANDROID host. Must match XHero Android deeplink parser.")]
    public string xheroAndroidHost = "xhero.deeplink";

    [Tooltip("ANDROID package name of XHero app.")]
    public string xheroAndroidPackageName = "com.xhero_app";

    [Tooltip("iOS host. Must match XHero iOS deeplink parser.")]
    public string xheroIosHost = "xhero.deeplink";

    [Tooltip("Fallback host if platform host is empty.")]
    public string xheroHost = "xhero.deeplink";

    [Tooltip("Deep link path. Usually empty. Leave empty if XHero parses query from host root.")]
    public string xheroPath = "";

    public string codeParamName = "authLMSCode";
    public string timestampParamName = "timestamp";

    [Header("Extra param required by XHero")]
    public string functionParamName = "function";
    public string functionValue = "auth-for-lms";

    [Header("Store fallback")]
    [Tooltip("Android Play Store package id. Usually same as xheroAndroidPackageName.")]
    public string androidStorePackageName = "com.xhero_app";

    [Tooltip("Optional Android store url override. If empty, market://details?id=... will be used.")]
    public string androidStoreUrlOverride = "";

    [Tooltip("Optional Android web fallback url.")]
    public string androidStoreWebUrl = "https://play.google.com/store/apps/details?id=com.xhero_app";

    [Tooltip("iOS App Store app id.")]
    public string iosAppStoreId = "6504331040";

    [Tooltip("Optional iOS store url override. If empty, itms-apps://itunes.apple.com/app/id... will be used.")]
    public string iosStoreUrlOverride = "itms-apps://itunes.apple.com/app/id6504331040";

    [Tooltip("Optional iOS web fallback url.")]
    public string iosStoreWebUrl = "https://apps.apple.com/vn/app/xhero/id6504331040?l=vi";

    [Header("Flow")]
    public float waitTokenTimeoutSeconds = 25f;
    public float antiSpamSeconds = 3f;

    [Header("Step=2 fallback")]
    public bool requestStep2WhenReturningFromXHero = true;
    public float step2FallbackRetryIntervalSeconds = 2f;

    [Serializable]
    public class StringEvent : UnityEvent<string> { }

    public StringEvent onLoginSuccess;

    private bool _subscribedFirebase = false;
    private bool _isRunning = false;
    private bool _loggedIn = false;
    private float _antiSpamUntil = 0f;

    private Coroutine _flowCo;
    private string _currentCode;
    private string _currentTimestamp;
    private bool _openedXHeroApp = false;
    private float _nextStep2FallbackAt = 0f;

    private void OnEnable()
    {
        TrySubscribeFirebase();
    }

    private void OnDisable()
    {
        if (_isRunning && !_loggedIn)
        {
            Debug.Log("[LmsDeepLinkAuthUI] OnDisable ignored because login flow is running.");
            return;
        }

        StopFlow();
        UnsubscribeFirebase();
    }

    private void Update()
    {
        if (!_subscribedFirebase)
            TrySubscribeFirebase();
    }

    private void OnApplicationPause(bool pause)
    {
        Debug.Log("[LmsDeepLinkAuthUI] OnApplicationPause: " + pause);

        if (!pause)
        {
            TrySubscribeFirebase();

            if (_isRunning && !_loggedIn && !string.IsNullOrEmpty(_currentCode))
            {
                Debug.Log("[LmsDeepLinkAuthUI] App resumed while waiting token. code=" + _currentCode);
                TryRequestStep2Fallback("app_resumed");
            }
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        Debug.Log("[LmsDeepLinkAuthUI] OnApplicationFocus: " + hasFocus);

        if (hasFocus)
        {
            TrySubscribeFirebase();

            if (_isRunning && !_loggedIn && !string.IsNullOrEmpty(_currentCode))
            {
                Debug.Log("[LmsDeepLinkAuthUI] App focused while waiting token. code=" + _currentCode);
                TryRequestStep2Fallback("app_focused");
            }
        }
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

        if (_isRunning)
        {
            Debug.LogWarning("[LmsDeepLinkAuthUI] Login flow already running.");
            return;
        }

        _loggedIn = false;
        _currentCode = null;
        _currentTimestamp = null;
        _openedXHeroApp = false;
        _nextStep2FallbackAt = 0f;

        TrySubscribeFirebase();

        StopFlow();
        _flowCo = StartCoroutine(CoLoginFlow());
    }

    public void OpenXHeroStoreManually()
    {
        OpenXHeroStore();
    }

    // ===================== FLOW CONTROL =====================

    private void StopFlow()
    {
        _isRunning = false;

        if (_flowCo != null)
        {
            StopCoroutine(_flowCo);
            _flowCo = null;
        }
    }

    private void StopFirebaseListenSafe(string reason)
    {
        try
        {
            if (FirebaseLoginQrPerCode.Instance != null)
            {
                FirebaseLoginQrPerCode.Instance.StopListen();
                Debug.Log("[LmsDeepLinkAuthUI] Firebase StopListen. reason=" + reason);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[LmsDeepLinkAuthUI] StopListen failed. reason=" + reason + " | " + e);
        }
    }

    // ===================== SUBSCRIBE FIREBASE =====================

    private void TrySubscribeFirebase()
    {
        if (_subscribedFirebase)
            return;

        var fb = EnsureFirebase();

        fb.OnAccessTokenReceived -= OnFirebaseAccessToken;
        fb.OnAccessTokenReceived += OnFirebaseAccessToken;

        _subscribedFirebase = true;
        Debug.Log("[LmsDeepLinkAuthUI] Subscribed FirebaseLoginQrPerCode.OnAccessTokenReceived");
    }

    private void UnsubscribeFirebase()
    {
        if (!_subscribedFirebase)
            return;

        if (FirebaseLoginQrPerCode.Instance != null)
            FirebaseLoginQrPerCode.Instance.OnAccessTokenReceived -= OnFirebaseAccessToken;

        _subscribedFirebase = false;
        Debug.Log("[LmsDeepLinkAuthUI] Unsubscribed FirebaseLoginQrPerCode.OnAccessTokenReceived");
    }

    // ===================== FIREBASE CALLBACK =====================

    private void OnFirebaseAccessToken(string accessToken)
    {
        Debug.Log("[LmsDeepLinkAuthUI] OnFirebaseAccessToken: " + accessToken);

        if (string.IsNullOrEmpty(accessToken))
        {
            Debug.LogWarning("[LmsDeepLinkAuthUI] Access token is null or empty.");
            return;
        }

        if (_loggedIn)
        {
            Debug.LogWarning("[LmsDeepLinkAuthUI] Already logged in. Ignore duplicated token.");
            return;
        }

        _loggedIn = true;
        _isRunning = false;

        LoadingUI.Hide();

        LoginController.LoginWithQrToken(accessToken);
        onLoginSuccess?.Invoke(accessToken);

        if (LmsStore.Instance != null)
            LmsStore.Instance.ClearQrLoginCache();

        StopFirebaseListenSafe("login_success");
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

            Debug.LogError("[LmsDeepLinkAuthUI] Missing LmsStore/baseUrl.");
            LoginController.ShowWarning("Thiếu cấu hình baseUrl. Vui lòng kiểm tra LmsStore.");
            yield break;
        }

        string url = $"{LmsStore.Instance.baseUrl}{path}?step=1&platform={UnityWebRequest.EscapeURL(platform)}";
        Debug.Log("[LmsDeepLinkAuthUI] step=1 URL = " + url);

        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = Mathf.Max(1, Mathf.RoundToInt(requestTimeout));

            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool hasError = req.result != UnityWebRequest.Result.Success;
#else
            bool hasError = req.isNetworkError || req.isHttpError;
#endif

            if (hasError)
            {
                _isRunning = false;
                LoadingUI.Hide();

                Debug.LogError(
                    "[LmsDeepLinkAuthUI] step=1 Request error: " + req.error +
                    " | responseCode=" + req.responseCode +
                    " | body=" + (req.downloadHandler != null ? req.downloadHandler.text : "<null>")
                );

                LoginController.ShowWarning("Không thể tạo mã đăng nhập. Vui lòng kiểm tra mạng và thử lại.");
                yield break;
            }

            LmsAuthResponse resp;

            try
            {
                resp = JsonUtility.FromJson<LmsAuthResponse>(req.downloadHandler.text);
            }
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

        /*
         * iOS important:
         * Firebase must listen BEFORE Application.OpenURL.
         * If OpenURL sends Unity to background first, code after OpenURL may not run reliably.
         */
        try
        {
            EnsureFirebase().StartListen(_currentCode);
            Debug.Log("[LmsDeepLinkAuthUI] Firebase StartListen OK BEFORE open deeplink. code=" + _currentCode);
        }
        catch (Exception e)
        {
            _isRunning = false;
            LoadingUI.Hide();

            Debug.LogError("[LmsDeepLinkAuthUI] Firebase StartListen failed BEFORE open deeplink: " + e);
            LoginController.ShowWarning("Không thể bắt đầu lắng nghe đăng nhập. Vui lòng thử lại.");
            yield break;
        }

        if (!OpenXHeroDeepLink(_currentCode, _currentTimestamp))
        {
            StopFirebaseListenSafe("open_xhero_failed");
            yield break;
        }

        float t = waitTokenTimeoutSeconds;

        while (t > 0f && !_loggedIn)
        {
            TryRequestStep2Fallback("wait_loop");
            t -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (!_loggedIn)
        {
            _isRunning = false;
            LoadingUI.Hide();

            StopFirebaseListenSafe("wait_token_timeout");

            LoginController.ShowWarning(
                "Hãy mở XHero và xác nhận đăng nhập lại."
            );
        }
    }

    // ===================== DEEPLINK =====================

    private string GetXHeroHostForPlatform()
    {
#if UNITY_IOS
        if (!string.IsNullOrEmpty(xheroIosHost))
            return xheroIosHost;
#elif UNITY_ANDROID
        if (!string.IsNullOrEmpty(xheroAndroidHost))
            return xheroAndroidHost;
#endif

        return string.IsNullOrEmpty(xheroHost) ? "xhero.deeplink" : xheroHost;
    }

private string BuildXHeroDeepLinkUrl(string code, string timestamp)
{
    string codeEnc = UnityWebRequest.EscapeURL(code ?? "");
    string tsEnc = UnityWebRequest.EscapeURL(timestamp ?? "");
    string fnEnc = UnityWebRequest.EscapeURL(functionValue ?? "");

    // Giữ giống bản cũ: luôn dùng xhero.deeplink
    string host = string.IsNullOrEmpty(xheroHost) ? "xhero.deeplink" : xheroHost;

    string deepLinkUrl =
        $"{xheroScheme}://{host}" +
        $"?{codeParamName}={codeEnc}" +
        $"&{timestampParamName}={tsEnc}" +
        $"&{functionParamName}={fnEnc}";

    return deepLinkUrl;
}

    private bool OpenXHeroDeepLink(string code, string timestamp)
    {
        if (!CanOpenXHeroApp())
        {
            _isRunning = false;
            _loggedIn = false;

            LoadingUI.Hide();

            LoadingUI.ShowErrorPopup(
                "Thiết bị của bạn chưa có ứng dụng XHero để đăng nhập,\nỨng dụng sẽ chuyển bạn đến cửa hàng để tải XHero.",
                "Thông báo"
            );

            OpenXHeroStore();

            StopFlow();
            return false;
        }

        string deepLinkUrl = BuildXHeroDeepLinkUrl(code, timestamp);

        Debug.Log($"[LmsDeepLinkAuthUI] Open deep link ({Application.platform}): {deepLinkUrl}");

        _openedXHeroApp = true;
        _nextStep2FallbackAt = Time.unscaledTime + Mathf.Max(0.5f, step2FallbackRetryIntervalSeconds);
        Application.OpenURL(deepLinkUrl);
        return true;
    }

    private void TryRequestStep2Fallback(string reason)
    {
        if (!requestStep2WhenReturningFromXHero || !_openedXHeroApp || !_isRunning || _loggedIn)
            return;

        if (string.IsNullOrEmpty(_currentCode) || string.IsNullOrEmpty(_currentTimestamp))
            return;

        if (Time.unscaledTime < _nextStep2FallbackAt)
            return;

        float retryInterval = Mathf.Max(0.5f, step2FallbackRetryIntervalSeconds);
        _nextStep2FallbackAt = Time.unscaledTime + retryInterval;

        try
        {
            EnsureFirebase().RequestStep2Token(_currentCode, _currentTimestamp, reason);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[LmsDeepLinkAuthUI] Step=2 fallback request failed. reason=" + reason + " | " + e);
        }
    }

    private bool CanOpenXHeroApp()
    {
#if UNITY_EDITOR
        return true;

#elif UNITY_ANDROID
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var pm = activity.Call<AndroidJavaObject>("getPackageManager"))
            using (var intent = new AndroidJavaObject("android.content.Intent", "android.intent.action.VIEW"))
            using (var uriClass = new AndroidJavaClass("android.net.Uri"))
            {
                string testUrl = $"{xheroScheme}://{GetXHeroHostForPlatform()}";

                using (var uri = uriClass.CallStatic<AndroidJavaObject>("parse", testUrl))
                {
                    intent.Call<AndroidJavaObject>("setData", uri);

                    var resolved = pm.Call<AndroidJavaObject>("resolveActivity", intent, 0);
                    bool canOpen = resolved != null;

                    Debug.Log("[LmsDeepLinkAuthUI] Android CanOpenXHeroApp: " + canOpen + " | testUrl=" + testUrl);

                    return canOpen;
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[LmsDeepLinkAuthUI] Android deeplink resolve check failed: " + e);

            /*
             * Fail-safe:
             * Do not block login if resolveActivity fails unexpectedly.
             * Application.OpenURL may still work.
             */
            return true;
        }

#elif UNITY_IOS
        string testUrl = $"{xheroScheme}://";

        bool canOpen = IOSUrlChecker.CanOpen(testUrl);

        Debug.Log("[LmsDeepLinkAuthUI] iOS CanOpenXHeroApp: " + canOpen + " | testUrl=" + testUrl);

        return canOpen;

#else
        return true;
#endif
    }

    // ===================== STORE FALLBACK =====================

    private void OpenXHeroStore()
    {
        string storeUrl = GetXHeroStoreUrl();

        if (string.IsNullOrEmpty(storeUrl))
        {
            Debug.LogError("[LmsDeepLinkAuthUI] Store URL is empty. Please set Android package name or iOS App Store ID.");
            return;
        }

        Debug.Log("[LmsDeepLinkAuthUI] Open XHero store: " + storeUrl);
        Application.OpenURL(storeUrl);
    }

    private string GetXHeroStoreUrl()
    {
#if UNITY_ANDROID
        if (!string.IsNullOrEmpty(androidStoreUrlOverride))
            return androidStoreUrlOverride;

        string pkg = !string.IsNullOrEmpty(androidStorePackageName)
            ? androidStorePackageName
            : xheroAndroidPackageName;

        if (!string.IsNullOrEmpty(pkg))
            return "market://details?id=" + pkg;

        if (!string.IsNullOrEmpty(androidStoreWebUrl))
            return androidStoreWebUrl;

        return "";

#elif UNITY_IOS
        if (!string.IsNullOrEmpty(iosStoreUrlOverride))
            return iosStoreUrlOverride;

        if (!string.IsNullOrEmpty(iosAppStoreId))
            return "itms-apps://itunes.apple.com/app/id" + iosAppStoreId;

        if (!string.IsNullOrEmpty(iosStoreWebUrl))
            return iosStoreWebUrl;

        return "";

#else
        return "";
#endif
    }

    // ===================== JSON =====================

    [Serializable]
    private class LmsAuthResponse
    {
        public bool status;
        public LmsAuthData data;
    }

    [Serializable]
    private class LmsAuthData
    {
        public string code;
        public string timestamp;
    }

    // ===================== FIREBASE INSTANCE =====================

    private FirebaseLoginQrPerCode EnsureFirebase()
    {
        if (FirebaseLoginQrPerCode.Instance != null)
            return FirebaseLoginQrPerCode.Instance;

        var found = FindAnyObjectByType<FirebaseLoginQrPerCode>();
        if (found != null)
            return found;

        var go = new GameObject(nameof(FirebaseLoginQrPerCode));
        var inst = go.AddComponent<FirebaseLoginQrPerCode>();

        DontDestroyOnLoad(go);

        Debug.LogWarning("[LmsDeepLinkAuthUI] Created FirebaseLoginQrPerCode runtime.");

        return inst;
    }
}