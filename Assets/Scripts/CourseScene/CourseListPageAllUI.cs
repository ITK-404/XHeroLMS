using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;

public class CourseListPageLoader : MonoBehaviour
{
    [Header("API")]
    public string baseUrl = "https://apis-dev.xheroapp.com";
    public string allCoursesPath = "/lms/courses";
    public string myCoursesPath  = "/users/lms/courses";

    [Header("All Courses query")]
    public int skip = 0;
    public int limit = 100;
    public string keyword = "";
    public string category = "";
    public string tag = "";
    public string sortBy = "";
    public string order = "";   // "asc" | "desc"

    [Header("Auth (cho My Courses)")]
    public string overrideAccessToken = ""; // rỗng thì dùng TokenStore.AccessToken

    [Header("UI")]
    public Transform contentParent;
    public GameObject itemPrefab;        // prefab phải có 3 TMP
    public TextMeshProUGUI headerText;   // optional

    [Header("Prefab TMP Paths (hoặc Names)")]
    [Tooltip("Đường dẫn Transform tới TMP tiêu đề, ví dụ: Root/TitleText. Để trống thì sẽ tìm theo tên 'Title'.")]
    public string titlePathOrName = "Title";
    [Tooltip("Đường dẫn Transform tới TMP Giá 1 (originalPrice), ví dụ: Root/Prices/Price1. Để trống thì sẽ tìm theo tên 'Price1'.")]
    public string price1PathOrName = "Price1";
    [Tooltip("Đường dẫn Transform tới TMP Giá 2 (currentPrice), ví dụ: Root/Prices/Price2. Để trống thì sẽ tìm theo tên 'Price2'.")]
    public string price2PathOrName = "Price2";

    void Start()
    {
        var key = CourseMenuButtons.GetSavedKey();
        if (headerText) headerText.text = key;
        StartCoroutine(Run(key));
    }

    IEnumerator Run(string key)
    {
        bool isMy = string.Equals(key, CourseMenuButtons.KEY_MY, StringComparison.OrdinalIgnoreCase);
        string url = isMy ? $"{baseUrl}{myCoursesPath}" : BuildAllCoursesUrl();

        // Token nếu cần
        string token = null;
        if (isMy)
        {
            token = GetToken();
            if (string.IsNullOrWhiteSpace(token))
            {
                Debug.LogWarning("[LMS] My Courses cần token.");
                yield break;
            }
        }

        // Call API
        string json = null;
        yield return GET(url, token, s => json = s, err => Debug.LogWarning(err));
        if (string.IsNullOrEmpty(json)) yield break;

        // Parse nhanh
        var items = ParseCourses(json);

        // Build UI
        if (!contentParent || !itemPrefab)
        {
            Debug.LogWarning("[LMS] Thiếu contentParent hoặc itemPrefab.");
            yield break;
        }
        for (int i = contentParent.childCount - 1; i >= 0; i--) Destroy(contentParent.GetChild(i).gameObject);

        foreach (var it in items)
        {
            var go = Instantiate(itemPrefab, contentParent);

            // Lấy TMP theo path (ưu tiên) hoặc theo tên (fallback)
            var titleTMP  = FindTMPByPathOrName(go.transform, titlePathOrName);
            var price1TMP = FindTMPByPathOrName(go.transform, price1PathOrName);
            var price2TMP = FindTMPByPathOrName(go.transform, price2PathOrName);

            if (titleTMP)  titleTMP.text  = string.IsNullOrEmpty(it.title) ? "(no title)" : it.title;

            // Gán giá: nếu có cả 2 -> gạch ngang giá gốc
            if (price1TMP)
            {
                if (!string.IsNullOrEmpty(it.originalPrice) && !string.IsNullOrEmpty(it.currentPrice))
                    price1TMP.text = $"<s>{FormatPrice(it.originalPrice)}</s>";
                else
                    price1TMP.text = string.IsNullOrEmpty(it.originalPrice) ? "" : FormatPrice(it.originalPrice);
            }
            if (price2TMP)
                price2TMP.text = string.IsNullOrEmpty(it.currentPrice) ? "" : FormatPrice(it.currentPrice);
        }

        Debug.Log($"[LMS] Spawned {items.Count} prefabs.");
    }

    TextMeshProUGUI FindTMPByPathOrName(Transform root, string pathOrName)
    {
        if (string.IsNullOrEmpty(pathOrName)) return null;

        // Thử như PATH (hỗ trợ nested)
        var t = root.Find(pathOrName);
        if (t != null)
        {
            var tmp = t.GetComponent<TextMeshProUGUI>();
            if (tmp) return tmp;
        }

        // Fallback: tìm trực tiếp theo tên ở cấp con (không tốn kém)
        var direct = root.Find(pathOrName);
        if (direct != null)
        {
            var tmp = direct.GetComponent<TextMeshProUGUI>();
            if (tmp) return tmp;
        }

        // Fallback cuối: quét tất cả TMP con và chọn theo tên khớp
        var tmps = root.GetComponentsInChildren<TextMeshProUGUI>(true);
        foreach (var tmp in tmps)
        {
            if (tmp.name.Equals(pathOrName, StringComparison.OrdinalIgnoreCase))
                return tmp;
        }
        return null;
    }
    
