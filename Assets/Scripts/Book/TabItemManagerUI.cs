using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using static Unity.Burst.Intrinsics.X86.Avx;
using System.Collections;

#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
#endif

public class TabItemManagerUI : MonoBehaviour
{
    private Dictionary<CourseLessonTabID, BookShelfManager> tabList = new();
    private BookShelfManager[] tabIDs;
    private TabUI[] tabButtonsList;
    private CourseLessonTabID currentItemID;

    [SerializeField] Button returnBtn;
    [SerializeField] private GameObject shelfContainer;
    [SerializeField] private GameObject tabContainer;

    public GameObject container;

    // ================== KY MON EXCEPTION ==================
    private CourseListPageKyMonUI kyMonPage;
    private Coroutine kyMonRefreshRoutine;

    private void Awake()
    {
        // Tự tìm CourseListPageKyMonUI, không cần kéo thả.
        kyMonPage = FindKyMonPageAuto();

        // Quan trọng: includeInactive = true
        // nếu không các tab đang inactive sẽ không được lấy vào tabList.
        tabIDs = GetComponentsInChildren<BookShelfManager>(true);
        tabButtonsList = GetComponentsInChildren<TabUI>(true);

        if (returnBtn != null)
        {
            returnBtn.onClick.RemoveAllListeners();
            returnBtn.onClick.AddListener(() =>
            {
                // LoadingTransition.Load("New Scene");
                // LoadingTransition.Load_Scene("New Scene");
                LoadingTransition.LoadPreviousSceneOrDefault();
            });
        }

        tabList.Clear();

        foreach (var item in tabIDs)
        {
            if (item == null)
                continue;

            if (!tabList.ContainsKey(item.CourseID))
            {
                tabList.Add(item.CourseID, item);
            }
            else
            {
                Debug.LogWarning("[TabItemManagerUI] Duplicate CourseID: " + item.CourseID);
            }
        }

        foreach (var item in tabButtonsList)
        {
            if (item == null)
                continue;

            item.manager = this;

            if (item.nameTitle != null)
                item.nameTitle.text = GetNameCourseTitle(item.tabID);
        }

        if (kyMonPage != null)
        {
            currentItemID = GetKyMonDefaultContentID();
            Debug.Log("[TabItemManagerUI/KY-MON] Found CourseListPageKyMonUI in Awake.");
            Debug.Log("[TabItemManagerUI/KY-MON] Init content ID: " + currentItemID);
        }

        ActiveTab(currentItemID);
    }

    private string GetNameCourseTitle(CourseLessonTabID ID)
    {
        switch (ID)
        {
            case CourseLessonTabID.CoBan:
                return "Cơ bản";
            case CourseLessonTabID.NangCao:
                return "Nâng cao";
            case CourseLessonTabID.ChuyenSau:
                return "Chuyên sâu";
            case CourseLessonTabID.DoanhNghiep:
                return "Doanh Nghiệp";
            default:
                break;
        }

        return default;
    }

    public void ActiveTab(CourseLessonTabID activeTabID)
    {
        // Nếu lúc Awake chưa tìm thấy do thứ tự init, thử tìm lại lần nữa.
        if (kyMonPage == null)
            kyMonPage = FindKyMonPageAuto();

        bool isKyMonException = kyMonPage != null;

        if (isKyMonException)
        {
            ActiveTabKyMonException(activeTabID);
            return;
        }

        // ================== LOGIC CŨ GIỮ NGUYÊN ==================

        Debug.Log("Find active ID: " + activeTabID);
        Debug.Log("List Count; " + tabList.Count);

        foreach (var item in tabList)
        {
            Debug.Log("Đã duyệt qua item: " + item.Key.ToString());

            if (item.Key == activeTabID)
            {
                Debug.Log("active ID: " + activeTabID);
                item.Value.gameObject.SetActive(true);
                item.Value.ResetScrollContent();
            }
            else
            {
                Debug.Log("deactive ID: " + activeTabID);
                item.Value.gameObject.SetActive(false);
            }
        }

        foreach (var item in tabButtonsList)
        {
            bool isActiveCurrentUI = activeTabID == item.tabID;
            item.ActiveState(isActiveCurrentUI);
        }
    }

    // ================== KY MON EXCEPTION ==================

