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
    public TMP_Text countdownText;   // text hiển thị thời gian đếm ngược
    public Button   refreshButton;   // nút reset / gửi lại
    public RawImage qrImage;         // nơi hiển thị QR

    [Header("Thời gian sống của QR (giây)")]
    public float qrLifetimeSeconds = 120f; // 2 phút

    [Header("Polling result (nếu backend có step=2)")]
    public bool  autoPollResult = false;   // mặc định TẮT, vì hiện tại dùng Firebase
    public float pollInterval   = 1.0f;

    // Khi đăng nhập thành công sẽ trả accessToken
    [Serializable]
    public class StringEvent : UnityEvent<string> { }
    public StringEvent onLoginSuccess;

    private Coroutine _countdownCo;
    private Coroutine _pollCo;

    private string _currentCode;
    private string _currentTimestamp;
    private bool   _loggedIn;

    private void Awake()
    {
        if (refreshButton != null)
            refreshButton.onClick.AddListener(OnClickRefresh);
    }

    private void OnEnable()
    {
        // Đăng ký lắng nghe event từ Firebase singleton (nếu đã có trong scene)
        SubscribeFirebase();
        RequestNewQr();
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

    // ===================== SUBSCRIBE FIREBASE =====================
    private void SubscribeFirebase()
    {
        if (FirebaseLoginQrPerCode.Instance != null)
        {
            FirebaseLoginQrPerCode.Instance.OnAccessTokenReceived -= OnFirebaseAccessToken; // tránh double
            FirebaseLoginQrPerCode.Instance.OnAccessTokenReceived += OnFirebaseAccessToken;
        }
        else
        {
            Debug.LogWarning("[LmsQrAuthUI] FirebaseLoginQrPerCode.Instance == null (chưa có object trong scene?)");
        }
    }

    private void UnsubscribeFirebase()
    {
        if (FirebaseLoginQrPerCode.Instance != null)
        {
            FirebaseLoginQrPerCode.Instance.OnAccessTokenReceived -= OnFirebaseAccessToken;
        }
    }

    // Callback khi Firebase gửi accessToken về
    private void OnFirebaseAccessToken(string accessToken)
    {
        if (string.IsNullOrEmpty(accessToken))
        {
            Debug.LogWarning("[LmsQrAuthUI] OnFirebaseAccessToken nhận token rỗng.");
            return;
        }

        if (_loggedIn)
        {
            // đã xử lý login rồi, bỏ qua các event lặp lại
            return;
        }

        _loggedIn = true;
        Debug.Log("[LmsQrAuthUI] Login success from Firebase, token = " + accessToken);

        // Dừng countdown & poll (nếu có)
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

        // Bắn event ra ngoài để LoginManager / LmsStore xử lý tiếp
        onLoginSuccess?.Invoke(accessToken);
    }

    // ===================== UI EVENT =====================
    private void OnClickRefresh()
    {
        RequestNewQr();
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

            // GỌI FIREBASE LISTEN Ở ĐÂY
            if (FirebaseLoginQrPerCode.Instance != null)
            {
                FirebaseLoginQrPerCode.Instance.StartListen(_currentCode);
            }
            else
            {
                Debug.LogWarning("[LmsQrAuthUI] FirebaseLoginQrPerCode.Instance == null");
            }

            // Nội dung encode vào QR (tuỳ backend có cần timestamp hay không)
            // string qrContent = _currentCode;
            string qrContent =
                $"{{\"code\":\"{_currentCode}\",\"timestamp\":\"{_currentTimestamp}\",\"function\":\"auth-for-lms\"}}";

            // string qrContent = $"{_currentCode}|{_currentTimestamp}";

            GenerateQrToImage(qrContent);
        }

        _countdownCo = StartCoroutine(CoCountdown(qrLifetimeSeconds));

        if (autoPollResult && !string.IsNullOrEmpty(_currentCode))
        {
            _pollCo = StartCoroutine(CoPollLoginResult(_currentCode, _currentTimestamp));
        }
    }

    // ===================== QR GENERATION (WEB) =====================
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
        yield break; // Không dùng nữa
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
        public bool       status;
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
