using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;

public class CourseListPageAllUI : MonoBehaviour
{
    [Header("API")]
    private string baseUrl;

    [Header("Auth")]
    [Tooltip("Để trống nếu dùng TokenStore.AccessToken")]
    public string overrideAccessToken = "";
    public bool useTokenFromStore = true;

    [Header("Auto-run gate (optional)")]
    public bool runOnlyWhenKeyMatches = true;
    public string requiredKey = CourseMenuButtons.KEY_ALL;

    [Header("Query (/lms/courses)")]
    public int limitPerPage = 100;
    public string keyword = "";
    public string category = "";
    public string sortBy = "";
    public string order = "";

    [Header("UI – Kệ sách (global default)")]
    public BookShelfUI globalShelfPrefab;

    [Header("Price display")]
    public bool useCurrentPriceFirst = true;
    public string priceFormat = "{0:#.0}'đ";

    [Header("Layout (cho mỗi view)")]
    public float shelfSpacingY = 260f;
    public Vector2 firstShelfOffset = Vector2.zero;
    public bool clearOldOnReload = true;
    public bool autoRunOnStart = true;

    [Serializable]
    public class GroupView
    {
        public GameObject root;
        public RectTransform contentParent;
        public BookShelfUI shelfPrefabOverride;

        public GameObject emptyTextObj;
    }

    [Header("4 View tương ứng 4 group")]
    public GroupView basicView;
    public GroupView advancedView;
    public GroupView intensiveView;
    public GroupView businessView;

    private readonly List<CourseData> _courses = new List<CourseData>();
    private TabUI tabUI;
    private string _currentDesiredGroup = null;

    [Serializable]
    public class CourseData
    {
        public string id;
        public string title;
        public string sku;
        public string seoUrl;

        public float? originalPrice;
        public float? currentPrice;

        public bool? isJoined;

        public bool? isFree;    // coursePrice.isFree
        public bool? needLogin; // settings.needLogin

        public string group;
        public List<string> groups;
    }

    public bool defaultOpenBasic = true;

    private void Awake()
    {
        baseUrl = LmsStore.Instance.baseUrl;

        if (basicView?.emptyTextObj)     basicView.emptyTextObj.SetActive(false);
        if (advancedView?.emptyTextObj)  advancedView.emptyTextObj.SetActive(false);
        if (intensiveView?.emptyTextObj) intensiveView.emptyTextObj.SetActive(false);
        if (businessView?.emptyTextObj)  businessView.emptyTextObj.SetActive(false);

        ToggleAllRoots(false);

        if (defaultOpenBasic)
            _currentDesiredGroup = "basic";
    }

    private void Start()
    {
        tabUI = GetComponentInParent<TabUI>();
        if (!autoRunOnStart) return;

        bool ok = true;
        if (runOnlyWhenKeyMatches)
            ok = CourseMenuButtons.GetSavedKey() == requiredKey;

        if (ok) StartCoroutine(LoadAndSpawnAll());
    }

    public void RefreshForTab(CourseLessonTabID id)
    {
        _currentDesiredGroup = MapGroup(id);
        RenderAccordingToCurrentGroup();
    }

    public static string MapGroup(CourseLessonTabID id)
    {
        switch (id)
        {
            case CourseLessonTabID.CoBan:       return "basic";
            case CourseLessonTabID.NangCao:     return "advanced";
            case CourseLessonTabID.ChuyenSau:   return "intensive";
            case CourseLessonTabID.DoanhNghiep: return "business";
            default:                            return null;
        }
    }

