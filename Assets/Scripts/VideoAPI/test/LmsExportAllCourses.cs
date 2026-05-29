using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LmsExportAllCourses : MonoBehaviour
{
    [Header("API")]
    private string baseUrl;

    [Header("Auth")]
    [Tooltip("Dán token tại đây. Để trống nếu dùng TokenStore.AccessToken.")]
    public string overrideAccessToken = "";
    public bool useTokenFromStore = true;

    public Button startButton;

    [Header("Query (/lms/courses)")]
    public int skip = 0;
    public int limit = 100;
    public string keyword = "";
    public string category = "";
    public string tag = "";
    public string sortBy = "";
    public string order = "";

    [Header("Scene Map")]
    [Tooltip("Scene mặc định cho tất cả course khi export. Sau đó bạn sửa thủ công course nào vào scene nào.")]
    public string defaultSceneName = "<scene_01>";

    [Header("Output")]
    public bool prettyPrintJson = true;
    public string outputFileName = "courses_scene_map.json";

    string SavedPath(string name) => Path.Combine(Application.persistentDataPath, name);

    private void Awake()
    {
        baseUrl = LmsStore.Instance.baseUrl;
    }

    void Start()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(() => StartCoroutine(Run()));
        }

        // Nếu muốn auto chạy thì mở dòng này:
        // StartCoroutine(Run());
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

        string url = BuildMarketUrl();
        string json = null;

        yield return GET(url, token, s => json = s, onErrorBody =>
        {
            SaveText("courses_scene_map_error_raw.json", onErrorBody, prettyPrintJson);
        });

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("[LMS] Empty body from /lms/courses.");
            yield break;
        }

        string sceneMapJson = TransformMarketJson(json);

        SaveText(outputFileName, sceneMapJson, prettyPrintJson);
        Debug.Log($"[LMS] Saved course scene map: {SavedPath(outputFileName)}");
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

            if (error)
            {
                Debug.LogWarning($"[LMS] GET error:\n{body}");
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

    string NormalizeBearer(string raw)
    {
        var t = raw?.Trim() ?? "";

        if (t.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            t = t.Substring("Bearer ".Length).Trim();

        return t;
    }

    // ---------- Build URL ----------
    string BuildMarketUrl()
    {
        var sb = new StringBuilder($"{baseUrl}/lms/courses?skip={skip}&limit={limit}");

        if (!string.IsNullOrEmpty(keyword))
            sb.Append("&keyword=").Append(UnityWebRequest.EscapeURL(keyword));

        if (!string.IsNullOrEmpty(sortBy))
            sb.Append("&sortBy=").Append(UnityWebRequest.EscapeURL(sortBy));

        if (!string.IsNullOrEmpty(order))
            sb.Append("&order=").Append(UnityWebRequest.EscapeURL(order));

        if (!string.IsNullOrEmpty(tag))
            sb.Append("&tag=").Append(UnityWebRequest.EscapeURL(tag));

        if (!string.IsNullOrEmpty(category))
            sb.Append("&category=").Append(UnityWebRequest.EscapeURL(category));

        return sb.ToString();
    }

    // ---------- Transform JSON ----------
    // Output đúng format map:
    // [
    //   {
    //     "sceneName": "<scene_01>",
    //     "_id": "66cd74f1cf0a681e2153fd90",
    //     "seo": "dai-dao-chi-gian-phong-thuy-co-hoc-(trai-nghiem)",
    //     "image": "https://...",
    //     "title": "Đại Đạo Chí Giản..."
    //   }
    // ]
    string TransformMarketJson(string rawJson)
    {
        string arr = ExtractItemsArray(rawJson);
        if (string.IsNullOrEmpty(arr))
        {
            Debug.LogWarning("[LMS] Cannot find items array in API response.");
            return "[]";
        }

        var objects = SplitTopLevelObjects(arr);

        var sb = new StringBuilder();
        sb.Append('[');

        bool first = true;
        int exportedCount = 0;
        int skippedCount = 0;

        foreach (var obj in objects)
        {
            string id = MatchMongoId(obj);
            string seo = MatchNestedStringField(obj, "seo", "url");
            string image = MatchStringField(obj, "image");
            string title = MatchStringField(obj, "title");

            if (string.IsNullOrEmpty(id))
            {
                skippedCount++;
                continue;
            }

            if (!first)
                sb.Append(',');

            first = false;
            exportedCount++;

            sb.Append('{');

            AppendStringPair(sb, "sceneName", defaultSceneName, false);
            AppendStringPair(sb, "_id", id, true);
            AppendStringPair(sb, "seo", seo, true);
            AppendStringPair(sb, "image", image, true);
            AppendStringPair(sb, "title", title, true);

            sb.Append('}');
        }

        sb.Append(']');

        Debug.Log($"[LMS] Scene map export done. Exported: {exportedCount}, Skipped: {skippedCount}");

        return sb.ToString();
    }

    void AppendStringPair(StringBuilder sb, string key, string value, bool prependComma)
    {
        if (prependComma)
            sb.Append(',');

        sb.Append('\"').Append(JsonEscape(key)).Append("\":");

        if (value == null)
        {
            sb.Append("null");
        }
        else
        {
            sb.Append('\"').Append(JsonEscape(value)).Append('\"');
        }
    }

    // ---------- Extract Array ----------
    string ExtractItemsArray(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return null;

        // Ưu tiên lấy "items": [...]
        var itemsIdx = raw.IndexOf("\"items\"", StringComparison.OrdinalIgnoreCase);
        if (itemsIdx >= 0)
        {
            int bracket = raw.IndexOf('[', itemsIdx);
            if (bracket >= 0)
            {
                int end = FindMatchingBracket(raw, bracket, '[', ']');
                if (end > bracket)
                    return raw.Substring(bracket, end - bracket + 1);
            }
        }

        // Nếu API trả trực tiếp array thì lấy array đầu tiên.
        int firstArr = raw.IndexOf('[');
        if (firstArr >= 0)
        {
            int end = FindMatchingBracket(raw, firstArr, '[', ']');
            if (end > firstArr)
                return raw.Substring(firstArr, end - firstArr + 1);
        }

        return null;
    }

    int FindMatchingBracket(string s, int openIdx, char openCh, char closeCh)
    {
        int depth = 0;

        for (int i = openIdx; i < s.Length; i++)
        {
            char c = s[i];

            if (c == '"')
            {
                i = SkipString(s, i);
                continue;
            }

            if (c == openCh)
            {
                depth++;
            }
            else if (c == closeCh)
            {
                depth--;

                if (depth == 0)
                    return i;
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

            if (escaped)
            {
                escaped = false;
                continue;
            }

            if (c == '\\')
            {
                escaped = true;
                continue;
            }

            if (c == '"')
                break;
        }

        return i;
    }

    List<string> SplitTopLevelObjects(string arrJson)
    {
        var list = new List<string>();

        if (string.IsNullOrEmpty(arrJson))
            return list;

        int start = arrJson.IndexOf('[');
        int end = arrJson.LastIndexOf(']');

        if (start < 0 || end <= start)
            return list;

        int i = start + 1;

        while (i < end)
        {
            while (i < end && char.IsWhiteSpace(arrJson[i]))
                i++;

            if (i < end && arrJson[i] == ',')
            {
                i++;
                continue;
            }

            while (i < end && char.IsWhiteSpace(arrJson[i]))
                i++;

            if (i >= end)
                break;

            if (arrJson[i] == '{')
            {
                int objEnd = FindMatchingBracket(arrJson, i, '{', '}');

                if (objEnd > i)
                {
                    string obj = arrJson.Substring(i, objEnd - i + 1);
                    list.Add(obj);
                    i = objEnd + 1;
                    continue;
                }

                break;
            }

            i++;
        }

        return list;
    }

    // ---------- Matchers ----------
    string MatchStringField(string objJson, string field)
    {
        if (string.IsNullOrEmpty(objJson))
            return null;

        var rx = new Regex(
            $"\"{Regex.Escape(field)}\"\\s*:\\s*\"((?:\\\\.|[^\"])*)\"",
            RegexOptions.IgnoreCase
        );

        var m = rx.Match(objJson);
        return m.Success ? JsonUnescapeSimple(m.Groups[1].Value) : null;
    }

    string MatchNestedStringField(string objJson, string parent, string child)
    {
        int pIdx = objJson.IndexOf($"\"{parent}\"", StringComparison.OrdinalIgnoreCase);
        if (pIdx < 0)
            return null;

        int braceIdx = objJson.IndexOf('{', pIdx);
        if (braceIdx < 0)
            return null;

        int end = FindMatchingBracket(objJson, braceIdx, '{', '}');
        if (end <= braceIdx)
            return null;

        string sub = objJson.Substring(braceIdx, end - braceIdx + 1);
        return MatchStringField(sub, child);
    }

    // Hỗ trợ cả 2 dạng API:
    // "_id": "66cd..."
    // "_id": { "$oid": "66cd..." }
    // Output cuối cùng luôn là string: "_id": "66cd..."
    string MatchMongoId(string objJson)
    {
        if (string.IsNullOrEmpty(objJson))
            return null;

        string directId = MatchStringField(objJson, "_id");
        if (!string.IsNullOrEmpty(directId))
            return directId;

        int idIdx = objJson.IndexOf("\"_id\"", StringComparison.OrdinalIgnoreCase);
        if (idIdx < 0)
            return null;

        int braceIdx = objJson.IndexOf('{', idIdx);
        if (braceIdx < 0)
            return null;

        int end = FindMatchingBracket(objJson, braceIdx, '{', '}');
        if (end <= braceIdx)
            return null;

        string idBlock = objJson.Substring(braceIdx, end - braceIdx + 1);
        return MatchStringField(idBlock, "$oid");
    }

    string JsonEscape(string s)
    {
        if (s == null)
            return null;

        return s
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"")
            .Replace("\n", "\\n")
            .Replace("\r", "\\r")
            .Replace("\t", "\\t");
    }

    string JsonUnescapeSimple(string s)
    {
        if (s == null)
            return null;

        return s
            .Replace("\\\"", "\"")
            .Replace("\\\\", "\\")
            .Replace("\\n", "\n")
            .Replace("\\r", "\r")
            .Replace("\\t", "\t");
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
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[LMS] Save failed ({fileName}): {ex.Message}");
        }
    }

    bool LooksLikeJson(string s)
    {
        if (string.IsNullOrEmpty(s))
            return false;

        s = s.TrimStart();
        return s.StartsWith("{") || s.StartsWith("[");
    }

    string PrettyJson(string json)
    {
        var sb = new StringBuilder();
        bool quoted = false;
        int indent = 0;

        for (int i = 0; i < json.Length; i++)
        {
            char ch = json[i];

            switch (ch)
            {
                case '{':
                case '[':
                    sb.Append(ch);
                    if (!quoted)
                    {
                        sb.AppendLine();
                        sb.Append(new string(' ', ++indent * 2));
                    }
                    break;

                case '}':
                case ']':
                    if (!quoted)
                    {
                        sb.AppendLine();
                        sb.Append(new string(' ', --indent * 2));
                    }
                    sb.Append(ch);
                    break;

                case '"':
                    sb.Append(ch);

                    bool escaped = false;
                    int j = i;

                    while (j > 0 && json[--j] == '\\')
                        escaped = !escaped;

                    if (!escaped)
                        quoted = !quoted;

                    break;

                case ',':
                    sb.Append(ch);
                    if (!quoted)
                    {
                        sb.AppendLine();
                        sb.Append(new string(' ', indent * 2));
                    }
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