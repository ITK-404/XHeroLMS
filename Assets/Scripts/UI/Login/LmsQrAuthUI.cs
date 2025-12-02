using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.Events;
using TMPro;

public class LmsQrAuthUI : MonoBehaviour
{
    [Header("API config")]
    public string path = "/auth-for-lms/request";
    public string platform = "pc";
    public float  requestTimeout = 10f;

    [Header("UI")]
    public TMP_Text countdownText;
    public Button   refreshButton;
    public RawImage qrImage;

    [Header("Thời gian sống của QR (giây)")]
    public float qrLifetimeSeconds = 120f; // 2 phút

    [Header("Polling result (nếu backend có step=2)")]
    public bool  autoPollResult = false;
    public float pollInterval   = 1.0f;

    [Serializable]
    public class StringEvent : UnityEvent<string> { }
    public StringEvent onLoginSuccess;

    private Coroutine _countdownCo;
    private Coroutine _pollCo;

    private string _currentCode;
    private string _currentTimestamp;
    private bool   _loggedIn;
    
    public Button btnBack;
    public GameObject currentPanel;
    public GameObject backPanel;

    private bool _subscribedFirebase = false;
    private string baseUrl;

    private void Awake()
    {
        baseUrl = LmsStore.Instance.baseUrl;

        if (refreshButton != null)
            refreshButton.onClick.AddListener(OnClickRefresh);
    }

    private void Start()
    {
        if (btnBack) btnBack.onClick.AddListener(BackAndReset);
    }

    private void BackAndReset()
    {
        if (currentPanel) currentPanel.SetActive(false);
        if (backPanel)    backPanel.SetActive(true);
    }

    private void OnEnable()
    {
        _loggedIn = false;
        _subscribedFirebase = false;

        // Chỉ chuẩn bị subscribe Firebase, KHÔNG request QR ở đây
        TrySubscribeFirebase();

        // Không gọi RequestNewQr() nữa
        // RequestNewQr();
    }

    private void OnDisable()
    {
        if (_countdownCo != null)
        {
            StopCoroutine(_countdownCo);
            _countdownCo = null;
        }

        if (_pollCo != null)
        {
            StopCoroutine(_pollCo);
            _pollCo = null;
        }

        UnsubscribeFirebase();
    }

    private void Update()
    {
        // Retry subscribe Firebase nếu cần
        if (!_subscribedFirebase)
        {
            TrySubscribeFirebase();
        }
    }

    // === PUBLIC API: gọi khi user bấm "Đăng nhập bằng QR" ===
    public void StartQrLogin()
    {
        _loggedIn = false;
        _currentCode = null;
        _currentTimestamp = null;

        TrySubscribeFirebase();
        RequestNewQr();     // Chỉ lúc này mới request QR + start countdown
    }

    // ===================== SUBSCRIBE FIREBASE =====================
    private void TrySubscribeFirebase()
    {
        if (_subscribedFirebase) return;

        if (FirebaseLoginQrPerCode.Instance != null)
        {
            FirebaseLoginQrPerCode.Instance.OnAccessTokenReceived -= OnFirebaseAccessToken;
            FirebaseLoginQrPerCode.Instance.OnAccessTokenReceived += OnFirebaseAccessToken;
            _subscribedFirebase = true;
            Debug.Log("[LmsQrAuthUI] Subscribed to FirebaseLoginQrPerCode.OnAccessTokenReceived");
        }
    }

    private void UnsubscribeFirebase()
    {
        if (!_subscribedFirebase) return;

        if (FirebaseLoginQrPerCode.Instance != null)
        {
            FirebaseLoginQrPerCode.Instance.OnAccessTokenReceived -= OnFirebaseAccessToken;
            Debug.Log("[LmsQrAuthUI] Unsubscribed FirebaseLoginQrPerCode.OnAccessTokenReceived");
        }

        _subscribedFirebase = false;
    }

    // ===================== CALLBACK TỪ FIREBASE =====================
    private void OnFirebaseAccessToken(string accessToken)
    {
        Debug.Log("[LmsQrAuthUI] OnFirebaseAccessToken CALLED, token = " + accessToken);

        if (string.IsNullOrEmpty(accessToken))
        {
            Debug.LogWarning("[LmsQrAuthUI] OnFirebaseAccessToken nhận token rỗng.");
            return;
        }

        if (_loggedIn)
            return;

        _loggedIn = true;
        Debug.Log("[LmsQrAuthUI] Login success from Firebase, token = " + accessToken);

        if (_countdownCo != null)
        {
            StopCoroutine(_countdownCo);
            _countdownCo = null;
        }

        if (_pollCo != null)
        {
            StopCoroutine(_pollCo);
            _pollCo = null;
        }

        if (countdownText != null)
            countdownText.text = "Đã đăng nhập";

        LoginController.LoginWithQrToken(accessToken);
        onLoginSuccess?.Invoke(accessToken);

        gameObject.SetActive(false);

        if (LmsStore.Instance != null)
        {
            LmsStore.Instance.ClearQrLoginCache();
        }
    }

    // ===================== UI EVENT =====================
    private void OnClickRefresh()
    {
        // User bấm làm mới => luôn request QR mới + reset timer
        StartQrLogin();
    }

