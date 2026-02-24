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
    /// <summary>
    /// Gọi hàm này để chuyển sang LoadingScene.
    /// LoadingScene sẽ đọc TargetSceneName và load async scene đích.
    /// </summary>
public static void Load(string sceneName)
{
    PreviousSceneName = SceneManager.GetActiveScene().name;
    TargetSceneName = sceneName;
    UseAddressables = false;

    SceneManager.LoadScene("LoadingScene", LoadSceneMode.Additive);
}

public static void LoadAssetBundle(string sceneName)
{
    PreviousSceneName = SceneManager.GetActiveScene().name;
    TargetSceneName = sceneName;
    UseAddressables = true;

    Debug.Log($"[LoadingTransition] Load ADDRESSABLE scene --> {sceneName}");
    SceneManager.LoadScene("LoadingScene", LoadSceneMode.Additive);
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
