using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class CourseDetailApiClient : MonoBehaviour
{
    [Header("API")]
    public string baseUrl;

    [Header("Auth")]
    public string overrideAccessToken = "";
    public bool useTokenFromStore = true;

    [Header("Detail endpoint")]
    public string detailPathFormat = "/lms/courses/{0}";

    private void Awake()
    {
        if (string.IsNullOrEmpty(baseUrl))
            baseUrl = LmsStore.Instance != null ? LmsStore.Instance.baseUrl : baseUrl;
    }

    public IEnumerator FetchCourseDetail(string courseId, Action<CourseModels.CourseDetail> onDone, Action<string> onError = null)
    {
        if (onDone == null) onDone = _ => { };

        if (string.IsNullOrEmpty(courseId))
        {
            onError?.Invoke("courseId null/empty");
            onDone(null);
            yield break;
        }

        string token = GetToken();
        string url = baseUrl + string.Format(detailPathFormat, UnityWebRequest.EscapeURL(courseId));

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
                onError?.Invoke(body);
                onDone(null);
                yield break;
            }

            CourseModels.CourseDetailResponse resp = null;
            try
            {
                resp = JsonUtility.FromJson<CourseModels.CourseDetailResponse>(body);
            }
            catch (Exception e)
            {
                onError?.Invoke("[CourseDetailApiClient] JSON parse exception: " + e);
                onDone(null);
                yield break;
            }

            onDone(resp != null ? resp.data : null);
        }
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