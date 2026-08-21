using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;

#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
#endif

[DefaultExecutionOrder(-20000)]
public class BootFlow : MonoBehaviour
{
    public static BootFlow Instance { get; private set; }

    [Header("Bootstrap References")]
    public AddressablesPreload preload;
    public IntroManager intro;

    [Header("Main Scene")]
    public bool mainSceneIsAddressable = true;

    // Nếu mainSceneIsAddressable=true -> key scene Addressables mặc định khi không có saved session
    string mainAddressableSceneKey = SceneNameAliases.NewSceneAddress;
    private const string NewSceneFirstLateSceneKey = "New Scene Late 01";

    [Tooltip("Nếu mainSceneIsAddressable=false -> build index của Scene main")]
    public int mainSceneBuildIndex = 1;

    [Header("Behavior")]
    public bool allowEnterMainWhenPreloadFailed = true;
    public float minHoldBeforeEnterMain = 0.05f;

    // Nếu chưa có UserID nhưng chỉ có 1 save hợp lệ trên máy, cho phép load thẳng save đó.
    bool allowSingleSaveFallbackWhenNoUserID = true;
    bool restoreTokenStoreFromDiskBeforeResolveSave = true;

    private bool _loadingMain;
    private bool _waitingForNetworkRecovery;
    private Coroutine _networkRecoveryRoutine;
    private string _resolvedMainSceneKey;
    private bool _triedRestoreTokenStoreFromDisk;

    // Lưu full session data để BootFlow gọi GameSessionHandler.LoadGameSessionData2(data)
    private GameSessionData _resolvedSessionData;

    [Header("Game Session")]
    private GameSessionHandler gameSessionHandler;
    private bool waitSavedSessionDataBeforeLoadScene = true;
    private float savedSessionDataTimeoutSeconds = 10f;
    private float introEnterMainGateTimeoutSeconds = 12f;
    private bool prepareNewSceneLateContentBeforeEnter = true;

#if ADDRESSABLES
    [Header("Scene Dependency Download Recovery")]
    private bool downloadSceneDependenciesInBootFlow = true;

    private long firstNewSceneLatePrepareBudgetBytes = 2L * 1024L * 1024L;

    private float firstNewSceneLateEntryGateTimeoutSeconds = 2f;

    private int sceneDownloadMaxRetries = 3;

    private float sceneDownloadRetryDelaySeconds = 1.5f;

    private float sceneDownloadTimeoutSeconds = 420f;

    private float sceneDownloadStallTimeoutSeconds = 60f;

    private bool clearUnityCacheTempOnSceneDownloadFail = true;

    private bool clearAllUnityCacheOnLastRetry = false;

    private bool tryLoadSceneDirectlyAfterDependencyFail = true;

    private long sceneVerifyThresholdBytes = 0;
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (preload == null)
            preload = AddressablesPreload.Instance;

        if (preload == null)
        {
            var go = new GameObject("[AddressablesPreload]");
            DontDestroyOnLoad(go);
            preload = go.AddComponent<AddressablesPreload>();
        }

        if (intro != null)
            intro.SetExternalPreload(preload);

        if (gameSessionHandler == null)
            gameSessionHandler = FindObjectOfType<GameSessionHandler>();