    public IEnumerator LoadAndSpawnAll()
    {
        _courses.Clear();

        string token = GetToken();

        int nextSkip = 0;
        int page = 0;
        while (true)
        {
            string url = BuildUrl(nextSkip, limitPerPage);
            string body = null;

            yield return GET(url, token,
                onSuccess: s => body = s,
                onErrorBody: err => Debug.LogError($"[CourseList/ALL] GET failed page={page}, skip={nextSkip}. Body:\n{err}")
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

        if (string.IsNullOrEmpty(_currentDesiredGroup) && tabUI != null)
            _currentDesiredGroup = MapGroup(tabUI.tabID);

        RenderAccordingToCurrentGroup();
    }

    // ================== RENDER ==================
    private const int BOOKS_PER_SHELF = 4;

    void RenderAccordingToCurrentGroup()
    {
        if (!string.IsNullOrEmpty(_currentDesiredGroup))
        {
            ToggleAllRoots(false);

            var view = GetViewByGroup(_currentDesiredGroup);
            if (view == null || view.contentParent == null)
            {
                Debug.LogError($"[CourseList] View for group '{_currentDesiredGroup}' chưa gán contentParent.");
                return;
            }

            if (view.root != null) view.root.SetActive(true);

            if (clearOldOnReload)
                ClearContent(view.contentParent);

            // var list = _courses.FindAll(c => MatchesGroup(c, _currentDesiredGroup));
            var list = _courses.FindAll(c => MatchesGroup(c, _currentDesiredGroup));

            // Preview/Review mode: chỉ hiện khóa đã sở hữu
            if (IsPreviewMode())
                list = list.FindAll(IsOwned);


            bool isEmpty = (list == null || list.Count == 0);
            SetEmptyState(view, isEmpty);

            if (!isEmpty)
                SpawnShelvesInto(view, list);

            return;
        }

        RenderAllGroupsToTheirViews();
    }
    private bool IsPreviewMode()
    {
    #if UNITY_ANDROID || UNITY_IOS
        return AppDataGlobal.isInReviewMode;
    #else
        return false;
    #endif
    }

    private bool IsOwned(CourseData c)
    {
        if (c == null) return false;
        bool joined = c.isJoined ?? false;
        bool isFree = c.isFree ?? false;
        return joined || isFree;
    }

    void RenderAllGroupsToTheirViews()
    {
        RenderOneGroup("basic",     basicView);
        RenderOneGroup("advanced",  advancedView);
        RenderOneGroup("intensive", intensiveView);
        RenderOneGroup("business",  businessView);
    }

    void RenderOneGroup(string groupKey, GroupView view)
    {
        if (view == null || view.contentParent == null) return;

        if (view.root != null) view.root.SetActive(true);
        if (clearOldOnReload) ClearContent(view.contentParent);

        // var list = _courses.FindAll(c => MatchesGroup(c, groupKey));
        var list = _courses.FindAll(c => MatchesGroup(c, groupKey));

        // Preview/Review mode: chỉ hiện khóa đã sở hữu
        if (IsPreviewMode())
            list = list.FindAll(IsOwned);

        bool isEmpty = (list == null || list.Count == 0);
        SetEmptyState(view, isEmpty);

        if (!isEmpty)
            SpawnShelvesInto(view, list);
    }

    void ToggleAllRoots(bool active)
    {
        if (basicView != null && basicView.root != null) basicView.root.SetActive(active);
        if (advancedView != null && advancedView.root != null) advancedView.root.SetActive(active);
        if (intensiveView != null && intensiveView.root != null) intensiveView.root.SetActive(active);
        if (businessView != null && businessView.root != null) businessView.root.SetActive(active);
    }

    GroupView GetViewByGroup(string groupKey)
    {
        switch ((groupKey ?? "").ToLowerInvariant())
        {
            case "basic":     return basicView;
            case "advanced":  return advancedView;
            case "intensive": return intensiveView;
            case "business":  return businessView;
            default:          return null;
        }
    }

    void ClearContent(RectTransform content)
    {
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
    }

    void SpawnShelvesInto(GroupView view, List<CourseData> list)
    {
        if (view == null || view.contentParent == null) return;

        var shelfPrefab = view.shelfPrefabOverride != null ? view.shelfPrefabOverride : globalShelfPrefab;
        if (shelfPrefab == null)
        {
            Debug.LogError("[CourseList] Chưa gán shelfPrefab (global hoặc view override).");
            return;
        }

        int shelfCount = Mathf.CeilToInt(list.Count / (float)BOOKS_PER_SHELF);
        for (int shelfIndex = 0; shelfIndex < shelfCount; shelfIndex++)
        {
            var shelf = Instantiate(shelfPrefab, view.contentParent);
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
            int take = Mathf.Min(BOOKS_PER_SHELF, list.Count - start);
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
            slot.book_sku  = data.sku ?? "";
            slot.book_seo  = data.seoUrl ?? "";
            slot.RefreshBookCover();

            float? displayPrice = useCurrentPriceFirst ? (data.currentPrice ?? data.originalPrice)
                                                       : (data.originalPrice ?? data.currentPrice);

            if (slot.bookHandleUI != null)
            {
                if (slot.bookHandleUI.priceText != null)
                {
                    slot.bookHandleUI.priceText.text = displayPrice.HasValue
                        ? string.Format(System.Globalization.CultureInfo.InvariantCulture, priceFormat, displayPrice.Value)
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

            bool joined = data.isJoined ?? false;
            float priceVal = displayPrice ?? 0f;

            var viewUI = slot.bookHandleUI as BookViewUI;
            if (viewUI != null)
            {
                viewUI.ApplyCourseState(joined, priceVal);
            }
            else
            {
                // Fallback theo logic cũ: mua nếu price > 0
                bool showBuy = priceVal > 0;
                slot.SetBuyCourse(showBuy);
            }

            slot.OnRequestEnterCourse = HandleEnterCourseRequest;
        }
    }

    // ================== ENTER COURSE GATE ==================
    private void HandleEnterCourseRequest(BookHandler book)
    {
        if (book == null) return;
        StartCoroutine(CoHandleEnterCourse(book));
    }

    CourseData FindCourseDataForBook(BookHandler book)
    {
        if (book == null) return null;

        if (!string.IsNullOrEmpty(book.book_seo))
        {
            var d = _courses.Find(c =>
                !string.IsNullOrEmpty(c.seoUrl) &&
                string.Equals(c.seoUrl, book.book_seo, StringComparison.OrdinalIgnoreCase)
            );
            if (d != null) return d;
        }

        if (!string.IsNullOrEmpty(book.book_sku))
        {
            var d = _courses.Find(c =>
                !string.IsNullOrEmpty(c.sku) &&
                string.Equals(c.sku, book.book_sku, StringComparison.OrdinalIgnoreCase)
            );
            if (d != null) return d;
        }

        return null;
    }

    IEnumerator CoHandleEnterCourse(BookHandler book)
    {
        string token = GetToken();
        bool loggedIn = !string.IsNullOrWhiteSpace(token);

        CourseData data = FindCourseDataForBook(book);

        if (data == null)
        {
            Debug.LogWarning("[CourseGate] Không tìm thấy CourseData theo SEO/SKU -> fallback vào TryEnterCourse()");
            yield return book.TryEnterCourse();
            yield break;
        }

        // đã joined thì vào học luôn
        if (data.isJoined ?? false)
        {
            yield return book.TryEnterCourse();
            yield break;
        }

        bool needLogin = data.needLogin ?? false;
        bool isFree = data.isFree ?? false;

        if (!loggedIn)
        {
            if (needLogin) yield break;
            if (!isFree) yield break;

            yield return book.TryEnterCourse();
            yield break;
        }

        if (isFree)
        {
            if (string.IsNullOrEmpty(data.id))
            {
                yield return book.TryEnterCourse();
                yield break;
            }

            bool ok = false;
            yield return GrantFreeCourse(data.id, token, done => ok = done);

            if (!ok)
            {
                yield return book.TryEnterCourse();
                yield break;
            }
        }

        yield return book.TryEnterCourse();
    }

    IEnumerator GrantFreeCourse(string courseId, string token, Action<bool> onDone)
    {
        if (onDone == null) onDone = _ => { };

        token = NormalizeBearer(token);

        if (string.IsNullOrEmpty(courseId) || string.IsNullOrWhiteSpace(token))
        {
            onDone(false);
            yield break;
        }

        string url = $"{baseUrl}/users/lms/courses/{courseId}/free";

        using (var req = new UnityWebRequest(url, UnityWebRequest.kHttpVerbPOST))
        {
            req.uploadHandler = new UploadHandlerRaw(Array.Empty<byte>());
            req.downloadHandler = new DownloadHandlerBuffer();

            req.SetRequestHeader("Authorization", "Bearer " + token);
            req.SetRequestHeader("Accept", "application/json");
            req.SetRequestHeader("Content-Type", "application/json");

            string xData = LmsSecurityHeader.BuildXDataHeader();
            req.SetRequestHeader("x-data", xData);

            yield return req.SendWebRequest();

            long code = req.responseCode;
            bool ok = (code == 200 || code == 201 || code == 204 || code == 409);
            onDone(ok);
        }
    }

    // ================== NETWORK ==================
    IEnumerator GET(string url, string token, Action<string> onSuccess, Action<string> onErrorBody)
    {
        using (var req = UnityWebRequest.Get(url))
        {
            token = NormalizeBearer(token);

            if (!string.IsNullOrWhiteSpace(token))
                req.SetRequestHeader("Authorization", "Bearer " + token);

            req.SetRequestHeader("Accept", "application/json");

            string xData = LmsSecurityHeader.BuildXDataHeader();
            req.SetRequestHeader("x-data", xData);

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

    // ---------------- Minimal JSON parse ----------------
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
        int end = arrJson.LastIndexOf(']');
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

        bool? isFree = TryParseBool(MatchNestedBoolField(objJson, "coursePrice", "isFree"));
        bool? needLogin = TryParseBool(MatchNestedBoolField(objJson, "settings", "needLogin"));

        bool? isJoined = TryParseBool(MatchBoolField(objJson, "isJoined"));
        if (isJoined == null) isJoined = TryParseBool(MatchBoolField(objJson, "joined"));

        string group = MatchStringField(objJson, "group");
        var groups = MatchStringArrayField(objJson, "groups");

        return new CourseData
        {
            id = id,
            title = title,
            sku = sku,
            seoUrl = seoUrl,
            originalPrice = p1,
            currentPrice = p2,
            isJoined = isJoined,
            isFree = isFree,
            needLogin = needLogin,
            group = string.IsNullOrEmpty(group) ? null : group.Trim(),
            groups = groups
        };
    }

    float? TryParseFloat(string s)
    {
        if (string.IsNullOrEmpty(s)) return null;
        float v;
        if (float.TryParse(s, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out v))
            return v;
        return null;
    }

    bool? TryParseBool(string s)
    {
        if (string.IsNullOrEmpty(s)) return null;
        if (bool.TryParse(s, out var b)) return b;
        return null;
    }

    string MatchStringField(string objJson, string field)
    {
        var rx = new Regex("\"" + Regex.Escape(field) + "\"\\s*:\\s*\"([^\"]*)\"", RegexOptions.IgnoreCase);
        var m = rx.Match(objJson);
        return m.Success ? m.Groups[1].Value : null;
    }

    string MatchNumberField(string objJson, string field)
    {
        var rx = new Regex("\"" + Regex.Escape(field) + "\"\\s*:\\s*(-?\\d+(?:\\.\\d+)?)", RegexOptions.IgnoreCase);
        var m = rx.Match(objJson);
        return m.Success ? m.Groups[1].Value : null;
    }

    string MatchBoolField(string objJson, string field)
    {
        var rx = new Regex("\"" + Regex.Escape(field) + "\"\\s*:\\s*(true|false)", RegexOptions.IgnoreCase);
        var m = rx.Match(objJson);
        return m.Success ? m.Groups[1].Value.ToLowerInvariant() : null;
    }

    string MatchNestedStringField(string objJson, string parent, string child)
    {
        int pIdx = objJson.IndexOf("\"" + parent + "\"", StringComparison.OrdinalIgnoreCase);
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
        int pIdx = objJson.IndexOf("\"" + parent + "\"", StringComparison.OrdinalIgnoreCase);
        if (pIdx < 0) return null;

        int braceIdx = objJson.IndexOf('{', pIdx);
        if (braceIdx < 0) return null;

        int end = FindMatchingBracket(objJson, braceIdx, '{', '}');
        if (end <= braceIdx) return null;

        string sub = objJson.Substring(braceIdx, end - braceIdx + 1);
        return MatchNumberField(sub, child);
    }

    string MatchNestedBoolField(string objJson, string parent, string child)
    {
        int pIdx = objJson.IndexOf("\"" + parent + "\"", StringComparison.OrdinalIgnoreCase);
        if (pIdx < 0) return null;

        int braceIdx = objJson.IndexOf('{', pIdx);
        if (braceIdx < 0) return null;

        int end = FindMatchingBracket(objJson, braceIdx, '{', '}');
        if (end <= braceIdx) return null;

        string sub = objJson.Substring(braceIdx, end - braceIdx + 1);

        var rx = new Regex("\"" + Regex.Escape(child) + "\"\\s*:\\s*(true|false)", RegexOptions.IgnoreCase);
        var m = rx.Match(sub);
        return m.Success ? m.Groups[1].Value.ToLowerInvariant() : null;
    }

    List<string> MatchStringArrayField(string objJson, string field)
    {
        int fIdx = objJson.IndexOf("\"" + field + "\"", StringComparison.OrdinalIgnoreCase);
        if (fIdx < 0) return null;

        int arrStart = objJson.IndexOf('[', fIdx);
        if (arrStart < 0) return null;

        int arrEnd = FindMatchingBracket(objJson, arrStart, '[', ']');
        if (arrEnd <= arrStart) return null;

        string arr = objJson.Substring(arrStart + 1, arrEnd - arrStart - 1);
        var list = new List<string>();
        var rxItem = new Regex("\"([^\"]*)\"");
        foreach (Match m in rxItem.Matches(arr))
        {
            var s = m.Groups[1].Value != null ? m.Groups[1].Value.Trim() : null;
            if (!string.IsNullOrEmpty(s)) list.Add(s);
        }
        return list.Count > 0 ? list : null;
    }

    bool MatchesGroup(CourseData c, string desired)
    {
        if (string.IsNullOrEmpty(desired) || c == null) return true;

        if (!string.IsNullOrEmpty(c.group) &&
            string.Equals(c.group, desired, StringComparison.OrdinalIgnoreCase))
            return true;

        if (c.groups != null)
        {
            for (int i = 0; i < c.groups.Count; i++)
                if (string.Equals(c.groups[i], desired, StringComparison.OrdinalIgnoreCase))
                    return true;
        }
        return false;
    }
    void SetEmptyState(GroupView view, bool isEmpty)
    {
        if (view == null) return;
        if (view.emptyTextObj != null)
            view.emptyTextObj.SetActive(isEmpty);
    }
}
