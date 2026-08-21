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

    private async UniTaskVoid LoadPlayerPositionAfterConfig(string sceneName, Scene targetScene)
    {
        await LoadConfig(destroyCancellationToken);

        if (config == null)
        {
            Debug.LogWarning("[SceneLocationHandler] Cannot restore position because config is null.");
            return;
        }

        await UniTask.Yield();
        LoadPlayerPosition(sceneName, targetScene);
    }

    public void RestoreSavedPlayerPosition(string sceneName)
    {
        RestoreSavedPlayerPositionRoutine(sceneName, default).Forget();
    }

    public void RestoreSavedPlayerPosition(Scene targetScene)
    {
        if (!targetScene.IsValid())
        {
            RestoreSavedPlayerPosition(SceneManager.GetActiveScene().name);
            return;
        }

        RestoreSavedPlayerPositionRoutine(targetScene.name, targetScene).Forget();
    }

    private async UniTaskVoid RestoreSavedPlayerPositionRoutine(string sceneName, Scene targetScene)
    {
        await LoadConfig(destroyCancellationToken);

        await WaitForNewSceneLateContent(sceneName);

        const int maxWaitFrames = 180;
        for (int frame = 0; frame < maxWaitFrames; frame++)
        {
            GameObject playerObject = FindPlayerForRestore(targetScene);
            if (playerObject != null && playerObject.GetComponent<PointClickSystem>() != null)
            {
                Debug.Log("[SceneLocationHandler] Explicit restore after world scene became ready: "
                          + sceneName
                          + ", playerScene="
                          + playerObject.scene.name);
                LoadPlayerPosition(sceneName, targetScene);
                return;
            }

            await UniTask.Yield();
        }

        Debug.LogWarning("[SceneLocationHandler] Timed out waiting for Player to restore saved position: " + sceneName);
    }

    private async UniTask WaitForNewSceneLateContent(string sceneName)
    {
        if (!SceneNameAliases.IsNewSceneFamily(sceneName))
            return;

        const int maxWaitFrames = 600;
        Scene activeScene = SceneManager.GetActiveScene();

        for (int frame = 0; frame < maxWaitFrames; frame++)
        {
            AddressableAdditiveSceneLoader[] loaders = FindObjectsByType<AddressableAdditiveSceneLoader>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            AddressableAdditiveSceneLoader activeLoader = null;
            foreach (AddressableAdditiveSceneLoader loader in loaders)
            {
                if (loader != null && loader.gameObject.scene == activeScene)
                {
                    activeLoader = loader;
                    break;
                }
            }

            if (activeLoader == null || activeLoader.IsComplete)
                return;

            await UniTask.Yield();
        }

        Debug.LogWarning("[SceneLocationHandler] Late scene content chưa hoàn tất sau thời gian chờ; tiếp tục restore bằng vị trí đã lưu.");
    }

    public void LoadPlayerPosition(string sceneName)
    {
        LoadPlayerPosition(sceneName, default);
    }

    private void LoadPlayerPosition(string sceneName, Scene targetScene)
    {
        if (config == null)
        {
            LoadPlayerPositionAfterConfig(sceneName, targetScene).Forget();
            return;
        }

        var playerObject = FindPlayerForRestore(targetScene);
        var player = playerObject != null ? playerObject.GetComponent<PointClickSystem>() : null;
        if (player == null)
        {
            Debug.LogWarning("[SceneLocationHandler] Player was not ready in target scene while restoring position. scene="
                             + sceneName
                             + ", targetScene="
                             + (targetScene.IsValid() ? targetScene.name : "<any>"));
            return;
        }

        if (targetScene.IsValid() && playerObject.scene != targetScene)
        {
            Debug.LogWarning("[SceneLocationHandler] Refuse to restore using a Player from another scene. target="
                             + targetScene.name
                             + ", playerScene="
                             + playerObject.scene.name);
            return;
        }

        Debug.Log($"[SceneLocationHandler] Cập nhật vị trí khi load scene");
        if (TryGetSceneLocation(sceneName, out SceneLocation savedLocation, out Vector3 position, out Quaternion rotation))
        {
            Debug.Log("[SceneLocationHandler] Tìm thấy data của scene trong danh sách, bắt đầu cập nhật vị trí");
            if (!player.TeleportDelay(position))
            {
                Debug.LogWarning("[SceneLocationHandler] Teleport vị trí đã lưu thất bại; giữ lại data để thử lại.");
                return;
            }

            // TeleMapController._mapActive = true;
            // player.transform.position = position;
            player.transform.rotation = rotation;
            // TeleMapController._mapActive = false;
            sceneLocationList.Remove(savedLocation);
            Debug.Log("[SceneLocationHandler] Khôi phục vị trí thành công và đã consume data.");
        }
    }

    private GameObject FindPlayerForRestore(Scene targetScene)
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        if (targetScene.IsValid())
        {
            foreach (GameObject player in players)
            {
                if (player != null && player.scene == targetScene && player.GetComponent<PointClickSystem>() != null)
                    return player;
            }

            return null;
        }

        foreach (GameObject player in players)
        {
            if (player != null && player.GetComponent<PointClickSystem>() != null)
                return player;
        }

        return null;
    }
    
    public bool TryExtractSceneLocation(string sceneName, out Vector3 position, out Quaternion rotation)
    {
        if (!TryGetSceneLocation(sceneName, out SceneLocation sceneLocation, out position, out rotation))
            return false;

        sceneLocationList.Remove(sceneLocation);

        Debug.Log($"[SceneLocationHandler] Cố gắng extract vị trí trong scene {sceneName} Thành công");

        return true;
    }

    private bool TryGetSceneLocation(
        string sceneName,
        out SceneLocation sceneLocation,
        out Vector3 position,
        out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = new Quaternion(0, 0, 0, 0);
        sceneLocation = GetItemBySceneName(sceneName);

        if (sceneLocation == null)
        {
            Debug.Log($"[SceneLocationHandler] Cố gắng lấy vị trí trong scene {sceneName} Thất bại");
            return false;
        }

        position = sceneLocation.Position + (config != null ? config.offset : Vector3.zero);
        rotation = sceneLocation.Rotation;
        return true;
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
        SaveTransitionPosition(SceneManager.GetActiveScene().name, position, rotation);
    }

    public void SaveTransitionPosition(Vector3 position, Quaternion rotation)
    {
        SaveTransitionPosition(SceneManager.GetActiveScene().name, position, rotation);
    }

    public void SaveTransitionPosition(string sourceSceneName, Vector3 position, Quaternion rotation)
    {
        var currentScene = SceneNameAliases.ToSavedSceneName(sourceSceneName);
        if (string.IsNullOrWhiteSpace(currentScene))
        {
            Debug.LogWarning("[SceneLocationHandler] Cannot save door return position because source scene is empty.");
            return;
        }

        Debug.Log("[SceneLocationHandler] Save door return position. scene="
                  + currentScene
                  + ", source="
                  + sourceSceneName
                  + ", position="
                  + position
                  + ", rotation="
                  + rotation);

        TryAddOrUpdate(new SceneLocation(currentScene, position, rotation));
    }

    public void TryAddOrUpdate(SceneLocation addLocation)
    {
        Debug.Log($"[SceneLocationHandle] Try add or update {addLocation.SceneName} {addLocation.Position} {addLocation.Rotation}");
        bool isExitData = false;
        foreach (var item in sceneLocationList)
        {
            if (SceneNameAliases.AreSameScene(item.SceneName, addLocation.SceneName))
            {
                item.Position = addLocation.Position;
                item.Rotation = addLocation.Rotation;
                item.SceneName = SceneNameAliases.ToSavedSceneName(addLocation.SceneName);
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
