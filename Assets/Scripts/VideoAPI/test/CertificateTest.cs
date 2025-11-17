using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class CertificateTest : MonoBehaviour
{
    [Header("Call option")]
    public bool autoCallOnStart = true;

    [Header("Endpoint")]
    public string path = "/users/certificates";
    public int skip = 0;
    public int limit = 10;

    [Header("Network")]
    public float requestTimeout = 15f;
    public bool debugVerbose = true;

    private string saveFileName = "certificates.json";

    private void Start()
    {
        if (autoCallOnStart)
            StartRequest();
    }

    [ContextMenu("Test /users/certificates")]
    public void StartRequest()
    {
        StartCoroutine(FetchCertificates());
    }

    private IEnumerator FetchCertificates()
    {
        string baseUrl = GetBaseUrl();
        if (string.IsNullOrEmpty(baseUrl))
        {
            Debug.LogError("[CertificateTest] baseUrl rỗng. Kiểm tra LmsStore.Instance.baseUrl.");
            yield break;
        }

        if (!baseUrl.EndsWith("/"))
            baseUrl += "/";

        string url = baseUrl + path.TrimStart('/');
        url += $"?skip={skip}&limit={limit}";

        string token = GetAccessToken();

        Debug.Log("=============== TOKEN INFO ===============");
        Debug.Log($"Raw Token:\n{token}");
        Debug.Log($"Authorization :\nBearer {token}");
        Debug.Log("==========================================");

        if (string.IsNullOrEmpty(token))
        {
            Debug.LogWarning("[CertificateTest] Token rỗng, không gọi API.");
            yield break;
        }

        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = Mathf.CeilToInt(Mathf.Max(1f, requestTimeout));
            req.SetRequestHeader("Authorization", "Bearer " + token);
            req.SetRequestHeader("Accept", "application/json");

            if (debugVerbose)
                Debug.Log("[CertificateTest] GET " + url);

            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool hasErr = req.result != UnityWebRequest.Result.Success;
#else
            bool hasErr = req.isNetworkError || req.isHttpError;
#endif

            if (hasErr)
            {
                Debug.LogError($"[CertificateTest] ERROR: {req.responseCode} {req.error}\nBody: {req.downloadHandler?.text}");
                yield break;
            }

            string raw = req.downloadHandler?.text;
            if (string.IsNullOrEmpty(raw))
            {
                Debug.LogWarning("[CertificateTest] Response rỗng.");
                yield break;
            }

            Debug.Log($"[CertificateTest] JSON trả về:\n{raw}");

            SaveJsonToFile(raw);
        }
    }

    private void SaveJsonToFile(string json)
    {
        string path = Path.Combine(Application.persistentDataPath, saveFileName);

        try
        {
            File.WriteAllText(path, json);
            Debug.Log($"[CertificateTest] Đã lưu certificates JSON vào:\n{path}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[CertificateTest] Lỗi khi ghi file JSON:\n{ex}");
        }
    }

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
                        Debug.Log($"[CertificateTest] TokenStore.AccessToken {(string.IsNullOrEmpty(value) ? "EMPTY" : "OK")}");
                    return value;
                }
            }
        }
        catch { }

        return null;
    }
}
