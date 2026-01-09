using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

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

[CustomPropertyDrawer(typeof(PlotAreaData))]
public class PlotAreaPropertyDrawer : PropertyDrawer
{
    private static SceneSeoList map = null;
    private static Dictionary<string, string> dropDownData = new();

    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var container = new VisualElement();
        var popupView = GetPopupView();

        container.Add(popupView);

        // handle default property
        container.style.marginTop = 20;
        var titleProperty = property.FindPropertyRelative("courseTitle");
        var seoUrlProperty = property.FindPropertyRelative("seo_url");
        var emptyAreaProperty = property.FindPropertyRelative("isEmptyArea");

        var titleField = new TextField(titleProperty.name);
        titleField.BindProperty(titleProperty);

        var seoField = new TextField(seoUrlProperty.name);
        seoField.BindProperty(seoUrlProperty);

        var toggle = new Toggle(emptyAreaProperty.name);
        toggle.RegisterValueChangedCallback((evt) =>
        {
            bool isEnable = evt.newValue;
            if (!isEnable)
            {
                popupView.value = map.items[0];
               
                // seoField.value = string.Empty;
                // titleField.value = string.Empty;
            }
            seoField.SetEnabled(isEnable);
            titleField.SetEnabled(isEnable);
            popupView.SetEnabled(isEnable);
        });

        container.Add(titleField);
        container.Add(seoField);
        container.Add(toggle);

        popupView.RegisterValueChangedCallback((evt) =>
        {
            // titleProperty.stringValue = evt.newValue.title;
            // seoUrlProperty.stringValue = evt.newValue.seo;
            seoField.value = evt.newValue.seo;
            titleField.value = evt.newValue.title;
        });

        return container;
    }

    private PopupField<SceneSeoItem> GetPopupView()
    {
        LoadJsonData();
        var popupField = new PopupField<SceneSeoItem>(
            "Chọn khoá học đại diện cho khu vực này",
            map.items,
            0,
            (data) => data.title,
            (data) => data.title);

        popupField.choices = map.items;

        popupField.RegisterValueChangedCallback((evt) =>
        {
            SceneSeoItem selected = evt.newValue;
            Debug.Log($"Bạn chọn {selected.title} có SEOID: {selected.seo}");
        });

        return popupField;
    }

    private void LoadJsonData()
    {
        if (map != null)
            return;
        Debug.Log("Find Map Data");
        TextAsset targetFile = Resources.Load<TextAsset>("courses");

        var wrapped = "{\"items\":" + targetFile.text + "}";

        map = JsonUtility.FromJson<SceneSeoList>(wrapped);

        var emptySeoItem = new SceneSeoItem();
        emptySeoItem.title = "None (Empty)";
        map.items.Add(emptySeoItem);
    }
}


public class SceneSeoDropdownAttribute : PropertyAttribute 
{
    // Bạn có thể thêm các tham số vào đây nếu cần (ví dụ: tên file json)
}
[CustomPropertyDrawer(typeof(SceneSeoDropdownAttribute))]
public class SceneSeoDrawer : PropertyDrawer
{
    private static List<SceneSeoItem> cachedItems = null;

    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var container = new VisualElement();
        LoadData();

        // Tìm item hiện tại dựa trên giá trị seo_url đang lưu trong property
        int selectedIndex = cachedItems.FindIndex(x => x.seo == property.stringValue);
        if (selectedIndex < 0) selectedIndex = 0; // Mặc định là "None"

        var popupField = new PopupField<SceneSeoItem>(
            property.displayName,
            cachedItems,
            selectedIndex,
            (item) => item.title, // Hiển thị Title
            (item) => item.title  // Hiển thị Title khi đã chọn
        );

        popupField.RegisterValueChangedCallback(evt =>
        {
            // Khi chọn Title, set giá trị property thành SEO_URL
            property.stringValue = evt.newValue.seo;
            property.serializedObject.ApplyModifiedProperties();
        });

        container.Add(popupField);
        return container;
    }

    private void LoadData()
    {
        if (cachedItems != null) return;

        cachedItems = new List<SceneSeoItem>();
        // Thêm option mặc định là Trống
        var defaultSeoItem = new SceneSeoItem();
        defaultSeoItem.title = "None (Empty)";
        
        cachedItems.Add(defaultSeoItem);

        TextAsset targetFile = Resources.Load<TextAsset>("courses");
        if (targetFile != null)
        {
            var wrapped = "{\"items\":" + targetFile.text + "}";
            var data = JsonUtility.FromJson<SceneSeoList>(wrapped);
            if (data != null && data.items != null)
            {
                cachedItems.AddRange(data.items);
            }
        }
    }
}