        if (gameSessionHandler == null && GameInitializer.Instance != null)
            gameSessionHandler = GameInitializer.Instance.EnsureGameSessionHandler();
    }

    private void Start()
    {
        StartCoroutine(CoBoot());
    }

    public bool NeedsNetworkRecovery
    {
        get
        {
            if (_waitingForNetworkRecovery)
                return true;

            return preload != null && preload.HasFailed;
        }
    }

    public void RetryAfterNetworkRestored()
    {
        if (_networkRecoveryRoutine != null)
            return;

        _networkRecoveryRoutine = StartCoroutine(CoRetryAfterNetworkRestored());
    }

    private IEnumerator CoRetryAfterNetworkRestored()
    {
        _waitingForNetworkRecovery = false;
        _loadingMain = false;

        if (intro != null)
            intro.ClearFatalFailAfterNetworkRestored();

        if (preload == null)
            preload = AddressablesPreload.Instance;

        if (preload != null && (!preload.IsReady || preload.HasFailed))
        {
            Debug.Log("[BootFlow] Network restored. Retrying Addressables preload before entering main.");
            preload.RequestRetry();

            yield return null;

            while (preload != null && !preload.IsReady && !preload.HasFailed)
            {
                if (intro != null)
                    intro.SetBootProgress01(Mathf.Clamp01(preload.DownloadPercent01));

                yield return null;
            }

            if (preload != null && preload.HasFailed)
            {
                Debug.LogWarning("[BootFlow] Network recovery retry failed. Waiting for the next online check. LastError=" + preload.LastError);
                _waitingForNetworkRecovery = true;
                _networkRecoveryRoutine = null;
                yield break;
            }
        }

        _networkRecoveryRoutine = null;
        EnterMain();
    }

    private IEnumerator CoBoot()
    {
        while (preload == null)
        {
            preload = AddressablesPreload.Instance;
            yield return null;
        }

        while (!preload.IsReady && !preload.HasFailed)
        {
            if (intro != null && preload != null)
            {
                float p = Mathf.Clamp01(preload.DownloadPercent01);
                intro.SetBootProgress01(p);
            }

            yield return null;
        }

        if (preload.HasFailed)
        {
            Debug.LogWarning("[BootFlow] Preload failed, but BootFlow will continue because allowEnterMainWhenPreloadFailed=" + allowEnterMainWhenPreloadFailed);
            Debug.LogWarning("[BootFlow] Preload LastError: " + preload.LastError);
        }

        if (preload.HasFailed && !allowEnterMainWhenPreloadFailed)
        {
            if (intro != null)
                intro.ShowFatalFail(preload.LastError);

            yield break;
        }

        if (intro != null)
        {
            intro.SetBootProgress01(preload != null ? preload.DownloadPercent01 : 0f);
        }

        if (minHoldBeforeEnterMain > 0f)
            yield return new WaitForSecondsRealtime(minHoldBeforeEnterMain);

        EnterMain();
    }

    public void EnterMain()
    {
        if (_loadingMain)
            return;

        _loadingMain = true;
        StartCoroutine(CoEnterMain());
    }

    private IEnumerator CoEnterMain()
    {
        if (intro != null)
            intro.OnAboutToEnterMain();

        _resolvedMainSceneKey = ResolveMainSceneKey();

        if (waitSavedSessionDataBeforeLoadScene && _resolvedSessionData != null)
        {
            bool dataReady = false;

            yield return CoPrepareSavedSessionDataBeforeSceneLoad(
                _resolvedSessionData,
                result => dataReady = result
            );

            if (!dataReady)
            {
                Debug.LogWarning(
                    "[BootFlow] GameSessionHandler.LoadGameSessionData2 failed or timeout, " +
                    "Nhưng cảnh đã lưu vẫn còn. Tiếp tục tải cảnh đã lưu: " + _resolvedMainSceneKey
                );
            }
        }

#if ADDRESSABLES
        if (mainSceneIsAddressable)
        {
            if (string.IsNullOrWhiteSpace(_resolvedMainSceneKey))
            {
                Debug.LogError("[BootFlow] Resolved scene key is empty.");

                if (intro != null)
                    intro.ShowFatalFail("Main scene key is empty.");

                _loadingMain = false;
                yield break;
            }

            Debug.Log("[BootFlow] Resolved addressable scene key: " + _resolvedMainSceneKey);

            bool lateWorldFullyCached = false;

            if (downloadSceneDependenciesInBootFlow)
            {
                bool dependencyReady = false;

                if (prepareNewSceneLateContentBeforeEnter &&
                    SceneNameAliases.IsNewSceneFamily(_resolvedMainSceneKey))
                {
                    yield return CoCheckNewSceneLateWorldCached(result => lateWorldFullyCached = result);
                }

                string[] dependencyKeys = BuildSceneDependencyPrepareKeys(_resolvedMainSceneKey, lateWorldFullyCached);

                yield return CoClampFirstNewScenePrepareKeys(
                    _resolvedMainSceneKey,
                    lateWorldFullyCached,
                    dependencyKeys,
                    result => dependencyKeys = result
                );

                yield return CoPrepareSceneDependenciesWithPreload(
                    dependencyKeys,
                    result => dependencyReady = result
                );

                if (!dependencyReady)
                {
                    Debug.LogError("[BootFlow] Scene/world dependency prepare failed.");

                    if (IsRecoverablePreloadFailure())
                    {
                        Debug.LogWarning("[BootFlow] Scene/world dependency failed because network/preload is not ready. Waiting for NetworkGameplayGuard recovery.");
                        _waitingForNetworkRecovery = true;
                        _loadingMain = false;
                        yield break;
                    }

                    if (tryLoadSceneDirectlyAfterDependencyFail)
                    {
                        Debug.LogWarning("[BootFlow] Trying LoadSceneAsync directly after dependency fail...");
                        Addressables.LoadSceneAsync(_resolvedMainSceneKey, LoadSceneMode.Single, true);
                        yield break;
                    }

                    if (intro != null)
                        intro.ShowFatalFail("Scene download failed.");

                    _loadingMain = false;
                    yield break;
                }
            }
            else
            {
                Debug.LogWarning("[BootFlow] downloadSceneDependenciesInBootFlow=false. Loading scene directly.");
            }

            if (ShouldLoadAddressableWorldBehindIntro(_resolvedMainSceneKey))
            {
                bool worldReady = false;

                yield return CoLoadAddressableWorldBehindIntro(
                    _resolvedMainSceneKey,
                    lateWorldFullyCached,
                    result => worldReady = result
                );

                if (!worldReady)
                {
                    if (intro != null)
                        intro.ShowFatalFail("World load failed.");

                    _loadingMain = false;
                }

                yield break;
            }

            if (intro != null)
            {
                intro.SetBootProgress01(1f, true);

                float introGateTimer = 0f;
                while (!intro.CanEnterMain)
                {
                    introGateTimer += Time.unscaledDeltaTime;

                    if (introEnterMainGateTimeoutSeconds > 0f &&
                        introGateTimer >= introEnterMainGateTimeoutSeconds)
                    {
                        Debug.LogWarning("[BootFlow] Intro gate timeout. Continue loading main scene: " + _resolvedMainSceneKey);
                        break;
                    }

                    yield return null;
                }
            }

            Debug.Log("[BootFlow] Loading scene once: " + _resolvedMainSceneKey);
            Addressables.LoadSceneAsync(_resolvedMainSceneKey, LoadSceneMode.Single, true);
            yield break;
        }
#endif

        Debug.Log("[BootFlow] Load main by BuildIndex: " + mainSceneBuildIndex);
        SceneManager.LoadScene(mainSceneBuildIndex, LoadSceneMode.Single);
        yield break;
    }

    private bool IsRecoverablePreloadFailure()
    {
        if (preload == null)
            return true;

        if (!preload.IsReady)
            return true;

        if (!preload.HasFailed)
            return false;

        string error = preload.LastError ?? "";

        if (string.IsNullOrWhiteSpace(error))
            return true;

        return error.IndexOf("network", StringComparison.OrdinalIgnoreCase) >= 0 ||
               error.IndexOf("internet", StringComparison.OrdinalIgnoreCase) >= 0 ||
               error.IndexOf("timeout", StringComparison.OrdinalIgnoreCase) >= 0 ||
               error.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0 ||
               error.IndexOf("download", StringComparison.OrdinalIgnoreCase) >= 0 ||
               error.IndexOf("http", StringComparison.OrdinalIgnoreCase) >= 0 ||
               error.IndexOf("catalog", StringComparison.OrdinalIgnoreCase) >= 0 ||
               error.IndexOf("probe", StringComparison.OrdinalIgnoreCase) >= 0 ||
               error.IndexOf("host", StringComparison.OrdinalIgnoreCase) >= 0 ||
               error.IndexOf("resolve", StringComparison.OrdinalIgnoreCase) >= 0 ||
               error.IndexOf("connection", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private IEnumerator CoPrepareSavedSessionDataBeforeSceneLoad(GameSessionData data, Action<bool> onDone)
    {
        if (data == null)
        {
            Debug.LogWarning("[BootFlow] Cannot prepare saved session data because data is null.");
            onDone?.Invoke(false);
            yield break;
        }

        if (gameSessionHandler == null)
            gameSessionHandler = FindObjectOfType<GameSessionHandler>();

        if (gameSessionHandler == null && GameInitializer.Instance != null)
            gameSessionHandler = GameInitializer.Instance.EnsureGameSessionHandler();

        if (gameSessionHandler == null)
        {
            Debug.LogWarning("[BootFlow] GameSessionHandler not found. Cannot call LoadGameSessionData2.");
            onDone?.Invoke(false);
            yield break;
        }

        bool done = false;
        bool result = false;
        Exception exception = null;

        Debug.Log("[BootFlow] Waiting GameSessionHandler.LoadGameSessionData2 before loading saved scene...");

        RunLoadGameSessionData2ForBootFlow(
            data,
            (success, ex) =>
            {
                result = success;
                exception = ex;
                done = true;
            }
        );

        float timer = 0f;

        while (!done)
        {
            timer += Time.unscaledDeltaTime;

            if (savedSessionDataTimeoutSeconds > 0f && timer >= savedSessionDataTimeoutSeconds)
            {
                Debug.LogWarning("[BootFlow] Timeout waiting GameSessionHandler.LoadGameSessionData2.");
                onDone?.Invoke(false);
                yield break;
            }

            yield return null;
        }

        if (exception != null)
        {
            Debug.LogError("[BootFlow] GameSessionHandler.LoadGameSessionData2 exception: " + exception);
            onDone?.Invoke(false);
            yield break;
        }

        Debug.Log("[BootFlow] GameSessionHandler.LoadGameSessionData2 finished. result=" + result);
        onDone?.Invoke(result);
    }

    private async UniTaskVoid RunLoadGameSessionData2ForBootFlow(GameSessionData data, Action<bool, Exception> onDone)
    {
        try
        {
            if (gameSessionHandler == null)
                gameSessionHandler = FindObjectOfType<GameSessionHandler>();

            if (gameSessionHandler == null && GameInitializer.Instance != null)
                gameSessionHandler = GameInitializer.Instance.EnsureGameSessionHandler();

            if (gameSessionHandler == null)
            {
                onDone?.Invoke(false, null);
                return;
            }

            bool result = await gameSessionHandler.LoadGameSessionData2(data);

            onDone?.Invoke(result, null);
        }
        catch (Exception e)
        {
            onDone?.Invoke(false, e);
        }
    }

#if ADDRESSABLES
    private string[] BuildSceneDependencyPrepareKeys(string sceneKey, bool lateWorldFullyCached)
    {
        if (prepareNewSceneLateContentBeforeEnter &&
            SceneNameAliases.IsNewSceneFamily(sceneKey))
        {
            if (lateWorldFullyCached)
            {
                Debug.Log("[BootFlow] New Scene late world is fully cached. Wait all late content before entering.");
                return new[]
                {
                    sceneKey,
                    SceneNameAliases.NewSceneLateLabel
                };
            }

            Debug.Log("[BootFlow] New Scene late world is not fully cached. Prepare only first late scene for first entry; remaining models load in background.");
            return new[]
            {
                sceneKey,
                NewSceneFirstLateSceneKey
            };
        }

        return new[] { sceneKey };
    }

    private IEnumerator CoClampFirstNewScenePrepareKeys(
        string sceneKey,
        bool lateWorldFullyCached,
        string[] dependencyKeys,
        Action<string[]> onDone)
    {
        if (!prepareNewSceneLateContentBeforeEnter ||
            lateWorldFullyCached ||
            !SceneNameAliases.IsNewSceneFamily(sceneKey) ||
            dependencyKeys == null ||
            dependencyKeys.Length <= 1)
        {
            onDone?.Invoke(dependencyKeys ?? new[] { sceneKey });
            yield break;
        }

        long firstLateBytes = -1;
        bool sizeOk = false;

        yield return CoGetDownloadSize(
            NewSceneFirstLateSceneKey,
            result =>
            {
                firstLateBytes = result;
                sizeOk = true;
            }
        );

        if (!sizeOk)
        {
            Debug.LogWarning("[BootFlow] Cannot check first New Scene late size. Keep original prepare keys.");
            onDone?.Invoke(dependencyKeys);
            yield break;
        }

        if (firstLateBytes > firstNewSceneLatePrepareBudgetBytes)
        {
            Debug.LogWarning("[BootFlow] First New Scene late key is too large before entry. "
                             + "key="
                             + NewSceneFirstLateSceneKey
                             + ", size="
                             + FormatBytes(firstLateBytes)
                             + ", budget="
                             + FormatBytes(firstNewSceneLatePrepareBudgetBytes)
                             + ". Enter after main scene; late loader will continue in background.");

            onDone?.Invoke(new[] { sceneKey });
            yield break;
        }

        onDone?.Invoke(dependencyKeys);
    }

    private IEnumerator CoCheckNewSceneLateWorldCached(Action<bool> onDone)
    {
        if (preload == null)
        {
            onDone?.Invoke(false);
            yield break;
        }

        bool cached = false;
        yield return preload.IsAddressableKeyCachedRoutine(
            SceneNameAliases.NewSceneLateLabel,
            result => cached = result);

        onDone?.Invoke(cached);
    }

    private bool ShouldLoadAddressableWorldBehindIntro(string sceneKey)
    {
        return prepareNewSceneLateContentBeforeEnter &&
               SceneNameAliases.IsNewSceneFamily(sceneKey);
    }

    private IEnumerator CoLoadAddressableWorldBehindIntro(string sceneKey, bool waitForAllLateContent, Action<bool> onDone)
    {
        Scene introScene = intro != null ? intro.gameObject.scene : SceneManager.GetActiveScene();
        AsyncOperationHandle<SceneInstance> handle = default;

        Debug.Log("[BootFlow] Loading New Scene behind intro before allowing 100%: " + sceneKey);

        try
        {
            handle = Addressables.LoadSceneAsync(sceneKey, LoadSceneMode.Additive, true);
        }
        catch (Exception e)
        {
            Debug.LogError("[BootFlow] Addressables.LoadSceneAsync additive threw exception: " + e);
            onDone?.Invoke(false);
            yield break;
        }

        while (handle.IsValid() && !handle.IsDone)
        {
            if (intro != null)
                intro.SetBootProgress01(Mathf.Lerp(0.70f, 0.86f, Mathf.Clamp01(handle.PercentComplete)));

            yield return null;
        }

        if (!handle.IsValid())
        {
            Debug.LogError("[BootFlow] New Scene additive load handle invalid.");
            onDone?.Invoke(false);
            yield break;
        }

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            string err = handle.OperationException != null
                ? handle.OperationException.ToString()
                : handle.Status.ToString();

            Debug.LogError("[BootFlow] New Scene additive load failed: " + err);
            SafeRelease(handle);
            onDone?.Invoke(false);
            yield break;
        }

        Scene worldScene = handle.Result.Scene;
        AddressableAdditiveSceneLoader lateLoader = FindLateLoaderInScene(worldScene);
        List<GameObject> hiddenWorldRoots = HideWorldRootsUntilReady(worldScene, lateLoader);

        if (intro != null)
            intro.SetBootProgress01(0.86f);

        yield return null;
        yield return null;

        if (lateLoader != null)
        {
            lateLoader.BeginLoad();
            float firstLateGateTimer = 0f;

            while (!IsRequiredLateContentReady(lateLoader, waitForAllLateContent))
            {
                if (intro != null)
                    intro.SetBootProgress01(Mathf.Lerp(0.86f, 0.99f, Mathf.Clamp01(lateLoader.Progress01)));

                if (!waitForAllLateContent && firstNewSceneLateEntryGateTimeoutSeconds > 0f)
                {
                    firstLateGateTimer += Time.unscaledDeltaTime;

                    if (firstLateGateTimer >= firstNewSceneLateEntryGateTimeoutSeconds)
                    {
                        Debug.LogWarning("[BootFlow] First New Scene late gate timeout after "
                                         + firstNewSceneLateEntryGateTimeoutSeconds
                                         + "s. Enter now; late loader keeps loading in background.");
                        break;
                    }
                }

                yield return null;
            }

            if (lateLoader.FailedSceneCount > 0)
            {
                Debug.LogWarning("[BootFlow] New Scene late loader completed with failed scenes. loaded="
                                 + lateLoader.LoadedSceneCount
                                 + "/"
                                 + lateLoader.TotalSceneCount
                                 + ", failed="
                                 + lateLoader.FailedSceneCount);
            }
        }
        else
        {
            Debug.LogWarning("[BootFlow] New Scene late loader not found after main scene load. Continue with main scene only.");
        }

    if (worldScene.IsValid() && worldScene.isLoaded)
        SceneManager.SetActiveScene(worldScene);

    RestoreHiddenWorldRoots(hiddenWorldRoots);

    if (_resolvedSessionData != null &&
        GameInitializer.Instance != null &&
        GameInitializer.Instance.SceneLocationHandle != null)
    {
        Debug.Log(
            "[BootFlow] Restore saved player position immediately after world activation: "
            + worldScene.name
        );

        GameInitializer.Instance.SceneLocationHandle
            .RestoreSavedPlayerPosition(worldScene);
    }

    yield return null;
    yield return new WaitForEndOfFrame();
    yield return null;

    if (intro != null)
    {
        intro.SetBootProgress01(1f, true);

        float introGateTimer = 0f;

        while (!intro.CanEnterMain)
        {
            introGateTimer += Time.unscaledDeltaTime;

            if (introEnterMainGateTimeoutSeconds > 0f &&
                introGateTimer >= introEnterMainGateTimeoutSeconds)
            {
                Debug.LogWarning(
                    "[BootFlow] Intro gate timeout after world runtime ready. " +
                    "Continue into loaded scene: " + sceneKey
                );

                break;
            }

            yield return null;
        }
    }


    // =====================================================
    // BÂY GIỜ MỚI CHO LOADING LÊN 100%
    // =====================================================
    if (intro != null)
    {
        intro.SetBootProgress01(1f, true);

        float introGateTimer = 0f;

        while (!intro.CanEnterMain)
        {
            introGateTimer += Time.unscaledDeltaTime;

            if (introEnterMainGateTimeoutSeconds > 0f &&
                introGateTimer >= introEnterMainGateTimeoutSeconds)
            {
                Debug.LogWarning(
                    "[BootFlow] Intro gate timeout after world runtime ready. " +
                    "Continue into loaded scene: " + sceneKey
                );

                break;
            }

            yield return null;
        }
    }


    // =====================================================
    // CUỐI CÙNG MỚI GỠ INTRO
    // =====================================================
    if (introScene.IsValid() &&
        introScene.isLoaded &&
        (!worldScene.IsValid() || introScene != worldScene))
    {
        AsyncOperation unloadIntro =
            SceneManager.UnloadSceneAsync(introScene);

        while (unloadIntro != null && !unloadIntro.isDone)
            yield return null;
    }

    onDone?.Invoke(true);
    }

    private bool IsRequiredLateContentReady(AddressableAdditiveSceneLoader lateLoader, bool waitForAllLateContent)
    {
        if (lateLoader == null)
            return true;

        if (waitForAllLateContent)
            return lateLoader.IsComplete;

        Scene firstLateScene = SceneManager.GetSceneByName(NewSceneFirstLateSceneKey);

        if (firstLateScene.IsValid() && firstLateScene.isLoaded)
            return true;

        return lateLoader.IsComplete;
    }

    private List<GameObject> HideWorldRootsUntilReady(Scene scene, AddressableAdditiveSceneLoader lateLoader)
    {
        List<GameObject> hiddenRoots = new List<GameObject>();

        if (!scene.IsValid() || !scene.isLoaded)
            return hiddenRoots;

        GameObject loaderRoot = lateLoader != null
            ? lateLoader.transform.root.gameObject
            : null;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            if (root == null)
                continue;

            if (loaderRoot != null && root == loaderRoot)
                continue;

            if (!root.activeSelf)
                continue;

            root.SetActive(false);
            hiddenRoots.Add(root);
        }

        if (hiddenRoots.Count > 0)
            Debug.Log("[BootFlow] Hidden New Scene roots until late content is ready: " + hiddenRoots.Count);

        return hiddenRoots;
    }

    private void RestoreHiddenWorldRoots(List<GameObject> hiddenRoots)
    {
        if (hiddenRoots == null || hiddenRoots.Count == 0)
            return;

        for (int i = 0; i < hiddenRoots.Count; i++)
        {
            GameObject root = hiddenRoots[i];

            if (root != null)
                root.SetActive(true);
        }

        Debug.Log("[BootFlow] Restored New Scene roots after late content ready: " + hiddenRoots.Count);
    }

    private AddressableAdditiveSceneLoader FindLateLoaderInScene(Scene scene)
    {
        AddressableAdditiveSceneLoader[] loaders =
            FindObjectsByType<AddressableAdditiveSceneLoader>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < loaders.Length; i++)
        {
            AddressableAdditiveSceneLoader loader = loaders[i];

            if (loader == null)
                continue;

            if (scene.IsValid() && loader.gameObject.scene == scene)
                return loader;
        }

        return loaders.Length > 0 ? loaders[0] : null;
    }

    private IEnumerator CoPrepareSceneDependenciesWithPreload(string sceneKey, Action<bool> onDone)
    {
        yield return CoPrepareSceneDependenciesWithPreload(new[] { sceneKey }, onDone);
    }

    private IEnumerator CoPrepareSceneDependenciesWithPreload(IReadOnlyList<string> sceneKeys, Action<bool> onDone)
    {
        if (preload == null)
        {
            Debug.LogError("[BootFlow] Cannot prepare scene dependencies because preload is null.");
            onDone?.Invoke(false);
            yield break;
        }

        if (!preload.IsReady)
        {
            Debug.LogError("[BootFlow] Cannot prepare scene dependencies because Addressables catalog is not ready.");
            onDone?.Invoke(false);
            yield break;
        }

        Debug.Log("[BootFlow] Preparing scene/world dependencies via AddressablesPreload: " + string.Join(", ", sceneKeys));

        yield return preload.PrepareAddressableKeysRoutine(sceneKeys);

        if (preload.HasFailed)
        {
            Debug.LogError("[BootFlow] AddressablesPreload prepare failed: " + preload.LastError);
            onDone?.Invoke(false);
            yield break;
        }

        Debug.Log("[BootFlow] Scene/world dependencies prepared via AddressablesPreload: " + string.Join(", ", sceneKeys));
        onDone?.Invoke(true);
    }
