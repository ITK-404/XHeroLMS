using System;
using System.Collections.Generic;
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