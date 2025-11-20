using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class CertificatesController : MonoBehaviour
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

    [Header("UI - Shelf Layout (3 bằng / kệ)")]
    [Tooltip("Nơi chứa các kệ (Shelf) – giống content của ScrollView nhưng cho 3D")]
    public Transform shelfParent;              // content

    [Tooltip("Prefab 1 kệ, trên đó có CertificateShelfUI + 3 CertificateItemUI")]
    public CertificateShelfUI shelfPrefab;     // prefab kệ

    [Tooltip("Số bằng trên mỗi kệ (mặc định 3)")]
    public int certificatesPerShelf = 3;

    [Tooltip("Xoá hết kệ cũ trước khi load lại")]
    public bool clearOldOnReload = true;

    [Header("UI - Optional")]
    public TMP_Text emptyMessageText;  // thông báo khi không có chứng chỉ
    public TMP_Text errorText;         // hiển thị lỗi nếu có

    private void Start()
    {
        if (autoCallOnStart)
            RefreshCertificates();
    }

    [ContextMenu("Test /users/certificates")]
    public void RefreshCertificates()
    {
        StartCoroutine(FetchAndSpawnCertificates());
    }

    private IEnumerator FetchAndSpawnCertificates()
    {
        // ====== Validate Inspector ======
        if (shelfParent == null || shelfPrefab == null)
        {
            LogError("[CertificatesController] Shelf mode nhưng chưa gán shelfParent hoặc shelfPrefab.");
            yield break;
        }

        string baseUrl = GetBaseUrl();
        if (string.IsNullOrEmpty(baseUrl))
        {
            LogError("[CertificatesController] baseUrl rỗng. Kiểm tra LmsStore.Instance.baseUrl.");
            yield break;
        }

        if (!baseUrl.EndsWith("/"))
            baseUrl += "/";

        string url = baseUrl + path.TrimStart('/');
        url += $"?skip={skip}&limit={limit}";

        string token = GetAccessToken();
        if (string.IsNullOrEmpty(token))
        {
            LogError("[CertificatesController] Token rỗng, không gọi API.");
            yield break;
        }

        if (debugVerbose)
        {
            Debug.Log("=============== TOKEN INFO ===============");
            Debug.Log($"Raw Token:\n{token}");
            Debug.Log($"Authorization :\nBearer {token}");
            Debug.Log("==========================================");
            Debug.Log("[CertificatesController] GET " + url);
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
                LogError($"[CertificatesController] ERROR: {req.responseCode} {req.error}\nBody: {raw}");
                yield break;
            }

            if (string.IsNullOrEmpty(raw))
            {
                LogError("[CertificatesController] Response rỗng.");
                yield break;
            }

            if (debugVerbose)
                Debug.Log($"[CertificatesController] JSON trả về:\n{raw}");

            CertificateRoot root = null;
            try
            {
                root = JsonUtility.FromJson<CertificateRoot>(raw);
            }
            catch (Exception e)
            {
                LogError("[CertificatesController] FromJson FAILED: " + e);
                yield break;
            }

            if (root?.data == null || root.data.data == null || root.data.data.Length == 0)
            {
                if (clearOldOnReload)
                    ClearContent();

                if (emptyMessageText != null)
                    emptyMessageText.text = "Bạn chưa có chứng chỉ nào.";

                Debug.Log("[CertificatesController] Không có certificate nào trong data.");
                yield break;
            }

            // Có dữ liệu -> clear text "empty" nếu có
            if (emptyMessageText != null)
                emptyMessageText.text = "";

            if (clearOldOnReload)
                ClearContent();

            // ====== SPAWN THEO KỆ ======
            CertificateShelfUI currentShelf = null;
            int slotIndex = 0;

            foreach (var cert in root.data.data)
            {
                // Nếu chưa có kệ, hoặc kệ hiện tại đã đầy -> tạo kệ mới
                if (currentShelf == null || slotIndex >= certificatesPerShelf)
                {
                    currentShelf = Instantiate(shelfPrefab, shelfParent);
                    currentShelf.ClearSlots();
                    slotIndex = 0;
                }

                currentShelf.SetupSlot(
                    slotIndex,
                    cert.fullName,
                    cert.certName,
                    cert.createdAt,
                    cert.certImg
                );

                slotIndex++;
            }
        }
    }

    private void ClearContent()
    {
        if (shelfParent == null) return;

        for (int i = shelfParent.childCount - 1; i >= 0; i--)
        {
            Destroy(shelfParent.GetChild(i).gameObject);
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
                        Debug.Log($"[CertificatesController] TokenStore.AccessToken {(string.IsNullOrEmpty(value) ? "EMPTY" : "OK")}");
                    return value;
                }
            }
        }
        catch { }

        return null;
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
