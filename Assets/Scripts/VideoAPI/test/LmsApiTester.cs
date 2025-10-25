using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class LmsApiMinimalExporter : MonoBehaviour
{
    [Header("API")]
    public string baseUrl = "https://apis-dev.xheroapp.com";

    [Header("Auth")]
    [Tooltip("Dán token tại đây. Để trống nếu dùng TokenStore.AccessToken.")]
    public string overrideAccessToken = "";
    public bool useTokenFromStore = true;

    [Header("Query (tùy chọn cho /lms/courses)")]
    public int skip = 0;
    public int limit = 20;
    public string keyword = "";
    public string category = "";
    public string sortBy = "";
    public string order = "";

    [Header("Output")]
    public bool prettyPrintJson = true;

    string SavedPath(string name) => Path.Combine(Application.persistentDataPath, name);

    void Start()
    {
        StartCoroutine(RunThreeCalls());
    }

    IEnumerator RunThreeCalls()
    {
        string token = GetToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            // Debug.LogError("[LMS] No token. Set overrideAccessToken or TokenStore.AccessToken.");
            yield break;
        }

        Debug.Log($"[LMS] Output folder:\n{Application.persistentDataPath}");

        // 1) /lms/courses  (market)
        string marketUrl = BuildMarketUrl();
        string marketJson = null;
        yield return GET(marketUrl, token, s => marketJson = s, onErrorBody =>
        {
            SaveText("courses_market_error.json", onErrorBody, prettyPrintJson);
        });
        if (!string.IsNullOrEmpty(marketJson))
            SaveText("courses_market.json", marketJson, prettyPrintJson);

        // 2) /users/lms/courses (my courses)
        string myCoursesUrl = $"{baseUrl}/users/lms/courses?skip=0&limit=100";
        string myCoursesJson = null;
        yield return GET(myCoursesUrl, token, s => myCoursesJson = s, onErrorBody =>
        {
            SaveText("courses_my_error.json", onErrorBody, prettyPrintJson);
        });
        if (!string.IsNullOrEmpty(myCoursesJson))
            SaveText("courses_my.json", myCoursesJson, prettyPrintJson);

        // 3) /lms/courses/{course._id}/private cho TẤT CẢ khóa trong /users/lms/courses
        var allCourseIds = ExtractAllCourseIdsFromMyCourses(myCoursesJson);
        if (allCourseIds.Count == 0)
        {
            // Debug.LogWarning("[LMS] Không trích được course._id nào từ courses_my.json");
            yield break;
        }

        foreach (var courseId in allCourseIds)
        {
            string privateUrl = $"{baseUrl}/lms/courses/{courseId}/private";
            string privateJson = null;
            bool privateOk = true;

            // dùng GET với cả 2 header auth để tương thích swagger (/private yêu cầu 'authorization' raw token)
            yield return GET(privateUrl, token, s => privateJson = s, onErrorBody =>
            {
                privateOk = false;
                SaveText($"course_private_{courseId}_error.json", onErrorBody, prettyPrintJson);
            });

            if (privateOk && !string.IsNullOrEmpty(privateJson))
                SaveText($"course_private_{courseId}.json", privateJson, prettyPrintJson);

            // thư thả một frame
            yield return null;
        }

        // Debug.Log("[LMS] DONE. Đã xuất nhiều JSON cho /private (mỗi khóa một file).");
    }

    // ---------- HTTP ----------
    IEnumerator GET(string url, string token, Action<string> onSuccess, Action<string> onErrorBody)
    {
        using (var req = UnityWebRequest.Get(url))
        {
            // Gắn CẢ HAI header để chắc chắn:
            // - Nhiều endpoint chấp nhận 'Authorization: Bearer <token>'
            // - Swagger của /private hiển thị 'authorization: <token>' (raw, không Bearer)
            req.SetRequestHeader("authorization", token);
            req.SetRequestHeader("Authorization", "Bearer " + token);
            req.SetRequestHeader("Accept", "application/json");

            // Debug.Log("[HTTP GET] " + url);
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool error = req.result == UnityWebRequest.Result.ConnectionError ||
                         req.result == UnityWebRequest.Result.ProtocolError;
#else
            bool error = req.isNetworkError || req.isHttpError;
#endif
            string body = req.downloadHandler.text;

            if (error)
            {
                // Debug.LogError($"[HTTP] {req.responseCode} {req.error}\nBody: {body}");
                onErrorBody?.Invoke(body);
            }
            else
            {
                onSuccess?.Invoke(body);
            }
        }
    }

    // ---------- Token ----------
    string GetToken()
    {
        if (!string.IsNullOrWhiteSpace(overrideAccessToken))
            return NormalizeBearer(overrideAccessToken);

        if (useTokenFromStore && !string.IsNullOrWhiteSpace(TokenStore.AccessToken))
            return NormalizeBearer(TokenStore.AccessToken);

        return null;
    }

    // Nếu lỡ dán "Bearer xxx" thì cắt bỏ "Bearer "
    string NormalizeBearer(string raw)
    {
        var t = raw?.Trim() ?? "";
        if (t.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            t = t.Substring("Bearer ".Length).Trim();
        return t;
    }

    // ---------- Build URLs ----------
    string BuildMarketUrl()
    {
        var sb = new StringBuilder($"{baseUrl}/lms/courses?skip={skip}&limit={limit}");
        if (!string.IsNullOrEmpty(keyword))  sb.Append("&keyword=").Append(UnityWebRequest.EscapeURL(keyword));
        if (!string.IsNullOrEmpty(sortBy))   sb.Append("&sortBy=").Append(UnityWebRequest.EscapeURL(sortBy));
        if (!string.IsNullOrEmpty(order))    sb.Append("&order=").Append(UnityWebRequest.EscapeURL(order));
        if (!string.IsNullOrEmpty(tag))      sb.Append("&tag=").Append(UnityWebRequest.EscapeURL(tag));
        if (!string.IsNullOrEmpty(category)) sb.Append("&category=").Append(UnityWebRequest.EscapeURL(category));
        return sb.ToString();
    }

    // ---------- Helpers: parsing tối giản ----------
    string ExtractFirstId(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        var m = Regex.Match(json, "\"_id\"\\s*:\\s*\"([^\"]+)\"");
        return m.Success ? m.Groups[1].Value : null;
    }

    string ExtractFirstJoinedId(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;

        var idRx = new Regex("\"_id\"\\s*:\\s*\"([^\"]+)\"");
        var joinedTrueRx = new Regex("isJoined\\s*:\\s*true", RegexOptions.IgnoreCase);

        var m = idRx.Match(json);
        while (m.Success)
        {
            int start = Math.Max(0, m.Index - 400);
            int end = Math.Min(json.Length, m.Index + 400);
            string window = json.Substring(start, end - start);
            if (joinedTrueRx.IsMatch(window))
                return m.Groups[1].Value;

            m = m.NextMatch();
        }
        return null;
    }

    // LẤY TẤT CẢ course._id từ /users/lms/courses
    List<string> ExtractAllCourseIdsFromMyCourses(string json)
    {
        var ids = new List<string>();
        if (string.IsNullOrEmpty(json)) return ids;

        // tìm "course": { ... "_id": "<id>" ... }
        var rx = new Regex("\"course\"\\s*:\\s*\\{[\\s\\S]*?\"_id\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase);
        var m = rx.Match(json);
        int guard = 0;
        while (m.Success && guard++ < 10000)
        {
            var id = m.Groups[1].Value;
            if (!ids.Contains(id)) ids.Add(id);
            m = m.NextMatch();
        }
        return ids;
    }

    // ---------- Save ----------
    void SaveText(string fileName, string content, bool pretty)
    {
        try
        {
            if (pretty && LooksLikeJson(content))
                content = PrettyJson(content);

            var full = SavedPath(fileName);
            File.WriteAllText(full, content, Encoding.UTF8);
            // Debug.Log($"[LMS] Saved: {full}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LMS] Save failed ({fileName}): {ex.Message}");
        }
    }

    bool LooksLikeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return false;
        s = s.TrimStart();
        return s.StartsWith("{") || s.StartsWith("[");
    }

    // Pretty JSON (không dùng thư viện ngoài)
    string PrettyJson(string json)
    {
        var sb = new StringBuilder();
        bool quoted = false; int indent = 0;

        for (int i = 0; i < json.Length; i++)
        {
            char ch = json[i];
            switch (ch)
            {
                case '{': case '[':
                    sb.Append(ch);
                    if (!quoted) { sb.AppendLine(); sb.Append(new string(' ', ++indent * 2)); }
                    break;
                case '}': case ']':
                    if (!quoted) { sb.AppendLine(); sb.Append(new string(' ', --indent * 2)); }
                    sb.Append(ch);
                    break;
                case '"':
                    sb.Append(ch);
                    bool escaped = false; int j = i;
                    while (j > 0 && json[--j] == '\\') escaped = !escaped;
                    if (!escaped) quoted = !quoted;
                    break;
                case ',':
                    sb.Append(ch);
                    if (!quoted) { sb.AppendLine(); sb.Append(new string(' ', indent * 2)); }
                    break;
                case ':':
                    sb.Append(quoted ? ":" : ": ");
                    break;
                default:
                    sb.Append(ch);
                    break;
            }
        }
        return sb.ToString();
    }
}
