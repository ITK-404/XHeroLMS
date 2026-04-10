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

    // Auto Load
    private bool autoLoadOnEnable = true;
    private bool reloadOnPushReceived = true;
    private bool reloadOnAppResumed = true;
    private string defaultTab = "system";

    [Header("Options")]
    [SerializeField] private bool debugLog = true;

    private Coroutine loadRoutine;
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

        if (string.IsNullOrWhiteSpace(platform))
            platform = "lms3d";

        currentTab = NormalizeTab(defaultTab);
    }

    private void OnEnable()
    {
        FCMManager.OnPushNotificationReceived += HandlePushReceived;
        FCMManager.OnAppResumed += HandleAppResumed;

        if (autoLoadOnEnable)
            Load(currentTab);
    }

    private void OnDisable()
    {
        FCMManager.OnPushNotificationReceived -= HandlePushReceived;
        FCMManager.OnAppResumed -= HandleAppResumed;

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
        tab = NormalizeTab(tab);
        currentTab = tab;

        if (loadRoutine != null)
            StopCoroutine(loadRoutine);

        CancelActiveRequest();
        loadRoutine = StartCoroutine(CoLoad(tab));
    }

    private void HandlePushReceived()
    {
        if (!reloadOnPushReceived) return;

        if (debugLog)
            Debug.Log("[NotificationsLoader] Push received -> reload current tab: " + currentTab);

        ReloadCurrentTab();
    }

    private void HandleAppResumed()
    {
        if (!reloadOnAppResumed) return;

        if (debugLog)
            Debug.Log("[NotificationsLoader] App resumed -> reload current tab: " + currentTab);

        ReloadCurrentTab();
    }

    private string BuildNotificationsUrl(string tab)
    {
        return $"{baseUrl}/notifications" +
               $"?tab={UnityWebRequest.EscapeURL(tab)}" +
               $"&skip={skip}" +
               $"&limit={limit}" +
               $"&platforms={UnityWebRequest.EscapeURL(platform)}";
    }

    private IEnumerator CoLoad(string tab)
    {
        int myVersion = ++loadVersion;

        NotificationsStaticStore.SetLoading(tab);
        // có thể bỏ chỗ này -> mặc định flow mới access token luôn được load nếu có
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
    // vẫn phát tín hiệu để UI rebuild lại theo tab/content parent hiện tại
                NotificationsStaticStore.SetData(tab, root.data);
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