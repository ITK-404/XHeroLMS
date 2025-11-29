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
    [Tooltip("Relative path từ baseUrl của LMS")]
    public string path = "/users/certificates";
    public int skip = 0;
    public int limit = 1;   // chỉ cần 1 certificate để preview

    [Header("Preview 2D")]
    public RawImage certificateImage;
    public Image    frameObject;

    [Header("Texts")]
    public TMP_Text nameText;
    public TMP_Text dateText;

    [Header("Toggles")]
    public Toggle toggleWithFrame;
    public Toggle toggleWithoutFrame;

    public GameObject previewRoot;

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
    }

    private void Start()
    {
        if (toggleWithFrame != null)    toggleWithFrame.isOn    = true;
        if (toggleWithoutFrame != null) toggleWithoutFrame.isOn = false;

        ApplyFrameState();

        if (previewRoot != null)
            previewRoot.SetActive(false);

        // StartCoroutine(FetchAndShowCertificate());
    }

    public void OnClickPreviewButton()
    {
        StartCoroutine(FetchAndShowCertificate());
    }

    // ================== MAIN FLOW ==================

    private IEnumerator FetchAndShowCertificate()
    {
        string baseUrl = GetBaseUrl();
        if (string.IsNullOrEmpty(baseUrl))
        {
            Debug.LogWarning("[Certificate2DPreviewUI] baseUrl rỗng, kiểm tra LmsStore.Instance.baseUrl.");
            yield break;
        }

        if (!baseUrl.EndsWith("/"))
            baseUrl += "/";

        string url = baseUrl + path.TrimStart('/');
        url += $"?skip={skip}&limit={limit}";

        string token = GetAccessToken();
        if (string.IsNullOrEmpty(token))
        {
            Debug.LogWarning("[Certificate2DPreviewUI] Token rỗng, không gọi API.");
            yield break;
        }

        Debug.Log($"[Certificate2DPreviewUI] GET {url}");

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
                yield break;
            }

            if (string.IsNullOrEmpty(raw))
            {
                Debug.LogWarning("[Certificate2DPreviewUI] Response rỗng.");
                yield break;
            }

            CertificateRoot root = null;
            try
            {
                root = JsonUtility.FromJson<CertificateRoot>(raw);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Certificate2DPreviewUI] FromJson FAILED: " + e);
                yield break;
            }

            if (root?.data == null || root.data.data == null || root.data.data.Length == 0)
            {
                Debug.LogWarning("[Certificate2DPreviewUI] Không có certificate nào trong data.");
                yield break;
            }

            // lấy certificate đầu tiên
            CertificateItem cert = root.data.data[0];

            string imgUrl = cert.certImg;

            // Nếu server trả về path tương đối thì ghép với baseUrl
            if (!string.IsNullOrEmpty(imgUrl) && !imgUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                imgUrl = baseUrl.TrimEnd('/') + "/" + imgUrl.TrimStart('/');
            }

            Debug.Log("[Certificate2DPreviewUI] Download image: " + imgUrl);

            // tải ảnh
            yield return StartCoroutine(DownloadTextureAndShow(imgUrl, cert.fullName, cert.createdAt));
        }
    }

    private IEnumerator DownloadTextureAndShow(string imgUrl, string fullName, string createdAt)
    {
        if (string.IsNullOrEmpty(imgUrl))
        {
            Debug.LogWarning("[Certificate2DPreviewUI] imgUrl rỗng.");
            yield break;
        }

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
                yield break;
            }

            Texture2D tex = DownloadHandlerTexture.GetContent(req);
            Debug.Log("[Certificate2DPreviewUI] Texture downloaded: " +
                      (tex == null ? "NULL" : tex.width + "x" + tex.height));

            SetCertificate(tex, fullName, createdAt);
        }
    }

    // ================== APPLY DATA ==================

    private void SetCertificate(Texture2D tex, string userName, string dateString)
    {
        if (certificateImage != null)
        {
            certificateImage.texture = tex;
            certificateImage.color = tex != null ? Color.white : Color.clear;
        }

        if (nameText != null)
            nameText.text = userName ?? "";

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
            string verbose = $"{dt:dd} tháng {dt:MM} năm {dt:yyyy}";

            return verbose;
        }

        return raw;
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
        if (previewRoot != null)
            previewRoot.SetActive(false);
    }

    // ================== BASE URL & TOKEN ==================

    private string GetBaseUrl()
    {
        try
        {
            var t = Type.GetType("LmsStore");
            var inst = t?.GetProperty("Instance")?.GetValue(null, null);
            if (inst == null) return null;

            var field = t.GetField("baseUrl");
            if (field != null) return field.GetValue(inst) as string;

            var prop = t.GetProperty("baseUrl");
            if (prop != null) return prop.GetValue(inst, null) as string;
        }
        catch { }

        return null;
    }

    private string GetAccessToken()
    {
        try
        {
            var t = Type.GetType("TokenStore");
            if (t != null)
            {
                var prop = t.GetProperty("AccessToken");
                if (prop != null)
                    return prop.GetValue(null) as string;
            }
        }
        catch { }

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
