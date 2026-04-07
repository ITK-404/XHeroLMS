using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
[CreateAssetMenu(fileName = "Rollback Scene Config",menuName = "Rollback Scene Config")]
public class RollbackConfig : ScriptableObject
{
    [SceneDropdown]
    public string defaultSceneName = "";
    [Serializable]
    public struct SceneConfig
    {
        [SceneDropdown]
        public string sceneName;
        [SceneDropdown]
        public string rollbackScene;
    }
    [SerializeField]
    private List<SceneConfig> SceneConfigs = new();
}
public class SceneDropdownAttribute : PropertyAttribute { }

[CustomPropertyDrawer(typeof(SceneDropdownAttribute))]
public class SceneDropdownDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        if (property.propertyType != SerializedPropertyType.String)
        {
            EditorGUI.HelpBox(position,"Scene dropdown chỉ dùng với string", MessageType.Error);
            return;
        }

        string[] scenesName = EditorBuildSettings.scenes.Select(s => System.IO.Path.GetFileNameWithoutExtension(s.path))
            .ToArray();

        if (scenesName.Length == 0)
        {
            EditorGUI.HelpBox(position,"Không có scene nào trong build setting",MessageType.Error);
            return;
        }

        int currentIndex = System.Array.IndexOf(scenesName, property.stringValue);

        if (currentIndex < 0) currentIndex = 0;

        EditorGUI.BeginProperty(position, label, property);
        int selectedIndex = EditorGUI.Popup(position, label.text, currentIndex, scenesName);
        property.stringValue = scenesName[selectedIndex];
        EditorGUI.EndProperty();

    }
}

[CustomEditor(typeof(RollbackConfig))]
public class RollbackConfigEditor : Editor
{
    private string[] sceneNames;

    private void OnEnable()
    {
        sceneNames = EditorBuildSettings.scenes.Select(s => System.IO.Path.GetFileNameWithoutExtension(s.path))
            .ToArray();
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        serializedObject.Update();
        
        var allValues = CollectAllSceneValues();
        var defaultSceneProp = serializedObject.FindProperty("defaultSceneName");
        
        DrawSceneDropdown(defaultSceneProp, allValues);

    }

    private void DrawSceneDropdown(SerializedProperty property, List<string> scopeValues, string selfValue = null)
    {
        if (sceneNames.Length == 0)
        {
            EditorGUILayout.HelpBox("Không có scene nào trong Build Settings.", MessageType.Warning);
            return;
        }
        int currentIndex = System.Array.IndexOf(sceneNames, property.stringValue);
        if (currentIndex < 0) currentIndex = 0;

        EditorGUI.BeginChangeCheck();
        int selectedIndex = EditorGUILayout.Popup(property.displayName, currentIndex, sceneNames);
        if (EditorGUI.EndChangeCheck())
            property.stringValue = sceneNames[selectedIndex];

        string val = property.stringValue;
        bool isDuplicate = !string.IsNullOrEmpty(val)
                           && scopeValues.Count(v => v == val) > 1;

        if (isDuplicate)
            EditorGUILayout.HelpBox($"Scene \"{val}\" bị trùng trong cùng scope!", MessageType.Warning);
    }

    private List<string> CollectAllSceneValues()
    {
        var result = new List<string>();
        var iter = serializedObject.GetIterator();
        iter.NextVisible(true);
        do
        {
            if (iter.propertyType == SerializedPropertyType.String && !string.IsNullOrEmpty(iter.stringValue))
                result.Add(iter.stringValue);
        }
        while (iter.NextVisible(true));
        return result;
    }
    
}