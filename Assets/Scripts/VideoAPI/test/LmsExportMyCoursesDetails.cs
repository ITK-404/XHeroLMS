using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LmsExportMyCoursesDetails : MonoBehaviour
{
    [Header("API")]
    private string baseUrl = "https://apis-dev.xheroapp.com";

    [Header("Auth")]
    [Tooltip("Dán token tại đây. Để trống nếu dùng TokenStore.AccessToken.")]
    public string overrideAccessToken = "";
    public bool useTokenFromStore = true;

    public Button startButton;
    
    [Header("Fetch /users/lms/courses")]
    public int skip = 0;
    public int limit = 200;

    [Header("Output")]
    public bool prettyPrintJson = true;
    public bool alsoSaveMyListRaw = true; // lưu file gốc từ /users/lms/courses
    public string minimizedOutputFile = "courses_my_min.json"; // file rút gọn

    string SavedPath(string name) => Path.Combine(Application.persistentDataPath, name);

    void Start()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(() => StartCoroutine(Run()));
        }
        // hoặc auto: StartCoroutine(Run());
    }

    IEnumerator Run()
    {
        string token = GetToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            Debug.LogWarning("[LMS] No token. Set overrideAccessToken or TokenStore.AccessToken.");
            yield break;
        }

        Debug.Log($"[LMS] Output folder:\n{Application.persistentDataPath}");

        // 1) Lấy danh sách khóa học của user
        string myCoursesUrl = $"{baseUrl}/users/lms/courses?skip={skip}&limit={limit}";
        string myCoursesJson = null;

        yield return GET(myCoursesUrl, token, s => myCoursesJson = s, onErrorBody =>
        {
            SaveText("courses_my_error.json", onErrorBody, prettyPrintJson);
        });

        if (string.IsNullOrEmpty(myCoursesJson))
        {
            Debug.LogWarning("[LMS] /users/lms/courses trả về rỗng hoặc lỗi.");
            yield break;
        }

        if (alsoSaveMyListRaw)
        {
            SaveText("courses_my.json", myCoursesJson, prettyPrintJson);
            Debug.Log($"[LMS] Saved: {SavedPath("courses_my.json")}");
        }

        // 2) Rút gọn dữ liệu: id (= course._id), seo.url, title, sku
        string minimized = TransformMyCoursesJson(myCoursesJson);
        SaveText(minimizedOutputFile, minimized, prettyPrintJson);
        Debug.Log($"[LMS] Saved minimized list: {SavedPath(minimizedOutputFile)}");
    }

    // ---------- HTTP ----------
    IEnumerator GET(string url, string token, Action<string> onSuccess, Action<string> onErrorBody)
    {
        using (var req = UnityWebRequest.Get(url))
        {
            req.SetRequestHeader("authorization", token);
            req.SetRequestHeader("Authorization", "Bearer " + token);
            req.SetRequestHeader("Accept", "application/json");

            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool error = req.result == UnityWebRequest.Result.ConnectionError ||
                         req.result == UnityWebRequest.Result.ProtocolError;
#else
            bool error = req.isNetworkError || req.isHttpError;
#endif
            string body = req.downloadHandler.text;

            if (error) onErrorBody?.Invoke(body);
            else onSuccess?.Invoke(body);
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

    string NormalizeBearer(string raw)
    {
        var t = raw?.Trim() ?? "";
        if (t.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            t = t.Substring("Bearer ".Length).Trim();
        return t;
    }

    // ---------- trích toàn bộ course._id từ /users/lms/courses ----------
    List<string> ExtractAllCourseIdsFromMyCourses(string json)
    {
        var ids = new List<string>();
        if (string.IsNullOrEmpty(json)) return ids;

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

    // ---------- Transform JSON (/users/lms/courses) ----------
    // Giữ lại: course._id -> "_id", course.seo.url, course.title, course.sku
    string TransformMyCoursesJson(string rawJson)
    {
        // Tìm mảng "list":[ ... ] ở bất kỳ cấp nào
        string arr = ExtractNamedArray(rawJson, "list");
        if (string.IsNullOrEmpty(arr))
            return "[]";

        var items = SplitTopLevelObjects(arr);

        var sb = new StringBuilder();
        sb.Append('[');
        bool firstOut = true;

        foreach (var item in items)
        {
            // Lấy block "course": { ... }
            string courseObj = ExtractNamedObject(item, "course");
            if (string.IsNullOrEmpty(courseObj))
                continue;

            string id = MatchStringField(courseObj, "_id");
            string title = MatchStringField(courseObj, "title");
            string sku = MatchStringField(courseObj, "sku");

            // seo.url
            string seoObj = ExtractNamedObject(courseObj, "seo");
            string seoUrl = string.IsNullOrEmpty(seoObj) ? null : MatchStringField(seoObj, "url");

            if (string.IsNullOrEmpty(id))
                continue;

            if (!firstOut) sb.Append(',');
            firstOut = false;

            sb.Append('{');

            bool needComma = false;
            void AddStr(string key, string val)
            {
                if (string.IsNullOrEmpty(val)) return;
                if (needComma) sb.Append(',');
                sb.Append('\"').Append(key).Append("\":\"").Append(JsonEscape(val)).Append('\"');
                needComma = true;
            }

            // _id
            AddStr("_id", id);

            // seo.url
            if (!string.IsNullOrEmpty(seoUrl))
            {
                if (needComma) sb.Append(',');
                sb.Append("\"seo\":{");
                sb.Append("\"url\":\"").Append(JsonEscape(seoUrl)).Append("\"}");
                needComma = true;
            }

            // title
            AddStr("title", title);

            // sku
            AddStr("sku", sku);

            sb.Append('}');
        }

        sb.Append(']');
        return sb.ToString();
    }
    
    // Tìm "[ ... ]" theo tên thuộc tính (ví dụ "list")
    string ExtractNamedArray(string raw, string name)
    {
        if (string.IsNullOrEmpty(raw)) return null;
        int key = raw.IndexOf($"\"{name}\"", StringComparison.OrdinalIgnoreCase);
        if (key < 0) return null;

        int bracket = raw.IndexOf('[', key);
        if (bracket < 0) return null;

        int end = FindMatchingBracket(raw, bracket, '[', ']');
        if (end <= bracket) return null;

        return raw.Substring(bracket, end - bracket + 1);
    }

    // Tìm "{ ... }" theo tên thuộc tính (ví dụ "course" trong item)
    string ExtractNamedObject(string raw, string name)
    {
        if (string.IsNullOrEmpty(raw)) return null;
        int key = raw.IndexOf($"\"{name}\"", StringComparison.OrdinalIgnoreCase);
        if (key < 0) return null;

        int brace = raw.IndexOf('{', key);
        if (brace < 0) return null;

        int end = FindMatchingBracket(raw, brace, '{', '}');
        if (end <= brace) return null;

        return raw.Substring(brace, end - brace + 1);
    }

    // Tách các object top-level trong "[ {...}, {...} ]"
    List<string> SplitTopLevelObjects(string arrJson)
    {
        var list = new List<string>();
        if (string.IsNullOrEmpty(arrJson)) return list;

        int start = arrJson.IndexOf('[');
        int end   = arrJson.LastIndexOf(']');
        if (start < 0 || end <= start) return list;

        int i = start + 1;
        while (i < end)
        {
            while (i < end && char.IsWhiteSpace(arrJson[i])) i++;
            if (i < end && arrJson[i] == ',') { i++; continue; }
            while (i < end && char.IsWhiteSpace(arrJson[i])) i++;
            if (i >= end) break;

            if (arrJson[i] == '{')
            {
                int objEnd = FindMatchingBracket(arrJson, i, '{', '}');
                if (objEnd > i)
                {
                    list.Add(arrJson.Substring(i, objEnd - i + 1));
                    i = objEnd + 1;
                    continue;
                }
                else break;
            }
            else i++;
        }
        return list;
    }

    int FindMatchingBracket(string s, int openIdx, char openCh, char closeCh)
    {
        int depth = 0;
        for (int i = openIdx; i < s.Length; i++)
        {
            char c = s[i];
            if (c == '\"')
            {
                i = SkipString(s, i);
                continue;
            }
            if (c == openCh) depth++;
            else if (c == closeCh)
            {
                depth--;
                if (depth == 0) return i;
            }
        }
        return -1;
    }

    int SkipString(string s, int startQuoteIdx)
    {
        int i = startQuoteIdx + 1;
        bool escaped = false;
        for (; i < s.Length; i++)
        {
            char c = s[i];
            if (escaped) { escaped = false; continue; }
            if (c == '\\') { escaped = true; continue; }
            if (c == '\"') break;
        }
        return i;
    }

    string MatchStringField(string objJson, string field)
    {
        if (string.IsNullOrEmpty(objJson)) return null;
        var rx = new Regex($"\"{Regex.Escape(field)}\"\\s*:\\s*\"([^\"]*)\"", RegexOptions.IgnoreCase);
        var m = rx.Match(objJson);
        return m.Success ? m.Groups[1].Value : null;
    }

    string JsonEscape(string s)
    {
        if (s == null) return null;
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    }

    // ---------- Save / Pretty ----------
    void SaveText(string fileName, string content, bool pretty)
    {
        try
        {
            if (pretty && LooksLikeJson(content))
                content = PrettyJson(content);

            var full = SavedPath(fileName);
            File.WriteAllText(full, content, Encoding.UTF8);
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
