using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

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