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
            LoadPlayerPosition(scene.name);
        }
    }

    private void OnLoadSceneEvent()
    {
        // try save player location in scene
        SavePlayerInformation();
    }

    public void LoadPlayerPosition(string sceneName)
    {
        if (TryExtractSceneLocation(sceneName, out Vector3 position, out Quaternion rotation))
        {
            Debug.Log("[SceneLocationHandler] Tim thay scene valid + vi tri");
            var player = GameObject.FindGameObjectWithTag("Player").GetComponent<PointClickSystem>();
            player.TeleportDelay(position);
            // player.transform.position = position;
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

        string debugValue = sceneLocation != null ? "Thành công" : "Thất bại";
        Debug.Log($"[SceneLocationHandler] Cố gắng extract vị trí trong scene {sceneName} {debugValue}");

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
            TryAddOrUpdate(currentScene, player.transform.position, player.transform.rotation);
        }
    }

    public void TryAddOrUpdate(string currentScene, Vector3 position, Quaternion rotation)
    {
        Debug.Log($"[SceneLocationHandle] Try add or update {currentScene} {position} {rotation}");
        bool isExitData = false;
        foreach (var item in sceneLocationList)
        {
            if (item.SceneName == currentScene)
            {
                item.Position = position;
                item.Rotation = rotation;
                Debug.Log($"[SceneLocationHandle] Cập nhật vị trí trong scene thành công {currentScene}");
                isExitData = true;
                break;
            }
        }

        if (!isExitData)
        {
            SceneLocation sceneLocation = SceneLocation.CaptureFromPlayer(position, rotation);
            sceneLocationList.Add(sceneLocation);
            Debug.Log($"[SceneLocationHandle] Thêm vị trí trong scene thành công {currentScene}");
        }
    }
}