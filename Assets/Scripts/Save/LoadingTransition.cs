using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
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
    private static SceneNavigationHistory sceneHistory;
    private static RollbackConfig rollbackConfig;

    private static MonoBehaviour runner;
    private static Coroutine _coroutine;

    public static Action OnLoadSceneEvent;
    
    public static async UniTaskVoid Init(MonoBehaviour _runner, SceneNavigationHistory sceneNavigationHistory,CancellationToken token)
    {
        runner = _runner;
        
        var result = await Addressables.LoadAssetAsync<RollbackConfig>("Rollback Scene Config").WithCancellation(token);
        await UniTask.SwitchToMainThread();
        if (result != null)
        {
            Debug.Log("[Loading Transition] Init Complete");
            rollbackConfig = result;
        }

        sceneHistory = sceneNavigationHistory;
    }

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

    public static void Load_Scene(string targetScene, bool isSaveHistory = false)
    {
        if (_coroutine != null)
        {
            Debug.Log("Stop Coroutine loading");
            runner.StopCoroutine(_coroutine);
        }

        _coroutine = runner.StartCoroutine(LoadScene(targetScene));
        OnLoadSceneEvent?.Invoke();

        var currentScene = SceneManager.GetActiveScene().name;

        if (isSaveHistory)
        {
            sceneHistory.Record(currentScene);
        }
    }

    public static void LoadPreviousScene()
    {
        var targetScene = rollbackConfig.defaultSceneName;
        if (sceneHistory.HasHistory())
        {
            targetScene = sceneHistory.GetPrevious();
        }
        else
        {
            var currentScene = SceneManager.GetActiveScene().name;
            targetScene = rollbackConfig.GetRollbackScene(currentScene);
        }
        
        Load_Scene(targetScene);
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