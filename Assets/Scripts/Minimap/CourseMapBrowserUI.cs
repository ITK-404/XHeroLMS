using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class CourseMapBrowserUI : MonoBehaviour
{
    [Header("API")]
    private string baseUrl;

    [Header("Auth")]
    [Tooltip("Để trống nếu dùng TokenStore.AccessToken")]
    public string overrideAccessToken = "";
    public bool useTokenFromStore = true;

    [Header("Query (/lms/courses)")]
    public int limitPerPage = 100;
    public string keyword = "";
    public string category = "";
    public string sortBy = "";
    public string order = "";

    [Header("UI - Tabs (Toggle)")]
    public FindCourseTypeOptionUI tabAll;
    public FindCourseTypeOptionUI tabBasic;
    public FindCourseTypeOptionUI tabAdvanced;
    public FindCourseTypeOptionUI tabIntensive;
    public FindCourseTypeOptionUI tabBusiness;

    [Header("UI - List")]
    public RectTransform contentParent;
    public MinimapCourseDisplayUI itemPrefab;
    public bool clearOldOnReload = true;
    public bool autoRunOnStart = true;

    [Header("Price format")]
    public bool useCurrentPriceFirst = true;
    public string priceFormat = "{0:#,0}'đ";

    public Action<CourseData> OnClickBuy;
    public Action<CourseData> OnClickFindWay;

    private readonly List<CourseData> _courses = new();
    private string _currentGroup = "all";

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
        public bool? isFree;
        public bool? needLogin;

        public string group;
        public List<string> groups;
    }

    private void Awake()
    {
        baseUrl = LmsStore.Instance.baseUrl;

        SetupTabs();
    }

    private void Start()
    {
        if (!autoRunOnStart) return;

        // đảm bảo có tab mặc định bật (nếu prefab đang tắt hết)
        EnsureDefaultTabOn();

        StartCoroutine(LoadCoursesThenRender());
    }

    // ===================== TAB SETUP =====================
    private void SetupTabs()
    {
        UnhookTab(tabAll);
        UnhookTab(tabBasic);
        UnhookTab(tabAdvanced);
        UnhookTab(tabIntensive);
        UnhookTab(tabBusiness);

        HookTab(tabAll, "all");
        HookTab(tabBasic, "basic");
        HookTab(tabAdvanced, "advanced");
        HookTab(tabIntensive, "intensive");
        HookTab(tabBusiness, "business");
    }

    private void HookTab(FindCourseTypeOptionUI tab, string groupKey)
    {
        if (tab == null || tab.Toggle == null) return;

        tab.Toggle.onValueChanged.AddListener(isOn =>
        {
            if (!isOn) return;                 // chỉ xử lý lúc bật
            SetExclusive(tab);                 // tắt các tab khác
            SelectGroup(groupKey);             // render theo group
        });
    }

    private void UnhookTab(FindCourseTypeOptionUI tab)
    {
        if (tab == null || tab.Toggle == null) return;
    }

    private void SetExclusive(FindCourseTypeOptionUI activeTab)
    {
        // dùng SetIsOnWithoutNotify để tránh trigger vòng lặp event
        SetToggleWithoutNotify(tabAll, activeTab == tabAll);
        SetToggleWithoutNotify(tabBasic, activeTab == tabBasic);
        SetToggleWithoutNotify(tabAdvanced, activeTab == tabAdvanced);
        SetToggleWithoutNotify(tabIntensive, activeTab == tabIntensive);
        SetToggleWithoutNotify(tabBusiness, activeTab == tabBusiness);

        // đảm bảo tab đang chọn vẫn ON (trường hợp activeTab null)
        if (activeTab != null && activeTab.Toggle != null)
            activeTab.Toggle.SetIsOnWithoutNotify(true);
    }

    private void SetToggleWithoutNotify(FindCourseTypeOptionUI tab, bool isOn)
    {
        if (tab == null || tab.Toggle == null) return;
        tab.Toggle.SetIsOnWithoutNotify(isOn);
    }

    private void EnsureDefaultTabOn()
    {
        // nếu có cái nào đang ON thì dùng cái đó
        if (IsOn(tabAll) || IsOn(tabBasic) || IsOn(tabAdvanced) || IsOn(tabIntensive) || IsOn(tabBusiness))
        {
            // sync group theo tab đang ON
            if (IsOn(tabAll)) _currentGroup = "all";
            else if (IsOn(tabBasic)) _currentGroup = "basic";
            else if (IsOn(tabAdvanced)) _currentGroup = "advanced";
            else if (IsOn(tabIntensive)) _currentGroup = "intensive";
            else if (IsOn(tabBusiness)) _currentGroup = "business";
            return;
        }

        // mặc định bật "Tất cả"
        if (tabAll != null && tabAll.Toggle != null)
        {
            tabAll.Toggle.isOn = true; // cái này sẽ trigger listener và render khi đã load
            _currentGroup = "all";
        }
        else
        {
            _currentGroup = "all";
        }
    }

    private bool IsOn(FindCourseTypeOptionUI tab)
    {
        return tab != null && tab.Toggle != null && tab.Toggle.isOn;
    }

    // ===================== Public API =====================
    public void SelectGroup(string groupKey)
    {
        _currentGroup = string.IsNullOrEmpty(groupKey) ? "all" : groupKey.ToLowerInvariant();
        RenderCurrentGroup();
    }

    public IEnumerator LoadCoursesThenRender()
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
                onErrorBody: err => Debug.LogError($"[CourseMap] GET failed page={page}, skip={nextSkip}. Body:\n{err}")
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

        RenderCurrentGroup();
    }

    // ===================== Render =====================
    private void RenderCurrentGroup()
    {
        if (contentParent == null || itemPrefab == null)
        {
            Debug.LogError("[CourseMap] Chưa gán contentParent hoặc itemPrefab.");
            return;
        }

        if (clearOldOnReload) ClearContent(contentParent);

        List<CourseData> list = (_currentGroup == "all")
            ? new List<CourseData>(_courses)
            : _courses.FindAll(c => MatchesGroup(c, _currentGroup));

        for (int i = 0; i < list.Count; i++)
        {
            var data = list[i];
            var item = Instantiate(itemPrefab, contentParent);
            BindItem(item, data);
        }
    }

    private void BindItem(MinimapCourseDisplayUI ui, CourseData data)
    {
        if (ui == null || data == null) return;

        // truyền meta giữ nguyên như bạn muốn
        ui.SetMeta(data.sku, data.seoUrl);

        ui.SetDisplayCourseName(string.IsNullOrEmpty(data.title) ? "(no title)" : data.title);

        float? displayPrice = useCurrentPriceFirst
            ? (data.currentPrice ?? data.originalPrice)
            : (data.originalPrice ?? data.currentPrice);

        string priceText = displayPrice.HasValue
            ? string.Format(System.Globalization.CultureInfo.InvariantCulture, priceFormat, displayPrice.Value)
            : "";

        bool joined = data.isJoined ?? false;
        bool isFree = data.isFree ?? false;
        bool owned = joined || isFree;

        if (!owned) ui.SetPriceText(priceText);
        ui.SetOwnedUI(owned);

        // Bind click giữ nguyên theo CourseMapBrowserUI
        // if (ui.FindWayBtn)
        // {
        //     ui.FindWayBtn.onClick.RemoveAllListeners();
        //     ui.FindWayBtn.onClick.AddListener(() => OnClickFindWay?.Invoke(data));
        // }
        //
        // if (ui.BuyCourseBtn)
        // {
        //     ui.BuyCourseBtn.onClick.RemoveAllListeners();
        //     ui.BuyCourseBtn.onClick.AddListener(() => OnClickBuy?.Invoke(data));
        // }
    }

    private void ClearContent(RectTransform content)
    {
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
    }

    private bool MatchesGroup(CourseData c, string desired)
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

    // ===================== Network =====================
    private IEnumerator GET(string url, string token, Action<string> onSuccess, Action<string> onErrorBody)
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

    private string BuildUrl(int skip, int limit)
    {
        var sb = new StringBuilder($"{baseUrl}/lms/courses?skip={skip}&limit={limit}");
        if (!string.IsNullOrEmpty(keyword)) sb.Append("&keyword=").Append(UnityWebRequest.EscapeURL(keyword));
        if (!string.IsNullOrEmpty(sortBy)) sb.Append("&sortBy=").Append(UnityWebRequest.EscapeURL(sortBy));
        if (!string.IsNullOrEmpty(order)) sb.Append("&order=").Append(UnityWebRequest.EscapeURL(order));
        if (!string.IsNullOrEmpty(category)) sb.Append("&category=").Append(UnityWebRequest.EscapeURL(category));
        return sb.ToString();
    }

    private string GetToken()
    {
        if (!string.IsNullOrWhiteSpace(overrideAccessToken))
            return NormalizeBearer(overrideAccessToken);

        if (useTokenFromStore && !string.IsNullOrWhiteSpace(TokenStore.AccessToken))
            return NormalizeBearer(TokenStore.AccessToken);

        return null;
    }

    private string NormalizeBearer(string raw)
    {
        var t = raw != null ? raw.Trim() : "";
        if (t.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            t = t.Substring("Bearer ".Length).Trim();
        return t;
    }

    // ===== Minimal JSON parse (giữ nguyên như bạn đang dùng) =====
    private string ExtractItemsArray(string raw)
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

    private int FindMatchingBracket(string s, int openIdx, char openCh, char closeCh)
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

    private int SkipString(string s, int startQuoteIdx)
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

    private List<string> SplitTopLevelObjects(string arrJson)
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

    private CourseData ParseCourse(string objJson)
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

    private float? TryParseFloat(string s)
    {
        if (string.IsNullOrEmpty(s)) return null;
        if (float.TryParse(s, System.Globalization.NumberStyles.Any,
            System.Globalization.CultureInfo.InvariantCulture, out var v))
            return v;
        return null;
    }

    private bool? TryParseBool(string s)
    {
        if (string.IsNullOrEmpty(s)) return null;
        if (bool.TryParse(s, out var b)) return b;
        return null;
    }

    private string MatchStringField(string objJson, string field)
    {
        var rx = new Regex("\"" + Regex.Escape(field) + "\"\\s*:\\s*\"([^\"]*)\"", RegexOptions.IgnoreCase);
        var m = rx.Match(objJson);
        return m.Success ? m.Groups[1].Value : null;
    }

    private string MatchNumberField(string objJson, string field)
    {
        var rx = new Regex("\"" + Regex.Escape(field) + "\"\\s*:\\s*(-?\\d+(?:\\.\\d+)?)", RegexOptions.IgnoreCase);
        var m = rx.Match(objJson);
        return m.Success ? m.Groups[1].Value : null;
    }

    private string MatchBoolField(string objJson, string field)
    {
        var rx = new Regex("\"" + Regex.Escape(field) + "\"\\s*:\\s*(true|false)", RegexOptions.IgnoreCase);
        var m = rx.Match(objJson);
        return m.Success ? m.Groups[1].Value.ToLowerInvariant() : null;
    }

    private string MatchNestedStringField(string objJson, string parent, string child)
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

    private string MatchNestedNumberField(string objJson, string parent, string child)
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

    private string MatchNestedBoolField(string objJson, string parent, string child)
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

    private List<string> MatchStringArrayField(string objJson, string field)
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
}
