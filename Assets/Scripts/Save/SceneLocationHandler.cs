using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLocationHandler : MonoBehaviour
{
    [SerializeField] private List<SceneLocation> sceneLocationList = new();

    private void Awake()
    {
        LoadingTransition.OnLoadSceneEvent += OnLoadSceneEvent;
        SceneManager.sceneLoaded += SceneManagerOnsceneLoaded;
    }

    private void OnDestroy()
    {
        LoadingTransition.OnLoadSceneEvent -= OnLoadSceneEvent;
        SceneManager.sceneLoaded -= SceneManagerOnsceneLoaded;
    }

    private void SceneManagerOnsceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (loadSceneMode == LoadSceneMode.Single)
        {
            LoadPlayerPosition();
        }
    }
    
    private void OnLoadSceneEvent()
    {
        // try save player location in scene
        SavePlayerInformation();
    }

    private void LoadPlayerPosition()
    {
        var currentScene = SceneManager.GetActiveScene().name;

        if (TryExtractSceneLocation(currentScene, out Vector3 position, out Quaternion rotation))
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            player.transform.position = position;
            player.transform.rotation = rotation;
        }
    }

    public bool TryExtractSceneLocation(string sceneName, out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = new Quaternion(0, 0, 0, 0);
        
        var sceneLocation = GetItemBySceneName(sceneName);
        if (sceneLocation != null)
        {
            position = sceneLocation.Position;
            rotation = sceneLocation.Rotation;
            
            sceneLocationList.Remove(sceneLocation);
        }

        return sceneLocation != null;
    }

    private SceneLocation GetItemBySceneName(string sceneName)
    {
        foreach (var item in sceneLocationList)
        {
            if (item.SceneName == sceneName)
            {
                return item;
            }
        }

        return null;
    }
    
    public void SavePlayerInformation()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        var currentScene = SceneManager.GetActiveScene().name;
        Debug.Log($"Try save player");
        if (player != null)
        {
            Debug.Log("[SceneLocationHandler] Try Save Player Pdosition");

            bool isExitData = false;
            foreach (var item in sceneLocationList)
            {
                if (item.SceneName == currentScene)
                {
                    item.Position = player.transform.position;
                    item.Rotation = player.transform.rotation;
                    isExitData = true;
                    break;
                }
            }

            if (!isExitData)
            {
                SceneLocation sceneLocation = new SceneLocation
                {
                    Position = player.transform.position,
                    Rotation = player.transform.rotation,
                    SceneName = currentScene 
                };
                sceneLocationList.Add(sceneLocation);
            }
        }
    }
    
}