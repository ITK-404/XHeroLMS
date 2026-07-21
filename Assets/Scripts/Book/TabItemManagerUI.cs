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
    // ultis camera
    public static Camera uiWorldSpaceCamera;
    
    private Dictionary<CourseLessonTabID, BookShelfManager> tabList = new();
    private BookShelfManager[] tabIDs;
    private TabUI[] tabButtonsList;
    [SerializeField] private CourseLessonTabID currentItemID;
    [SerializeField] Button returnBtn;
    [SerializeField] private GameObject shelfContainer;
    [SerializeField] private GameObject tabContainer;

    public GameObject container;

    public event Action OnClickReturnBtnEvent;

    [SerializeField] private bool isLoadToPreviousSceneOrTurnOff = false;
    private void Awake()
    {
        uiWorldSpaceCamera = GetComponent<Canvas>().worldCamera;
        if (uiWorldSpaceCamera == null)
            uiWorldSpaceCamera = Camera.main;
        
        if (uiWorldSpaceCamera)
        {
            Debug.Log($"TabItemManagerUI set camera: ", uiWorldSpaceCamera.gameObject);
        }
        tabIDs = GetComponentsInChildren<BookShelfManager>();
        tabButtonsList = GetComponentsInChildren<TabUI>();
        returnBtn.onClick.AddListener(OnClickPreviousScene);
        foreach (var item in tabIDs)
        {
            tabList.Add(item.CourseID, item);
        }

        foreach(var item in tabButtonsList)
        {
            item.manager = this;
            item.nameTitle.text = GetNameCourseTitle(item.tabID);
        }

        ActiveTab(currentItemID);
    }

    private void OnDestroy()
    {
        returnBtn.onClick.RemoveListener(OnClickPreviousScene);
    }

    private void OnClickPreviousScene()
    {
        OnClickReturnBtnEvent?.Invoke();
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
        Debug.Log("Find active ID: " + activeTabID);
        Debug.Log("List Count; " + tabList.Count);
        foreach(var item in tabList)
        {
            Debug.Log("Đã duyệt qua item: " + item.Key.ToString());
            if(item.Key == activeTabID)
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
        Debug.Log($"CurrentTabID: {tabButtonsList.Length}");
        foreach(var item in tabButtonsList)
        {
            bool isActiveCurrentUI = activeTabID == item.tabID;
            item.ActiveState(isActiveCurrentUI);
            Debug.Log($"CurrentTabID active state: {isActiveCurrentUI}",gameObject);
        }
    }   

    public void Show()
    {
        container.gameObject.SetActive(true);
    }

    public void Hide()
    {
        container.gameObject.SetActive(false);
    }
}
