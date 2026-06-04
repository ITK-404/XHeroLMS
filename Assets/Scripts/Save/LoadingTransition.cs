using System;
using System.Collections;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
#endif

public static class LoadingTransition
{
    public static string TargetSceneName;
    public static string PreviousSceneName;

    /// <summary>
    /// True nếu target scene là Addressables scene.
    /// LoadingScreenController sẽ dựa vào flag này để load Addressables hoặc Build Scene.
    /// </summary>
    public static bool UseAddressables;

    /// <summary>
    /// Key dùng để prepare Addressables.
    /// Mặc định bằng TargetSceneName.
    /// Không truyền "cloud" nếu cloud đang gắn nhiều scene.
    /// </summary>
    public static string TargetPrepareKey;

    /// <summary>
    /// True sau khi AddressablesPreload prepare xong scene hiện tại.
    /// </summary>
    public static bool TargetAddressablesPrepared { get; private set; }

    public static bool IsPreparingTargetAddressables { get; private set; }
    public static bool HasPrepareFailed { get; private set; }
    public static string LastPrepareError { get; private set; } = "";

    private static SceneNavigationHistory sceneHistory;
    private static SceneLocationHandler sceneLocationHandler;
    private static RollbackConfig rollbackConfig;

    private static MonoBehaviour runner;
    private static Coroutine _coroutine;

    public static Action OnLoadSceneEvent;

#if ADDRESSABLES
    private static AsyncOperationHandle<RollbackConfig>? rollbackConfigHandle;
#endif

    public static async UniTaskVoid Init(
        MonoBehaviour _runner,
        SceneNavigationHistory sceneNavigationHistory,
        SceneLocationHandler _sceneLocationHandler,
        CancellationToken token)
    {
        runner = _runner;
        sceneHistory = sceneNavigationHistory;
        sceneLocationHandler = _sceneLocationHandler;

#if ADDRESSABLES
        try
        {
            var handle = Addressables.LoadAssetAsync<RollbackConfig>("Rollback Scene Config");
            rollbackConfigHandle = handle;

            RollbackConfig result = await handle.WithCancellation(token);

            await UniTask.SwitchToMainThread();

            if (result != null)
            {
                rollbackConfig = result;
                Debug.Log("[LoadingTransition] Init Complete. RollbackConfig loaded.");
            }
            else
            {
                Debug.LogWarning("[LoadingTransition] RollbackConfig result is null.");
            }
        }
        catch (OperationCanceledException)
        {
            Debug.LogWarning("[LoadingTransition] Init canceled.");
        }
        catch (Exception e)
        {
            Debug.LogError("[LoadingTransition] Init failed: " + e);
        }
#else
        Debug.LogWarning("[LoadingTransition] ADDRESSABLES define is OFF. RollbackConfig cannot be loaded from Addressables.");
        await UniTask.SwitchToMainThread();
#endif
    }

    // ============================================================
    // PUBLIC LOAD API
    // ============================================================

    /// <summary>
    /// API chính cho tất cả UI gọi.
    /// Hàm này tự check scene là Addressable hay Build Scene.
    /// </summary>
    public static void Load_Scene(string targetScene, bool isSaveHistory = true)
    {
        if (string.IsNullOrWhiteSpace(targetScene))
        {
            Debug.LogError("[LoadingTransition] Load_Scene failed: targetScene is empty.");
            return;
        }

        if (runner == null)
        {
            Debug.LogError("[LoadingTransition] runner is null. Did you call LoadingTransition.Init?");
            return;
        }

        if (_coroutine != null)
        {
            Debug.Log("[LoadingTransition] Stop previous loading coroutine.");
            runner.StopCoroutine(_coroutine);
            _coroutine = null;
        }

        _coroutine = runner.StartCoroutine(CoLoadScene(targetScene.Trim(), isSaveHistory));

        Debug.Log("[LoadingTransition] Load_Scene requested: " + targetScene);
        OnLoadSceneEvent?.Invoke();
    }

