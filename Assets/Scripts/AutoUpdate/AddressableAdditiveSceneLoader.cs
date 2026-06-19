using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    [SerializeField] private bool loadScenesDirectly = true;
    [SerializeField] private int maxConcurrentSceneLoads = 2;
    [SerializeField] private bool loadCachedScenesWithoutDelay = true;
    [SerializeField] private int cachedMaxConcurrentSceneLoads = 8;
    [SerializeField] private float cachedDelayBetweenScenesSeconds = 0f;
    [SerializeField] private float cachedDependencyCheckTimeoutSeconds = 3f;
    [SerializeField] private bool downloadDependenciesTogether = true;
    [SerializeField] private bool loadSceneAsSoonAsDependenciesReady = true;
    [SerializeField] private bool showBlockingOverlayUntilLoaded = true;
    [SerializeField] private string blockingOverlayText = "Đang dựng thế giới...";
    [SerializeField] private float minimumOverlaySeconds = 0.1f;
    [SerializeField] private bool controlBoxLoad = true;
    [SerializeField] private string boxLoadObjectName = "boxLoad";
    [SerializeField] private bool boxLoadOnlyOnMobile = true;
    [SerializeField] private bool showBoxLoadOnlyWhenDependenciesMissing = true;
    [SerializeField] private List<string> sceneKeys = new List<string>();

    private bool _isLoading;
    private bool _loadStarted;
    private bool _loadComplete;
    private bool _cacheStateKnown;
    private bool _allDependenciesCached;
    private int _totalSceneCount;
    private int _loadedSceneCount;
    private int _failedSceneCount;
    private float _overlayShownAt;
    private GameObject _blockingOverlayRoot;
    private GameObject _boxLoadObject;
    private bool _boxLoadRequestedVisible;
    private bool _boxLoadWarningLogged;
    private Scene _ownerScene;

    public bool IsLoading => _isLoading;
    public bool IsComplete => _loadComplete && !_isLoading;
    public int TotalSceneCount => _totalSceneCount;
    public int LoadedSceneCount => _loadedSceneCount;
    public int FailedSceneCount => _failedSceneCount;
    public bool CacheStateKnown => _cacheStateKnown;
    public bool AllDependenciesCached => _allDependenciesCached;

    public float Progress01
    {
        get
        {
            if (_totalSceneCount <= 0)
                return IsComplete ? 1f : 0f;

            return Mathf.Clamp01((float)(_loadedSceneCount + _failedSceneCount) / _totalSceneCount);
        }
    }

#if ADDRESSABLES
    private readonly List<AsyncOperationHandle<SceneInstance>> _loadedSceneHandles =
        new List<AsyncOperationHandle<SceneInstance>>();
