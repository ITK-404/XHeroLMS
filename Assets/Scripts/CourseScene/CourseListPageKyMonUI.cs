using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class CourseListPageKyMonUI : MonoBehaviour
{
    [Header("API")]
    private string baseUrl;

    [Header("Auth")]
    [Tooltip("Để trống nếu dùng TokenStore.AccessToken")]
    public string overrideAccessToken = "";
    public bool useTokenFromStore = true;

    [Header("Auto-run gate (optional)")]
    public bool runOnlyWhenKeyMatches = false;
    public string requiredKey = CourseMenuButtons.KEY_ALL;

    [Header("Filter SEO")]
    [Tooltip("Chỉ lấy những khóa học có seo.url chứa chuỗi này.")]
    public string seoUrlMustContain = "ky-mon";

    public enum KyMonGroupingMode
    {
        UseApiGroups,
        ShowAllKyMonInEveryTab,
        ForceBySeoRules
    }

    [Serializable]
    public class ForcedGroupRule
    {
        [Tooltip("Chuỗi cần tìm trong SEO hoặc tiêu đề khóa học. Ví dụ: cau-tai-kinh-doanh")]
        public string seoOrTitleContains;

        [Tooltip("Tab mà khóa học sẽ bị cưỡng chế đưa vào.")]
        public CourseLessonTabID targetTab = CourseLessonTabID.CoBan;
    }

    [Header("Cưỡng chế phân nhóm Kỳ Môn tại Unity")]
    [Tooltip("ShowAllKyMonInEveryTab: toàn bộ khóa Kỳ Môn xuất hiện ở cả 4 tab.\n" +
             "ForceBySeoRules: ép từng khóa vào tab theo danh sách rule bên dưới.\n" +
             "UseApiGroups: dùng group/groups do API trả về như cũ.")]
    public KyMonGroupingMode kyMonGroupingMode = KyMonGroupingMode.ShowAllKyMonInEveryTab;

    [Tooltip("Chỉ dùng khi mode = ForceBySeoRules.")]
    public List<ForcedGroupRule> forcedGroupRules = new List<ForcedGroupRule>();

    [Tooltip("Khi mode = ForceBySeoRules nhưng khóa không khớp rule nào, có dùng group từ API làm dự phòng hay không.")]
    public bool fallbackToApiGroupWhenNoForcedRule = false;

    [Header("Default group")]
    [Tooltip("Để TRỐNG (mặc định) để tự động hiển thị đủ cả 4 nhóm: basic, advanced, intensive, business.\n" +
             "Chỉ điền tên 1 group vào đây nếu muốn trang này LUÔN cố định mở riêng group đó (ẩn 3 group còn lại).")]
    public string defaultOpenGroup = "";

    [Header("Quyền điều khiển hiển thị tab")]
    [Tooltip("Bật: TabUI/hệ thống tab hiện có tự bật tắt các root. Script này chỉ nạp dữ liệu vào content.")]
    public bool letTabSystemControlRoots = true;

    [Header("Tự liên kết đúng hierarchy Tab đang hiển thị")]
    [Tooltip("Tự tìm TabItemManagerUI và 4 TabUI thật trong scene, sau đó ghi đè các reference root/contentParent đang kéo sai trong Inspector.")]
    public bool autoBindLiveTabViews = true;

    [Tooltip("Sau khi cửa sổ khóa học được bật, render lại một lần vào hierarchy đang active để loại bỏ dữ liệu Đại Đạo Chí Giản cũ.")]
    public bool renderAgainWhenTabPanelBecomesVisible = true;

    [Tooltip("Tên GameObject content dùng làm fallback nếu TabUI không có ScrollRect.")]
    public string fallbackContentObjectName = "Content";

    [Header("Debug")]
    public bool debugLogSeoCheck = true;

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
    // public string priceFormat = "{0:#.0}'đ";
    string priceFormat = "{0:N0}đ";

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

    private bool _hasLoadedCourses;
    private bool _renderedWhenVisible;
    private float _nextLiveHierarchyCheck;

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

    private const int BOOKS_PER_SHELF = 4;

    private void Awake()
    {
        baseUrl = LmsStore.Instance.baseUrl;

        if (basicView?.emptyTextObj) basicView.emptyTextObj.SetActive(false);
        if (advancedView?.emptyTextObj) advancedView.emptyTextObj.SetActive(false);
        if (intensiveView?.emptyTextObj) intensiveView.emptyTextObj.SetActive(false);
        if (businessView?.emptyTextObj) businessView.emptyTextObj.SetActive(false);

        // Không tự tắt các panel khi TabUI đang quản lý việc chuyển tab.
        if (!letTabSystemControlRoots)
            ToggleAllRoots(false);

        if (!string.IsNullOrWhiteSpace(defaultOpenGroup))
            _currentDesiredGroup = defaultOpenGroup.Trim().ToLowerInvariant();
    }

    private void Start()
    {
        tabUI = GetComponentInParent<TabUI>();

        if (autoBindLiveTabViews)
            AutoBindLiveTabViews(logResult: true);

        if (!autoRunOnStart)
            return;

        bool ok = true;

        if (runOnlyWhenKeyMatches)
            ok = CourseMenuButtons.GetSavedKey() == requiredKey;

        if (ok)
            StartCoroutine(LoadAndSpawnAll());
    }

    private void Update()
    {
        if (!autoBindLiveTabViews ||
            !renderAgainWhenTabPanelBecomesVisible ||
            !_hasLoadedCourses ||
            _renderedWhenVisible)
        {
            return;
        }

        if (Time.unscaledTime < _nextLiveHierarchyCheck)
            return;

        _nextLiveHierarchyCheck = Time.unscaledTime + 0.25f;

        // UI khóa học được bật sau khi dữ liệu đã tải xong.
        // Khi một TabUI thật active, bind lại và render vào đúng hierarchy đó.
        AutoBindLiveTabViews(logResult: false);

        if (!AnyBoundTabRootActive())
            return;

        Debug.Log("[CourseList/KY-MON] Live tab hierarchy is now visible -> rebind and replace old shelf data.");

        RenderAllGroupsToTheirViews();
        _renderedWhenVisible = true;
    }

    public void RefreshForTab(CourseLessonTabID id)
    {
        if (autoBindLiveTabViews)
            AutoBindLiveTabViews(logResult: false);

        _currentDesiredGroup = MapGroup(id);
        RenderOneGroup(_currentDesiredGroup, GetViewByGroup(_currentDesiredGroup));
    }

    public static string MapGroup(CourseLessonTabID id)
    {
        switch (id)
        {
            case CourseLessonTabID.CoBan:
                return "basic";

            case CourseLessonTabID.NangCao:
                return "advanced";

            case CourseLessonTabID.ChuyenSau:
                return "intensive";

            case CourseLessonTabID.DoanhNghiep:
                return "business";

            default:
                return null;
        }
    }

    public IEnumerator LoadAndSpawnAll()
    {
        _courses.Clear();

        string token = GetToken();

        int nextSkip = 0;
        int page = 0;
        int totalObjects = 0;
        int totalMatched = 0;

        while (true)
        {
            string url = BuildUrl(nextSkip, limitPerPage);
            string body = null;

            yield return GET(
                url,
                token,
                onSuccess: s => body = s,
                onErrorBody: err => Debug.LogError($"[CourseList/KY-MON] GET failed page={page}, skip={nextSkip}. Body:\n{err}")
            );

            if (string.IsNullOrEmpty(body))
            {
                Debug.LogWarning($"[CourseList/KY-MON] Empty response page={page}, skip={nextSkip}");
                break;
            }

            string arr = ExtractItemsArray(body);
            var objects = SplitTopLevelObjects(arr);

            Debug.Log($"[CourseList/KY-MON] page={page}, skip={nextSkip}, objects={objects.Count}");

            if (objects.Count == 0)
                break;

            totalObjects += objects.Count;

            foreach (var obj in objects)
            {
                var data = ParseCourse(obj);

                if (data == null)
                    continue;

                bool matched = IsSeoUrlMatch(data.seoUrl);

                if (debugLogSeoCheck)
                {
                    Debug.Log(
                        $"[CourseList/KY-MON] Check SEO='{data.seoUrl}' | title='{data.title}' | group='{data.group}' | matched={matched}"
                    );
                }

                if (matched)
                {
                    _courses.Add(data);
                    totalMatched++;
                }
            }

            if (objects.Count < limitPerPage)
                break;

            nextSkip += limitPerPage;
            page++;
        }

        Debug.Log(
            $"[CourseList/KY-MON] Done. TotalObjects={totalObjects}, Matched={totalMatched}, Filter='{seoUrlMustContain}', CurrentGroup='{_currentDesiredGroup}', FORCE_ALL_TABS=True, TabControlsRoots={letTabSystemControlRoots}, ReviewFilter=False"
        );

        if (string.IsNullOrEmpty(_currentDesiredGroup) && tabUI != null)
            _currentDesiredGroup = MapGroup(tabUI.tabID);

        if (string.IsNullOrEmpty(_currentDesiredGroup) && !string.IsNullOrWhiteSpace(defaultOpenGroup))
            _currentDesiredGroup = defaultOpenGroup.Trim().ToLowerInvariant();

        _hasLoadedCourses = true;

        // Quan trọng: trước khi render phải lấy lại đúng Content nằm trong 4 TabUI thật.
        // Không dùng mù các reference Content đã kéo tay trong Inspector.
        if (autoBindLiveTabViews)
            AutoBindLiveTabViews(logResult: true);

        // Luôn spawn dữ liệu vào đủ 4 content. TabUI chỉ chịu trách nhiệm bật/tắt panel.
        RenderAllGroupsToTheirViews();

        // Nếu panel đã visible ngay lúc này thì xác nhận luôn.
        _renderedWhenVisible = AnyBoundTabRootActive();
    }

    private bool IsSeoUrlMatch(string seoUrl)
    {
        if (string.IsNullOrWhiteSpace(seoUrl))
            return false;

        if (string.IsNullOrWhiteSpace(seoUrlMustContain))
            return true;

        string url = seoUrl.Trim();
        string key = seoUrlMustContain.Trim();

        return url.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    // ================== AUTO-BIND LIVE TAB HIERARCHY ==================

    private bool AutoBindLiveTabViews(bool logResult)
    {
        TabUI[] tabs = FindBestTabSet();

        if (tabs == null || tabs.Length == 0)
        {
            if (logResult)
                Debug.LogError("[CourseList/KY-MON] Không tìm thấy TabUI thật để auto-bind.");

            return false;
        }

        bool b1 = BindGroupViewToTab(ref basicView, tabs, CourseLessonTabID.CoBan, "basic", logResult);
        bool b2 = BindGroupViewToTab(ref advancedView, tabs, CourseLessonTabID.NangCao, "advanced", logResult);
        bool b3 = BindGroupViewToTab(ref intensiveView, tabs, CourseLessonTabID.ChuyenSau, "intensive", logResult);
        bool b4 = BindGroupViewToTab(ref businessView, tabs, CourseLessonTabID.DoanhNghiep, "business", logResult);

        return b1 || b2 || b3 || b4;
    }

    private TabUI[] FindBestTabSet()
    {
        TabItemManagerUI[] managers = FindObjectsOfType<TabItemManagerUI>(true);

        TabItemManagerUI bestManager = null;
        int bestScore = -1;

        for (int i = 0; i < managers.Length; i++)
        {
            TabItemManagerUI manager = managers[i];

            if (manager == null)
                continue;

            TabUI[] candidates = manager.GetComponentsInChildren<TabUI>(true);
            int score = CountKyMonTabs(candidates);

            // Ưu tiên manager nằm cùng nhánh hierarchy với component này.
            if (transform.IsChildOf(manager.transform))
                score += 100;

            if (score > bestScore)
            {
                bestScore = score;
                bestManager = manager;
            }
        }

        if (bestManager != null)
            return bestManager.GetComponentsInChildren<TabUI>(true);

        // Fallback khi TabItemManagerUI không phải parent trực tiếp.
        return FindObjectsOfType<TabUI>(true);
    }

    private int CountKyMonTabs(TabUI[] tabs)
    {
        if (tabs == null)
            return 0;

        bool basic = false;
        bool advanced = false;
        bool intensive = false;
        bool business = false;

        for (int i = 0; i < tabs.Length; i++)
        {
            TabUI tab = tabs[i];

            if (tab == null)
                continue;

            switch (tab.tabID)
            {
                case CourseLessonTabID.CoBan:
                    basic = true;
                    break;

                case CourseLessonTabID.NangCao:
                    advanced = true;
                    break;

                case CourseLessonTabID.ChuyenSau:
                    intensive = true;
                    break;

                case CourseLessonTabID.DoanhNghiep:
                    business = true;
                    break;
            }
        }

        int count = 0;
        if (basic) count++;
        if (advanced) count++;
        if (intensive) count++;
        if (business) count++;
        return count;
    }

    private bool BindGroupViewToTab(
        ref GroupView view,
        TabUI[] tabs,
        CourseLessonTabID targetId,
        string groupKey,
        bool logResult)
    {
        TabUI targetTab = null;

        for (int i = 0; i < tabs.Length; i++)
        {
            if (tabs[i] != null && tabs[i].tabID == targetId)
            {
                targetTab = tabs[i];
                break;
            }
        }

        if (targetTab == null)
        {
            if (logResult)
                Debug.LogError($"[CourseList/KY-MON] Không tìm thấy TabUI có tabID='{targetId}'.");

            return false;
        }

        RectTransform liveContent = FindLiveContent(targetTab);

        if (liveContent == null)
        {
            if (logResult)
            {
                Debug.LogError(
                    $"[CourseList/KY-MON] Tìm thấy TabUI '{targetId}' nhưng không tìm thấy ScrollRect.content/" +
                    $"GameObject '{fallbackContentObjectName}'. Root={GetHierarchyPath(targetTab.transform)}"
                );
            }

            return false;
        }

        if (view == null)
            view = new GroupView();

        // Giữ lại shelfPrefabOverride và emptyTextObj nếu đã cấu hình,
        // nhưng bắt buộc ghi đè root/contentParent sang hierarchy thật.
        view.root = targetTab.gameObject;
        view.contentParent = liveContent;

        if (logResult)
        {
            Debug.Log(
                $"[CourseList/KY-MON] LIVE BIND group='{groupKey}' tabID='{targetId}'" +
                $" | root='{GetHierarchyPath(targetTab.transform)}'" +
                $" | content='{GetHierarchyPath(liveContent)}'" +
                $" | rootActive={targetTab.gameObject.activeInHierarchy}" +
                $" | contentActive={liveContent.gameObject.activeInHierarchy}"
            );
        }

        return true;
    }

    private RectTransform FindLiveContent(TabUI targetTab)
    {
        if (targetTab == null)
            return null;

        ScrollRect[] scrollRects = targetTab.GetComponentsInChildren<ScrollRect>(true);

        for (int i = 0; i < scrollRects.Length; i++)
        {
            if (scrollRects[i] != null && scrollRects[i].content != null)
                return scrollRects[i].content;
        }

        RectTransform[] rects = targetTab.GetComponentsInChildren<RectTransform>(true);

        for (int i = 0; i < rects.Length; i++)
        {
            RectTransform rect = rects[i];

            if (rect == null)
                continue;

            if (string.Equals(
                rect.gameObject.name,
                fallbackContentObjectName,
                StringComparison.OrdinalIgnoreCase))
            {
                return rect;
            }
        }

        return null;
    }

    private bool AnyBoundTabRootActive()
    {
        return IsViewRootActive(basicView) ||
               IsViewRootActive(advancedView) ||
               IsViewRootActive(intensiveView) ||
               IsViewRootActive(businessView);
    }

    private bool IsViewRootActive(GroupView view)
    {
        return view != null &&
               view.root != null &&
               view.root.activeInHierarchy;
    }

    private string GetHierarchyPath(Transform target)
    {
        if (target == null)
            return "(null)";

        string path = target.name;
        Transform parent = target.parent;

        while (parent != null)
        {
            path = parent.name + "/" + path;
            parent = parent.parent;
        }

        return path;
    }

    // ================== RENDER ==================
    // _currentDesiredGroup RỖNG  -> render ĐỦ CẢ 4 NHÓM (basic..business), mỗi nhóm vào đúng view của nó.
    // _currentDesiredGroup CÓ GIÁ TRỊ (do RefreshForTab() hoặc defaultOpenGroup gán) -> chỉ render 1 view đó, ẩn 3 view còn lại.
    // => Muốn mặc định hiển thị đủ 4 nhóm thì defaultOpenGroup PHẢI để trống, không được hard-code tên 1 group.
    [ContextMenu("RenderAccordingToCurrentGroup")]
    void RenderAccordingToCurrentGroup()
    {
        if (letTabSystemControlRoots)
        {
            RenderAllGroupsToTheirViews();
            return;
        }

        if (!string.IsNullOrEmpty(_currentDesiredGroup))
        {
            ToggleAllRoots(false);

            var view = GetViewByGroup(_currentDesiredGroup);
            if (view == null || view.contentParent == null)
            {
                Debug.LogError($"[CourseList/KY-MON] View for group '{_currentDesiredGroup}' chưa gán contentParent.");
                return;
            }

            if (view.root != null)
                view.root.SetActive(true);

            if (clearOldOnReload)
                ClearContent(view.contentParent);

            var list = _courses.FindAll(c => MatchesGroup(c, _currentDesiredGroup));

            // Cưỡng chế Kỳ Môn: không lọc theo isJoined/isFree hoặc review mode.

            bool isEmpty = list == null || list.Count == 0;

            Debug.Log(
                $"[CourseList/KY-MON] Render group='{_currentDesiredGroup}', listCount={(list == null ? 0 : list.Count)}, isEmpty={isEmpty}"
            );

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
        if (c == null)
            return false;

        bool joined = c.isJoined ?? false;
        bool isFree = c.isFree ?? false;

        return joined || isFree;
    }

    void RenderAllGroupsToTheirViews()
    {
        RenderOneGroup("basic", basicView);
        RenderOneGroup("advanced", advancedView);
        RenderOneGroup("intensive", intensiveView);
        RenderOneGroup("business", businessView);
    }

    void RenderOneGroup(string groupKey, GroupView view)
    {
        if (view == null || view.contentParent == null)
            return;

        if (!letTabSystemControlRoots && view.root != null)
            view.root.SetActive(true);

        if (clearOldOnReload)
            ClearContent(view.contentParent);

        var list = _courses.FindAll(c => MatchesGroup(c, groupKey));

        // Cưỡng chế Kỳ Môn: không lọc theo isJoined/isFree hoặc review mode.

        bool isEmpty = list == null || list.Count == 0;

        Debug.Log(
            $"[CourseList/KY-MON] RenderOneGroup group='{groupKey}', listCount={(list == null ? 0 : list.Count)}, isEmpty={isEmpty}"
        );

        SetEmptyState(view, isEmpty);

        if (!isEmpty)
            SpawnShelvesInto(view, list);
    }

    void ToggleAllRoots(bool active)
    {
        if (basicView != null && basicView.root != null)
            basicView.root.SetActive(active);

        if (advancedView != null && advancedView.root != null)
            advancedView.root.SetActive(active);

        if (intensiveView != null && intensiveView.root != null)
            intensiveView.root.SetActive(active);

        if (businessView != null && businessView.root != null)
            businessView.root.SetActive(active);
    }

    GroupView GetViewByGroup(string groupKey)
    {
        switch ((groupKey ?? "").ToLowerInvariant())
        {
            case "basic":
                return basicView;

            case "advanced":
                return advancedView;

            case "intensive":
                return intensiveView;

            case "business":
                return businessView;

            default:
                return null;
        }
    }

    void ClearContent(RectTransform content)
    {
        if (content == null)
            return;

        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
    }

    void SpawnShelvesInto(GroupView view, List<CourseData> list)
    {
        if (view == null || view.contentParent == null)
            return;

        var shelfPrefab = view.shelfPrefabOverride != null
            ? view.shelfPrefabOverride
            : globalShelfPrefab;

        if (shelfPrefab == null)
        {
            Debug.LogError("[CourseList/KY-MON] Chưa gán shelfPrefab.");
            return;
        }

        int shelfCount = Mathf.CeilToInt(list.Count / (float)BOOKS_PER_SHELF);

        Debug.Log(
            $"[CourseList/KY-MON] Spawn start | content='{GetHierarchyPath(view.contentParent)}'" +
            $" | activeSelf={view.contentParent.gameObject.activeSelf}" +
            $" | activeInHierarchy={view.contentParent.gameObject.activeInHierarchy}" +
            $" | courses={list.Count} | shelves={shelfCount}" +
            $" | prefab='{shelfPrefab.name}'"
        );

        for (int shelfIndex = 0; shelfIndex < shelfCount; shelfIndex++)
        {
            var shelf = Instantiate(shelfPrefab, view.contentParent);
            shelf.gameObject.SetActive(true);

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

            Debug.Log(
                $"[CourseList/KY-MON] Spawned shelf {shelfIndex + 1}/{shelfCount}" +
                $" | object='{shelf.name}' | books={slice.Count}" +
                $" | activeInHierarchy={shelf.gameObject.activeInHierarchy}"
            );
        }
    }

    void ApplyDataToShelf(BookShelfUI shelf, List<CourseData> slice)
    {
        if (shelf == null)
            return;

        if (shelf.books == null || shelf.books.Length == 0)
            shelf.books = shelf.GetComponentsInChildren<BookHandler>(true);

        for (int i = 0; i < BOOKS_PER_SHELF; i++)
        {
            bool hasData = i < slice.Count;
            var slot = i < shelf.books.Length ? shelf.books[i] : null;

            if (slot == null)
                continue;

            slot.gameObject.SetActive(hasData);

            if (!hasData)
                continue;

            var data = slice[i];

            slot.book_name = data.title ?? "(no title)";
            slot.book_sku = data.sku ?? "";
            slot.book_seo = data.seoUrl ?? "";
            slot.course_id = data.id;

            slot.RefreshBookCover();

            float? displayPrice = useCurrentPriceFirst
                ? data.currentPrice ?? data.originalPrice
                : data.originalPrice ?? data.currentPrice;

            bool isReview = IsPreviewMode();

            if (slot.bookHandleUI != null)
            {
                if (isReview)
                {
                    if (slot.bookHandleUI.priceText != null)
                        slot.bookHandleUI.priceText.text = " ";

                    if (slot.bookHandleUI.fullPriceText != null)
                        slot.bookHandleUI.fullPriceText.text = " ";
                }
                else
                {
                    if (slot.bookHandleUI.priceText != null)
                    {
                        slot.bookHandleUI.priceText.text = displayPrice.HasValue
                            ? string.Format(System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), priceFormat, displayPrice.Value)
                            : "";
                    }

                    if (slot.bookHandleUI.fullPriceText != null)
                    {
                        slot.bookHandleUI.fullPriceText.text = data.originalPrice.HasValue
                            ? string.Format(System.Globalization.CultureInfo.GetCultureInfo("vi-VN"), priceFormat, data.originalPrice.Value)
                            : "";
                    }

                    slot.bookHandleUI.RefreshColor();
                }
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
                bool showBuy = priceVal > 0;
                slot.SetBuyCourse(showBuy);
            }

            slot.OnRequestEnterCourse = HandleEnterCourseRequest;
        }
    }

    // ================== ENTER COURSE GATE ==================

    private void HandleEnterCourseRequest(BookHandler book)
    {
        if (book == null)
            return;

        StartCoroutine(CoHandleEnterCourse(book));
    }

    CourseData FindCourseDataForBook(BookHandler book)
    {
        if (book == null)
            return null;

        if (!string.IsNullOrEmpty(book.book_seo))
        {
            var d = _courses.Find(c =>
                !string.IsNullOrEmpty(c.seoUrl) &&
                string.Equals(c.seoUrl, book.book_seo, StringComparison.OrdinalIgnoreCase)
            );

            if (d != null)
                return d;
        }

        if (!string.IsNullOrEmpty(book.book_sku))
        {
            var d = _courses.Find(c =>
                !string.IsNullOrEmpty(c.sku) &&
                string.Equals(c.sku, book.book_sku, StringComparison.OrdinalIgnoreCase)
            );

            if (d != null)
                return d;
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
            Debug.LogWarning("[CourseGate/KY-MON] Không tìm thấy CourseData theo SEO/SKU -> fallback vào TryEnterCourse()");
            yield return book.TryEnterCourse();
            yield break;
        }

        if (data.isJoined ?? false)
        {
            yield return book.TryEnterCourse();
            yield break;
        }

        bool needLogin = data.needLogin ?? false;
        bool isFree = data.isFree ?? false;

        if (!loggedIn)
        {
            if (needLogin)
                yield break;

            if (!isFree)
                yield break;

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
        if (onDone == null)
            onDone = _ => { };

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
            bool ok = code == 200 || code == 201 || code == 204 || code == 409;

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
            bool error =
                req.result == UnityWebRequest.Result.ConnectionError ||
                req.result == UnityWebRequest.Result.ProtocolError;
#else
            bool error = req.isNetworkError || req.isHttpError;
#endif

            string body = req.downloadHandler.text;

            if (error)
                onErrorBody?.Invoke(body);
            else
                onSuccess?.Invoke(body);
        }
    }

    string BuildUrl(int skip, int limit)
    {
        var sb = new StringBuilder($"{baseUrl}/lms/courses?skip={skip}&limit={limit}");

        if (!string.IsNullOrEmpty(keyword))
            sb.Append("&keyword=").Append(UnityWebRequest.EscapeURL(keyword));

        if (!string.IsNullOrEmpty(sortBy))
            sb.Append("&sortBy=").Append(UnityWebRequest.EscapeURL(sortBy));

        if (!string.IsNullOrEmpty(order))
            sb.Append("&order=").Append(UnityWebRequest.EscapeURL(order));

        if (!string.IsNullOrEmpty(category))
            sb.Append("&category=").Append(UnityWebRequest.EscapeURL(category));

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

    // ================== JSON PARSE ==================

    string ExtractItemsArray(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return "[]";

        // API kiểu cũ: { "items": [...] }
        string byItems = ExtractArrayByField(raw, "items");
        if (LooksLikeCourseArray(byItems))
            return byItems;

        // API hiện tại của master:
        // {
        //   "status": true,
        //   "data": {
        //      "data": [ course... ]
        //   }
        // }
        string byData = ExtractArrayByField(raw, "data");
        if (LooksLikeCourseArray(byData))
            return byData;

        // Fallback chắc ăn:
        // quét tất cả array trong JSON và chọn array nào giống danh sách course.
        for (int i = 0; i < raw.Length; i++)
        {
            if (raw[i] == '"')
            {
                i = SkipString(raw, i);
                continue;
            }

            if (raw[i] != '[')
                continue;

            int end = FindMatchingBracket(raw, i, '[', ']');
            if (end <= i)
                continue;

            string candidate = raw.Substring(i, end - i + 1);

            if (LooksLikeCourseArray(candidate))
                return candidate;

            i = end;
        }

        return "[]";
    }

    string ExtractArrayByField(string raw, string field)
    {
        if (string.IsNullOrEmpty(raw) || string.IsNullOrEmpty(field))
            return "[]";

        string pattern = "\"" + field + "\"";
        int idx = raw.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);

        while (idx >= 0)
        {
            int colon = raw.IndexOf(':', idx);
            if (colon < 0)
                return "[]";

            int nextArray = -1;
            int nextObject = -1;

            for (int i = colon + 1; i < raw.Length; i++)
            {
                if (char.IsWhiteSpace(raw[i]))
                    continue;

                if (raw[i] == '[')
                {
                    nextArray = i;
                    break;
                }

                if (raw[i] == '{')
                {
                    nextObject = i;
                    break;
                }

                break;
            }

            // Chỉ lấy field có value là array trực tiếp.
            // Ví dụ: "items": [...]
            // Hoặc inner: "data": [...]
            if (nextArray >= 0 && (nextObject < 0 || nextArray < nextObject))
            {
                int end = FindMatchingBracket(raw, nextArray, '[', ']');
                if (end > nextArray)
                    return raw.Substring(nextArray, end - nextArray + 1);
            }

            idx = raw.IndexOf(pattern, idx + pattern.Length, StringComparison.OrdinalIgnoreCase);
        }

        return "[]";
    }

    bool LooksLikeCourseArray(string arrJson)
    {
        if (string.IsNullOrEmpty(arrJson) || arrJson == "[]")
            return false;

        return arrJson.IndexOf("\"_id\"", StringComparison.OrdinalIgnoreCase) >= 0 &&
               arrJson.IndexOf("\"seo\"", StringComparison.OrdinalIgnoreCase) >= 0 &&
               arrJson.IndexOf("\"url\"", StringComparison.OrdinalIgnoreCase) >= 0;
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
                    list.Add(arrJson.Substring(i, objEnd - i + 1));
                    i = objEnd + 1;
                }
                else
                {
                    break;
                }
            }
            else
            {
                i++;
            }
        }

        return list;
    }

    CourseData ParseCourse(string objJson)
    {
        if (string.IsNullOrEmpty(objJson))
            return null;

        string id = MatchStringField(objJson, "_id");
        if (string.IsNullOrEmpty(id))
            return null;

        string title = MatchStringField(objJson, "title");
        string sku = MatchStringField(objJson, "sku");
        string seoUrl = MatchNestedStringField(objJson, "seo", "url");

        float? p1 = TryParseFloat(MatchNestedNumberField(objJson, "coursePrice", "originalPrice"));
        float? p2 = TryParseFloat(MatchNestedNumberField(objJson, "coursePrice", "currentPrice"));

        bool? isFree = TryParseBool(MatchNestedBoolField(objJson, "coursePrice", "isFree"));
        bool? needLogin = TryParseBool(MatchNestedBoolField(objJson, "settings", "needLogin"));

        bool? isJoined = TryParseBool(MatchBoolField(objJson, "isJoined"));
        if (isJoined == null)
            isJoined = TryParseBool(MatchBoolField(objJson, "joined"));

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
        if (string.IsNullOrEmpty(s))
            return null;

        float v;

        if (float.TryParse(
                s,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out v))
        {
            return v;
        }

        return null;
    }

    bool? TryParseBool(string s)
    {
        if (string.IsNullOrEmpty(s))
            return null;

        bool b;

        if (bool.TryParse(s, out b))
            return b;

        return null;
    }

    string MatchStringField(string objJson, string field)
    {
        var rx = new Regex(
            "\"" + Regex.Escape(field) + "\"\\s*:\\s*\"([^\"]*)\"",
            RegexOptions.IgnoreCase
        );

        var m = rx.Match(objJson);
        return m.Success ? m.Groups[1].Value : null;
    }

    string MatchNumberField(string objJson, string field)
    {
        var rx = new Regex(
            "\"" + Regex.Escape(field) + "\"\\s*:\\s*(-?\\d+(?:\\.\\d+)?)",
            RegexOptions.IgnoreCase
        );

        var m = rx.Match(objJson);
        return m.Success ? m.Groups[1].Value : null;
    }

    string MatchBoolField(string objJson, string field)
    {
        var rx = new Regex(
            "\"" + Regex.Escape(field) + "\"\\s*:\\s*(true|false)",
            RegexOptions.IgnoreCase
        );

        var m = rx.Match(objJson);
        return m.Success ? m.Groups[1].Value.ToLowerInvariant() : null;
    }

    string MatchNestedStringField(string objJson, string parent, string child)
    {
        int pIdx = objJson.IndexOf("\"" + parent + "\"", StringComparison.OrdinalIgnoreCase);
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

    string MatchNestedNumberField(string objJson, string parent, string child)
    {
        int pIdx = objJson.IndexOf("\"" + parent + "\"", StringComparison.OrdinalIgnoreCase);
        if (pIdx < 0)
            return null;

        int braceIdx = objJson.IndexOf('{', pIdx);
        if (braceIdx < 0)
            return null;

        int end = FindMatchingBracket(objJson, braceIdx, '{', '}');
        if (end <= braceIdx)
            return null;

        string sub = objJson.Substring(braceIdx, end - braceIdx + 1);
        return MatchNumberField(sub, child);
    }

    string MatchNestedBoolField(string objJson, string parent, string child)
    {
        int pIdx = objJson.IndexOf("\"" + parent + "\"", StringComparison.OrdinalIgnoreCase);
        if (pIdx < 0)
            return null;

        int braceIdx = objJson.IndexOf('{', pIdx);
        if (braceIdx < 0)
            return null;

        int end = FindMatchingBracket(objJson, braceIdx, '{', '}');
        if (end <= braceIdx)
            return null;

        string sub = objJson.Substring(braceIdx, end - braceIdx + 1);

        var rx = new Regex(
            "\"" + Regex.Escape(child) + "\"\\s*:\\s*(true|false)",
            RegexOptions.IgnoreCase
        );

        var m = rx.Match(sub);
        return m.Success ? m.Groups[1].Value.ToLowerInvariant() : null;
    }

    List<string> MatchStringArrayField(string objJson, string field)
    {
        int fIdx = objJson.IndexOf("\"" + field + "\"", StringComparison.OrdinalIgnoreCase);
        if (fIdx < 0)
            return null;

        int arrStart = objJson.IndexOf('[', fIdx);
        if (arrStart < 0)
            return null;

        int arrEnd = FindMatchingBracket(objJson, arrStart, '[', ']');
        if (arrEnd <= arrStart)
            return null;

        string arr = objJson.Substring(arrStart + 1, arrEnd - arrStart - 1);

        var list = new List<string>();
        var rxItem = new Regex("\"([^\"]*)\"");

        foreach (Match m in rxItem.Matches(arr))
        {
            var s = m.Groups[1].Value != null ? m.Groups[1].Value.Trim() : null;

            if (!string.IsNullOrEmpty(s))
                list.Add(s);
        }

        return list.Count > 0 ? list : null;
    }

    bool MatchesGroup(CourseData c, string desired)
    {
        if (c == null)
            return false;

        // CƯỠNG CHẾ TUYỆT ĐỐI:
        // Mọi khóa đã vượt qua bộ lọc SEO Kỳ Môn đều thuộc cả 4 tab.
        // Không dùng group/groups từ API và không phụ thuộc enum trong Inspector.
        return IsSeoUrlMatch(c.seoUrl);
    }

    string ResolveForcedGroup(CourseData c)
    {
        if (c == null || forcedGroupRules == null)
            return null;

        string seo = c.seoUrl ?? string.Empty;
        string title = c.title ?? string.Empty;

        for (int i = 0; i < forcedGroupRules.Count; i++)
        {
            ForcedGroupRule rule = forcedGroupRules[i];

            if (rule == null || string.IsNullOrWhiteSpace(rule.seoOrTitleContains))
                continue;

            string key = rule.seoOrTitleContains.Trim();

            bool matchedSeo = seo.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0;
            bool matchedTitle = title.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0;

            if (!matchedSeo && !matchedTitle)
                continue;

            string group = MapGroup(rule.targetTab);

            Debug.Log(
                $"[CourseList/KY-MON] Forced group='{group}' | SEO='{c.seoUrl}' | title='{c.title}' | rule='{key}'"
            );

            return group;
        }

        return null;
    }

    bool MatchesApiGroup(CourseData c, string desired)
    {
        if (c == null)
            return false;

        if (!string.IsNullOrEmpty(c.group) &&
            string.Equals(c.group, desired, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (c.groups != null)
        {
            for (int i = 0; i < c.groups.Count; i++)
            {
                if (string.Equals(c.groups[i], desired, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
        }

        return false;
    }

    void SetEmptyState(GroupView view, bool isEmpty)
    {
        if (view == null)
            return;

        if (view.emptyTextObj != null)
            view.emptyTextObj.SetActive(isEmpty);
    }
}