#endif

    private string ResolveMainSceneKey()
    {
        _resolvedSessionData = TryGetSavedSessionData();

        if (_resolvedSessionData != null &&
            _resolvedSessionData.SceneLocation != null &&
            !string.IsNullOrWhiteSpace(_resolvedSessionData.SceneLocation.SceneName))
        {
            string savedScene = _resolvedSessionData.SceneLocation.SceneName;
            string sceneKey = ConvertSceneNameToAddressableKey(savedScene);

            Debug.Log("[BootFlow] Có session cũ. Chờ GameSessionHandler.LoadGameSessionData2 rồi load scene đã lưu. SavedScene="
                      + savedScene + ", ResolvedKey=" + sceneKey);

            return sceneKey;
        }

        Debug.Log("[BootFlow] Không có session cũ hoặc không xác định được account. Load scene mặc định: " + mainAddressableSceneKey);
        return mainAddressableSceneKey;
    }

    private GameSessionData TryGetSavedSessionData()
    {
        try
        {
            TryRestoreTokenStoreFromDiskForBootFlow();

            Debug.Log("[BootFlow] Resolve saved session local. IsAuthenticated="
                      + TokenStore.IsAuthenticated
                      + ", UserID="
                      + TokenStore.UserID);

            SaveManager saveManager = new SaveManager();
            var saves = saveManager.LoadAllGameSession();

            if (saves == null || saves.Count == 0)
            {
                Debug.Log("[BootFlow] Máy này chưa có saved session.");
                return null;
            }

            Debug.Log("[BootFlow] Tổng saved session tìm thấy: " + saves.Count);

            if (!string.IsNullOrWhiteSpace(TokenStore.UserID))
            {
                foreach (var item in saves)
                {
                    if (item == null)
                        continue;

                    Debug.Log("[BootFlow] Check saved session item. SavedUserID="
                              + item.UserID
                              + ", CurrentUserID="
                              + TokenStore.UserID);

                    if (item.UserID != TokenStore.UserID)
                        continue;

                    if (item.SceneLocation == null)
                    {
                        Debug.LogWarning("[BootFlow] Saved session đúng UserID nhưng SceneLocation null.");
                        return null;
                    }

                    if (!SceneNameAliases.CanUseSavedSceneForResume(item.SceneLocation.SceneName))
                    {
                        Debug.LogWarning("[BootFlow] Saved session đúng UserID nhưng SceneName rỗng.");
                        return null;
                    }

                    Debug.Log("[BootFlow] Tìm thấy saved session đúng UserID. SceneName="
                              + item.SceneLocation.SceneName);

                    return item;
                }

                Debug.Log("[BootFlow] Có UserID nhưng không tìm thấy saved session cho account hiện tại.");
                return null;
            }

            if (!allowSingleSaveFallbackWhenNoUserID)
            {
                Debug.LogWarning("[BootFlow] Chưa có UserID và single-save fallback đang tắt.");
                return null;
            }

            GameSessionData onlyValidSave = null;
            int validCount = 0;

            foreach (var item in saves)
            {
                if (item == null)
                    continue;

                if (item.SceneLocation == null)
                    continue;

                if (!SceneNameAliases.CanUseSavedSceneForResume(item.SceneLocation.SceneName))
                    continue;

                onlyValidSave = item;
                validCount++;

                Debug.Log("[BootFlow] Valid save candidate without UserID. SavedUserID="
                          + item.UserID
                          + ", SceneName="
                          + item.SceneLocation.SceneName);
            }

            if (validCount == 1 && onlyValidSave != null)
            {
                Debug.LogWarning("[BootFlow] Chưa có UserID nhưng chỉ có 1 saved session hợp lệ. Load saved session: "
                                 + onlyValidSave.SceneLocation.SceneName);

                return onlyValidSave;
            }

            Debug.LogWarning("[BootFlow] Chưa có UserID và số saved session hợp lệ = "
                             + validCount
                             + ". Không thể biết account nào, fallback scene mặc định.");

            return null;
        }
        catch (Exception e)
        {
            Debug.LogWarning("[BootFlow] TryGetSavedSessionData failed: " + e);
            return null;
        }
    }

    private void TryRestoreTokenStoreFromDiskForBootFlow()
    {
        if (!restoreTokenStoreFromDiskBeforeResolveSave)
            return;

        if (_triedRestoreTokenStoreFromDisk)
            return;

        _triedRestoreTokenStoreFromDisk = true;

        if (!string.IsNullOrWhiteSpace(TokenStore.UserID))
            return;

        bool restored = TokenStore.TryRestoreFromDisk();

        Debug.Log("[BootFlow] TokenStore restore before saved session resolve. restored="
                  + restored
                  + ", IsAuthenticated="
                  + TokenStore.IsAuthenticated
                  + ", UserID="
                  + TokenStore.UserID);
    }

    private string ConvertSceneNameToAddressableKey(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return sceneName;

        return SceneNameAliases.ToAddressableSceneKey(sceneName);
    }

