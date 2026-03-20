using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class CourseDetailLoader : MonoBehaviour
{
    [Header("API")]
    [SerializeField] private string baseUrl = "";

    [Header("Config")]
    [SerializeField] private int timeoutSeconds = 20;
    [SerializeField] private bool clearStoreBeforeLoad = true;

    [Header("Auth")]
    [SerializeField] private string overrideAccessToken = "";
    [SerializeField] private bool useTokenFromStore = true;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private Coroutine _loadRoutine;
    private UnityWebRequest _activeRequest;
    private int _loadVersion = 0;

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            if (LmsStore.Instance != null && !string.IsNullOrWhiteSpace(LmsStore.Instance.baseUrl))
                baseUrl = LmsStore.Instance.baseUrl;
        }

        baseUrl = (baseUrl ?? "").Trim().TrimEnd('/');

        if (debugLog)
            Debug.Log($"[CourseDetailLoader] Awake baseUrl='{baseUrl}'");
    }

    public void Load(string courseId) => Load(courseId, false);

    public void Load(string courseId, bool forceReload)
    {
        if (string.IsNullOrWhiteSpace(courseId))
        {
            CourseDetailStaticStore.SetError(courseId, "courseId is null/empty");
            return;
        }

        courseId = courseId.Trim();

        if (debugLog)
        {
            Debug.Log(
                $"[CourseDetailLoader] Load({courseId}) forceReload={forceReload} | " +
                $"StoreId={CourseDetailStaticStore.CurrentCourseId} | HasData={CourseDetailStaticStore.HasData}"
            );
        }

        if (!forceReload &&
            CourseDetailStaticStore.HasData &&
            CourseDetailStaticStore.IsCurrent(courseId))
        {
            if (debugLog)
                Debug.Log("[CourseDetailLoader] Skip reload because detail store already has this course.");
            return;
        }

        Dispose();
        _loadVersion++;

        if (clearStoreBeforeLoad)
            CourseDetailStaticStore.Reset();

        _loadRoutine = StartCoroutine(LoadRoutine(courseId, _loadVersion));
    }

    public void Dispose()
    {
        if (_loadRoutine != null)
        {
            StopCoroutine(_loadRoutine);
            _loadRoutine = null;
        }

        if (_activeRequest != null)
        {
            try { _activeRequest.Abort(); } catch { }
            try { _activeRequest.Dispose(); } catch { }
            _activeRequest = null;
        }
    }

    private IEnumerator LoadRoutine(string courseId, int version)
    {
        CourseDetailStaticStore.SetLoading(courseId);

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            CourseDetailStaticStore.SetError(courseId, "baseUrl is empty");
            yield break;
        }

        string token = GetToken();
        string url = $"{baseUrl}/lms/courses/{courseId}?t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        if (debugLog)
            Debug.Log($"[CourseDetailLoader] v{version} GET: {url}");

        _activeRequest = UnityWebRequest.Get(url);
        _activeRequest.timeout = timeoutSeconds;

        _activeRequest.SetRequestHeader("Accept", "application/json");
        _activeRequest.SetRequestHeader("Cache-Control", "no-cache, no-store, must-revalidate");
        _activeRequest.SetRequestHeader("Pragma", "no-cache");
        _activeRequest.SetRequestHeader("Expires", "0");
        _activeRequest.SetRequestHeader("x-data", LmsSecurityHeader.BuildXDataHeader());

        if (!string.IsNullOrWhiteSpace(token))
            _activeRequest.SetRequestHeader("Authorization", "Bearer " + token);

        yield return _activeRequest.SendWebRequest();

        if (version != _loadVersion)
        {
            if (debugLog)
                Debug.LogWarning($"[CourseDetailLoader] v{version} ignored because newer load exists ({_loadVersion}).");

            SafeDisposeRequest();
            yield break;
        }

        if (_activeRequest == null)
        {
            if (debugLog)
                Debug.LogWarning($"[CourseDetailLoader] v{version} request already disposed.");
            yield break;
        }

#if UNITY_2020_2_OR_NEWER
        bool hasError = _activeRequest.result == UnityWebRequest.Result.ConnectionError ||
                        _activeRequest.result == UnityWebRequest.Result.ProtocolError;
#else
        bool hasError = _activeRequest.isNetworkError || _activeRequest.isHttpError;
#endif

        if (hasError)
        {
            string err = $"HTTP Error: {_activeRequest.responseCode} | {_activeRequest.error}";
            if (debugLog) Debug.LogError($"[CourseDetailLoader] v{version} {err}");
            CourseDetailStaticStore.SetError(courseId, err);
            SafeDisposeRequest();
            _loadRoutine = null;
            yield break;
        }

        string json = _activeRequest.downloadHandler != null
            ? _activeRequest.downloadHandler.text
            : null;

        if (debugLog)
        {
            string head = json ?? "null";
            if (head.Length > 400) head = head.Substring(0, 400);
            Debug.Log($"[CourseDetailLoader] v{version} OK code={_activeRequest.responseCode} jsonHead={head}");
        }

        CourseModels.CourseDetailResponse resp = null;
        try
        {
            resp = JsonUtility.FromJson<CourseModels.CourseDetailResponse>(json);
        }
        catch (Exception e)
        {
            string err = "JSON parse failed: " + e.Message;
            if (debugLog) Debug.LogError($"[CourseDetailLoader] v{version} {err}");
            CourseDetailStaticStore.SetError(courseId, err);
            SafeDisposeRequest();
            _loadRoutine = null;
            yield break;
        }

        if (resp == null || !resp.status || resp.course == null)
        {
            string err = "Invalid response or status=false or course=null";
            if (debugLog) Debug.LogError($"[CourseDetailLoader] v{version} {err}");
            CourseDetailStaticStore.SetError(courseId, err);
            SafeDisposeRequest();
            _loadRoutine = null;
            yield break;
        }

        CourseDetailStaticStore.SetCourse(courseId, resp.course);

        if (debugLog)
        {
            int bannerCount = resp.course.banner != null ? resp.course.banner.Length : 0;
            int paymentCount = (resp.course.coursePrice != null && resp.course.coursePrice.paymentOptions != null)
                ? resp.course.coursePrice.paymentOptions.Length
                : 0;

            Debug.Log(
                $"[CourseDetailLoader] v{version} SetCourse OK | " +
                $"id={courseId} | title={resp.course.title} | banners={bannerCount} | paymentOptions={paymentCount}"
            );
        }

        SafeDisposeRequest();
        _loadRoutine = null;
    }

    private void SafeDisposeRequest()
    {
        if (_activeRequest != null)
        {
            try { _activeRequest.Dispose(); } catch { }
            _activeRequest = null;
        }
    }

    private string GetToken()
    {
        if (!string.IsNullOrWhiteSpace(overrideAccessToken))
            return NormalizeBearer(overrideAccessToken);

        if (useTokenFromStore && !string.IsNullOrWhiteSpace(TokenStore.AccessToken))
            return NormalizeBearer(TokenStore.AccessToken);

        return null;
    }

    private string NormalizeBearer(string raw)
    {
        string t = raw != null ? raw.Trim() : "";
        if (t.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            t = t.Substring("Bearer ".Length).Trim();
        return t;
    }

    private void OnDestroy()
    {
        Dispose();
    }
}