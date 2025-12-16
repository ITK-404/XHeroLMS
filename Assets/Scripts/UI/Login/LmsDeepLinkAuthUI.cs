using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Events;
using TMPro;

public class LmsDeepLinkAuthUI : MonoBehaviour
{
    [Header("API config")]
    public string path = "/auth-for-lms/request";
    public string platform = "pc";
    public float requestTimeout = 10f;

    [Header("UI")]
    public TMP_Text countdownText;
    public Button startButton;     // nút "Đăng nhập qua App"
    public Button refreshButton;   // xin code mới (anti-spam giống QR)
    public Button openStoreButton; // optional: mở store thủ công (phòng iOS)
    public Button btnBack;
    public GameObject currentPanel;
    public GameObject backPanel;

    [Header("Thời gian sống của code (giây)")]
    public float codeLifetimeSeconds = 120f;

    [Header("Deep link config")]
    [Tooltip("Custom scheme của XHero. Ví dụ: xhero")]
    public string xheroScheme = "xhero";

    [Tooltip("Host/path mà app XHero hiểu. Ví dụ: auth-for-lms")]
    public string xheroHostPath = "auth-for-lms";

    [Tooltip("Android package id của app XHero (để check đã cài chưa)")]
    public string androidPackageName = "com.yourcompany.xhero";

    [Tooltip("Link CH Play fallback")]
    public string androidPlayStoreUrl = "https://play.google.com/store/apps/details?id=com.yourcompany.xhero";

    [Tooltip("Link AppStore fallback (iOS)")]
    public string iosAppStoreUrl = "https://apps.apple.com/app/id0000000000";

    [Header("Anti spam refresh")]
    public float antiSpamSeconds = 120f; // giống _qrExpireTime

    [Serializable]
    public class StringEvent : UnityEvent<string> { }
    public StringEvent onLoginSuccess;

    private Coroutine _countdownCo;

    private string _currentCode;
    private string _currentTimestamp;
    private bool _loggedIn;

    private bool _subscribedFirebase = false;
    private float _expireAt = 0f;      // code lifetime
    private float _antiSpamUntil = 0f; // chống spam refresh

    private string baseUrl;

    private void Awake()
    {
        baseUrl = LmsStore.Instance.baseUrl;

        if (startButton != null)
            startButton.onClick.AddListener(StartDeepLinkLogin);

        if (refreshButton != null)
            refreshButton.onClick.AddListener(OnClickRefresh);

        if (openStoreButton != null)
            openStoreButton.onClick.AddListener(OpenStore);
    }

    private void Start()
    {
        if (btnBack) btnBack.onClick.AddListener(BackAndReset);
    }

    private void BackAndReset()
    {
        if (currentPanel) currentPanel.SetActive(false);
        if (backPanel) backPanel.SetActive(true);
    }

    private void OnEnable()
    {
        _loggedIn = false;
        _subscribedFirebase = false;

        // Chỉ subscribe Firebase, chưa xin code cho tới khi user bấm
        TrySubscribeFirebase();
    }

    private void OnDisable()
    {
        if (_countdownCo != null)
        {
            StopCoroutine(_countdownCo);
            _countdownCo = null;
        }

        UnsubscribeFirebase();
    }

    private void Update()
    {
        // Retry subscribe Firebase nếu cần
        if (!_subscribedFirebase)
            TrySubscribeFirebase();
    }

    // ========== PUBLIC ==========
    // gọi khi user bấm "Đăng nhập qua App"
    public void StartDeepLinkLogin()
    {
        _loggedIn = false;
        _currentCode = null;
        _currentTimestamp = null;

        TrySubscribeFirebase();

        // xin code mới + gọi deeplink
        RequestNewCodeAndOpenDeepLink();
    }