#if ADDRESSABLES
    private IEnumerator CoEnsureSceneDependenciesReady(string sceneKey, Action<bool> onDone)
    {
        int maxRetry = Mathf.Max(1, sceneDownloadMaxRetries);

        for (int attempt = 1; attempt <= maxRetry; attempt++)
        {
            Debug.Log($"[BootFlow] ===== Scene dependency attempt {attempt}/{maxRetry} =====");

            long remainBefore = -1;
            bool sizeOk = false;

            yield return CoGetDownloadSize(
                sceneKey,
                result =>
                {
                    remainBefore = result;
                    sizeOk = true;
                }
            );

            if (!sizeOk)
            {
                Debug.LogWarning("[BootFlow] GetDownloadSizeAsync failed before download. Will retry.");

                yield return CoRecoverCacheAfterFail(attempt, maxRetry);
                yield return WaitRetryDelay();
                continue;
            }

            Debug.Log($"[BootFlow] Scene remaining bytes before download = {remainBefore}");

            if (remainBefore <= sceneVerifyThresholdBytes)
            {
                Debug.Log("[BootFlow] Scene dependencies already ready.");
                onDone?.Invoke(true);
                yield break;
            }

            bool downloadOk = false;

            yield return CoDownloadSceneDependencies(
                sceneKey,
                attempt,
                result => downloadOk = result
            );

            if (!downloadOk)
            {
                Debug.LogWarning($"[BootFlow] Scene dependency download failed on attempt {attempt}/{maxRetry}.");

                yield return CoRecoverCacheAfterFail(attempt, maxRetry);
                yield return WaitRetryDelay();
                continue;
            }

            long remainAfter = -1;
            bool verifyOk = false;

            yield return CoGetDownloadSize(
                sceneKey,
                result =>
                {
                    remainAfter = result;
                    verifyOk = true;
                }
            );

            if (!verifyOk)
            {
                Debug.LogWarning("[BootFlow] Verify GetDownloadSizeAsync failed after download. Will retry.");

                yield return CoRecoverCacheAfterFail(attempt, maxRetry);
                yield return WaitRetryDelay();
                continue;
            }

            Debug.Log($"[BootFlow] Scene remaining bytes after download = {remainAfter}");

            if (remainAfter <= sceneVerifyThresholdBytes)
            {
                Debug.Log("[BootFlow] Scene dependencies verified ready.");
                onDone?.Invoke(true);
                yield break;
            }

            Debug.LogWarning($"[BootFlow] Scene verify failed. remainAfter={remainAfter}. Will retry.");

            yield return CoRecoverCacheAfterFail(attempt, maxRetry);
            yield return WaitRetryDelay();
        }

        onDone?.Invoke(false);
    }

    private IEnumerator CoGetDownloadSize(string key, Action<long> onSuccess)
    {
        AsyncOperationHandle<long> sizeHandle = default;

        try
        {
            Debug.Log("[BootFlow] Checking remaining download size for: " + key);
            sizeHandle = Addressables.GetDownloadSizeAsync(key);
        }
        catch (Exception e)
        {
            Debug.LogError("[BootFlow] GetDownloadSizeAsync threw exception: " + e);
            yield break;
        }

        yield return sizeHandle;

        try
        {
            if (!sizeHandle.IsValid())
            {
                Debug.LogError("[BootFlow] GetDownloadSizeAsync handle invalid.");
                yield break;
            }

            if (sizeHandle.Status != AsyncOperationStatus.Succeeded)
            {
                string err = sizeHandle.OperationException != null
                    ? sizeHandle.OperationException.ToString()
                    : sizeHandle.Status.ToString();

                Debug.LogError("[BootFlow] GetDownloadSizeAsync failed: " + err);
                yield break;
            }

            onSuccess?.Invoke(sizeHandle.Result);
        }
        finally
        {
            SafeRelease(sizeHandle);
        }
    }

    private IEnumerator CoDownloadSceneDependencies(string key, int attempt, Action<bool> onDone)
    {
        AsyncOperationHandle dl = default;

        try
        {
            Debug.Log("[BootFlow] Pre-downloading scene dependencies: " + key);
            dl = Addressables.DownloadDependenciesAsync(key, false);
        }
        catch (Exception e)
        {
            Debug.LogError("[BootFlow] DownloadDependenciesAsync threw exception: " + e);
            onDone?.Invoke(false);
            yield break;
        }

        float totalTimer = 0f;
        float stallTimer = 0f;
        float lastProgress = -1f;
        float logTimer = 0f;

        while (dl.IsValid() && !dl.IsDone)
        {
            float dt = Time.unscaledDeltaTime;
            totalTimer += dt;
            stallTimer += dt;
            logTimer += dt;

            float p = Mathf.Clamp01(dl.PercentComplete);

            if (intro != null)
                intro.ForceProgress(p);

            if (p > lastProgress + 0.0005f)
            {
                lastProgress = p;
                stallTimer = 0f;
            }

            if (logTimer >= 2f)
            {
                logTimer = 0f;
                Debug.Log($"[BootFlow] Scene dependency downloading attempt={attempt}, progress={p:P1}");
            }

            if (sceneDownloadTimeoutSeconds > 0f && totalTimer >= sceneDownloadTimeoutSeconds)
            {
                Debug.LogError($"[BootFlow] Scene dependency download timeout after {sceneDownloadTimeoutSeconds}s.");
                SafeRelease(dl);
                onDone?.Invoke(false);
                yield break;
            }

            if (sceneDownloadStallTimeoutSeconds > 0f && stallTimer >= sceneDownloadStallTimeoutSeconds)
            {
                Debug.LogError($"[BootFlow] Scene dependency download stalled. No progress for {sceneDownloadStallTimeoutSeconds}s.");
                SafeRelease(dl);
                onDone?.Invoke(false);
                yield break;
            }

            yield return null;
        }

        if (!dl.IsValid())
        {
            Debug.LogError("[BootFlow] Scene dependency download handle invalid.");
            onDone?.Invoke(false);
            yield break;
        }

        if (dl.Status != AsyncOperationStatus.Succeeded)
        {
            string err = dl.OperationException != null
                ? dl.OperationException.ToString()
                : dl.Status.ToString();

            Debug.LogError("[BootFlow] Scene dependency download FAILED:\n" + err);

            SafeRelease(dl);
            onDone?.Invoke(false);
            yield break;
        }

        SafeRelease(dl);

        Debug.Log("[BootFlow] Scene dependency download DONE.");
        onDone?.Invoke(true);
    }

    private IEnumerator CoRecoverCacheAfterFail(int attempt, int maxRetry)
    {
        if (clearUnityCacheTempOnSceneDownloadFail)
        {
            ClearUnityCacheTemp();
            yield return null;
        }

        if (clearAllUnityCacheOnLastRetry && attempt >= maxRetry)
        {
            Debug.LogWarning("[BootFlow] Last retry reached. Clearing ALL Unity cache via Caching.ClearCache().");

            bool ok = false;

            try
            {
                ok = Caching.ClearCache();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[BootFlow] Caching.ClearCache exception: " + e.Message);
            }

            Debug.LogWarning("[BootFlow] Caching.ClearCache result = " + ok);
            yield return null;
        }
    }

    private IEnumerator WaitRetryDelay()
    {
        if (sceneDownloadRetryDelaySeconds > 0f)
            yield return new WaitForSecondsRealtime(sceneDownloadRetryDelaySeconds);
    }

    private void ClearUnityCacheTemp()
    {
        try
        {
            string unityCacheDir = Path.Combine(Application.persistentDataPath, "UnityCache");
            string tempDir = Path.Combine(unityCacheDir, "Temp");

            if (Directory.Exists(tempDir))
            {
                Directory.Delete(tempDir, true);
                Debug.LogWarning("[BootFlow] Deleted UnityCache Temp: " + tempDir);
            }
            else
            {
                Debug.Log("[BootFlow] UnityCache Temp not found: " + tempDir);
            }

            Directory.CreateDirectory(tempDir);
        }
        catch (Exception e)
        {
            Debug.LogWarning("[BootFlow] ClearUnityCacheTemp failed: " + e.Message);
        }
    }

    private void SafeRelease(AsyncOperationHandle handle)
    {
        try
        {
            if (handle.IsValid())
                Addressables.Release(handle);
        }
        catch
        {
            // ignore
        }
    }

    private void SafeRelease<T>(AsyncOperationHandle<T> handle)
    {
        try
        {
            if (handle.IsValid())
                Addressables.Release(handle);
        }
        catch
        {
            // ignore
        }
    }

    private static string FormatBytes(long bytes)
    {
        const double kb = 1024d;
        const double mb = kb * 1024d;
        const double gb = mb * 1024d;

        if (bytes >= gb)
            return (bytes / gb).ToString("0.##") + " GB";

        if (bytes >= mb)
            return (bytes / mb).ToString("0.##") + " MB";

        if (bytes >= kb)
            return (bytes / kb).ToString("0.##") + " KB";

        return bytes + " B";
    }
#endif
}