    public static void LoadPreviousSceneOrDefault()
    {
        Debug.Log("[LoadingTransition] Load Previous Scene");

        string targetScene = "";

        if (rollbackConfig != null)
            targetScene = rollbackConfig.defaultSceneName;

        if (sceneHistory != null && sceneHistory.HasHistory())
        {
            targetScene = sceneHistory.GetPrevious();
        }
        else
        {
            string currentScene = SceneManager.GetActiveScene().name;

            if (rollbackConfig != null)
                targetScene = rollbackConfig.GetRollbackScene(currentScene);
        }

        if (string.IsNullOrWhiteSpace(targetScene))
        {
            Debug.LogError("[LoadingTransition] Cannot resolve previous/default scene.");
            return;
        }

        Load_Scene(targetScene, false);
    }

    public static void SavePosition(Vector3 position, Quaternion rotation)
    {
        if (sceneLocationHandler == null)
        {
            Debug.LogWarning("[LoadingTransition] sceneLocationHandler is null. Cannot save position.");
            return;
        }

        sceneLocationHandler.SavePlayerPosition(position, rotation);
    }

    // ============================================================
    // CORE FLOW
    // ============================================================

    private static IEnumerator CoLoadScene(string targetScene, bool isSaveHistory)
    {
        ResetTargetPrepareState();

        bool isCloudScene = false;

#if ADDRESSABLES
        Debug.Log("[LoadingTransition] Start checking Addressables scene: " + targetScene);
        yield return CoCheckIsCloudScene(targetScene, r => isCloudScene = r);
#else
        isCloudScene = false;
#endif

        PreviousSceneName = SceneManager.GetActiveScene().name;
        TargetSceneName = targetScene;
        TargetPrepareKey = targetScene;
        UseAddressables = isCloudScene;

        Debug.Log(
            $"[LoadingTransition] Scene resolved. " +
            $"target={TargetSceneName}, previous={PreviousSceneName}, useAddressables={UseAddressables}, prepareKey={TargetPrepareKey}"
        );

        if (isSaveHistory && sceneHistory != null)
        {
            string currentScene = SceneManager.GetActiveScene().name;

            Debug.Log("[LoadingTransition] Save scene to history: " + currentScene);
            sceneHistory.Record(currentScene);
        }

        // Không prepare ở đây để tránh đứng ở scene cũ.
        // LoadingScene sẽ hiện trước, rồi LoadingScreenController gọi PrepareTargetAddressablesRoutine().
        SceneManager.LoadScene("LoadingScene", LoadSceneMode.Additive);

        Debug.Log("[LoadingTransition] LoadingScene opened.");
        _coroutine = null;
    }

    // ============================================================
    // ADDRESSABLES PREPARE API
    // LoadingScreenController gọi hàm này trước khi LoadSceneAsync.
    // ============================================================

    public static IEnumerator PrepareTargetAddressablesRoutine()
    {
#if ADDRESSABLES
        if (!UseAddressables)
            yield break;

        if (TargetAddressablesPrepared)
            yield break;

        if (string.IsNullOrWhiteSpace(TargetPrepareKey))
        {
            MarkPrepareFailed("TargetPrepareKey is empty.");
            yield break;
        }

        if (AddressablesPreload.Instance == null)
        {
            MarkPrepareFailed("AddressablesPreload.Instance is null.");
            yield break;
        }

        IsPreparingTargetAddressables = true;
        HasPrepareFailed = false;
        LastPrepareError = "";

        Debug.Log(
            $"[LoadingTransition] Prepare target addressables. " +
            $"scene={TargetSceneName}, key={TargetPrepareKey}"
        );

        yield return AddressablesPreload.Instance.PrepareAddressableKeyRoutine(TargetPrepareKey);

        if (AddressablesPreload.Instance.HasFailed)
        {
            MarkPrepareFailed(AddressablesPreload.Instance.LastError);
            yield break;
        }

        TargetAddressablesPrepared = true;
        IsPreparingTargetAddressables = false;

        Debug.Log("[LoadingTransition] Prepare target addressables DONE: " + TargetPrepareKey);
#else
        if (UseAddressables)
            MarkPrepareFailed("ADDRESSABLES define is OFF.");

        yield break;
#endif
    }

#if ADDRESSABLES
    public static AsyncOperationHandle<SceneInstance> LoadAddressableAsync()
    {
        if (!TargetAddressablesPrepared)
        {
            Debug.LogWarning(
                "[LoadingTransition] LoadAddressableAsync called before PrepareTargetAddressablesRoutine completed. " +
                "It may still download during LoadSceneAsync."
            );
        }

        Debug.Log($"[LoadingTransition] Addressables.LoadSceneAsync --> {TargetSceneName}");

        return Addressables.LoadSceneAsync(
            TargetSceneName,
            LoadSceneMode.Single,
            activateOnLoad: true
        );
    }

