using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class CourseReviewLoader : MonoBehaviour
{
    [Header("API")]
    [SerializeField] private string baseUrlOverride = "";
    [SerializeField] private int limit = 10;
    [SerializeField] private bool useAuthHeader = true;
    [SerializeField] private bool useXDataHeader = true;

    [Header("Options")]
    [SerializeField] private bool autoLoadOnEnable = true;
    [SerializeField] private bool listenCourseDetailStore = true;
    [SerializeField] private bool forceReloadSameCourse = false;

    [Header("Debug")]
    [SerializeField] private string currentDetailId;

    private Coroutine loadRoutine;
    private string lastLoadedCourseId;

    private void OnEnable()
    {
        if (listenCourseDetailStore)
            CourseDetailStaticStore.OnChanged += HandleCourseDetailChanged;

        if (autoLoadOnEnable)
        {
            string courseId = ResolveCurrentDetailId();
            if (!string.IsNullOrEmpty(courseId))
                LoadReviews(courseId);
        }
    }

    private void OnDisable()
    {
        if (listenCourseDetailStore)
            CourseDetailStaticStore.OnChanged -= HandleCourseDetailChanged;

        if (loadRoutine != null)
        {
            StopCoroutine(loadRoutine);
            loadRoutine = null;
        }
    }

    private void HandleCourseDetailChanged()
    {
        string courseId = ResolveCurrentDetailId();
        if (string.IsNullOrEmpty(courseId))
        {
            CourseReviewStaticStore.Reset();
            return;
        }

        if (!forceReloadSameCourse && courseId == lastLoadedCourseId)
            return;

        LoadReviews(courseId);
    }

    private string ResolveCurrentDetailId()
    {
        if (CourseDetailStaticStore.CurrentDetail != null &&
            !string.IsNullOrEmpty(CourseDetailStaticStore.CurrentDetail._id))
        {
            return CourseDetailStaticStore.CurrentDetail._id;
        }

        if (!string.IsNullOrEmpty(CourseDetailStaticStore.CurrentCourseId))
            return CourseDetailStaticStore.CurrentCourseId;

        return currentDetailId;
    }

    public void LoadReviews(string courseId)
    {
        if (string.IsNullOrEmpty(courseId))
        {
            Debug.LogWarning("[PTS_CourseReviewLoader] courseId null/empty");
            CourseReviewStaticStore.Reset();
            return;
        }

        currentDetailId = courseId;

        if (loadRoutine != null)
            StopCoroutine(loadRoutine);

        loadRoutine = StartCoroutine(CoLoadReviews(courseId));
    }

    public void ReloadCurrent()
    {
        string courseId = ResolveCurrentDetailId();
        if (!string.IsNullOrEmpty(courseId))
            LoadReviews(courseId);
    }

    private IEnumerator CoLoadReviews(string courseId)
    {
        bool isDifferentCourse = courseId != lastLoadedCourseId;

        CourseReviewStaticStore.SetLoading(courseId, clearOldData: isDifferentCourse);

        string baseUrl = !string.IsNullOrEmpty(baseUrlOverride)
            ? baseUrlOverride
            : (LmsStore.Instance != null ? LmsStore.Instance.baseUrl : "");

        baseUrl = (baseUrl ?? "").Trim().TrimEnd('/');

        if (string.IsNullOrEmpty(baseUrl))
        {
            Debug.LogError("[PTS_CourseReviewLoader] Base URL is empty");
            CourseReviewStaticStore.SetError(courseId, "Base URL is empty");
            loadRoutine = null;
            yield break;
        }

        string url = $"{baseUrl}/lms/reviews/{courseId}?limit={limit}";

        using (UnityWebRequest req = UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("Accept", "application/json");

            if (useAuthHeader && !string.IsNullOrEmpty(TokenStore.AccessToken))
                req.SetRequestHeader("Authorization", "Bearer " + NormalizeBearer(TokenStore.AccessToken));

            if (useXDataHeader)
            {
                string xData = LmsSecurityHeader.BuildXDataHeader();
                if (!string.IsNullOrEmpty(xData))
                    req.SetRequestHeader("x-data", xData);
            }

            Debug.Log("[PTS_CourseReviewLoader] GET " + url);
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool hasError = req.result == UnityWebRequest.Result.ConnectionError ||
                            req.result == UnityWebRequest.Result.ProtocolError;
#else
            bool hasError = req.isNetworkError || req.isHttpError;
#endif

            string body = req.downloadHandler != null ? req.downloadHandler.text : "";

            if (hasError)
            {
                Debug.LogError($"[PTS_CourseReviewLoader] HTTP Error: {req.responseCode} | {req.error}\n{body}");
                CourseReviewStaticStore.SetError(courseId, $"HTTP {req.responseCode}: {req.error}");
                loadRoutine = null;
                yield break;
            }

            CourseReviewApiResponse response = null;
            try
            {
                Debug.Log("[PTS_CourseReviewLoader] body = " + body);

                string normalized = NormalizeReviewJson(body);
                response = JsonUtility.FromJson<CourseReviewApiResponse>(normalized);

                Debug.Log("[PTS_CourseReviewLoader] response null = " + (response == null));
                Debug.Log("[PTS_CourseReviewLoader] review count = " + (response?.data != null ? response.data.Count : -1));
                Debug.Log("[PTS_CourseReviewLoader] total = " + (response?.statistics != null ? response.statistics.total : -1));
            }
            catch (System.Exception e)
            {
                Debug.LogError("[PTS_CourseReviewLoader] Parse Error: " + e.Message);
                CourseReviewStaticStore.SetError(courseId, "Parse response failed");
                loadRoutine = null;
                yield break;
            }

            if (response == null)
            {
                CourseReviewStaticStore.SetError(courseId, "Response null");
                loadRoutine = null;
                yield break;
            }

            var reviews = response.data ?? new List<LmsCourseReviewItem>();
            var statistics = response.statistics;

            CourseReviewStaticStore.SetData(courseId, reviews, statistics);
            lastLoadedCourseId = courseId;

            Debug.Log($"[PTS_CourseReviewLoader] Loaded {reviews.Count} reviews for courseId={courseId}");
        }

        loadRoutine = null;
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

    private string NormalizeBearer(string raw)
    {
        var t = raw != null ? raw.Trim() : "";
        if (t.StartsWith("Bearer ", System.StringComparison.OrdinalIgnoreCase))
            t = t.Substring("Bearer ".Length).Trim();
        return t;
    }
}