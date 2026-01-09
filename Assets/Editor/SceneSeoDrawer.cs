using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

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