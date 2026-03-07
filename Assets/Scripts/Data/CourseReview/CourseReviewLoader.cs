using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class CourseReviewLoader : MonoBehaviour
{
    [Header("API")]
    private string baseUrlOverride = "";
    [SerializeField] private int limit = 10;
    [SerializeField] private int timeoutSeconds = 20;
    [SerializeField] private bool useAuthHeader = true;
    [SerializeField] private bool useXDataHeader = true;

    [Header("Options")]
    [SerializeField] private bool autoLoadOnEnable = true;
    [SerializeField] private bool listenCourseDetailStore = true;
    [SerializeField] private bool forceReloadSameCourse = false;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;
    [SerializeField] private string currentCourseId;

    // runtime
    private Coroutine _loadRoutine;
    private UnityWebRequest _activeRequest;

    // chặn request cũ ghi đè request mới
    private int _loadVersion = 0;

    // lưu course load thành công gần nhất
    private string lastLoadedCourseId;

    private void OnEnable()
    {
        if (listenCourseDetailStore)
            CourseDetailStaticStore.OnChanged += HandleCourseDetailChanged;

        if (autoLoadOnEnable)
        {
            string courseId = ResolveCurrentCourseId();
            if (!string.IsNullOrEmpty(courseId))
                LoadReviews(courseId, forceReloadSameCourse);
        }
    }

    private void OnDisable()
    {
        if (listenCourseDetailStore)
            CourseDetailStaticStore.OnChanged -= HandleCourseDetailChanged;
    }

    private void OnDestroy()
    {
        Dispose();
    }

    private void HandleCourseDetailChanged()
    {
        string courseId = ResolveCurrentCourseId();

        if (debugLog)
        {
            Debug.Log($"[CourseReviewLoader] HandleCourseDetailChanged | resolvedCourseId='{courseId}' | currentCourseId='{currentCourseId}' | lastLoadedCourseId='{lastLoadedCourseId}'");
        }

        if (string.IsNullOrEmpty(courseId))
        {
            Dispose();
            currentCourseId = null;
            lastLoadedCourseId = null;
            CourseReviewStaticStore.Reset();
            return;
        }

        if (!forceReloadSameCourse && CourseReviewStaticStore.HasData && CourseReviewStaticStore.CurrentCourseId == courseId)
        {
            if (debugLog)
                Debug.Log("[CourseReviewLoader] Skip reload because review store already has this courseId.");
            return;
        }

        LoadReviews(courseId, forceReloadSameCourse);
    }

    private string ResolveCurrentCourseId()
    {
        if (CourseDetailStaticStore.CurrentCourse != null &&
            !string.IsNullOrEmpty(CourseDetailStaticStore.CurrentCourse._id))
        {
            return CourseDetailStaticStore.CurrentCourse._id;
        }

        return CourseDetailStaticStore.CurrentCourseId;
    }

    public void LoadReviews(string courseId)
    {
        LoadReviews(courseId, false);
    }

    public void LoadReviews(string courseId, bool forceReload)
    {
        if (string.IsNullOrWhiteSpace(courseId))
        {
            if (debugLog)
                Debug.LogWarning("[CourseReviewLoader] courseId null/empty");
            CourseReviewStaticStore.Reset();
            return;
        }

        courseId = courseId.Trim();

        if (debugLog)
        {
            Debug.Log($"[CourseReviewLoader] LoadReviews('{courseId}') forceReload={forceReload} | StoreId={CourseReviewStaticStore.CurrentCourseId} | HasData={CourseReviewStaticStore.HasData}");
        }

        // Nếu store đã có đúng course này rồi thì bỏ qua, trừ khi force reload
        if (!forceReload &&
            CourseReviewStaticStore.HasData &&
            CourseReviewStaticStore.CurrentCourseId == courseId)
        {
            if (debugLog)
                Debug.Log("[CourseReviewLoader] Skip reload because store already has this courseId.");
            return;
        }

        currentCourseId = courseId;

        // hủy request/coroutine cũ
        Dispose();

        // invalidate mọi request cũ
        _loadVersion++;

        bool isDifferentCourse = courseId != lastLoadedCourseId;

        // Nếu đổi course thì clear data cũ luôn
        if (isDifferentCourse)
            CourseReviewStaticStore.Reset();

        _loadRoutine = StartCoroutine(CoLoadReviews(courseId, _loadVersion, isDifferentCourse));
    }

    public void ReloadCurrent()
    {
        if (!string.IsNullOrEmpty(currentCourseId))
            LoadReviews(currentCourseId, true);
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
            try { _activeRequest.Abort(); } catch { /* ignore */ }
            try { _activeRequest.Dispose(); } catch { /* ignore */ }
            _activeRequest = null;
        }
    }

    private IEnumerator CoLoadReviews(string courseId, int version, bool clearOldData)
    {
        CourseReviewStaticStore.SetLoading(courseId, clearOldData);

        string baseUrl = !string.IsNullOrWhiteSpace(baseUrlOverride)
            ? baseUrlOverride.Trim().TrimEnd('/')
            : (LmsStore.Instance != null ? (LmsStore.Instance.baseUrl ?? "").Trim().TrimEnd('/') : "");

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            CourseReviewStaticStore.SetError(courseId, "baseUrl is empty");
            _loadRoutine = null;
            yield break;
        }

        string url = $"{baseUrl}/lms/reviews/{courseId}?limit={limit}&t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        if (debugLog)
            Debug.Log($"[CourseReviewLoader] v{version} GET: {url}");

        _activeRequest = UnityWebRequest.Get(url);
        _activeRequest.timeout = timeoutSeconds;
        _activeRequest.SetRequestHeader("Accept", "application/json");

        // chặn cache ở client/proxy
        _activeRequest.SetRequestHeader("Cache-Control", "no-cache, no-store, must-revalidate");
        _activeRequest.SetRequestHeader("Pragma", "no-cache");
        _activeRequest.SetRequestHeader("Expires", "0");

        if (useAuthHeader && !string.IsNullOrEmpty(TokenStore.AccessToken))
            _activeRequest.SetRequestHeader("Authorization", "Bearer " + TokenStore.AccessToken);

        if (useXDataHeader)
        {
            string xData = LmsSecurityHeader.BuildXDataHeader();
            if (!string.IsNullOrEmpty(xData))
                _activeRequest.SetRequestHeader("x-data", xData);
        }

        yield return _activeRequest.SendWebRequest();

        // Nếu đã có request mới hơn thì bỏ kết quả cũ
        if (version != _loadVersion)
        {
            if (debugLog)
                Debug.LogWarning($"[CourseReviewLoader] v{version} ignored (newer loadVersion={_loadVersion}).");
            SafeDisposeRequest();
            _loadRoutine = null;
            yield break;
        }

        if (_activeRequest == null)
        {
            if (debugLog)
                Debug.LogWarning($"[CourseReviewLoader] v{version} request was disposed during wait.");
            _loadRoutine = null;
            yield break;
        }