    public static AsyncOperationHandle<SceneInstance> LoadAddressableAsync(bool activateOnLoad)
    {
        if (!TargetAddressablesPrepared)
        {
            Debug.LogWarning(
                "[LoadingTransition] LoadAddressableAsync called before PrepareTargetAddressablesRoutine completed. " +
                "It may still download during LoadSceneAsync."
            );
        }

        Debug.Log($"[LoadingTransition] Addressables.LoadSceneAsync --> {TargetSceneName}, activateOnLoad={activateOnLoad}");

        return Addressables.LoadSceneAsync(
            TargetSceneName,
            LoadSceneMode.Single,
            activateOnLoad
        );
    }
#endif

    // ============================================================
    // ADDRESSABLES CHECK
    // ============================================================

#if ADDRESSABLES
    private static IEnumerator CoCheckIsCloudScene(string sceneKeyOrName, Action<bool> result)
    {
        if (string.IsNullOrWhiteSpace(sceneKeyOrName))
        {
            result?.Invoke(false);
            yield break;
        }

        AsyncOperationHandle<IList<UnityEngine.ResourceManagement.ResourceLocations.IResourceLocation>> h =
            Addressables.LoadResourceLocationsAsync(sceneKeyOrName, typeof(SceneInstance));

        yield return h;

        bool ok = false;

        if (h.IsValid() &&
            h.Status == AsyncOperationStatus.Succeeded &&
            h.Result != null &&
            h.Result.Count > 0)
        {
            ok = true;
        }
        else
        {
            // Fallback: một số setup không resolve được với typeof(SceneInstance),
            // nên check lại không filter type.
            if (h.IsValid())
                Addressables.Release(h);

            var h2 = Addressables.LoadResourceLocationsAsync(sceneKeyOrName);
            yield return h2;

            ok = h2.IsValid() &&
                 h2.Status == AsyncOperationStatus.Succeeded &&
                 h2.Result != null &&
                 h2.Result.Count > 0;

            if (h2.IsValid())
                Addressables.Release(h2);

            result?.Invoke(ok);
            yield break;
        }

        if (h.IsValid())
            Addressables.Release(h);

        result?.Invoke(ok);
    }
#endif

    // ============================================================
    // STATE HELPERS
    // ============================================================

    private static void ResetTargetPrepareState()
    {
        TargetAddressablesPrepared = false;
        IsPreparingTargetAddressables = false;
        HasPrepareFailed = false;
        LastPrepareError = "";
        TargetPrepareKey = "";
    }

    private static void MarkPrepareFailed(string error)
    {
        TargetAddressablesPrepared = false;
        IsPreparingTargetAddressables = false;
        HasPrepareFailed = true;
        LastPrepareError = string.IsNullOrWhiteSpace(error) ? "Unknown Addressables prepare error." : error;

        Debug.LogError("[LoadingTransition] Prepare failed: " + LastPrepareError);
    }
}