using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
#endif

public static class LoadingTransition
{
    public static string TargetSceneName;
    public static string PreviousSceneName;

    public static bool UseAddressables;
    private static SceneNavigationHistory sceneHistory = new (isDebug:false);
    private static RollbackConfig rollbackConfig;
    // /// <summary>
    // /// Dùng để quay về scene trước đó, nếu lịch sử trống thì rollback default scene của scene hiện tại
    // /// </summary>
    // /// <param name="currentScene"></param>
    // /// <returns></returns>
    // public static IEnumerator RollbackScene(string currentScene)
    // {
    //     var sceneToLoad = GetSceneToLoad(currentScene);
    //
    // }
    //
    // private string GetSceneToLoad(string currentScene)
    // {
    //     string sceneToLoad = rollbackConfig.defaultSceneName;
    //     if (sceneHistory.HasHistory())
    //     {
    //         var sceneLocation = sceneHistory.GetPrevious();
    //     }
    //
    //     return sceneToLoad;
    // }
    /// <summary>
    /// Gọi hàm này để chuyển sang LoadingScene.
    /// LoadingScene sẽ đọc TargetSceneName và load async scene đích.
    /// </summary>
    private static void Load(string sceneName)
    {
        PreviousSceneName = SceneManager.GetActiveScene().name;
        TargetSceneName = sceneName;
        UseAddressables = false;

        SceneManager.LoadScene("LoadingScene", LoadSceneMode.Additive);
    }

    private static void LoadAssetBundle(string sceneName)
    {
        PreviousSceneName = SceneManager.GetActiveScene().name;
        TargetSceneName = sceneName;
        UseAddressables = true;

        Debug.Log($"[LoadingTransition] Load ADDRESSABLE scene --> {sceneName}");
        SceneManager.LoadScene("LoadingScene", LoadSceneMode.Additive);
    }

    public static IEnumerator LoadScene(string targetScene)
    {
        bool isCloud = false;
        yield return CoCheckIsCloudScene(targetScene, r => isCloud = r);

        if (isCloud)
            LoadAssetBundle(targetScene);
        else
            Load(targetScene);
    }

    private static IEnumerator CoCheckIsCloudScene(string sceneKeyOrName, System.Action<bool> result)
    {
        // var h = Addressables.LoadResourceLocationsAsync(sceneKeyOrName, typeof(SceneInstance));
        var h = Addressables.LoadResourceLocationsAsync(sceneKeyOrName);

        yield return h;

        bool ok = (h.Status == AsyncOperationStatus.Succeeded && h.Result != null && h.Result.Count > 0);

        // Release handle (tránh leak)
        Addressables.Release(h);

        result?.Invoke(ok);
    }


#if ADDRESSABLES
    public static AsyncOperationHandle<SceneInstance> LoadAddressableAsync()
    {
        Debug.Log($"[LoadingTransition] Addressables.LoadSceneAsync --> {TargetSceneName}");

        return Addressables.LoadSceneAsync(
            TargetSceneName,
            LoadSceneMode.Single,
            true
        );
    }
#endif
}