    // ========== SUBSCRIBE FIREBASE ==========
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
    }

    private void UnsubscribeFirebase()
    {
        if (!_subscribedFirebase) return;

        if (FirebaseLoginQrPerCode.Instance != null)
        {
            FirebaseLoginQrPerCode.Instance.OnAccessTokenReceived -= OnFirebaseAccessToken;
            Debug.Log("[LmsDeepLinkAuthUI] Unsubscribed FirebaseLoginQrPerCode.OnAccessTokenReceived");
        }

        _subscribedFirebase = false;
    }

    // ========== CALLBACK FROM FIREBASE ==========
    private void OnFirebaseAccessToken(string accessToken)
    {
        Debug.Log("[LmsDeepLinkAuthUI] OnFirebaseAccessToken CALLED, token = " + accessToken);

        if (string.IsNullOrEmpty(accessToken))
        {
            Debug.LogWarning("[LmsDeepLinkAuthUI] Token rỗng.");
            return;
        }

        if (_loggedIn)
            return;

        _loggedIn = true;

        if (_countdownCo != null)
        {
            StopCoroutine(_countdownCo);
            _countdownCo = null;
        }

        if (countdownText != null)
            countdownText.text = "Đã đăng nhập";

        LoginController.LoginWithQrToken(accessToken);
        onLoginSuccess?.Invoke(accessToken);

        gameObject.SetActive(false);

        if (LmsStore.Instance != null)
            LmsStore.Instance.ClearQrLoginCache();
    }

    // ========== UI ==========
    private void OnClickRefresh()
    {
        if (_loggedIn) return;

        // anti-spam: chưa hết antiSpamSeconds thì không cho xin code mới
        if (Time.unscaledTime < _antiSpamUntil)
        {
            LoginController.ShowWarning(
                "Mã hiện tại vẫn còn hiệu lực.\n" +
                "Vui lòng dùng mã đang có hoặc đợi hết thời gian rồi lấy mã mới."
            );
            return;
        }

        StartDeepLinkLogin();
    }

    private void OpenStore()
    {
#if UNITY_ANDROID
        Application.OpenURL(androidPlayStoreUrl);
#elif UNITY_IOS
        Application.OpenURL(iosAppStoreUrl);
#else
        Application.OpenURL(androidPlayStoreUrl);
#endif
    }

    // ========== MAIN FLOW ==========
    private void RequestNewCodeAndOpenDeepLink()
    {
        _loggedIn = false;
        _currentCode = null;
        _currentTimestamp = null;

        if (_countdownCo != null)
        {
            StopCoroutine(_countdownCo);
            _countdownCo = null;
        }

        if (LmsStore.Instance != null)
            LmsStore.Instance.ClearQrLoginCache();

        // anti spam
        _antiSpamUntil = Time.unscaledTime + antiSpamSeconds;

        StartCoroutine(CoRequestCodeThenDeepLink());
    }

    private IEnumerator CoRequestCodeThenDeepLink()
    {
        if (countdownText != null)
            countdownText.text = "Đang tạo mã...";

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
                Debug.LogError("[LmsDeepLinkAuthUI] Request error: " + req.error);
                if (countdownText != null) countdownText.text = "Lỗi kết nối";
                yield break;
            }

            string json = req.downloadHandler.text;
            Debug.Log("[LmsDeepLinkAuthUI] Response step=1: " + json);

            LmsAuthResponse resp = null;
            try
            {
                resp = JsonUtility.FromJson<LmsAuthResponse>(json);
            }
            catch (Exception e)
            {
                Debug.LogError("[LmsDeepLinkAuthUI] Parse JSON fail: " + e);
                if (countdownText != null) countdownText.text = "Lỗi dữ liệu";
                yield break;
            }

            if (resp == null || resp.data == null || string.IsNullOrEmpty(resp.data.code))
            {
                Debug.LogError("[LmsDeepLinkAuthUI] JSON không chứa data.code.");
                if (countdownText != null) countdownText.text = "Dữ liệu không hợp lệ";
                yield break;
            }

            _currentCode = resp.data.code;
            _currentTimestamp = resp.data.timestamp;

            // lưu cache giống bản QR
            if (LmsStore.Instance != null)
            {
                LmsStore.Instance.lastLoginQrCode = _currentCode;
                LmsStore.Instance.lastLoginQrTimestamp = _currentTimestamp;
            }

            // start listen firebase theo code (y chang flow QR)
            if (FirebaseLoginQrPerCode.Instance != null)
                FirebaseLoginQrPerCode.Instance.StartListen(_currentCode);

            // set expire + countdown
            _expireAt = Time.unscaledTime + codeLifetimeSeconds;
            _countdownCo = StartCoroutine(CoCountdown(codeLifetimeSeconds));

            // tạo payload để đưa sang app XHero
            // Bạn có thể đổi sang base64 nếu muốn gọn hơn, ở đây dùng JSON + EscapeURL cho đơn giản.
            string payloadJson =
                $"{{\"code\":\"{_currentCode}\",\"timestamp\":\"{_currentTimestamp}\",\"function\":\"auth-for-lms\"}}";

            OpenXHeroWithPayload(payloadJson);
        }
    }

    private void OpenXHeroWithPayload(string payloadJson)
    {
        // Deep link dạng:
        // xhero://auth-for-lms?payload=<urlencoded_json>
        string encodedPayload = UnityWebRequest.EscapeURL(payloadJson);
        string deepLinkUrl = $"{xheroScheme}://{xheroHostPath}?payload={encodedPayload}";

#if UNITY_ANDROID
        // Android: check installed => nếu không có thì mở Play Store
        if (!IsAndroidAppInstalled(androidPackageName))
        {
            Debug.LogWarning("[LmsDeepLinkAuthUI] XHero chưa cài. Mở CH Play...");
            Application.OpenURL(androidPlayStoreUrl);
            return;
        }
#endif

        Debug.Log("[LmsDeepLinkAuthUI] Open deep link: " + deepLinkUrl);
        Application.OpenURL(deepLinkUrl);

        // iOS: Unity không check chắc chắn "có app hay không"
        // => Bạn có thể hiển thị thêm nút "Tải XHero" (openStoreButton) để user bấm nếu cần.
#if UNITY_IOS
        // optional UX: nếu muốn tự động fallback store sau vài giây thì làm coroutine,
        // nhưng sẽ gây khó chịu nếu user đã có app.
#endif
    }

#if UNITY_ANDROID
    private bool IsAndroidAppInstalled(string packageName)
    {
        if (string.IsNullOrEmpty(packageName)) return false;

        try
        {
            using (AndroidJavaClass unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (AndroidJavaObject activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (AndroidJavaObject pm = activity.Call<AndroidJavaObject>("getPackageManager"))
            {
                // getPackageInfo(packageName, 0) -> throws if not found
                AndroidJavaObject pkgInfo = pm.Call<AndroidJavaObject>("getPackageInfo", packageName, 0);
                return pkgInfo != null;
            }
        }
        catch
        {
            return false;
        }
    }
#endif

    // ========== COUNTDOWN ==========
    private IEnumerator CoCountdown(float time)
    {
        float t = time;

        while (t > 0f && !_loggedIn)
        {
            t -= Time.deltaTime;

            if (countdownText != null)
            {
                int sec = Mathf.Max(0, Mathf.CeilToInt(t));
                int mm = sec / 60;
                int ss = sec % 60;
                countdownText.text = $"{mm:00}:{ss:00}";
            }

            yield return null;
        }

        if (countdownText != null)
            countdownText.text = _loggedIn ? "Đã đăng nhập" : "00:00";
    }

    // ========== JSON ==========
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
}