    string BuildAllCoursesUrl()
    {
        var sb = new StringBuilder($"{baseUrl}{allCoursesPath}?skip={skip}&limit={limit}");
        if (!string.IsNullOrEmpty(keyword))  sb.Append("&keyword=").Append(UnityWebRequest.EscapeURL(keyword));
        if (!string.IsNullOrEmpty(sortBy))   sb.Append("&sortBy=").Append(UnityWebRequest.EscapeURL(sortBy));
        if (!string.IsNullOrEmpty(order))    sb.Append("&order=").Append(UnityWebRequest.EscapeURL(order));
        if (!string.IsNullOrEmpty(tag))      sb.Append("&tag=").Append(UnityWebRequest.EscapeURL(tag));
        if (!string.IsNullOrEmpty(category)) sb.Append("&category=").Append(UnityWebRequest.EscapeURL(category));
        return sb.ToString();
    }

    string GetToken()
    {
        string t = !string.IsNullOrWhiteSpace(overrideAccessToken) ? overrideAccessToken : TokenStore.AccessToken;
        if (string.IsNullOrWhiteSpace(t)) return null;
        t = t.Trim();
        if (t.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)) t = t.Substring(7).Trim();
        return t;
    }

    IEnumerator GET(string url, string bearerToken, Action<string> ok, Action<string> fail)
    {
        using (var req = UnityWebRequest.Get(url))
        {
            if (!string.IsNullOrEmpty(bearerToken)) req.SetRequestHeader("Authorization", "Bearer " + bearerToken);
            req.SetRequestHeader("Accept", "application/json");
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool error = req.result == UnityWebRequest.Result.ConnectionError || req.result == UnityWebRequest.Result.ProtocolError;
#else
            bool error = req.isNetworkError || req.isHttpError;
#endif
            string body = req.downloadHandler.text;
            if (error) fail?.Invoke(body); else ok?.Invoke(body);
        }
    }

    [Serializable] class Brief { public string title, originalPrice, currentPrice; }

    List<Brief> ParseCourses(string raw)
    {
        var list = new List<Brief>();
        string arr = ExtractItemsArray(raw) ?? ExtractFirstArray(raw);
        if (string.IsNullOrEmpty(arr)) return list;

        foreach (var obj in SplitTopLevelObjects(arr))
        {
            var title = MatchStringField(obj, "title");
            var op    = MatchNestedNumberField(obj, "coursePrice", "originalPrice");
            var cp    = MatchNestedNumberField(obj, "coursePrice", "currentPrice");
            if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(op) && string.IsNullOrEmpty(cp)) continue;
            list.Add(new Brief { title = title, originalPrice = op, currentPrice = cp });
        }
        return list;
    }

    string ExtractItemsArray(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;
        int idx = raw.IndexOf("\"items\"", StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return null;
        int b = raw.IndexOf('[', idx); if (b < 0) return null;
        int e = FindMatching(raw, b, '[', ']'); if (e <= b) return null;
        return raw.Substring(b, e - b + 1);
    }
    string ExtractFirstArray(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;
        int b = raw.IndexOf('['); if (b < 0) return null;
        int e = FindMatching(raw, b, '[', ']'); if (e <= b) return null;
        return raw.Substring(b, e - b + 1);
    }
    int FindMatching(string s, int open, char o, char c)
    {
        int d = 0;
        for (int i = open; i < s.Length; i++)
        {
            char ch = s[i];
            if (ch == '\"') { i = SkipString(s, i); continue; }
            if (ch == o) d++; else if (ch == c) { d--; if (d == 0) return i; }
        }
        return -1;
    }
    int SkipString(string s, int q)
    {
        int i = q + 1; bool esc = false;
        for (; i < s.Length; i++) { char c = s[i]; if (esc) { esc = false; continue; } if (c == '\\') { esc = true; continue; } if (c == '\"') break; }
        return i;
    }
    List<string> SplitTopLevelObjects(string arr)
    {
        var list = new List<string>();
        int b = arr.IndexOf('['), e = arr.LastIndexOf(']'); if (b < 0 || e <= b) return list;
        for (int i = b + 1; i < e; i++)
        {
            while (i < e && char.IsWhiteSpace(arr[i])) i++;
            if (i < e && arr[i] == ',') { i++; continue; }
            while (i < e && char.IsWhiteSpace(arr[i])) i++;
            if (i >= e) break;
            if (arr[i] == '{') { int j = FindMatching(arr, i, '{', '}'); if (j > i) { list.Add(arr.Substring(i, j - i + 1)); i = j; } }
        }
        return list;
    }
    string MatchStringField(string obj, string field)
    {
        var m = Regex.Match(obj, $"\"{Regex.Escape(field)}\"\\s*:\\s*\"([^\"]*)\"", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : null;
    }
    string MatchNestedNumberField(string obj, string parent, string child)
    {
        int p = obj.IndexOf($"\"{parent}\"", StringComparison.OrdinalIgnoreCase); if (p < 0) return null;
        int b = obj.IndexOf('{', p); if (b < 0) return null;
        int e = FindMatching(obj, b, '{', '}'); if (e <= b) return null;
        string sub = obj.Substring(b, e - b + 1);
        var m = Regex.Match(sub, $"\"{Regex.Escape(child)}\"\\s*:\\s*(-?\\d+(?:\\.\\d+)?)", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : null;
    }

    string FormatPrice(string raw)
    {
        if (decimal.TryParse(raw, out var v)) return string.Format("{0:N0}", v);
        return raw;
    }
}
