using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;

public class CourseListPageLoader : MonoBehaviour
{
    [Header("API")]
    public string baseUrl = "https://apis-dev.xheroapp.com";

    [Header("Auth")]
    [Tooltip("Để trống nếu dùng TokenStore.AccessToken")]
    public string overrideAccessToken = "";
    public bool useTokenFromStore = true;

    [Header("Query (/lms/courses)")]
    public int limitPerPage = 100;
    public string keyword = "";
    public string category = "";
    public string tag = "";
    public string sortBy = "";   // ví dụ "createdAt"
    public string order = "";    // "asc" | "desc"

    [Header("UI – Kệ sách")]
    [Tooltip("Content (RectTransform) để chứa các kệ")]
    public RectTransform contentParent;
    [Tooltip("Prefab KỆ (chứa BookShelfUI với 4 BookHandler con)")]
    public BookShelfUI shelfPrefab;

    [Tooltip("Khoảng cách dọc giữa các kệ (UI anchoredPosition)")]
    public float shelfSpacingY = 260f;
    [Tooltip("Offset X,Y cho kệ đầu tiên (UI anchoredPosition)")]
    public Vector2 firstShelfOffset = Vector2.zero;

    public bool autoRunOnStart = true;
    public bool clearOldOnReload = true;

    [Header("Price display")]
    public bool useCurrentPriceFirst = true;
    public string priceFormat = "{0:#,0}₫";
    
    [Header("Book model tuning (không ảnh hưởng kệ)")]
    [Tooltip("Scale đồng nhất cho BookModel (1 = giữ nguyên)")]
    [Range(0.1f, 2f)] public float bookModelScale = 0.85f;
    [Tooltip("Offset local position cho BookModel")]
    public Vector3 bookModelLocalOffset = Vector3.zero;
    [Tooltip("Offset local rotation (Euler) cho BookModel")]
    public Vector3 bookModelLocalEulerOffset = Vector3.zero;

    // cache
    private readonly List<CourseData> _courses = new();

    [Serializable]
    public class CourseData
    {
        public string id;
        public string title;
        public string sku;
        public string seoUrl;
        public float? originalPrice;
        public float? currentPrice;
    }

    private void Start()
    {
        if (autoRunOnStart) StartCoroutine(LoadAndSpawnAll());
    }

    /// <summary>Fetch toàn bộ rồi render thành nhiều kệ; mỗi kệ 4 quyển.</summary>
    public IEnumerator LoadAndSpawnAll()
    {
        if (shelfPrefab == null)
        {
            Debug.LogError("[CourseList] shelfPrefab (BookShelfUI) chưa được gán.");
            yield break;
        }
        if (contentParent == null)
        {
            Debug.LogError("[CourseList] contentParent (RectTransform) chưa được gán.");
            yield break;
        }

        if (clearOldOnReload)
        {
            for (int i = contentParent.childCount - 1; i >= 0; i--)
                Destroy(contentParent.GetChild(i).gameObject);
        }

        _courses.Clear();

        string token = GetToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            Debug.LogWarning("[CourseList] No token. Set overrideAccessToken or TokenStore.AccessToken.");
            yield break;
        }

        int nextSkip = 0;
        int page = 0;
        while (true)
        {
            string url = BuildUrl(nextSkip, limitPerPage);
            string body = null;

            yield return GET(url, token,
                onSuccess: s => body = s,
                onErrorBody: err => Debug.LogError($"[CourseList] GET failed page={page}, skip={nextSkip}. Body:\n{err}")
            );

            if (string.IsNullOrEmpty(body)) break;

            string arr = ExtractItemsArray(body);
            var objects = SplitTopLevelObjects(arr);
            if (objects.Count == 0) break;

            foreach (var obj in objects)
            {
                var data = ParseCourse(obj);
                if (data != null) _courses.Add(data);
            }

            if (objects.Count < limitPerPage) break;
            nextSkip += limitPerPage;
            page++;
        }

