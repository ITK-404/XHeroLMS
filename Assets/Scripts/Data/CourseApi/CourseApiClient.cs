using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class CourseApiClient : MonoBehaviour
{
    private const string DefaultBaseUrl = "https://apis-lms.xheroapp.com";
    private string baseUrl;

    [Header("Auth")]
    [SerializeField] private string overrideAccessToken = "";
    [SerializeField] private bool useTokenFromStore = true;

    [Header("Query (/lms/courses)")]
    [SerializeField] private int limitPerPage = 100;
    [SerializeField] private int maxPagesToFetch = 100;
    [SerializeField] private string keyword = "";
    [SerializeField] private string category = "";
    [SerializeField] private string sortBy = "";
    [SerializeField] private string order = "";

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private void Awake()
    {
        EnsureBaseUrl();

        if (debugLog)
            Debug.Log($"[CourseApiClient] Awake baseUrl='{baseUrl}'");
    }

    public IEnumerator FetchCoursesPage(
        int skip,
        int limit,
        Action<List<CourseListItemData>, bool> onDone,
        Action<string> onError = null)
    {
        if (onDone == null) onDone = (_, __) => { };

        string token = GetToken();
        string url = BuildUrl(skip, Mathf.Max(1, limit));

        string body = null;
        bool hasError = false;

        yield return GET(url, token,
            onSuccess: s => body = s,
            onErrorBody: err =>
            {
                hasError = true;
                onError?.Invoke(err);
            });

        if (hasError || string.IsNullOrEmpty(body))
        {
            if (skip == 0 && !hasError)
                onError?.Invoke("[CourseApiClient] Empty response body.");

            onDone(new List<CourseListItemData>(), false);
            yield break;
        }

        CourseModels.CourseListResponse resp = null;
        try
        {
            resp = JsonUtility.FromJson<CourseModels.CourseListResponse>(body);
        }
        catch (Exception e)
        {
            onError?.Invoke("[CourseApiClient] JSON parse exception: " + e.Message +
                            "\nBody head:\n" + body.Substring(0, Mathf.Min(400, body.Length)));
            onDone(new List<CourseListItemData>(), false);
            yield break;
        }

        var arr = ResolveCourseArray(resp);
        if (arr == null || arr.Length == 0)
        {
            if (skip == 0)
            {
                string head = body.Substring(0, Mathf.Min(400, body.Length));
                onError?.Invoke("[CourseApiClient] First page returned no course data.\nBody head:\n" + head);
            }

            onDone(new List<CourseListItemData>(), false);
            yield break;
        }

        var result = new List<CourseListItemData>(arr.Length);
        for (int i = 0; i < arr.Length; i++)
        {
            var mapped = CourseModels.ToListItem(arr[i]);
            if (mapped != null)
                result.Add(mapped);
        }

        bool hasMore = ResolveHasMore(resp, skip, limit, arr.Length);

        if (debugLog)
            Debug.Log($"[CourseApiClient] FetchCoursesPage skip={skip} limit={limit} result={result.Count} hasMore={hasMore}");

        onDone(result, hasMore);
    }

    public IEnumerator FetchAllCourseListItems(
        Action<List<CourseListItemData>> onDone,
        Action<string> onError = null,
        Action<List<CourseListItemData>> onFirstPage = null)
    {
        if (onDone == null) onDone = _ => { };

        var all = new List<CourseListItemData>();
        var indexById = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        int nextSkip = 0;
        int safeLimit = Mathf.Max(1, limitPerPage);
        int pagesFetched = 0;

        while (true)
        {
            bool done = false;
            bool requestFailed = false;
            List<CourseListItemData> pageItems = null;
            bool hasMore = false;

            yield return FetchCoursesPage(
                skip: nextSkip,
                limit: safeLimit,
                onDone: (items, more) =>
                {
                    pageItems = items;
                    hasMore = more;
                    done = true;
                },
                onError: err =>
                {
                    requestFailed = true;
                    onError?.Invoke(err);
                    done = true;
                });

            if (requestFailed)
                yield break;

            if (!done || pageItems == null || pageItems.Count == 0)
                break;

            AppendOrReplaceById(all, indexById, pageItems);
            pagesFetched++;

            if (pagesFetched == 1)
                onFirstPage?.Invoke(new List<CourseListItemData>(all));

            if (!hasMore)
                break;

            if (pagesFetched >= Mathf.Max(1, maxPagesToFetch))
            {
                onError?.Invoke($"[CourseApiClient] Reached maxPagesToFetch={maxPagesToFetch}.");
                yield break;
            }

            nextSkip += safeLimit;
        }

        if (debugLog)
            Debug.Log($"[CourseApiClient] FetchAllCourseListItems total={all.Count}");

        if (all.Count == 0)
        {
            onError?.Invoke("[CourseApiClient] No course data returned.");
            yield break;
        }

        onDone(all);
    }

    private IEnumerator GET(string url, string token, Action<string> onSuccess, Action<string> onErrorBody)
    {
        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = 20;
            token = NormalizeBearer(token);

            if (!string.IsNullOrWhiteSpace(token))
                req.SetRequestHeader("Authorization", "Bearer " + token);

            req.SetRequestHeader("Accept", "application/json");
            req.SetRequestHeader("x-data", LmsSecurityHeader.BuildXDataHeader());

            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool error = req.result == UnityWebRequest.Result.ConnectionError ||
                         req.result == UnityWebRequest.Result.ProtocolError;
#else
            bool error = req.isNetworkError || req.isHttpError;
#endif

            string body = req.downloadHandler != null ? req.downloadHandler.text : "";

            if (error)
            {
                string errorText = !string.IsNullOrWhiteSpace(body)
                    ? body
                    : $"[CourseApiClient] GET failed url={url} code={req.responseCode} error={req.error}";

                if (debugLog)
                    Debug.LogError($"[CourseApiClient] GET ERROR url={url}\n{errorText}");

                onErrorBody?.Invoke(errorText);
            }
            else
            {
                onSuccess?.Invoke(body);
            }
        }
    }

    private string BuildUrl(int skip, int limit)
    {
        EnsureBaseUrl();

        var sb = new StringBuilder($"{baseUrl}/lms/courses?skip={skip}&limit={limit}");

        if (!string.IsNullOrEmpty(keyword))
            sb.Append("&keyword=").Append(UnityWebRequest.EscapeURL(keyword));

        if (!string.IsNullOrEmpty(sortBy))
            sb.Append("&sortBy=").Append(UnityWebRequest.EscapeURL(sortBy));

        if (!string.IsNullOrEmpty(order))
            sb.Append("&order=").Append(UnityWebRequest.EscapeURL(order));

        if (!string.IsNullOrEmpty(category))
            sb.Append("&category=").Append(UnityWebRequest.EscapeURL(category));

        return sb.ToString();
    }

    private void EnsureBaseUrl()
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            baseUrl = LmsStore.Instance != null ? LmsStore.Instance.baseUrl : "";

        if (string.IsNullOrWhiteSpace(baseUrl))
            baseUrl = DefaultBaseUrl;

        baseUrl = (baseUrl ?? "").Trim().TrimEnd('/');
    }

    private CourseModels.CourseLite[] ResolveCourseArray(CourseModels.CourseListResponse resp)
    {
        var payload = resp != null ? resp.data : null;
        if (payload == null)
            return null;

        if (payload.data != null && payload.data.Length > 0)
            return payload.data;

        if (payload.items != null && payload.items.Length > 0)
            return payload.items;

        return payload.courses;
    }

    private bool ResolveHasMore(CourseModels.CourseListResponse resp, int skip, int limit, int count)
    {
        var payload = resp != null ? resp.data : null;
        if (payload != null)
        {
            if (payload.hasMore || payload.hasNextPage)
                return true;

            if (payload.total > 0)
                return skip + count < payload.total;

            if (payload.totalPages > 0 && payload.page > 0)
                return payload.page < payload.totalPages;
        }

        return count >= Mathf.Max(1, limit);
    }

    private void AppendOrReplaceById(
        List<CourseListItemData> target,
        Dictionary<string, int> indexById,
        IReadOnlyList<CourseListItemData> pageItems)
    {
        for (int i = 0; i < pageItems.Count; i++)
        {
            var item = pageItems[i];
            if (item == null || string.IsNullOrWhiteSpace(item.id))
                continue;

            if (indexById.TryGetValue(item.id, out int index))
            {
                target[index] = item;
                continue;
            }

            indexById[item.id] = target.Count;
            target.Add(item);
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
        var t = raw != null ? raw.Trim() : "";
        if (t.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            t = t.Substring("Bearer ".Length).Trim();
        return t;
    }
}
