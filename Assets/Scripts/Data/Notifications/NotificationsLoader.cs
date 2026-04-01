using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class NotificationsLoader : MonoBehaviour
{
    [Header("API")]
    private string baseUrl = "";
    [SerializeField] private int skip = 0;
    [SerializeField] private int limit = 10;
    [SerializeField] private int timeoutSeconds = 20;
    [SerializeField] private string platform = "lms3d";

    [Header("Auto Load")]
    [SerializeField] private bool autoLoadOnEnable = true;
    [SerializeField] private string defaultTab = "system";

    [Header("Auto Refresh")]
    [SerializeField] private bool autoRefresh = true;
    [SerializeField] private float refreshInterval = 10f;

    [Header("Options")]
    [SerializeField] private bool debugLog = true;

    private Coroutine loadRoutine;
    private Coroutine refreshRoutine;
    private UnityWebRequest activeRequest;
    private int loadVersion = 0;
    private string currentTab = "system";
    private bool isRefreshingSilently = false;

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            if (LmsStore.Instance != null && !string.IsNullOrWhiteSpace(LmsStore.Instance.baseUrl))
                baseUrl = LmsStore.Instance.baseUrl;
        }

        baseUrl = (baseUrl ?? "").Trim().TrimEnd('/');

        if (string.IsNullOrWhiteSpace(defaultTab))
            defaultTab = "system";

        if (string.IsNullOrWhiteSpace(platform))
            platform = "lms3d";

        currentTab = NormalizeTab(defaultTab);
    }

    private void OnEnable()
    {
        if (autoLoadOnEnable)
            Load(currentTab);

        if (autoRefresh)
        {
            if (refreshRoutine != null)
                StopCoroutine(refreshRoutine);

            refreshRoutine = StartCoroutine(CoAutoRefresh());
        }
    }

    private void OnDisable()
    {
        if (refreshRoutine != null)
        {
            StopCoroutine(refreshRoutine);
            refreshRoutine = null;
        }

        if (loadRoutine != null)
        {
            StopCoroutine(loadRoutine);
            loadRoutine = null;
        }

        isRefreshingSilently = false;
        CancelActiveRequest();
    }

    public void LoadSystem()
    {
        Load("system");
    }

    public void LoadPersonal()
    {
        Load("personal");
    }

    public void ReloadCurrentTab()
    {
        Load(currentTab);
    }

    public void Load(string tab)
    {
        tab = NormalizeTab(tab);
        currentTab = tab;

        if (loadRoutine != null)
            StopCoroutine(loadRoutine);

        CancelActiveRequest();
        loadRoutine = StartCoroutine(CoLoad(tab));
    }

    private IEnumerator CoAutoRefresh()
    {
        while (true)
        {
            yield return new WaitForSeconds(refreshInterval);

            if (!isActiveAndEnabled)
                continue;

            if (isRefreshingSilently)
                continue;

            yield return CoSilentRefresh(currentTab);
        }
    }

    private string BuildNotificationsUrl(string tab)
    {
        return $"{baseUrl}/notifications" +
               $"?tab={UnityWebRequest.EscapeURL(tab)}" +
               $"&skip={skip}" +
               $"&limit={limit}" +
               $"&platforms={UnityWebRequest.EscapeURL(platform)}";
    }

    private IEnumerator CoSilentRefresh(string tab)
    {
        isRefreshingSilently = true;

        if (string.IsNullOrEmpty(TokenStore.AccessToken))
            TokenStore.TryRestoreFromDisk();

        string token = TokenStore.AccessToken;
        if (string.IsNullOrEmpty(token))
        {
            isRefreshingSilently = false;
            yield break;
        }

        string url = BuildNotificationsUrl(tab);

        if (debugLog)
            Debug.Log("[NotificationsLoader] Silent Refresh Request: " + url);

        using (var request = UnityWebRequest.Get(url))
        {
            request.timeout = timeoutSeconds;
            request.SetRequestHeader("authorization", "Bearer " + token);

            yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            bool failed = request.result != UnityWebRequest.Result.Success;
#else
            bool failed = request.isNetworkError || request.isHttpError;
#endif

            string responseText = request.downloadHandler != null ? request.downloadHandler.text : "";
            long statusCode = request.responseCode;

            if (debugLog)
            {
                Debug.Log("[NotificationsLoader] Silent Refresh Status Code: " + statusCode);
                Debug.Log("[NotificationsLoader] Silent Refresh Raw Response: " + responseText);
            }

            if (failed)
            {
                if (debugLog)
                {
                    string err = request.error;
                    Debug.LogWarning("[NotificationsLoader] Silent Refresh lỗi: " + err + "\n" + responseText);
                }

                isRefreshingSilently = false;
                yield break;
            }

            NotificationMailResponse root = null;
            try
            {
                root = JsonUtility.FromJson<NotificationMailResponse>(responseText);
            }
            catch (Exception e)
            {
                if (debugLog)
                    Debug.LogWarning("[NotificationsLoader] Silent Refresh parse lỗi: " + e.Message);

                isRefreshingSilently = false;
                yield break;
            }

            if (root != null && root.status && root.data != null)
            {
                if (!NotificationsStaticStore.IsSameData(tab, root.data))
                {
                    NotificationsStaticStore.SetData(tab, root.data);
                }
                else
                {
                    NotificationsStaticStore.SetLoadedWithoutNotify(tab);
                }
            }
        }

        isRefreshingSilently = false;
    }

    private IEnumerator CoLoad(string tab)
    {
        int myVersion = ++loadVersion;

        NotificationsStaticStore.SetLoading(tab);

        if (string.IsNullOrEmpty(TokenStore.AccessToken))
            TokenStore.TryRestoreFromDisk();

        string token = TokenStore.AccessToken;
        if (string.IsNullOrEmpty(token))
        {
            NotificationsStaticStore.SetError(tab, "Không có token đăng nhập.");
            loadRoutine = null;
            yield break;
        }

        string url = BuildNotificationsUrl(tab);

        if (debugLog)
            Debug.Log("[NotificationsLoader] Request: " + url);

        using (activeRequest = UnityWebRequest.Get(url))
        {
            activeRequest.timeout = timeoutSeconds;
            activeRequest.SetRequestHeader("authorization", "Bearer " + token);

            yield return activeRequest.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            bool failed = activeRequest.result != UnityWebRequest.Result.Success;
#else
            bool failed = activeRequest.isNetworkError || activeRequest.isHttpError;
#endif

            if (myVersion != loadVersion)
            {
                activeRequest = null;
                loadRoutine = null;
                yield break;
            }

            string responseText = activeRequest.downloadHandler != null ? activeRequest.downloadHandler.text : "";
            long statusCode = activeRequest.responseCode;

            if (debugLog)
            {
                Debug.Log("[NotificationsLoader] Status Code: " + statusCode);
                Debug.Log("[NotificationsLoader] Raw Response: " + responseText);
            }

            if (failed)
            {
                string err = activeRequest.error;
                if (debugLog)
                    Debug.LogError("[NotificationsLoader] API lỗi: " + err + "\n" + responseText);

                NotificationsStaticStore.SetError(tab, "Không tải được danh sách thông báo.");
                activeRequest = null;
                loadRoutine = null;
                yield break;
            }

            NotificationMailResponse root = null;
            try
            {
                root = JsonUtility.FromJson<NotificationMailResponse>(responseText);
            }
            catch (Exception e)
            {
                if (debugLog)
                    Debug.LogError("[NotificationsLoader] Parse JSON lỗi: " + e.Message);

                NotificationsStaticStore.SetError(tab, "Dữ liệu trả về không hợp lệ.");
                activeRequest = null;
                loadRoutine = null;
                yield break;
            }

            if (root == null || !root.status || root.data == null)
            {
                NotificationsStaticStore.SetError(tab, "Không có dữ liệu thông báo.");
                activeRequest = null;
                loadRoutine = null;
                yield break;
            }

            if (!NotificationsStaticStore.IsSameData(tab, root.data))
            {
                NotificationsStaticStore.SetData(tab, root.data);
            }
            else
            {
                NotificationsStaticStore.SetLoadedWithoutNotify(tab);
            }
        }

        activeRequest = null;
        loadRoutine = null;
    }

    private void CancelActiveRequest()
    {
        loadVersion++;

        if (activeRequest != null)
        {
            activeRequest.Abort();
            activeRequest.Dispose();
            activeRequest = null;
        }
    }

    private string NormalizeTab(string tab)
    {
        return string.IsNullOrWhiteSpace(tab) ? "system" : tab.Trim().ToLower();
    }
}