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

        currentTab = defaultTab;
    }

    private void OnEnable()
    {
        if (autoLoadOnEnable)
        {
            Load(currentTab);
        }

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
        if (string.IsNullOrWhiteSpace(tab))
            tab = "system";

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

            Load(currentTab);
        }
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

        string url = $"{baseUrl}/notifications?tab={UnityWebRequest.EscapeURL(tab)}&skip={skip}&limit={limit}";
        if (debugLog) Debug.Log("[NotificationsLoader] Request: " + url);

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

            if (failed)
            {
                string err = activeRequest.error;
                string body = activeRequest.downloadHandler != null ? activeRequest.downloadHandler.text : "";
                if (debugLog) Debug.LogError("[NotificationsLoader] API lỗi: " + err + "\n" + body);

                NotificationsStaticStore.SetError(tab, "Không tải được danh sách thông báo.");
                activeRequest = null;
                loadRoutine = null;
                yield break;
            }

            string json = activeRequest.downloadHandler.text;
            if (debugLog) Debug.Log("[NotificationsLoader] Response: " + json);

            NotificationMailResponse root = null;
            try
            {
                root = JsonUtility.FromJson<NotificationMailResponse>(json);
            }
            catch (Exception e)
            {
                if (debugLog) Debug.LogError("[NotificationsLoader] Parse JSON lỗi: " + e.Message);
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

            NotificationsStaticStore.SetData(tab, root.data);
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
}