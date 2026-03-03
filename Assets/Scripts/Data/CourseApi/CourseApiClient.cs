using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class CourseApiClient : MonoBehaviour
{
    string baseUrl;

    [Header("Auth")]
    public string overrideAccessToken = "";
    public bool useTokenFromStore = true;

    [Header("Query (/lms/courses)")]
    public int limitPerPage = 100;
    public string keyword = "";
    public string category = "";
    public string sortBy = "";
    public string order = "";

    private void Awake()
    {
        if (string.IsNullOrEmpty(baseUrl))
            baseUrl = LmsStore.Instance != null ? LmsStore.Instance.baseUrl : baseUrl;
    }

    public IEnumerator FetchAllCoursesLite(Action<CourseModels.CourseLite[]> onDone, Action<string> onError = null)
    {
        if (onDone == null) onDone = _ => { };

        string token = GetToken();
        int nextSkip = 0;

        var list = new List<CourseModels.CourseLite>();

        while (true)
        {
            string url = BuildUrl(nextSkip, limitPerPage);

            string body = null;
            // bool failed = false;

            yield return GET(url, token,
                onSuccess: s => body = s,
                onErrorBody: err =>
                {
                    // failed = true;
                    onError?.Invoke(err);
                    body = err; // giữ body để debug
                });

            if (string.IsNullOrEmpty(body)) break;

            CourseModels.CourseListResponse resp;
            try
            {
                resp = JsonUtility.FromJson<CourseModels.CourseListResponse>(body);
            }
            catch (Exception e)
            {
                onError?.Invoke("[CourseApiClient] JSON parse exception: " + e + "\nBody head:\n" +
                                body.Substring(0, Mathf.Min(400, body.Length)));
                break;
            }

            // data.data[]
            var arr = (resp != null && resp.data != null) ? resp.data.data : null;

            if (arr == null || arr.Length == 0) break;

            list.AddRange(arr);

            if (arr.Length < limitPerPage) break;
            nextSkip += limitPerPage;
        }

        onDone(list.ToArray());
    }

    IEnumerator GET(string url, string token, Action<string> onSuccess, Action<string> onErrorBody)
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
            if (error) onErrorBody?.Invoke(body);
            else onSuccess?.Invoke(body);
        }
    }

    string BuildUrl(int skip, int limit)
    {
        var sb = new System.Text.StringBuilder($"{baseUrl}/lms/courses?skip={skip}&limit={limit}");
        if (!string.IsNullOrEmpty(keyword)) sb.Append("&keyword=").Append(UnityWebRequest.EscapeURL(keyword));
        if (!string.IsNullOrEmpty(sortBy)) sb.Append("&sortBy=").Append(UnityWebRequest.EscapeURL(sortBy));
        if (!string.IsNullOrEmpty(order)) sb.Append("&order=").Append(UnityWebRequest.EscapeURL(order));
        if (!string.IsNullOrEmpty(category)) sb.Append("&category=").Append(UnityWebRequest.EscapeURL(category));
        return sb.ToString();
    }

    string GetToken()
    {
        if (!string.IsNullOrWhiteSpace(overrideAccessToken))
            return NormalizeBearer(overrideAccessToken);

        if (useTokenFromStore && !string.IsNullOrWhiteSpace(TokenStore.AccessToken))
            return NormalizeBearer(TokenStore.AccessToken);

        return null;
    }

    string NormalizeBearer(string raw)
    {
        var t = raw != null ? raw.Trim() : "";
        if (t.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            t = t.Substring("Bearer ".Length).Trim();
        return t;
    }
}