    private void ActiveTabKyMonException(CourseLessonTabID activeTabID)
    {
        CourseLessonTabID contentActiveID = GetKyMonDefaultContentID();

        Debug.Log("[TabItemManagerUI/KY-MON] Button active ID: " + activeTabID);
        Debug.Log("[TabItemManagerUI/KY-MON] Content active ID: " + contentActiveID);
        Debug.Log("[TabItemManagerUI/KY-MON] Skip BookShelfManager SetActive/ResetScrollContent.");

        // Quan trọng:
        // Với CourseListPageKyMonUI, KHÔNG đụng vào tabList nữa.
        // Không SetActive BookShelfManager.
        // Không ResetScrollContent.
        // Vì CourseListPageKyMonUI đã tự ToggleAllRoots + SpawnShelvesInto.
        foreach (var item in tabButtonsList)
        {
            if (item == null)
                continue;

            // Nút nào bấm thì nút đó sáng.
            bool isActiveCurrentUI = activeTabID == item.tabID;
            item.ActiveState(isActiveCurrentUI);
        }

        // Ép render lại đúng view mặc định của KyMon.
        // Gọi ngay + gọi lại cuối frame để tránh bị script UI khác đè sau click.
        if (kyMonPage != null)
        {
            kyMonPage.RefreshForTab(contentActiveID);

            if (kyMonRefreshRoutine != null)
                StopCoroutine(kyMonRefreshRoutine);

            kyMonRefreshRoutine = StartCoroutine(CoRefreshKyMonNextFrame(contentActiveID));
        }
    }

    private IEnumerator CoRefreshKyMonNextFrame(CourseLessonTabID contentActiveID)
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        if (kyMonPage == null)
            kyMonPage = FindKyMonPageAuto();

        if (kyMonPage != null)
        {
            Debug.Log("[TabItemManagerUI/KY-MON] Re-render after frame: " + contentActiveID);
            kyMonPage.RefreshForTab(contentActiveID);
        }

        kyMonRefreshRoutine = null;
    }

    // ================== KY MON EXCEPTION HELPERS ==================

    private CourseListPageKyMonUI FindKyMonPageAuto()
    {
        CourseListPageKyMonUI found = null;

        if (container != null)
        {
            found = container.GetComponentInChildren<CourseListPageKyMonUI>(true);
            if (found != null) return found;
        }

        if (shelfContainer != null)
        {
            found = shelfContainer.GetComponentInChildren<CourseListPageKyMonUI>(true);
            if (found != null) return found;
        }

        if (tabContainer != null)
        {
            found = tabContainer.GetComponentInChildren<CourseListPageKyMonUI>(true);
            if (found != null) return found;
        }

        found = GetComponentInChildren<CourseListPageKyMonUI>(true);
        if (found != null) return found;

        found = GetComponentInParent<CourseListPageKyMonUI>(true);
        if (found != null) return found;

        if (transform.root != null)
        {
            found = transform.root.GetComponentInChildren<CourseListPageKyMonUI>(true);
            if (found != null) return found;
        }

        // Trường hợp TabItemManagerUI nằm ở DontDestroyOnLoad,
        // còn CourseListPageKyMonUI nằm trong Course Scene.
        CourseListPageKyMonUI[] all = Resources.FindObjectsOfTypeAll<CourseListPageKyMonUI>();

        foreach (var page in all)
        {
            if (page == null)
                continue;

            if (page.gameObject == null)
                continue;

            // Bỏ prefab asset, chỉ lấy object thật trong scene.
            if (!page.gameObject.scene.IsValid())
                continue;

            return page;
        }

        return null;
    }

    private CourseLessonTabID GetKyMonDefaultContentID()
    {
        if (kyMonPage == null)
            return CourseLessonTabID.ChuyenSau;

        return GroupStringToTabID(kyMonPage.defaultOpenGroup);
    }

    private CourseLessonTabID GroupStringToTabID(string group)
    {
        if (string.IsNullOrWhiteSpace(group))
            return CourseLessonTabID.ChuyenSau;

        switch (group.Trim().ToLowerInvariant())
        {
            case "basic":
                return CourseLessonTabID.CoBan;

            case "advanced":
                return CourseLessonTabID.NangCao;

            case "intensive":
                return CourseLessonTabID.ChuyenSau;

            case "business":
                return CourseLessonTabID.DoanhNghiep;
        }

        return CourseLessonTabID.ChuyenSau;
    }

    public void Show()
    {
        if (container != null)
            container.gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (container != null)
            container.gameObject.SetActive(false);
    }
}