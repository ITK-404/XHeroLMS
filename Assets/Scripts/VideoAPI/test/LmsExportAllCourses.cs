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
    private string baseUrl = "https://apis-dev.xheroapp.com";

    [Header("Auth")]
    [Tooltip("Dán token tại đây. Để trống nếu dùng TokenStore.AccessToken.")]
    public string overrideAccessToken = "";
    public bool useTokenFromStore = true;

    public Button startButton;

    [Header("Query (/lms/courses)")]
    public int skip = 0;
    public int limit = 100;          // tùy bạn tăng/giảm
    public string keyword = "";
    public string category = "";
    public string tag = "";
    public string sortBy = "";       // ví dụ: "createdAt"
    public string order = "";        // "asc" | "desc"

    [Header("Output")]
    public bool prettyPrintJson = true;
    public string outputFileName = "courses_market.json";   // sẽ là JSON đã rút gọn

    string SavedPath(string name) => Path.Combine(Application.persistentDataPath, name);

    void Start()
    {
        if (startButton != null)
        {
            startButton.onClick.AddListener(() => StartCoroutine(Run()));
        }
        // Hoặc auto chạy:
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
            SaveText("courses_market_error_raw.json", onErrorBody, prettyPrintJson);
        });

        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("[LMS] Empty body from /lms/courses.");
            yield break;
        }

        // Biến đổi: giữ lại chỉ các field yêu cầu
        string minimized = TransformMarketJson(json);

        // Lưu file
        SaveText(outputFileName, minimized, prettyPrintJson);
        Debug.Log($"[LMS] Saved minimized list: {SavedPath(outputFileName)}");
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

    // Nếu lỡ dán "Bearer xxx" thì cắt bỏ "Bearer "
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
        if (!string.IsNullOrEmpty(keyword))  sb.Append("&keyword=").Append(UnityWebRequest.EscapeURL(keyword));
        if (!string.IsNullOrEmpty(sortBy))   sb.Append("&sortBy=").Append(UnityWebRequest.EscapeURL(sortBy));
        if (!string.IsNullOrEmpty(order))    sb.Append("&order=").Append(UnityWebRequest.EscapeURL(order));
        if (!string.IsNullOrEmpty(tag))      sb.Append("&tag=").Append(UnityWebRequest.EscapeURL(tag));
        if (!string.IsNullOrEmpty(category)) sb.Append("&category=").Append(UnityWebRequest.EscapeURL(category));
        return sb.ToString();
    }

    // ---------- Transform JSON ----------
    // Giữ lại: _id, seo.url, title, coursePrice.originalPrice, coursePrice.currentPrice, sku
    string TransformMarketJson(string rawJson)
    {
        // 1) Lấy ra phần array: ưu tiên "items":[...], nếu không có thì lấy array đầu tiên
        string arr = ExtractItemsArray(rawJson);
        if (string.IsNullOrEmpty(arr))
        {
            // Không tìm thấy array => trả rỗng dạng []
            return "[]";
        }

        // 2) Tách các object top-level trong array
        var objects = SplitTopLevelObjects(arr);

        // 3) Với mỗi object, trích field
        var sb = new StringBuilder();
        sb.Append('[');

        bool first = true;
        foreach (var obj in objects)
        {
            // _id
            string id = MatchStringField(obj, "_id");

            // title
            string title = MatchStringField(obj, "title");

            // sku
            string sku = MatchStringField(obj, "sku");

            // seo.url (nested)
            string seoUrl = MatchNestedStringField(obj, "seo", "url");

            // coursePrice.originalPrice & currentPrice (nested)
            string originalPrice = MatchNestedNumberField(obj, "coursePrice", "originalPrice");
            string currentPrice  = MatchNestedNumberField(obj, "coursePrice", "currentPrice");

            // Bỏ qua object nếu không có _id (tùy ý)
            if (string.IsNullOrEmpty(id)) continue;

            if (!first) sb.Append(',');
            first = false;

            // Ghi object rút gọn
            sb.Append('{');

            bool needComma = false;
            void AddPair(string key, string value, bool isString)
            {
                if (value == null) return; // không ghi field nếu null
                if (needComma) sb.Append(',');
                sb.Append('\"').Append(key).Append("\":");
                if (isString) sb.Append('\"').Append(JsonEscape(value)).Append('\"');
                else sb.Append(value); // number
                needComma = true;
            }

            AddPair("_id", id, true);

            if (!string.IsNullOrEmpty(seoUrl))
            {
                if (needComma) sb.Append(',');
                sb.Append("\"seo\":{");
                sb.Append("\"url\":\"").Append(JsonEscape(seoUrl)).Append("\"}");
                needComma = true;
            }

            AddPair("title", title, true);

            // coursePrice object (chỉ thêm nếu có ít nhất một giá trị)
            if (!string.IsNullOrEmpty(originalPrice) || !string.IsNullOrEmpty(currentPrice))
            {
                if (needComma) sb.Append(',');
                sb.Append("\"coursePrice\":{");
                bool cpFirst = true;
                void CpAdd(string k, string v)
                {
                    if (string.IsNullOrEmpty(v)) return;
                    if (!cpFirst) sb.Append(',');
                    sb.Append('\"').Append(k).Append("\":").Append(v);
                    cpFirst = false;
                }
                CpAdd("originalPrice", originalPrice);
                CpAdd("currentPrice",  currentPrice);
                sb.Append('}');
                needComma = true;
            }

            AddPair("sku", sku, true);

            sb.Append('}');
        }

        sb.Append(']');
        return sb.ToString();
    }

    // Lấy array phần tử từ raw JSON:
    // - Nếu có "items":[...], lấy phần ... đó
    // - Nếu không: tìm array đầu tiên ở top-level
    string ExtractItemsArray(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;
        // Thử tìm "items":[...]
        var itemsIdx = raw.IndexOf("\"items\"", StringComparison.OrdinalIgnoreCase);
        if (itemsIdx >= 0)
        {
            // Tìm dấu '[' sau "items"
            int bracket = raw.IndexOf('[', itemsIdx);
            if (bracket >= 0)
            {
                int end = FindMatchingBracket(raw, bracket, '[', ']');
                if (end > bracket) return raw.Substring(bracket, end - bracket + 1);
            }
        }

        // Không có "items": lấy array đầu tiên
        int firstArr = raw.IndexOf('[');
        if (firstArr >= 0)
        {
            int end = FindMatchingBracket(raw, firstArr, '[', ']');
            if (end > firstArr) return raw.Substring(firstArr, end - firstArr + 1);
        }
        return null;
    }

    // Tìm vị trí ngoặc đóng khớp cho ngoặc mở tại idx
    int FindMatchingBracket(string s, int openIdx, char openCh, char closeCh)
    {
        int depth = 0;
        for (int i = openIdx; i < s.Length; i++)
        {
            char c = s[i];
            if (c == '\"') // bỏ qua nội dung trong string
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
        // vị trí startQuoteIdx là dấu "
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

    // Tách các object top-level bên trong một JSON array "[ {...}, {...} ]"
    List<string> SplitTopLevelObjects(string arrJson)
    {
        var list = new List<string>();
        if (string.IsNullOrEmpty(arrJson)) return list;

        // Bỏ [] bên ngoài
        int start = arrJson.IndexOf('[');
        int end   = arrJson.LastIndexOf(']');
        if (start < 0 || end <= start) return list;

        int i = start + 1;
        while (i < end)
        {
            // bỏ whitespace và dấu phẩy
            while (i < end && char.IsWhiteSpace(arrJson[i])) i++;
            if (i < end && arrJson[i] == ',') { i++; continue; }
            while (i < end && char.IsWhiteSpace(arrJson[i])) i++;
            if (i >= end) break;

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
                else break; // hỏng cấu trúc
            }
            else
            {
                // không phải object -> bỏ qua phần tử
                i++;
            }
        }

        return list;
    }

    // --------- Matchers tối giản cho field ---------
    // Lưu ý: Regex tối giản, phù hợp dữ liệu sạch từ API (string dùng dấu ")
    string MatchStringField(string objJson, string field)
    {
        if (string.IsNullOrEmpty(objJson)) return null;
        var rx = new Regex($"\"{Regex.Escape(field)}\"\\s*:\\s*\"([^\"]*)\"", RegexOptions.IgnoreCase);
        var m = rx.Match(objJson);
        return m.Success ? m.Groups[1].Value : null;
    }

    string MatchNumberField(string objJson, string field)
    {
        if (string.IsNullOrEmpty(objJson)) return null;
        // number (nguyên hoặc thực). Cho phép null -> trả null
        var rx = new Regex($"\"{Regex.Escape(field)}\"\\s*:\\s*(-?\\d+(?:\\.\\d+)?)", RegexOptions.IgnoreCase);
        var m = rx.Match(objJson);
        return m.Success ? m.Groups[1].Value : null;
    }

    string MatchNestedStringField(string objJson, string parent, string child)
    {
        // tìm block "parent": { ... } rồi match child trong đó
        int pIdx = objJson.IndexOf($"\"{parent}\"", StringComparison.OrdinalIgnoreCase);
        if (pIdx < 0) return null;

        int braceIdx = objJson.IndexOf('{', pIdx);
        if (braceIdx < 0) return null;

        int end = FindMatchingBracket(objJson, braceIdx, '{', '}');
        if (end <= braceIdx) return null;

        string sub = objJson.Substring(braceIdx, end - braceIdx + 1);
        return MatchStringField(sub, child);
    }

    string MatchNestedNumberField(string objJson, string parent, string child)
    {
        int pIdx = objJson.IndexOf($"\"{parent}\"", StringComparison.OrdinalIgnoreCase);
        if (pIdx < 0) return null;

        int braceIdx = objJson.IndexOf('{', pIdx);
        if (braceIdx < 0) return null;

        int end = FindMatchingBracket(objJson, braceIdx, '{', '}');
        if (end <= braceIdx) return null;

        string sub = objJson.Substring(braceIdx, end - braceIdx + 1);
        return MatchNumberField(sub, child);
    }

    string JsonEscape(string s)
    {
        if (s == null) return null;
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    }

    // ---------- Save ----------
    void SaveText(string fileName, string content, bool pretty)
    {
        try
        {
            // Ở đây content đã là JSON rút gọn đúng cấu trúc; vẫn cho phép pretty nếu muốn
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

    // Pretty JSON (đơn giản)
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
