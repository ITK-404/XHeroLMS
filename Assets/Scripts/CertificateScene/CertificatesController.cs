using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class CertificatesController : MonoBehaviour
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

    [Header("UI - Single Item + Pager")]
    public CertificateItemUI certificateUI;

    [Header("Spawn Parent")]
    public Transform spawnParent;

    [Header("Navigation Buttons")]
    public Button btnPrev;
    public Button btnNext;

    // ====== INTERNAL DATA ======
    private readonly List<CertificateItem>   _certDataList = new List<CertificateItem>();
    private readonly List<CertificateItemUI> _certUIList   = new List<CertificateItemUI>();
    private int _currentIndex = 0;

    private void Awake()
    {
        if (btnPrev != null)
            btnPrev.onClick.AddListener(OnClickPrev);

        if (btnNext != null)
            btnNext.onClick.AddListener(OnClickNext);
    }

    private void Start()
    {
        if (autoCallOnStart)
            RefreshCertificates();
    }

    [ContextMenu("Test /users/certificates")]
    public void RefreshCertificates()
    {
        StartCoroutine(FetchCertificates());
    }

    private IEnumerator FetchCertificates()
    {
        if (certificateUI == null)
        {
            Debug.Log("[CertificatesController] Chưa gán certificateUI (Prefab / Object mẫu).");
            yield break;
        }

        if (spawnParent == null)
            spawnParent = this.transform;

        string baseUrl = GetBaseUrl();
        if (string.IsNullOrEmpty(baseUrl))
        {
            Debug.Log("[CertificatesController] baseUrl rỗng. Kiểm tra LmsStore.Instance.baseUrl.");
            yield break;
        }

        if (!baseUrl.EndsWith("/"))
            baseUrl += "/";

        string url = baseUrl + path.TrimStart('/');
        url += $"?skip={skip}&limit={limit}";

        string token = GetAccessToken();
        if (string.IsNullOrEmpty(token))
        {
            Debug.Log("[CertificatesController] Token rỗng, không gọi API.");
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
                Debug.Log($"[CertificatesController] ERROR: {req.responseCode} {req.error}\nBody: {raw}");
                yield break;
            }

            if (string.IsNullOrEmpty(raw))
            {
                Debug.Log("[CertificatesController] Response rỗng.");
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
                Debug.Log("[CertificatesController] FromJson FAILED: " + e);
                yield break;
            }

            ClearSpawnedCertificates();
            _certDataList.Clear();

            if (root?.data == null || root.data.data == null || root.data.data.Length == 0)
            {
                Debug.Log("[CertificatesController] Không có certificate nào trong data.");
                UpdateNavButtons();
                yield break;
            }

            _certDataList.AddRange(root.data.data);

            for (int i = 0; i < _certDataList.Count; i++)
            {
                var cert = _certDataList[i];

                CertificateItemUI clone = Instantiate(certificateUI, spawnParent);
                clone.gameObject.name = $"Certificate_{i}_{cert.certName}";
                clone.gameObject.SetActive(true);

                clone.Setup(
                    cert.fullName,
                    cert.certName,
                    cert.createdAt,
                    cert.certImg
                );

                _certUIList.Add(clone);
            }

            _currentIndex = 0;
            ShowIndex(_currentIndex);
        }
    }

    // ====== HIỂN THỊ / NAVIGATION ======

    private void OnClickNext()
    {
        Debug.Log($"[CertificatesController] Click NEXT. currentIndex={_currentIndex}, total={_certDataList.Count}");
        if (_certDataList.Count <= 1) return;
        if (_currentIndex >= _certDataList.Count - 1) return;
        ShowIndex(_currentIndex + 1);
    }

    private void OnClickPrev()
    {
        Debug.Log($"[CertificatesController] Click PREV. currentIndex={_currentIndex}, total={_certDataList.Count}");
        if (_certDataList.Count <= 1) return;
        if (_currentIndex <= 0) return;
        ShowIndex(_currentIndex - 1);
    }

    private void ShowIndex(int index)
    {
        if (_certDataList.Count == 0 || _certUIList.Count == 0)
        {
            Debug.Log("[CertificatesController] ShowIndex: EMPTY LIST");
            _currentIndex = 0;
            UpdateNavButtons();
            return;
        }

        if (index < 0) index = 0;
        if (index >= _certDataList.Count) index = _certDataList.Count - 1;

        _currentIndex = index;
        Debug.Log($"[CertificatesController] ShowIndex -> {_currentIndex}, uiCount={_certUIList.Count}, dataCount={_certDataList.Count}");

        for (int i = 0; i < _certUIList.Count; i++)
        {
            var ui = _certUIList[i];
            if (ui == null) continue;

            bool active = i == _currentIndex;
            ui.gameObject.SetActive(active);

            if (active)
            {
                var cert = _certDataList[_currentIndex];
                ui.Setup(
                    cert.fullName,
                    cert.certName,
                    cert.createdAt,
                    cert.certImg
                );
            }
        }

        UpdateNavButtons();
    }

    private void UpdateNavButtons()
    {
        bool hasAny = _certDataList.Count > 0;

        if (btnPrev != null)
            btnPrev.interactable = hasAny && _currentIndex > 0;

        if (btnNext != null)
            btnNext.interactable = hasAny && _currentIndex < _certDataList.Count - 1;
    }

    private void ClearSpawnedCertificates()
    {
        if (_certUIList.Count == 0) return;

        for (int i = 0; i < _certUIList.Count; i++)
        {
            if (_certUIList[i] != null)
                Destroy(_certUIList[i].gameObject);
        }

        _certUIList.Clear();
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
                        Debug.Log($"[CertificatesController] TokenStore.AccessToken {(string.IsNullOrEmpty(value) ? "EMPTY" : "OK")}");
                    return value;
                }
            }
        }
        catch { }

        return null;
    }

    public CertificateItemUI GetCurrentCertificateUI()
    {
        if (_certUIList == null || _certUIList.Count == 0)
            return null;

        if (_currentIndex < 0 || _currentIndex >= _certUIList.Count)
            return null;

        return _certUIList[_currentIndex];
    }

    public void SetCurrentCertificateVisible(bool visible)
    {
        var ui = GetCurrentCertificateUI();
        if (ui != null)
            ui.gameObject.SetActive(visible);
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
