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
    private UniTask<SceneLocationConfig>? configLoadTask;
    private void Awake()
    {
        LoadingTransition.OnLoadSceneEvent += OnLoadSceneEvent;
        SceneManager.sceneLoaded += SceneManagerOnsceneLoaded;

        LoadConfig(destroyCancellationToken).Forget();
    }

    private async UniTask LoadConfig(CancellationToken cancellationToken)
    {
        if (config != null)
            return;

        if (!configLoadTask.HasValue)
        {
            configLoadTask = Addressables
                .LoadAssetAsync<SceneLocationConfig>("SceneLocationConfig")
                .WithCancellation(cancellationToken);
        }

        try
        {
            config = await configLoadTask.Value;
        }
        catch (System.Exception e)
        {
            configLoadTask = null;
            Debug.LogWarning("[SceneLocationHandler] SceneLocationConfig load failed: " + e.Message);
        }
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
            RestoreSavedPlayerPosition(scene.name);
        }
    }

    private void OnLoadSceneEvent()
    {
        // try save player location in scene
        // SavePlayerInformation();
    }

    private async UniTaskVoid LoadPlayerPositionAfterConfig(string sceneName)
    {
        await LoadConfig(destroyCancellationToken);

        if (config == null)
        {
            Debug.LogWarning("[SceneLocationHandler] Cannot restore position because config is null.");
            return;
        }

        await UniTask.Yield();
        LoadPlayerPosition(sceneName);
    }

    public void RestoreSavedPlayerPosition(string sceneName)
    {
        RestoreSavedPlayerPositionRoutine(sceneName).Forget();
    }

    private async UniTaskVoid RestoreSavedPlayerPositionRoutine(string sceneName)
    {
        await LoadConfig(destroyCancellationToken);

        const int maxWaitFrames = 180;
        for (int frame = 0; frame < maxWaitFrames; frame++)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null && playerObject.GetComponent<PointClickSystem>() != null)
            {
                Debug.Log("[SceneLocationHandler] Explicit restore after world scene became ready: " + sceneName);
                LoadPlayerPosition(sceneName);
                return;
            }

            await UniTask.Yield();
        }

        Debug.LogWarning("[SceneLocationHandler] Timed out waiting for Player to restore saved position: " + sceneName);
    }

    public void LoadPlayerPosition(string sceneName)
    {
        if (config == null)
        {
            LoadPlayerPositionAfterConfig(sceneName).Forget();
            return;
        }

        var playerObject = GameObject.FindGameObjectWithTag("Player");
        var player = playerObject != null ? playerObject.GetComponent<PointClickSystem>() : null;
        if (player == null)
        {
            Debug.LogWarning("[SceneLocationHandler] Player was not ready while restoring position.");
            return;
        }

        Debug.Log($"[SceneLocationHandler] Cập nhật vị trí khi load scene");
        if (TryExtractSceneLocation(sceneName, out Vector3 position, out Quaternion rotation))
        {
            Debug.Log("[SceneLocationHandler] Tìm thấy data của scene trong danh sách, bắt đầu cập nhật vị trí");
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
            position = sceneLocation.Position + (config != null ? config.offset : Vector3.zero);
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
            if (SceneNameAliases.AreSameScene(item.SceneName, sceneName))
            {
                return item;
            }
        }

        return null;
    }

    public void SavePlayerInformation()
    {
        var currentScene = SceneNameAliases.ToSavedSceneName(SceneManager.GetActiveScene().name);
        if (config == null || !config.IsSceneCanSave(currentScene))
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

    public void SavePlayerPosition(Vector3 position, Quaternion rotation)
    {
        var currentScene = SceneNameAliases.ToSavedSceneName(SceneManager.GetActiveScene().name);
        if (config == null || !config.IsSceneCanSave(currentScene))
        {
            return;
        }
        Debug.Log($"Try save player");

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            Debug.Log("[SceneLocationHandler] Try Save Player Pdosition");
            SceneLocation playerLocation = new SceneLocation(currentScene, position, rotation);
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

    public void Clear()
    {
        sceneLocationList.Clear();
    }
}