    // ===================== MAIN FLOW =====================
    private void RequestNewQr()
    {
        _loggedIn = false;
        _currentCode = null;
        _currentTimestamp = null;

        if (_countdownCo != null)
        {
            StopCoroutine(_countdownCo);
            _countdownCo = null;
        }

        if (_pollCo != null)
        {
            StopCoroutine(_pollCo);
            _pollCo = null;
        }

        if (LmsStore.Instance != null)
        {
            LmsStore.Instance.ClearQrLoginCache();
        }

        StartCoroutine(CoRequestQrFromApi());
    }

    private IEnumerator CoRequestQrFromApi()
    {
        if (qrImage != null)
        {
            qrImage.texture = null;
            qrImage.color   = Color.clear;
        }

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
                Debug.LogError("[LmsQrAuthUI] Request error: " + req.error);
                if (countdownText != null)
                    countdownText.text = "Lỗi kết nối";
                yield break;
            }

            string json = req.downloadHandler.text;
            Debug.Log("[LmsQrAuthUI] Response step=1: " + json);

            LmsAuthResponse resp;
            try
            {
                resp = JsonUtility.FromJson<LmsAuthResponse>(json);
            }
            catch (Exception e)
            {
                Debug.LogError("[LmsQrAuthUI] Parse JSON fail: " + e);
                if (countdownText != null)
                    countdownText.text = "Lỗi dữ liệu";
                yield break;
            }

            if (resp == null || resp.data == null || string.IsNullOrEmpty(resp.data.code))
            {
                Debug.LogError("[LmsQrAuthUI] JSON không chứa data.code.");
                if (countdownText != null)
                    countdownText.text = "Dữ liệu không hợp lệ";
                yield break;
            }

            _currentCode      = resp.data.code;
            _currentTimestamp = resp.data.timestamp;

            Debug.Log($"[LmsQrAuthUI] step=1 OK, code = {_currentCode}, timestamp = {_currentTimestamp}");

            if (LmsStore.Instance != null)
            {
                LmsStore.Instance.lastLoginQrCode      = _currentCode;
                LmsStore.Instance.lastLoginQrTimestamp = _currentTimestamp;
            }

            if (FirebaseLoginQrPerCode.Instance != null)
            {
                FirebaseLoginQrPerCode.Instance.StartListen(_currentCode);
            }

            string qrContent =
                $"{{\"code\":\"{_currentCode}\",\"timestamp\":\"{_currentTimestamp}\",\"function\":\"auth-for-lms\"}}";

            GenerateQrToImage(qrContent);
        }

        _countdownCo = StartCoroutine(CoCountdown(qrLifetimeSeconds));

        if (autoPollResult && !string.IsNullOrEmpty(_currentCode))
        {
            _pollCo = StartCoroutine(CoPollLoginResult(_currentCode, _currentTimestamp));
        }
    }

    // ===================== QR GENERATION =====================
    private void GenerateQrToImage(string content)
    {
        StartCoroutine(CoDownloadQrImage(content));
    }

    private IEnumerator CoDownloadQrImage(string content)
    {
        if (qrImage != null)
        {
            qrImage.texture = null;
            qrImage.color   = Color.clear;
        }

        string encoded = UnityWebRequest.EscapeURL(content);
        string qrUrl   = $"https://api.qrserver.com/v1/create-qr-code/?size=512x512&data={encoded}";

        using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(qrUrl))
        {
            req.timeout = (int)requestTimeout;

            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool hasError = req.result != UnityWebRequest.Result.Success;
#else
            bool hasError = req.isNetworkError || req.isHttpError;
#endif

            if (hasError)
            {
                Debug.LogError("[LmsQrAuthUI] Download QR fail: " + req.error);
                if (countdownText != null)
                    countdownText.text = "Lỗi tải QR";
                yield break;
            }

            Texture2D tex = DownloadHandlerTexture.GetContent(req);
            if (qrImage != null)
            {
                qrImage.texture = tex;
                qrImage.color   = Color.white;
            }
        }
    }

    // ===================== POLL STEP=2 (OPTIONAL) =====================
    private IEnumerator CoPollLoginResult(string code, string timestamp)
    {
        yield break;
    }

    // ===================== COUNTDOWN =====================
    private IEnumerator CoCountdown(float time)
    {
        float t = time;

        while (t > 0f && !_loggedIn)
        {
            t -= Time.deltaTime;

            if (countdownText != null)
            {
                int sec = Mathf.Max(0, Mathf.CeilToInt(t));
                int mm  = sec / 60;
                int ss  = sec % 60;
                countdownText.text = $"{mm:00}:{ss:00}";
            }

            yield return null;
        }

        if (countdownText != null)
        {
            countdownText.text = _loggedIn ? "Đã đăng nhập" : "00:00";
        }
    }

    // ===================== JSON CLASSES =====================
    [Serializable]
    private class LmsAuthResponse
    {
        public bool        status;
        public LmsAuthData data;
    }

    [Serializable]
    private class LmsAuthData
    {
        public string code;
        public string timestamp;
    }

    [Serializable]
    private class LmsLoginCheckResponse
    {
        public bool              status;
        public string            message;
        public LmsLoginCheckData data;
    }

    [Serializable]
    private class LmsLoginCheckData
    {
        public string accessToken;
        public string userId;
    }
}
