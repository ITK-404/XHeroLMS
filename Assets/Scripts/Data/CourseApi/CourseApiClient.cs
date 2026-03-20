using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class CourseApiClient : MonoBehaviour
{
    private string baseUrl;

    [Header("Auth")]
    [SerializeField] private string overrideAccessToken = "";
    [SerializeField] private bool useTokenFromStore = true;

    [Header("Query (/lms/courses)")]
    [SerializeField] private int limitPerPage = 30;
    [SerializeField] private string keyword = "";
    [SerializeField] private string category = "";
    [SerializeField] private string sortBy = "";
    [SerializeField] private string order = "";

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            baseUrl = LmsStore.Instance != null ? LmsStore.Instance.baseUrl : "";

        baseUrl = (baseUrl ?? "").Trim().TrimEnd('/');

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

        var arr = (resp != null && resp.data != null) ? resp.data.data : null;
        if (arr == null || arr.Length == 0)
        {
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

        bool hasMore = arr.Length >= limit;

        if (debugLog)
            Debug.Log($"[CourseApiClient] FetchCoursesPage skip={skip} limit={limit} result={result.Count} hasMore={hasMore}");

        onDone(result, hasMore);
    }

    public IEnumerator FetchAllCourseListItems(
        Action<List<CourseListItemData>> onDone,
        Action<string> onError = null)
    {
        if (onDone == null) onDone = _ => { };

        var all = new List<CourseListItemData>();
        int nextSkip = 0;

        while (true)
        {
            bool done = false;
            List<CourseListItemData> pageItems = null;
            bool hasMore = false;

            yield return FetchCoursesPage(
                skip: nextSkip,
                limit: limitPerPage,
                onDone: (items, more) =>
                {
                    pageItems = items;
                    hasMore = more;
                    done = true;
                },
                onError: err =>
                {
                    onError?.Invoke(err);
                    done = true;
                });

            if (!done || pageItems == null || pageItems.Count == 0)
                break;

            all.AddRange(pageItems);

            if (!hasMore)
                break;

            nextSkip += limitPerPage;
        }

        if (debugLog)
            Debug.Log($"[CourseApiClient] FetchAllCourseListItems total={all.Count}");

        onDone(all);
    }

    private IEnumerator GET(string url, string token, Action<string> onSuccess, Action<string> onErrorBody)
    {
        using (var req = UnityWebRequest.Get(url))
        {
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
                if (debugLog)
                    Debug.LogError($"[CourseApiClient] GET ERROR url={url}\n{body}");
                onErrorBody?.Invoke(body);
            }
            else
            {
                onSuccess?.Invoke(body);
            }
        }
    }

    private string BuildUrl(int skip, int limit)
    {
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