#endif

    private void Start()
    {
        if (loadOnStart)
            BeginLoad();
    }

    public void BeginLoad()
    {
        if (_loadComplete)
        {
            SetBoxLoadVisible(false);
            return;
        }

        if (_loadStarted || _isLoading)
            return;

        _loadStarted = true;

        if (showBlockingOverlayUntilLoaded)
            ShowBlockingOverlay();

        StartCoroutine(LoadScenesRoutine());
    }

    public IEnumerator WaitUntilFinished()
    {
        BeginLoad();

        while (!IsComplete)
            yield return null;
    }

    private IEnumerator LoadScenesRoutine()
    {
        if (_isLoading)
            yield break;

        _isLoading = true;
        _loadComplete = false;
        _ownerScene = gameObject.scene;
        EnsureOwnerSceneActive();

#if ADDRESSABLES
        List<string> keys = BuildUniquePendingSceneKeys();
        BeginTracking(keys.Count);

        if (keys.Count == 0)
        {
            FinishLoad();
            yield break;
        }

        if (loadScenesDirectly)
        {
            yield return LoadScenesDirectlyRoutine(keys);
            FinishLoad();
            yield break;
        }

        if (downloadDependenciesTogether)
        {
            PrepareBoxLoadForPendingContent(keys.Count, false);
            yield return DownloadAndLoadTogetherRoutine(keys);
            FinishLoad();
            yield break;
        }
#endif

        if (initialDelaySeconds > 0f)
            yield return new WaitForSecondsRealtime(initialDelaySeconds);

#if ADDRESSABLES
        PrepareBoxLoadForPendingContent(keys.Count, false);

        for (int i = 0; i < keys.Count; i++)
        {
            RestoreRequestedBoxLoadVisibility();
            yield return LoadSingleSceneRoutine(keys[i]);

            if (delayBetweenScenesSeconds > 0f && i < keys.Count - 1)
                yield return new WaitForSecondsRealtime(delayBetweenScenesSeconds);
        }
#else
        Debug.LogWarning("[LateSceneLoader] ADDRESSABLES define is missing; late scenes cannot be loaded.");
#endif

        FinishLoad();
    }

    private void BeginTracking(int totalSceneCount)
    {
        _totalSceneCount = Mathf.Max(0, totalSceneCount);
        _loadedSceneCount = 0;
        _failedSceneCount = 0;
    }

    private void MarkSceneLoadFinished(bool success)
    {
        if (success)
            _loadedSceneCount++;
        else
            _failedSceneCount++;
    }

    private void FinishLoad()
    {
        EnsureOwnerSceneActive();
        SetBoxLoadVisible(false);

        _isLoading = false;
        _loadComplete = true;

        StartCoroutine(HideBlockingOverlayWhenReady());

        Debug.Log("[LateSceneLoader] Late scene loading finished. loaded="
                  + _loadedSceneCount
                  + "/"
                  + _totalSceneCount
                  + ", failed="
                  + _failedSceneCount);
    }

    private IEnumerator HideBlockingOverlayWhenReady()
    {
        if (_blockingOverlayRoot == null)
            yield break;

        float remaining = minimumOverlaySeconds - (Time.unscaledTime - _overlayShownAt);

        if (remaining > 0f)
            yield return new WaitForSecondsRealtime(remaining);

        if (_blockingOverlayRoot != null)
            Destroy(_blockingOverlayRoot);

        _blockingOverlayRoot = null;
    }

    private void PrepareBoxLoadForPendingContent(int pendingSceneCount, bool allDependenciesCached)
    {
        if (!controlBoxLoad)
            return;

        if (pendingSceneCount <= 0)
        {
            SetBoxLoadVisible(false);
            return;
        }

        if (!ShouldShowBoxLoadOnThisPlatform())
        {
            SetBoxLoadVisible(false);
            return;
        }

        if (showBoxLoadOnlyWhenDependenciesMissing && allDependenciesCached)
        {
            SetBoxLoadVisible(false);
            return;
        }

        SetBoxLoadVisible(true);
    }

    private bool ShouldShowBoxLoadOnThisPlatform()
    {
        RuntimePlatform platform = Application.platform;

        if (platform == RuntimePlatform.Android || platform == RuntimePlatform.IPhonePlayer)
            return true;

        return !boxLoadOnlyOnMobile;
    }

    private void SetBoxLoadVisible(bool visible)
    {
        if (!controlBoxLoad)
            return;

        _boxLoadRequestedVisible = visible;

        GameObject boxLoad = ResolveBoxLoadObject(visible);

        if (boxLoad == null)
            return;

        if (boxLoad.activeSelf == visible)
            return;

        boxLoad.SetActive(visible);

        Debug.Log("[LateSceneLoader] " + (visible ? "Enabled" : "Disabled") + " boxLoad.");
    }

    private void RestoreRequestedBoxLoadVisibility()
    {
        if (_boxLoadRequestedVisible)
            SetBoxLoadVisible(true);
    }

    private GameObject ResolveBoxLoadObject(bool logMissing)
    {
        if (_boxLoadObject != null)
            return _boxLoadObject;

        if (string.IsNullOrWhiteSpace(boxLoadObjectName))
            return null;

        if (!_ownerScene.IsValid() || !_ownerScene.isLoaded)
            _ownerScene = gameObject.scene;

        if (!_ownerScene.IsValid() || !_ownerScene.isLoaded)
            return null;

        foreach (GameObject root in _ownerScene.GetRootGameObjects())
        {
            if (string.Equals(root.name, boxLoadObjectName, StringComparison.Ordinal))
            {
                _boxLoadObject = root;
                return _boxLoadObject;
            }

            Transform child = FindChildRecursive(root.transform, boxLoadObjectName);

            if (child != null)
            {
                _boxLoadObject = child.gameObject;
                return _boxLoadObject;
            }
        }

        if (logMissing && !_boxLoadWarningLogged)
        {
            _boxLoadWarningLogged = true;
            Debug.LogWarning("[LateSceneLoader] boxLoad object not found in scene: " + _ownerScene.name);
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (string.Equals(child.name, childName, StringComparison.Ordinal))
                return child;

            Transform match = FindChildRecursive(child, childName);

            if (match != null)
                return match;
        }

        return null;
    }

    private void ShowBlockingOverlay()
    {
        if (_blockingOverlayRoot != null)
            return;

        _overlayShownAt = Time.unscaledTime;

        _blockingOverlayRoot = new GameObject("[New Scene Content Loading Overlay]");
        SceneManager.MoveGameObjectToScene(_blockingOverlayRoot, gameObject.scene);

        Canvas canvas = _blockingOverlayRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        CanvasScaler scaler = _blockingOverlayRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        _blockingOverlayRoot.AddComponent<GraphicRaycaster>();

        GameObject backgroundObject = new GameObject("Background", typeof(RectTransform), typeof(Image));
        backgroundObject.transform.SetParent(_blockingOverlayRoot.transform, false);

        RectTransform backgroundRect = backgroundObject.GetComponent<RectTransform>();
        backgroundRect.anchorMin = Vector2.zero;
        backgroundRect.anchorMax = Vector2.one;
        backgroundRect.offsetMin = Vector2.zero;
        backgroundRect.offsetMax = Vector2.zero;

        Image background = backgroundObject.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.92f);
        background.raycastTarget = true;

        GameObject textObject = new GameObject("Status", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(_blockingOverlayRoot.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.1f, 0.45f);
        textRect.anchorMax = new Vector2(0.9f, 0.55f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textObject.GetComponent<Text>();
        text.text = blockingOverlayText;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.fontSize = 34;
        text.raycastTarget = false;

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (font != null)
            text.font = font;
    }

    private void EnsureOwnerSceneActive()
    {
        if (!_ownerScene.IsValid() || !_ownerScene.isLoaded)
            _ownerScene = gameObject.scene;

        if (!_ownerScene.IsValid() || !_ownerScene.isLoaded)
            return;

        Scene activeScene = SceneManager.GetActiveScene();

        if (!activeScene.IsValid() || activeScene != _ownerScene)
            SceneManager.SetActiveScene(_ownerScene);
    }

#if ADDRESSABLES
    private IEnumerator LoadScenesDirectlyRoutine(List<string> keys)
    {
        if (initialDelaySeconds > 0f)
            yield return new WaitForSecondsRealtime(initialDelaySeconds);

        bool allDependenciesCached = false;

        if (loadCachedScenesWithoutDelay)
        {
            _cacheStateKnown = false;
            _allDependenciesCached = false;

            yield return CheckAllDependenciesCached(keys, result =>
            {
                allDependenciesCached = result;
                _allDependenciesCached = result;
                _cacheStateKnown = true;
            });
        }
        else
        {
            _cacheStateKnown = true;
            _allDependenciesCached = false;
        }

        int nextIndex = 0;
        int maxConcurrent = Mathf.Max(1, allDependenciesCached ? cachedMaxConcurrentSceneLoads : maxConcurrentSceneLoads);
        float sceneDelay = Mathf.Max(0f, allDependenciesCached ? cachedDelayBetweenScenesSeconds : delayBetweenScenesSeconds);
        List<RunningSceneLoad> running = new List<RunningSceneLoad>(maxConcurrent);

        if (allDependenciesCached)
            Debug.Log("[LateSceneLoader] All late scene dependencies are cached. Loading cached scenes immediately.");

        PrepareBoxLoadForPendingContent(keys.Count, allDependenciesCached);

        while (nextIndex < keys.Count || running.Count > 0)
        {
            RestoreRequestedBoxLoadVisibility();

            while (nextIndex < keys.Count && running.Count < maxConcurrent)
            {
                string key = keys[nextIndex++];

                if (IsSceneAlreadyLoaded(key))
                {
                    EnsureOwnerSceneActive();
                    MarkSceneLoadFinished(true);
                    continue;
                }

                Debug.Log("[LateSceneLoader] Start additive scene load: " + key);

                running.Add(new RunningSceneLoad
                {
                    Key = key,
                    Handle = Addressables.LoadSceneAsync(key, LoadSceneMode.Additive, true)
                });
            }

            bool completedAny = false;

            for (int i = running.Count - 1; i >= 0; i--)
            {
                RunningSceneLoad load = running[i];

                if (!load.Handle.IsValid() || !load.Handle.IsDone)
                    continue;

                if (load.Handle.Status == AsyncOperationStatus.Succeeded)
                {
                    Debug.Log("[LateSceneLoader] Additive scene loaded: " + load.Key);
                    _loadedSceneHandles.Add(load.Handle);
                    EnsureOwnerSceneActive();
                    MarkSceneLoadFinished(true);
                }
                else
                {
                    string error = load.Handle.OperationException != null
                        ? load.Handle.OperationException.ToString()
                        : load.Handle.Status.ToString();

                    Debug.LogError("[LateSceneLoader] Failed to load additive scene key=" + load.Key + ", error=" + error);
                    MarkSceneLoadFinished(false);
                }

                running.RemoveAt(i);
                completedAny = true;
            }

            if (completedAny && sceneDelay > 0f)
                yield return new WaitForSecondsRealtime(sceneDelay);
            else
                yield return null;
        }
    }

    private IEnumerator CheckAllDependenciesCached(List<string> keys, Action<bool> onDone)
    {
        if (keys == null || keys.Count == 0)
        {
            onDone?.Invoke(false);
            yield break;
        }

        List<AsyncOperationHandle<long>> handles = new List<AsyncOperationHandle<long>>(keys.Count);

        for (int i = 0; i < keys.Count; i++)
        {
            try
            {
                handles.Add(Addressables.GetDownloadSizeAsync(keys[i]));
            }
            catch (Exception e)
            {
                Debug.LogWarning("[LateSceneLoader] Cannot check cached size for " + keys[i] + ": " + e.Message);
                ReleaseSizeHandles(handles);
                onDone?.Invoke(false);
                yield break;
            }
        }

        bool waiting = true;
        float timer = 0f;

        while (waiting)
        {
            timer += Time.unscaledDeltaTime;
            waiting = false;

            for (int i = 0; i < handles.Count; i++)
            {
                if (handles[i].IsValid() && !handles[i].IsDone)
                {
                    waiting = true;
                    break;
                }
            }

            if (cachedDependencyCheckTimeoutSeconds > 0f &&
                timer >= cachedDependencyCheckTimeoutSeconds)
            {
                Debug.LogWarning("[LateSceneLoader] Cached dependency check timed out. Loading with normal pacing.");
                ReleaseSizeHandles(handles);
                onDone?.Invoke(false);
                yield break;
            }

            if (waiting)
                yield return null;
        }

        long remainingBytes = 0L;
        bool failed = false;

        for (int i = 0; i < handles.Count; i++)
        {
            AsyncOperationHandle<long> handle = handles[i];

            if (!handle.IsValid() || handle.Status != AsyncOperationStatus.Succeeded)
            {
                failed = true;
                continue;
            }

            remainingBytes += Math.Max(0L, handle.Result);
        }

        ReleaseSizeHandles(handles);

        if (failed)
        {
            onDone?.Invoke(false);
            yield break;
        }

        onDone?.Invoke(remainingBytes <= 0L);
    }

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
            RestoreRequestedBoxLoadVisibility();

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
                    EnsureOwnerSceneActive();
                    MarkSceneLoadFinished(true);
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
        {
            EnsureOwnerSceneActive();
            MarkSceneLoadFinished(true);
            yield break;
        }

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
            MarkSceneLoadFinished(false);
            yield break;
        }

        _loadedSceneHandles.Add(handle);
        EnsureOwnerSceneActive();
        MarkSceneLoadFinished(true);
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

    private sealed class RunningSceneLoad
    {
        public string Key;
        public AsyncOperationHandle<SceneInstance> Handle;
    }

    private static void ReleaseSizeHandles(List<AsyncOperationHandle<long>> handles)
    {
        for (int i = 0; i < handles.Count; i++)
        {
            AsyncOperationHandle<long> handle = handles[i];

            if (!handle.IsValid())
                continue;

            Addressables.Release(handle);
        }
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
