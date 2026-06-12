using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
#endif

[DisallowMultipleComponent]
public sealed class AddressableAdditiveSceneLoader : MonoBehaviour
{
    [SerializeField] private bool loadOnStart = true;
    [SerializeField] private float initialDelaySeconds = 0.75f;
    [SerializeField] private float delayBetweenScenesSeconds = 0.1f;
    [SerializeField] private bool downloadDependenciesTogether = true;
    [SerializeField] private bool loadSceneAsSoonAsDependenciesReady = true;
    [SerializeField] private List<string> sceneKeys = new List<string>();

    private bool _isLoading;

#if ADDRESSABLES
    private readonly List<AsyncOperationHandle<SceneInstance>> _loadedSceneHandles =
        new List<AsyncOperationHandle<SceneInstance>>();
#endif

    private IEnumerator Start()
    {
        if (loadOnStart)
            yield return LoadScenesRoutine();
    }

    public void BeginLoad()
    {
        if (!_isLoading)
            StartCoroutine(LoadScenesRoutine());
    }

    private IEnumerator LoadScenesRoutine()
    {
        if (_isLoading)
            yield break;

        _isLoading = true;

#if ADDRESSABLES
        List<string> keys = BuildUniquePendingSceneKeys();

        if (keys.Count == 0)
        {
            _isLoading = false;
            yield break;
        }

        if (downloadDependenciesTogether)
        {
            yield return DownloadAndLoadTogetherRoutine(keys);
            _isLoading = false;
            yield break;
        }
#endif

        if (initialDelaySeconds > 0f)
            yield return new WaitForSecondsRealtime(initialDelaySeconds);

#if ADDRESSABLES
        for (int i = 0; i < keys.Count; i++)
        {
            yield return LoadSingleSceneRoutine(keys[i]);

            if (delayBetweenScenesSeconds > 0f && i < keys.Count - 1)
                yield return new WaitForSecondsRealtime(delayBetweenScenesSeconds);
        }
#else
        Debug.LogWarning("[LateSceneLoader] ADDRESSABLES define is missing; late scenes cannot be loaded.");
#endif

        _isLoading = false;
    }

#if ADDRESSABLES
    private IEnumerator DownloadAndLoadTogetherRoutine(List<string> keys)
    {
        List<LateSceneState> states = new List<LateSceneState>(keys.Count);

        for (int i = 0; i < keys.Count; i++)
        {
            LateSceneState state = new LateSceneState { Key = keys[i] };
            state.DownloadHandle = Addressables.DownloadDependenciesAsync(state.Key, autoReleaseHandle: false);
            states.Add(state);

            Debug.Log("[LateSceneLoader] Start dependency download: " + state.Key);
        }

        if (initialDelaySeconds > 0f)
            yield return new WaitForSecondsRealtime(initialDelaySeconds);

        while (states.Any(s => !s.LoadFinished))
        {
            bool startedSceneLoad = false;

            for (int i = 0; i < states.Count; i++)
            {
                LateSceneState state = states[i];

                if (state.LoadFinished)
                    continue;

                if (IsSceneAlreadyLoaded(state.Key))
                {
                    ReleaseDownloadHandle(state);
                    state.LoadFinished = true;
                    continue;
                }

                if (!state.DownloadFinished)
                    UpdateDownloadState(state);

                if (!state.DownloadFinished)
                    continue;

                if (!loadSceneAsSoonAsDependenciesReady && states.Any(s => !s.DownloadFinished))
                    continue;

                yield return LoadSingleSceneRoutine(state.Key);

                state.LoadFinished = true;
                startedSceneLoad = true;

                if (delayBetweenScenesSeconds > 0f)
                    yield return new WaitForSecondsRealtime(delayBetweenScenesSeconds);

                break;
            }

            if (!startedSceneLoad)
                yield return null;
        }

        for (int i = 0; i < states.Count; i++)
            ReleaseDownloadHandle(states[i]);
    }

    private void UpdateDownloadState(LateSceneState state)
    {
        if (!state.DownloadHandle.IsValid())
        {
            state.DownloadFinished = true;
            Debug.LogWarning("[LateSceneLoader] Dependency download handle invalid, will try scene load directly: " + state.Key);
            return;
        }

        if (!state.DownloadHandle.IsDone)
            return;

        state.DownloadFinished = true;

        if (state.DownloadHandle.Status == AsyncOperationStatus.Succeeded)
        {
            Debug.Log("[LateSceneLoader] Dependency download ready: " + state.Key);
            return;
        }

        string error = state.DownloadHandle.OperationException != null
            ? state.DownloadHandle.OperationException.ToString()
            : state.DownloadHandle.Status.ToString();

        Debug.LogWarning("[LateSceneLoader] Dependency download failed, will try scene load directly. key="
                         + state.Key + ", error=" + error);
    }

    private IEnumerator LoadSingleSceneRoutine(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            yield break;

        key = key.Trim();

        if (IsSceneAlreadyLoaded(key))
            yield break;

        Debug.Log("[LateSceneLoader] Loading additive addressable scene: " + key);

        AsyncOperationHandle<SceneInstance> handle =
            Addressables.LoadSceneAsync(key, LoadSceneMode.Additive, true);

        yield return handle;

        if (!handle.IsValid() || handle.Status != AsyncOperationStatus.Succeeded)
        {
            string error = handle.OperationException != null
                ? handle.OperationException.ToString()
                : "Unknown error";

            Debug.LogError("[LateSceneLoader] Failed to load additive scene key=" + key + ", error=" + error);
            yield break;
        }

        _loadedSceneHandles.Add(handle);
    }

    private List<string> BuildUniquePendingSceneKeys()
    {
        List<string> keys = new List<string>();
        HashSet<string> seen = new HashSet<string>();

        for (int i = 0; i < sceneKeys.Count; i++)
        {
            string key = sceneKeys[i];

            if (string.IsNullOrWhiteSpace(key))
                continue;

            key = key.Trim();

            if (IsSceneAlreadyLoaded(key))
                continue;

            if (seen.Add(key))
                keys.Add(key);
        }

        return keys;
    }

    private static void ReleaseDownloadHandle(LateSceneState state)
    {
        if (state.DownloadReleased)
            return;

        state.DownloadReleased = true;

        if (!state.DownloadHandle.IsValid())
            return;

        Addressables.Release(state.DownloadHandle);
    }

    private sealed class LateSceneState
    {
        public string Key;
        public AsyncOperationHandle DownloadHandle;
        public bool DownloadFinished;
        public bool DownloadReleased;
        public bool LoadFinished;
    }
#endif

    private static bool IsSceneAlreadyLoaded(string sceneNameOrKey)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);

            if (!scene.isLoaded)
                continue;

            if (scene.name == sceneNameOrKey)
                return true;

            if (scene.path.EndsWith("/" + sceneNameOrKey + ".unity"))
                return true;
        }

        return false;
    }
}
