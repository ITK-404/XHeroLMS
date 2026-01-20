using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Events;
//xhero://xhero.deeplink?authLMSCode=1&timestamp=123
public class LmsDeepLinkAuthUI : MonoBehaviour
{
    [Header("API config")]
    public string path = "/auth-for-lms/request";
    public string platform = "pc";
    public float requestTimeout = 10f;

    [Header("XHero deeplink")]
    public string xheroScheme = "xhero";

    [Tooltip("ANDROID host (as in AndroidManifest intent-filter host)")]
    public string xheroAndroidHost = "com.xhero_app";
    public string xheroAndroidPackageName = "com.xhero_app";

    [Tooltip("iOS host (as in iOS URLTypes / associated host config if they use host)")]
    public string xheroIosHost = "com.xhero.app";

    [Tooltip("Deep link path. Example: '/', '/auth', ...")]
    public string xheroPath = "/";

    public string codeParamName = "authLMSCode";
    public string timestampParamName = "timestamp";

    [Header("Extra param (required by XHero)")]
    public string functionParamName = "function";
    public string functionValue = "auth-for-lms";
    [Header("XHero deeplink host")]
    public string xheroHost = "xhero.deeplink";

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

        // step=1: request code
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
                Debug.LogError("[LmsDeepLinkAuthUI] step=1 Request error: " + req.error +
                               " | body=" + (req.downloadHandler != null ? req.downloadHandler.text : "<null>"));
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

if (!OpenXHeroDeepLink(_currentCode, _currentTimestamp))
    yield break;

try
{
    EnsureFirebase().StartListen(_currentCode);
}
catch (Exception e)
{
    Debug.LogError("[LmsDeepLinkAuthUI] Firebase StartListen failed: " + e);
}

        // wait token
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
    private string GetXHeroHostForPlatform()
    {
#if UNITY_IOS
        return string.IsNullOrEmpty(xheroIosHost) ? xheroAndroidHost : xheroIosHost;
#else
        // default Android + Editor
        return xheroAndroidHost;
#endif
    }

    private bool OpenXHeroDeepLink(string code, string timestamp)
    {
        if (!CanOpenXHeroApp())
        {
            _isRunning = false;
            _loggedIn = false;

            // stop listen firebase nếu đang nghe
            try
            {
                if (FirebaseLoginQrPerCode.Instance != null)
                    FirebaseLoginQrPerCode.Instance.StopListen();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[LmsDeepLinkAuthUI] StopListen failed: " + e);
            }

            LoadingUI.Hide();
            LoadingUI.ShowErrorPopup(
                "Thiết bị của bạn chưa có ứng dụng XHero để đăng nhập,\nVui lòng tải ứng dụng và thử lại",
                "Thông báo"
            );

            StopFlow();
            return false;
        }

        string codeEnc = UnityWebRequest.EscapeURL(code ?? "");
        string tsEnc   = UnityWebRequest.EscapeURL(timestamp ?? "");
        string fnEnc   = UnityWebRequest.EscapeURL(functionValue ?? "");

        string host = "xhero.deeplink"; // hoặc xheroHost / GetXHeroHostForPlatform()

        string deepLinkUrl =
            $"{xheroScheme}://{host}" +
            $"?{codeParamName}={codeEnc}" +
            $"&{timestampParamName}={tsEnc}" +
            $"&{functionParamName}={fnEnc}";

        Debug.Log($"[LmsDeepLinkAuthUI] Open deep link ({Application.platform}): {deepLinkUrl}");
        Application.OpenURL(deepLinkUrl);
        return true;
    }
    private bool CanOpenXHeroApp()
    {
    #if UNITY_EDITOR
        return true; // Editor không check
    #elif UNITY_IOS
        // iOS: cần khai báo LSApplicationQueriesSchemes trong Info.plist mới CanOpenURL trả true
        try
        {
            return Application.CanOpenURL($"{xheroScheme}://");
        }
        catch (Exception e)
        {
            Debug.LogWarning("[LmsDeepLinkAuthUI] CanOpenURL exception: " + e);
            // Nếu lỗi môi trường, cho qua để không block login
            return true;
        }
    #elif UNITY_ANDROID
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var pm = activity.Call<AndroidJavaObject>("getPackageManager"))
            {
                // Có app => có launch intent
                AndroidJavaObject intent = pm.Call<AndroidJavaObject>(
                    "getLaunchIntentForPackage",
                    xheroAndroidPackageName
                );
                return intent != null;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[LmsDeepLinkAuthUI] Android package check failed: " + e);
            // Nếu check fail do device policy/manifest queries, cho qua để không block login
            return true;
        }
    #else
        return true;
    #endif
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