        SpawnShelves(_courses);
    }

    // ================== RENDER THEO KỆ (mỗi kệ 4 quyển) ==================

    private const int BOOKS_PER_SHELF = 4;

    void SpawnShelves(List<CourseData> list)
    {
        int shelfCount = Mathf.CeilToInt(list.Count / (float)BOOKS_PER_SHELF);
        for (int shelfIndex = 0; shelfIndex < shelfCount; shelfIndex++)
        {
            var shelf = Instantiate(shelfPrefab, contentParent);
            var rt = shelf.transform as RectTransform;
            if (rt != null)
            {
                // anchoredPosition: X giữ nguyên offset X, Y âm để xuống dưới
                rt.anchoredPosition = new Vector2(
                    firstShelfOffset.x,
                    firstShelfOffset.y - shelfIndex * shelfSpacingY
                );
                rt.localScale = Vector3.one;
            }

            int start = shelfIndex * BOOKS_PER_SHELF;
            int take  = Mathf.Min(BOOKS_PER_SHELF, list.Count - start);
            var slice = list.GetRange(start, take);

            ApplyDataToShelf(shelf, slice);
        }
    }

    void ApplyDataToShelf(BookShelfUI shelf, List<CourseData> slice)
    {
        if (shelf.books == null || shelf.books.Length == 0)
            shelf.books = shelf.GetComponentsInChildren<BookHandler>(true);

        for (int i = 0; i < BOOKS_PER_SHELF; i++)
        {
            var hasData = i < slice.Count;
            var slot = (i < shelf.books.Length) ? shelf.books[i] : null;
            if (slot == null) continue;

            slot.gameObject.SetActive(hasData);
            if (!hasData) continue;

            var data = slice[i];
            
            slot.book_name = data.title ?? "(no title)";
            slot.book_sku = data.sku ?? "";
            slot.book_seo = data.seoUrl ?? "";
            if (slot.bookHandleUI != null)
            {
                if (slot.bookHandleUI.priceText != null)
                {
                    float? price = useCurrentPriceFirst ? data.currentPrice ?? data.originalPrice
                                                        : data.originalPrice ?? data.currentPrice;
                    slot.bookHandleUI.priceText.text = price.HasValue
                        ? string.Format(System.Globalization.CultureInfo.InvariantCulture, priceFormat, price.Value)
                        : "";
                }

                if (slot.bookHandleUI.fullPriceText != null)
                {
                    slot.bookHandleUI.fullPriceText.text = data.originalPrice.HasValue
                        ? string.Format(System.Globalization.CultureInfo.InvariantCulture, priceFormat, data.originalPrice.Value)
                        : "";
                }

                slot.bookHandleUI.RefreshColor();
            }

            bool showEnterCourse = (data.currentPrice ?? data.originalPrice) > 0;
            slot.SetBuyCourse(showEnterCourse);
        }
    }

    // ================== NETWORK & JSON (giữ nguyên tinh gọn) ==================
    IEnumerator GET(string url, string token, Action<string> onSuccess, Action<string> onErrorBody)
    {
        using (var req = UnityWebRequest.Get(url))
        {
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

    string BuildUrl(int skip, int limit)
    {
        var sb = new StringBuilder($"{baseUrl}/lms/courses?skip={skip}&limit={limit}");
        if (!string.IsNullOrEmpty(keyword))  sb.Append("&keyword=").Append(UnityWebRequest.EscapeURL(keyword));
        if (!string.IsNullOrEmpty(sortBy))   sb.Append("&sortBy=").Append(UnityWebRequest.EscapeURL(sortBy));
        if (!string.IsNullOrEmpty(order))    sb.Append("&order=").Append(UnityWebRequest.EscapeURL(order));
        if (!string.IsNullOrEmpty(tag))      sb.Append("&tag=").Append(UnityWebRequest.EscapeURL(tag));
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
        var t = raw?.Trim() ?? "";
        if (t.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            t = t.Substring("Bearer ".Length).Trim();
        return t;
    }

    // ---------------- Minimal JSON helpers ----------------
    string ExtractItemsArray(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "[]";

        int idxItems = raw.IndexOf("\"items\"", StringComparison.OrdinalIgnoreCase);
        if (idxItems >= 0)
        {
            int b = raw.IndexOf('[', idxItems);
            if (b >= 0)
            {
                int e = FindMatchingBracket(raw, b, '[', ']');
                if (e > b) return raw.Substring(b, e - b + 1);
            }
        }

        int firstArr = raw.IndexOf('[');
        if (firstArr >= 0)
        {
            int end = FindMatchingBracket(raw, firstArr, '[', ']');
            if (end > firstArr) return raw.Substring(firstArr, end - firstArr + 1);
        }
        return "[]";
    }

    int FindMatchingBracket(string s, int openIdx, char openCh, char closeCh)
    {
        int depth = 0;
        for (int i = openIdx; i < s.Length; i++)
        {
            char c = s[i];
            if (c == '\"') { i = SkipString(s, i); continue; }
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
                }
                else break;
            }
            else i++;
        }
        return list;
    }

    CourseData ParseCourse(string objJson)
    {
        if (string.IsNullOrEmpty(objJson)) return null;

        string id = MatchStringField(objJson, "_id");
        if (string.IsNullOrEmpty(id)) return null;

        string title = MatchStringField(objJson, "title");
        string sku = MatchStringField(objJson, "sku");
        string seoUrl = MatchNestedStringField(objJson, "seo", "url");

        float? p1 = TryParseFloat(MatchNestedNumberField(objJson, "coursePrice", "originalPrice"));
        float? p2 = TryParseFloat(MatchNestedNumberField(objJson, "coursePrice", "currentPrice"));

        return new CourseData
        {
            id = id,
            title = title,
            sku = sku,
            seoUrl = seoUrl,
            originalPrice = p1,
            currentPrice = p2
        };
    }

    float? TryParseFloat(string s)
    {
        if (string.IsNullOrEmpty(s)) return null;
        if (float.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v))
            return v;
        return null;
    }

    string MatchStringField(string objJson, string field)
    {
        var rx = new Regex($"\"{Regex.Escape(field)}\"\\s*:\\s*\"([^\"]*)\"", RegexOptions.IgnoreCase);
        var m = rx.Match(objJson);
        return m.Success ? m.Groups[1].Value : null;
    }

    string MatchNumberField(string objJson, string field)
    {
        var rx = new Regex($"\"{Regex.Escape(field)}\"\\s*:\\s*(-?\\d+(?:\\.\\d+)?)", RegexOptions.IgnoreCase);
        var m = rx.Match(objJson);
        return m.Success ? m.Groups[1].Value : null;
    }

    string MatchNestedStringField(string objJson, string parent, string child)
    {
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
}
[DisallowMultipleComponent]
public class BookModelOriginal : MonoBehaviour
{
    public Vector3    baseLocalPos;
    public Quaternion baseLocalRot;
    public Vector3    baseLocalScale;
}
