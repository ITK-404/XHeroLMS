using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SceneLoactionConfig",menuName = "Scene Loaction Config")]
public class SceneLocationConfig : ScriptableObject
{
    [Serializable]
    public class SceneConfig
    {
        [SceneDropdown]public string sceneName;
        public bool canSaveLocation = false;
    }
    [Header("Config này dùng để quy định xem scene có được phép lưu vị trí sau khi load qua scene khác không")]
    [SerializeField] private List<SceneConfig> sceneConfigs;

    public bool IsSceneCanSave(string checkSceneName)
    {
        foreach (var item in sceneConfigs)
        {
            if (item.sceneName == checkSceneName)
            {
                return true;
            }
        }

        return false;
    }
}