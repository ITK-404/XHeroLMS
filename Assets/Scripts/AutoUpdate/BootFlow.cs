using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Collections;
using System.IO;
using Cysharp.Threading.Tasks;

#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
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
    string mainAddressableSceneKey = "New Scene";

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
    private float savedSessionDataTimeoutSeconds = 45f;

#if ADDRESSABLES
    [Header("Scene Dependency Download Recovery")]
    private bool downloadSceneDependenciesInBootFlow = true;

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

            if (downloadSceneDependenciesInBootFlow)
            {
                bool dependencyReady = false;

                yield return CoPrepareSceneDependenciesWithPreload(
                    _resolvedMainSceneKey,
                    result => dependencyReady = result
                );

                if (!dependencyReady)
                {
                    Debug.LogError("[BootFlow] Scene dependency prepare failed.");

                    if (IsRecoverablePreloadFailure())
                    {
                        Debug.LogWarning("[BootFlow] Scene dependency failed because network/preload is not ready. Waiting for NetworkGameplayGuard recovery.");
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

            if (intro != null)
            {
                intro.SetBootProgress01(1f, true);

                while (!intro.CanEnterMain)
                    yield return null;
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
    private IEnumerator CoPrepareSceneDependenciesWithPreload(string sceneKey, Action<bool> onDone)
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

        Debug.Log("[BootFlow] Preparing scene dependencies via AddressablesPreload: " + sceneKey);

        yield return preload.PrepareAddressableKeyRoutine(sceneKey);

        if (preload.HasFailed)
        {
            Debug.LogError("[BootFlow] AddressablesPreload prepare failed: " + preload.LastError);
            onDone?.Invoke(false);
            yield break;
        }

        Debug.Log("[BootFlow] Scene dependencies prepared via AddressablesPreload: " + sceneKey);
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

                    if (string.IsNullOrWhiteSpace(item.SceneLocation.SceneName))
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

                if (string.IsNullOrWhiteSpace(item.SceneLocation.SceneName))
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

        return sceneName;
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
#endif
}
