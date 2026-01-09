using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Area Map Data", menuName = "Area Map Data")]
public class AreaMapData : ScriptableObject
{
    public string displayName = "Test";
    public Sprite displayIcon;

    public string dropDownTest;

    [SerializeField] List<PlotAreaData> plotAreaList = new();

    public PlotAreaData GetPlotAreaData(string search_seo_url)
    {
        foreach (var item in plotAreaList)
        {
            if (item.seo_url == search_seo_url)
            {
                return item;
            }
        }

        return null;
    }
}

[Serializable]
public class PlotAreaData
{
    public string plotDropDown;
    public string courseTitle;
    public string seo_url;
    public bool isEmptyArea = true;
}


public class SceneSeoDropdownAttribute : PropertyAttribute 
{
    // Bạn có thể thêm các tham số vào đây nếu cần (ví dụ: tên file json)
}