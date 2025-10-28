using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static Unity.Burst.Intrinsics.X86.Avx;

public class TabItemManagerUI : MonoBehaviour
{
    private Dictionary<CourseLessonTabID, BookShelfManager> tabList = new();
    private BookShelfManager[] tabIDs;
    private TabUI[] tabButtonsList;
    private CourseLessonTabID currentItemID;

    [SerializeField] private GameObject shelfContainer;
    [SerializeField] private GameObject tabContainer;

    public GameObject container;
    
    private void Awake()
    {
        tabIDs = GetComponentsInChildren<BookShelfManager>();
        tabButtonsList = GetComponentsInChildren<TabUI>();

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

        foreach(var item in tabButtonsList)
        {
            bool isActiveCurrentUI = activeTabID == item.tabID;
            item.ActiveState(isActiveCurrentUI);
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
