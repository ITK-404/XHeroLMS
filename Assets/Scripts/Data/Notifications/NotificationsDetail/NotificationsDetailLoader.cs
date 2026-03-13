using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class NotificationsDetailLoader : MonoBehaviour
{
    [Header("API")]
    [SerializeField] private string baseUrl = "";
    [SerializeField] private int timeoutSeconds = 20;

    [Header("Auth")]
    [SerializeField] private string authorizationToken = "";

    [Header("Options")]
    [SerializeField] private bool autoLoadOnEnable = false;
    [SerializeField] private string notificationIdOnEnable = "";
    [SerializeField] private bool forceReloadSameId = false;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private Coroutine loadRoutine;
    private UnityWebRequest activeRequest;
    private string lastLoadedId;
    private int loadVersion = 0;

    public bool IsLoading => NotificationsDetailStaticStore.IsLoading;

    private void Awake()
    {
        if (LmsStore.Instance != null)
            baseUrl = (LmsStore.Instance.baseUrl ?? "").Trim().TrimEnd('/');

        if (debugLog)
            Debug.Log("[NotificationsDetailLoader] baseUrl = " + baseUrl);
    }

    private void OnEnable()
    {
        if (autoLoadOnEnable && !string.IsNullOrWhiteSpace(notificationIdOnEnable))
        {
            LoadById(notificationIdOnEnable, authorizationToken);
        }
    }

    private void OnDisable()
    {
        CancelActiveRequest();
    }

    public void SetAuthorizationToken(string token)
    {
        authorizationToken = NormalizeAuthorizationToken(token);
    }

    public void LoadById(string notificationId)
    {
        LoadById(notificationId, authorizationToken);
    }

    public void LoadById(string notificationId, string token)
    {
        if (string.IsNullOrWhiteSpace(notificationId))
        {
            NotificationsDetailStaticStore.SetError(notificationId, "Notification ID is empty.");
            Debug.LogWarning("[NotificationsDetailLoader] notificationId rỗng.");
            return;
        }

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            NotificationsDetailStaticStore.SetError(notificationId, "Base URL is empty.");
            Debug.LogWarning("[NotificationsDetailLoader] baseUrl rỗng.");
            return;
        }

        if (!forceReloadSameId &&
            !string.IsNullOrWhiteSpace(lastLoadedId) &&
            lastLoadedId == notificationId &&
            NotificationsDetailStaticStore.HasData)
        {
            if (debugLog)
                Debug.Log("[NotificationsDetailLoader] Same ID already loaded, skip: " + notificationId);
            return;
        }

        authorizationToken = NormalizeAuthorizationToken(token);

        if (loadRoutine != null)
            StopCoroutine(loadRoutine);

        CancelActiveRequest();

        loadRoutine = StartCoroutine(CoLoadById(notificationId, authorizationToken, ++loadVersion));
    }

    public void ReloadCurrent()
    {
        if (string.IsNullOrWhiteSpace(NotificationsDetailStaticStore.CurrentId))
        {
            if (debugLog)
                Debug.LogWarning("[NotificationsDetailLoader] No current notification id to reload.");
            return;
        }

        LoadById(NotificationsDetailStaticStore.CurrentId, authorizationToken);
    }

    private IEnumerator CoLoadById(string notificationId, string token, int version)
    {
        NotificationsDetailStaticStore.SetLoading(notificationId);

        string encodedId = UnityWebRequest.EscapeURL(notificationId);
        string url = $"{baseUrl}/notifications/{encodedId}";

        if (debugLog)
        {
            Debug.Log("[NotificationsDetailLoader] GET: " + url);
            Debug.Log("[NotificationsDetailLoader] authorization = " + (string.IsNullOrWhiteSpace(token) ? "(empty)" : token));
        }

        activeRequest = UnityWebRequest.Get(url);
        activeRequest.timeout = timeoutSeconds;
        activeRequest.downloadHandler = new DownloadHandlerBuffer();

        if (!string.IsNullOrWhiteSpace(token))
        {
            activeRequest.SetRequestHeader("authorization", token);
        }

        yield return activeRequest.SendWebRequest();

        if (version != loadVersion)
        {
            if (debugLog)
                Debug.LogWarning("[NotificationsDetailLoader] Response ignored because a newer request exists.");

            DisposeActiveRequestOnly();
            yield break;
        }

        long responseCode = activeRequest.responseCode;
        string body = activeRequest.downloadHandler != null ? activeRequest.downloadHandler.text : "";

        if (debugLog)
        {
            Debug.Log("[NotificationsDetailLoader] responseCode = " + responseCode);
            Debug.Log("[NotificationsDetailLoader] responseBody = " + body);
        }

        bool failed =
            activeRequest.result == UnityWebRequest.Result.ConnectionError ||
            activeRequest.result == UnityWebRequest.Result.ProtocolError ||
            activeRequest.result == UnityWebRequest.Result.DataProcessingError;

        if (failed)
        {
            string err = activeRequest.error;

            if (debugLog)
            {
                Debug.LogError("[NotificationsDetailLoader] Request failed: " + err);
                Debug.LogError("[NotificationsDetailLoader] Response body: " + body);
            }

            NotificationsDetailStaticStore.SetError(
                notificationId,
                $"Request failed. Code={responseCode}, Error={(string.IsNullOrWhiteSpace(err) ? "Unknown" : err)}"
            );

            DisposeActiveRequestOnly();
            loadRoutine = null;
            yield break;
        }

        NotificationDetailResponse response = null;

        try
        {
            response = JsonUtility.FromJson<NotificationDetailResponse>(body);
        }
        catch (Exception ex)
        {
            if (debugLog)
                Debug.LogError("[NotificationsDetailLoader] JSON parse exception: " + ex.Message);

            NotificationsDetailStaticStore.SetError(notificationId, "JSON parse failed: " + ex.Message);
            DisposeActiveRequestOnly();
            loadRoutine = null;
            yield break;
        }

        if (response == null)
        {
            NotificationsDetailStaticStore.SetError(notificationId, "Response is null.");
        }
        else if (!response.status)
        {
            NotificationsDetailStaticStore.SetError(notificationId, "API returned status = false.");
        }
        else if (response.data == null)
        {
            NotificationsDetailStaticStore.SetError(notificationId, "Notification detail data is null.");
        }
        else
        {
            lastLoadedId = notificationId;
            NotificationsDetailStaticStore.SetData(notificationId, response.data);

            if (debugLog)
                Debug.Log("[NotificationsDetailLoader] Detail loaded success for id = " + notificationId);
        }

        DisposeActiveRequestOnly();
        loadRoutine = null;
    }

    private string NormalizeAuthorizationToken(string token)
    {
        token = (token ?? "").Trim();

        if (string.IsNullOrWhiteSpace(token))
            return "";

        if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return token;

        return "Bearer " + token;
    }

    private void CancelActiveRequest()
    {
        if (activeRequest != null)
        {
            activeRequest.Abort();
            activeRequest.Dispose();
            activeRequest = null;
        }

        loadRoutine = null;
    }

    private void DisposeActiveRequestOnly()
    {
        if (activeRequest != null)
        {
            activeRequest.Dispose();
            activeRequest = null;
        }
    }
}