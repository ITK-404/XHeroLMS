using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class Certificate2DPreviewUI : MonoBehaviour
{
    [Header("API")]
    // Relative path từ baseUrl của LMS
    public string path = "/users/certificates";
    public int skip = 0;
    public int limit = 1;

    [Header("Preview 2D")]
    public RawImage certificateImage;
    public Image frameObject;

    [Header("Texts")]
    public TMP_Text nameText;
    public TMP_Text dateText;

    [Header("Toggles")]
    public Toggle toggleWithFrame;
    public Toggle toggleWithoutFrame;

    [Header("Root")]
    public GameObject previewRoot;

    [Header("Delay / Retry (server sync)")]
    public float initialDelaySeconds = 2f;

    public int maxRetries = 5;

    // Khoảng cách giữa mỗi lần retry
    public float retryDelaySeconds = 1.5f;

    [Header("Debug")]
    public bool logRawJson = false;
    public bool logRetry = true;

    private Coroutine _fetchCo;

    private void Awake()
    {
        if (toggleWithFrame != null)
            toggleWithFrame.onValueChanged.AddListener(OnToggleWithFrameChanged);

        if (toggleWithoutFrame != null)
            toggleWithoutFrame.onValueChanged.AddListener(OnToggleWithoutFrameChanged);
    }

    private void OnDestroy()
    {
        if (toggleWithFrame != null)
            toggleWithFrame.onValueChanged.RemoveListener(OnToggleWithFrameChanged);

        if (toggleWithoutFrame != null)
            toggleWithoutFrame.onValueChanged.RemoveListener(OnToggleWithoutFrameChanged);

        StopFetch();
    }

    private void Start()
    {
        if (toggleWithFrame != null) toggleWithFrame.isOn = true;
        if (toggleWithoutFrame != null) toggleWithoutFrame.isOn = false;

        ApplyFrameState();
        ClearPreviewUI(true); // mặc định ẩn
    }

    public void StartFetchAfterPassed()
    {
        StopFetch();
        ClearPreviewUI(true);
        _fetchCo = StartCoroutine(FetchAfterPassedWithDelayAndRetry());
    }

    public void OnClickPreviewButton()
    {
        StartFetchAfterPassed();
    }

    private IEnumerator FetchAfterPassedWithDelayAndRetry()
    {
        if (initialDelaySeconds > 0f)
            yield return new WaitForSeconds(initialDelaySeconds);

        // Retry check vài lần để chờ server sync
        for (int attempt = 0; attempt < Mathf.Max(1, maxRetries); attempt++)
        {
            bool hasCert = false;
            CertificateItem cert = null;

            yield return StartCoroutine(TryFetchCertificateFirstItem(result =>
            {
                hasCert = result.hasCert;
                cert = result.cert;
            }));

            if (hasCert && cert != null && !string.IsNullOrEmpty(cert.certImg))
            {
                // Có bằng -> bắt đầu tải ảnh và chỉ khi thành công mới bật previewRoot
                yield return StartCoroutine(DownloadAndShowCertificate(cert));
                yield break;
            }

            // Chưa có bằng -> retry
            if (attempt < maxRetries - 1)
            {
                if (logRetry)
                    Debug.Log($"[Certificate2DPreviewUI] Chưa có certificate, retry {attempt + 1}/{maxRetries} sau {retryDelaySeconds}s...");
                if (retryDelaySeconds > 0f)
                    yield return new WaitForSeconds(retryDelaySeconds);
            }
        }

        // Hết retry vẫn không có -> giữ ẩn, không bật previewRoot
        if (logRetry)
            Debug.LogWarning("[Certificate2DPreviewUI] Hết retry nhưng vẫn chưa có certificate. Giữ ẩn previewRoot.");
        ClearPreviewUI(true);
    }

    // ================== FETCH JSON (KHÔNG BẬT UI) ==================

    private struct FetchResult
    {
        public bool hasCert;
        public CertificateItem cert;
    }

    private IEnumerator TryFetchCertificateFirstItem(Action<FetchResult> onDone)
    {
        var result = new FetchResult { hasCert = false, cert = null };

        string baseUrl = GetBaseUrl();
        if (string.IsNullOrEmpty(baseUrl))
        {
            Debug.LogWarning("[Certificate2DPreviewUI] baseUrl rỗng.");
            onDone?.Invoke(result);
            yield break;
        }

        if (!baseUrl.EndsWith("/"))
            baseUrl += "/";

        string url = baseUrl + path.TrimStart('/');
        url += $"?skip={skip}&limit={limit}";

        string token = GetAccessToken();
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogWarning("[Certificate2DPreviewUI] Token rỗng.");
            onDone?.Invoke(result);
            yield break;
        }

        if (logRetry) Debug.Log($"[Certificate2DPreviewUI] GET {url}");

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.timeout = 15;
            req.SetRequestHeader("Authorization", "Bearer " + token);
            req.SetRequestHeader("Accept", "application/json");

            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool hasErr = req.result != UnityWebRequest.Result.Success;
#else
            bool hasErr = req.isNetworkError || req.isHttpError;
#endif

            string raw = req.downloadHandler?.text;

            if (hasErr)
            {
                Debug.LogWarning($"[Certificate2DPreviewUI] ERROR: {req.responseCode} {req.error}\nBody: {raw}");
                onDone?.Invoke(result);
                yield break;
            }

            if (string.IsNullOrEmpty(raw))
            {
                onDone?.Invoke(result);
                yield break;
            }

            if (logRawJson)
                Debug.Log("[Certificate2DPreviewUI] RAW JSON: " + raw);

            CertificateRoot root = null;
            try
            {
                root = JsonUtility.FromJson<CertificateRoot>(raw);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Certificate2DPreviewUI] FromJson FAILED: " + e);
                onDone?.Invoke(result);
                yield break;
            }

            if (root == null || root.data == null || root.data.data == null || root.data.data.Length == 0)
            {
                onDone?.Invoke(result);
                yield break;
            }

            result.hasCert = true;
            result.cert = root.data.data[0];
            onDone?.Invoke(result);
        }
    }

    // ================== DOWNLOAD + SHOW (CHỈ BẬT UI KHI OK) ==================

    private IEnumerator DownloadAndShowCertificate(CertificateItem cert)
    {
        // vẫn giữ ẩn trong lúc tải để tránh khung trắng
        ClearPreviewUI(true);

        string baseUrl = GetBaseUrl();
        if (string.IsNullOrEmpty(baseUrl))
        {
            yield break;
        }
        if (!baseUrl.EndsWith("/")) baseUrl += "/";

        string imgUrl = cert.certImg;

        if (string.IsNullOrEmpty(imgUrl))
        {
            yield break;
        }

        if (!imgUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            imgUrl = baseUrl.TrimEnd('/') + "/" + imgUrl.TrimStart('/');

        if (logRetry) Debug.Log("[Certificate2DPreviewUI] Download image: " + imgUrl);

        using (UnityWebRequest req = UnityWebRequestTexture.GetTexture(imgUrl))
        {
            req.timeout = 20;
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool hasErr = req.result != UnityWebRequest.Result.Success;
#else
            bool hasErr = req.isNetworkError || req.isHttpError;
#endif

            if (hasErr)
            {
                Debug.LogWarning($"[Certificate2DPreviewUI] ERROR tải ảnh: {req.responseCode} {req.error}");
                ClearPreviewUI(true);
                yield break;
            }

            Texture2D tex = DownloadHandlerTexture.GetContent(req);
            if (tex == null || tex.width <= 2 || tex.height <= 2)
            {
                Debug.LogWarning("[Certificate2DPreviewUI] Texture không hợp lệ -> không show.");
                ClearPreviewUI(true);
                yield break;
            }

            // Tải OK -> show UI
            SetCertificate(tex, cert.fullName, cert.createdAt);
        }
    }

    // ================== APPLY DATA ==================

    private void SetCertificate(Texture2D tex, string userName, string dateString)
    {
        if (tex == null)
        {
            ClearPreviewUI(true);
            return;
        }

        if (certificateImage != null)
        {
            certificateImage.texture = tex;
            certificateImage.color = Color.white;
        }

        if (nameText != null)
            nameText.text = TokenStore.FullName ?? "Người dùng mới";

        if (dateText != null)
            dateText.text = FormatDate(dateString);

        if (previewRoot != null)
            previewRoot.SetActive(true);
    }

    private string FormatDate(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return "";

        if (DateTime.TryParse(
                raw,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                out DateTime dt))
        {
            return $"{dt:dd} tháng {dt:MM} năm {dt:yyyy}";
        }

        return raw;
    }

    private void ClearPreviewUI(bool hideRoot = true)
    {
        if (certificateImage != null)
        {
            certificateImage.texture = null;
            certificateImage.color = Color.clear;
        }

        if (nameText != null) nameText.text = "";
        if (dateText != null) dateText.text = "";

        if (hideRoot && previewRoot != null)
            previewRoot.SetActive(false);
    }

    private void StopFetch()
    {
        if (_fetchCo != null)
        {
            StopCoroutine(_fetchCo);
            _fetchCo = null;
        }
    }

    // ================== TOGGLES ==================

    private void OnToggleWithFrameChanged(bool isOn)
    {
        if (isOn && toggleWithoutFrame != null && toggleWithoutFrame.isOn)
            toggleWithoutFrame.isOn = false;

        ApplyFrameState();
    }

    private void OnToggleWithoutFrameChanged(bool isOn)
    {
        if (isOn && toggleWithFrame != null && toggleWithFrame.isOn)
            toggleWithFrame.isOn = false;

        ApplyFrameState();
    }

    private void ApplyFrameState()
    {
        if (frameObject == null) return;

        bool noFrame = toggleWithoutFrame != null && toggleWithoutFrame.isOn;
        frameObject.gameObject.SetActive(!noFrame);
    }

    public void HidePreview()
    {
        StopFetch();
        ClearPreviewUI(true);
    }

    // ================== BASE URL & TOKEN ==================

    private string GetBaseUrl()
    {
        try
        {
            var t = Type.GetType("LmsStore, Assembly-CSharp");
            var inst = t?.GetProperty("Instance")?.GetValue(null, null);
            if (inst == null) return null;

            var field = t.GetField("baseUrl");
            if (field != null) return field.GetValue(inst) as string;

            var prop = t.GetProperty("baseUrl");
            if (prop != null) return prop.GetValue(inst, null) as string;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Certificate2DPreviewUI] GetBaseUrl error: " + e.Message);
        }

        return null;
    }

    private string GetAccessToken()
    {
        try
        {
            var t = Type.GetType("TokenStore, Assembly-CSharp");
            if (t != null)
            {
                var prop = t.GetProperty("AccessToken");
                if (prop != null)
                    return prop.GetValue(null) as string;
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[Certificate2DPreviewUI] GetAccessToken error: " + e.Message);
        }

        return null;
    }

    // ================== JSON MODELS ==================

    [Serializable]
    private class CertificateRoot
    {
        public bool status;
        public CertificateDataWrapper data;
    }

    [Serializable]
    private class CertificateDataWrapper
    {
        public CertificateItem[] data;
        public int total;
    }

    [Serializable]
    private class CertificateItem
    {
        public string serialNumber;
        public string expireTime;
        public string[] images;
        public string _id;
        public string user;
        public string course;
        public string fullName;
        public string certName;
        public string rawCertId;
        public string certImg;
        public string createdAt;
        public string updatedAt;
        public int __v;
    }
}
