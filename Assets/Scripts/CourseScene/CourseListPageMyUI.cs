using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

public class CourseListPageMyUI : MonoBehaviour
{
    [Header("API")]
    public string baseUrl = "https://apis-dev.xheroapp.com";

    [Header("Auth")]
    [Tooltip("Để trống nếu dùng TokenStore.AccessToken")]
    public string overrideAccessToken = "";
    public bool useTokenFromStore = true;

    [Header("Auto-run gate (optional)")]
    [Tooltip("Chỉ chạy khi key scene khớp (đọc từ CourseMenuButtons)")]
    public bool runOnlyWhenKeyMatches = true;
    public string requiredKey = CourseMenuButtons.KEY_MY;

    [Header("Query (/users/lms/courses)")]
    public int limitPerPage = 100;

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

    [Header("My Courses options")]
    [Tooltip("Ẩn Buy / bật Enter cho các khóa đã đăng ký")]
    public bool forceEnterForMyCourse = true;

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
        if (!autoRunOnStart) return;

        bool ok = true;
        if (runOnlyWhenKeyMatches)
            ok = CourseMenuButtons.GetSavedKey() == requiredKey;

        if (ok) StartCoroutine(LoadAndSpawnMyList());
    }

    /// <summary>MY: Lấy danh sách khóa học đã đăng ký bằng /users/lms/courses (phân trang) rồi render.</summary>
    public IEnumerator LoadAndSpawnMyList()
    {
        if (!EnsureUI()) yield break;
        _courses.Clear();

        string token = GetToken();
        if (string.IsNullOrWhiteSpace(token))
        {
            Debug.LogWarning("[CourseList/MY] No token. Set overrideAccessToken or TokenStore.AccessToken.");
            yield break;
        }

        int nextSkip = 0;
        int page = 0;
        while (true)
        {
            string url = $"{baseUrl}/users/lms/courses?skip={nextSkip}&limit={limitPerPage}";
            string body = null;

            yield return GET(url, token,
                onSuccess: s => body = s,
                onErrorBody: err => Debug.LogError($"[CourseList/MY] GET my-courses failed page={page}, skip={nextSkip}. Body:\n{err}")
            );

            if (string.IsNullOrEmpty(body)) break;

            // API thường trả {"list":[ { course: {...}, ... }, ... ]} hoặc {"items":[...]}
            string arr = ExtractNamedArray(body, "list");
            if (string.IsNullOrEmpty(arr)) arr = ExtractNamedArray(body, "items");
            if (string.IsNullOrEmpty(arr)) break;

            var items = SplitTopLevelObjects(arr);
            if (items.Count == 0) break;

            foreach (var item in items)
            {
                string courseObj = ExtractNamedObject(item, "course");
                if (string.IsNullOrEmpty(courseObj)) continue;

                var data = ParseCourse(courseObj);
                if (data != null) _courses.Add(data);
            }

            if (items.Count < limitPerPage) break;
            nextSkip += limitPerPage;
            page++;
        }

        SpawnShelves(_courses, isMy:true);
    }

    // ================== RENDER ==================
    private const int BOOKS_PER_SHELF = 4;

    bool EnsureUI()
    {
        if (shelfPrefab == null)
        {
            Debug.LogError("[CourseList/MY] shelfPrefab (BookShelfUI) chưa được gán.");
            return false;
        }
        if (contentParent == null)
        {
            Debug.LogError("[CourseList/MY] contentParent (RectTransform) chưa được gán.");
            return false;
        }

        if (clearOldOnReload)
        {
            for (int i = contentParent.childCount - 1; i >= 0; i--)
                Destroy(contentParent.GetChild(i).gameObject);
        }
        return true;
    }

    void SpawnShelves(List<CourseData> list, bool isMy)
    {
        int shelfCount = Mathf.CeilToInt(list.Count / (float)BOOKS_PER_SHELF);
        for (int shelfIndex = 0; shelfIndex < shelfCount; shelfIndex++)
        {
            var shelf = Instantiate(shelfPrefab, contentParent);
            var rt = shelf.transform as RectTransform;
            if (rt != null)
            {
                rt.anchoredPosition = new Vector2(
                    firstShelfOffset.x,
                    firstShelfOffset.y - shelfIndex * shelfSpacingY
                );
                rt.localScale = Vector3.one;
            }

            int start = shelfIndex * BOOKS_PER_SHELF;
            int take  = Mathf.Min(BOOKS_PER_SHELF, list.Count - start);
            var slice = list.GetRange(start, take);

            ApplyDataToShelf(shelf, slice, isMyCourse:isMy);
        }
    }

    void ApplyDataToShelf(BookShelfUI shelf, List<CourseData> slice, bool isMyCourse)
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
            slot.book_sku  = data.sku ?? "";
            slot.book_seo  = data.seoUrl ?? "";

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

            if (isMyCourse && forceEnterForMyCourse)
            {
                slot.SetBuyCourse(false);
            }
            else
            {
                bool showBuy = (data.currentPrice ?? data.originalPrice) > 0;
                slot.SetBuyCourse(showBuy);
            }
        }
    }

    // ================== NETWORK ==================
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

    // --- JSON ---
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
        var m = Regex.Match(objJson, $"\"{Regex.Escape(field)}\"\\s*:\\s*\"([^\"]*)\"", RegexOptions.IgnoreCase);
        return m.Success ? m.Groups[1].Value : null;
    }

    string MatchNumberField(string objJson, string field)
    {
        var m = Regex.Match(objJson, $"\"{Regex.Escape(field)}\"\\s*:\\s*(-?\\d+(?:\\.\\d+)?)", RegexOptions.IgnoreCase);
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
