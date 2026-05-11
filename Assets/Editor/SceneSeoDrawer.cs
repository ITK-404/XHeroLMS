using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[CustomPropertyDrawer(typeof(SceneSeoDropdownAttribute))]
public class SceneSeoDrawer : PropertyDrawer
{
    private static List<SceneSeoItem> cachedItems = null;

    // ---- UI Toolkit (giữ nguyên) ----
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        var container = new VisualElement();
        LoadData();

        int selectedIndex = cachedItems.FindIndex(x => x.seo == property.stringValue);
        if (selectedIndex < 0) selectedIndex = 0;

        var popupField = new PopupField<SceneSeoItem>(
            property.displayName,
            cachedItems,
            selectedIndex,
            (item) => item.title,
            (item) => item.title
        );

        popupField.RegisterValueChangedCallback(evt =>
        {
            property.stringValue = evt.newValue.seo;
            property.serializedObject.ApplyModifiedProperties();
        });

        container.Add(popupField);
        return container;
    }

    // ---- IMGUI fallback (thêm mới) ----
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        LoadData();

        int selectedIndex = cachedItems.FindIndex(x => x.seo == property.stringValue);
        if (selectedIndex < 0) selectedIndex = 0;

        var displayOptions = cachedItems.ConvertAll(x => x.title).ToArray();

        EditorGUI.BeginChangeCheck();
        int newIndex = EditorGUI.Popup(position, label.text, selectedIndex, displayOptions);
        if (EditorGUI.EndChangeCheck())
        {
            property.stringValue = cachedItems[newIndex].seo;
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }

    // ---- Shared ----
    private void LoadData()
    {
        if (cachedItems != null) return;

        cachedItems = new List<SceneSeoItem>();

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