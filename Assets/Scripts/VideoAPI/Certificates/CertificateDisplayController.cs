using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;

public class CertificateDisplayController : MonoBehaviour
{
    [Header("Call option")]
    public bool autoCallOnStart = true;

    [Header("Endpoint")]
    [Tooltip("Relative path từ baseUrl của LMS")]
    public string path = "/users/certificates";
    public int skip = 0;
    public int limit = 10;

    [Header("Network")]
    public float requestTimeout = 15f;
    public bool debugVerbose = true;

    [Header("UI - Image & Text cơ bản")]
    public Image certificateImage;      // Image để hiển thị certImg
    public TMP_Text nameText;          // Text hiển thị fullName

    [Header("UI - Optional")]
    public TMP_Text dateText;          // (optional) full date "dd/MM/yyyy"
    public TMP_Text certNameText;      // (optional) Tên chứng chỉ (certName)
    public TMP_Text errorText;         // (optional) hiển thị lỗi nếu có

    private void Start()
    {
        if (autoCallOnStart)
            StartRequest();
    }

    [ContextMenu("Test /users/certificates")]
    public void StartRequest()
    {
        StartCoroutine(FetchAndDisplayCertificate());
    }

    private IEnumerator FetchAndDisplayCertificate()
    {
        string baseUrl = GetBaseUrl();
        if (string.IsNullOrEmpty(baseUrl))
        {
            LogError("[CertificateDisplay] baseUrl rỗng. Kiểm tra LmsStore.Instance.baseUrl.");
            yield break;
        }

        if (!baseUrl.EndsWith("/"))
            baseUrl += "/";

        string url = baseUrl + path.TrimStart('/');
        url += $"?skip={skip}&limit={limit}";

        string token = GetAccessToken();
        if (string.IsNullOrEmpty(token))
        {
            LogError("[CertificateDisplay] Token rỗng, không gọi API.");
            yield break;
        }

        if (debugVerbose)
        {
            Debug.Log("=============== TOKEN INFO ===============");
            Debug.Log($"Raw Token:\n{token}");
            Debug.Log($"Authorization :\nBearer {token}");
            Debug.Log("==========================================");
            Debug.Log("[CertificateDisplay] GET " + url);
        }

        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = Mathf.CeilToInt(Mathf.Max(1f, requestTimeout));
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
                LogError($"[CertificateDisplay] ERROR: {req.responseCode} {req.error}\nBody: {raw}");
                yield break;
            }

            if (string.IsNullOrEmpty(raw))
            {
                LogError("[CertificateDisplay] Response rỗng.");
                yield break;
            }

            if (debugVerbose)
                Debug.Log($"[CertificateDisplay] JSON trả về:\n{raw}");

            // Parse JSON
            CertificateRoot root = null;
            try
            {
                root = JsonUtility.FromJson<CertificateRoot>(raw);
            }
            catch (Exception e)
            {
                LogError("[CertificateDisplay] FromJson FAILED: " + e);
                yield break;
            }

            if (root?.data == null || root.data.data == null || root.data.data.Length == 0)
            {
                LogError("[CertificateDisplay] Không có certificate nào trong data.");
                yield break;
            }

            // Lấy certificate đầu tiên
            var cert = root.data.data[0];

            // ====== GÁN TEXT ======
            if (nameText != null)
                nameText.text = cert.fullName ?? "";

            if (certNameText != null)
                certNameText.text = cert.certName ?? "";

            // Parse createdAt -> DateTime local
            if (TryParseIsoDate(cert.createdAt, out var dtLocal))
            {
                // Nếu còn dùng full dateText
                if (dateText != null)  dateText.text  = $"{dtLocal.Day} tháng {dtLocal.Month} năm {dtLocal.Year}";
            }
            else
            {
                // fallback: nếu parse fail, đẩy nguyên chuỗi vào dateText
                if (dateText != null) dateText.text = cert.createdAt ?? "";
            }

            // ====== LOAD ẢNH ======
            if (!string.IsNullOrEmpty(cert.certImg) && certificateImage != null)
            {
                yield return StartCoroutine(LoadImageIntoUI(cert.certImg, certificateImage));
            }
        }
    }

    // ===================== LOAD IMAGE =====================

    private IEnumerator LoadImageIntoUI(string imageUrl, Image targetImage)
    {
        using (var req = UnityWebRequestTexture.GetTexture(imageUrl))
        {
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool hasErr = req.result != UnityWebRequest.Result.Success;
#else
            bool hasErr = req.isNetworkError || req.isHttpError;
#endif

            if (hasErr)
            {
                LogError($"[CertificateDisplay] Load image FAIL: {req.responseCode} {req.error}");
                yield break;
            }

            var tex = DownloadHandlerTexture.GetContent(req);
            if (tex == null)
            {
                LogError("[CertificateDisplay] Texture rỗng sau khi download.");
                yield break;
            }

            var sprite = Sprite.Create(
                tex,
                new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f)
            );

            targetImage.sprite = sprite;
            targetImage.preserveAspect = true;
        }
    }

    // ===================== HELPERS =====================

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
                {
                    var value = prop.GetValue(null) as string;
                    if (debugVerbose)
                        Debug.Log($"[CertificateDisplay] TokenStore.AccessToken {(string.IsNullOrEmpty(value) ? "EMPTY" : "OK")}");
                    return value;
                }
            }
        }
        catch { }

        return null;
    }

    private bool TryParseIsoDate(string isoString, out DateTime dtLocal)
    {
        dtLocal = default;
        if (string.IsNullOrEmpty(isoString)) return false;

        // ví dụ: "2025-11-17T07:11:02.153Z"
        if (DateTime.TryParse(isoString, null, DateTimeStyles.AdjustToUniversal, out var dt))
        {
            dtLocal = dt.ToLocalTime();
            return true;
        }

        return false;
    }

    private void LogError(string msg)
    {
        Debug.LogError(msg);
        if (errorText != null)
            errorText.text = msg;
    }

    // ===================== JSON MODELS =====================

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
