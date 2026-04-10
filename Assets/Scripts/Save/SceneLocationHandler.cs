using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.SceneManagement;

public class SceneLocationHandler : MonoBehaviour
{
    [SerializeField] private List<SceneLocation> sceneLocationList = new();
    [SerializeField] private SceneLocationConfig config;
    private void Awake()
    {
        LoadingTransition.OnLoadSceneEvent += OnLoadSceneEvent;
        SceneManager.sceneLoaded += SceneManagerOnsceneLoaded;

        LoadConfig(destroyCancellationToken).Forget();
    }

    private async UniTask LoadConfig(CancellationToken cancellationToken)
    {
        config = await Addressables.LoadAssetAsync<SceneLocationConfig>("SceneLocationConfig").WithCancellation(cancellationToken);
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
        Debug.Log($"[SceneLocationHandler] Cập nhật vị trí khi load scene");
        if (TryExtractSceneLocation(sceneName, out Vector3 position, out Quaternion rotation))
        {
            Debug.Log("[SceneLocationHandler] Tìm thấy data của scene trong danh sách, bắt đầu cập nhật vị trí");
            var player = GameObject.FindGameObjectWithTag("Player").GetComponent<PointClickSystem>();
            player.TeleportDelay(position);
            // TeleMapController._mapActive = true;
            // player.transform.position = position;
            player.transform.rotation = rotation;
            // TeleMapController._mapActive = false;
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

    private void SavePlayerInformation()
    {
        var currentScene = SceneManager.GetActiveScene().name;
        if (!config.IsSceneCanSave(currentScene))
        {
            return;
        }
        Debug.Log($"Try save player");

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Debug.Log("[SceneLocationHandler] Try Save Player Pdosition");
            SceneLocation playerLocation = new SceneLocation(currentScene, player.transform.position, player.transform.rotation);
            TryAddOrUpdate(playerLocation);
        }
    }

    public void TryAddOrUpdate(SceneLocation addLocation)
    {
        Debug.Log($"[SceneLocationHandle] Try add or update {addLocation.SceneName} {addLocation.Position} {addLocation.Rotation}");
        bool isExitData = false;
        foreach (var item in sceneLocationList)
        {
            if (item.SceneName == addLocation.SceneName)
            {
                item.Position = addLocation.Position;
                item.Rotation = addLocation.Rotation;
                Debug.Log($"[SceneLocationHandle] Cập nhật vị trí trong scene thành công {addLocation.SceneName}");
                isExitData = true;
                break;
            }
        }

        if (!isExitData)
        {
            sceneLocationList.Add(addLocation);
            Debug.Log($"[SceneLocationHandle] Thêm vị trí trong scene thành công {addLocation.SceneName}");
        }
    }
}