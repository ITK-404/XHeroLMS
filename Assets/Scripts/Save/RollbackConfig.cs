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

    public string GetRollbackScene(string currentScene)
    {
        string rollbackScene = defaultSceneName;

        foreach (var item in SceneConfigs)
        {
            if (item.sceneName == currentScene)
            {
                rollbackScene = item.rollbackScene;
                break;
            }
        }
        Debug.Log($"Rollback scene of {currentScene} is {rollbackScene}");        
        return rollbackScene;
    }
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