#if UNITY_2020_2_OR_NEWER
        bool hasError = _activeRequest.result == UnityWebRequest.Result.ConnectionError ||
                        _activeRequest.result == UnityWebRequest.Result.ProtocolError;
#else
        bool hasError = _activeRequest.isNetworkError || _activeRequest.isHttpError;
#endif

        string body = _activeRequest.downloadHandler != null
            ? _activeRequest.downloadHandler.text
            : null;

        if (hasError)
        {
            Debug.LogError($"[CourseReviewLoader] v{version} HTTP Error: {_activeRequest.responseCode} | {_activeRequest.error}\n{body}");
            CourseReviewStaticStore.SetError(courseId, $"HTTP {_activeRequest.responseCode}: {_activeRequest.error}");
            SafeDisposeRequest();
            _loadRoutine = null;
            yield break;
        }

        CourseReviewApiResponse response = null;

        try
        {
            if (debugLog)
                Debug.Log("[CourseReviewLoader] v" + version + " body = " + body);

            string normalized = NormalizeReviewJson(body);
            response = JsonUtility.FromJson<CourseReviewApiResponse>(normalized);

            if (debugLog)
            {
                Debug.Log("[CourseReviewLoader] v" + version + " response null = " + (response == null));
                Debug.Log("[CourseReviewLoader] v" + version + " review count = " + (response?.data != null ? response.data.Count : -1));
                Debug.Log("[CourseReviewLoader] v" + version + " total = " + (response?.statistics != null ? response.statistics.total : -1));
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[CourseReviewLoader] v" + version + " Parse Error: " + e.Message);
            CourseReviewStaticStore.SetError(courseId, "Parse response failed");
            SafeDisposeRequest();
            _loadRoutine = null;
            yield break;
        }

        if (response == null)
        {
            CourseReviewStaticStore.SetError(courseId, "Response null");
            SafeDisposeRequest();
            _loadRoutine = null;
            yield break;
        }

        var reviews = response.data ?? new List<LmsCourseReviewItem>();
        var statistics = response.statistics;

        CourseReviewStaticStore.SetData(courseId, reviews, statistics);
        lastLoadedCourseId = courseId;

        if (debugLog)
            Debug.Log($"[CourseReviewLoader] v{version} Loaded {reviews.Count} reviews for courseId={courseId}");

        SafeDisposeRequest();
        _loadRoutine = null;
    }

    private void SafeDisposeRequest()
    {
        if (_activeRequest != null)
        {
            try { _activeRequest.Dispose(); } catch { /* ignore */ }
            _activeRequest = null;
        }
    }

    // JsonUtility không map được key "1","2","3","4","5"
    // nên đổi tạm thành _1,_2,_3,_4,_5 để parse được.
    private string NormalizeReviewJson(string json)
    {
        if (string.IsNullOrEmpty(json))
            return json;

        return json
            .Replace("\"1\":", "\"_1\":")
            .Replace("\"2\":", "\"_2\":")
            .Replace("\"3\":", "\"_3\":")
            .Replace("\"4\":", "\"_4\":")
            .Replace("\"5\":", "\"_5\":");
    }
}