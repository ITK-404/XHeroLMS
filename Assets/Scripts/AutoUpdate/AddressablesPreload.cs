using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;

#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.Networking;
#endif

[DefaultExecutionOrder(-15000)]
public class AddressablesPreload : MonoBehaviour
{
    public static AddressablesPreload Instance { get; private set; }

    public enum PreloadStage
    {
        None,
        Probe,
        ClearCache,
        Initialize,
        ForceLoadCatalog,
        CheckCatalog,
        UpdateCatalog,
        CleanOldBundleCache,
        CatalogReady,
        GetSize,
        Download,
        Verify,
        WarmupKeyData,
        Done,
        Failed
    }

    [Header("State (Read-only)")]
    public PreloadStage Stage { get; private set; } = PreloadStage.None;

    public bool IsReady { get; private set; }
    public bool HasFailed { get; private set; }
    public string LastError { get; private set; } = "";

    public float DownloadPercent01 { get; private set; }

    public long BytesToDownload { get; private set; }
    public long BytesDownloadedApprox { get; private set; }

    public bool IsCloudFullyDownloaded { get; private set; }

    /// <summary>
    /// Text thân thiện cho UI loading.
    /// Không dùng chữ catalog / bundle / Addressables ở đây.
    /// </summary>
    public string LoadingText { get; private set; } = "Đang kiểm tra tài nguyên: 0%";

    public long NetworkSpeedBytesPerSecond { get; private set; }

    public int LoadingPhaseId { get; private set; }

#if ADDRESSABLES

    [Header("Runtime Catalog From GCS")]
    [SerializeField] private bool enableProbeRemoteCatalog = true;
    [SerializeField] private int probeReadBytes = 64;
    [SerializeField] private bool forceLoadRemoteCatalog = true;

    [Header("On-Demand Mode")]
    [SerializeField] private bool catalogOnlyOnBoot = true;

    [Tooltip("Mỗi lần vào scene sẽ kiểm tra dữ liệu mới trên GCS. Nếu có mới thì update rồi tải lại đúng scene.")]
    [SerializeField] private bool checkCatalogBeforeEveryPrepare = false;

    [Tooltip("Sau khi update dữ liệu, xóa bundle cũ không còn dùng.")]
    [SerializeField] private bool cleanOldBundleCacheAfterCatalogUpdate = true;

    [Tooltip("Nếu scene đã chuẩn bị trong session này và dữ liệu không đổi thì bỏ qua tải lại.")]
    [SerializeField] private bool rememberPreparedKeysInSession = true;

    [Header("Verify")]
    [SerializeField] private bool verifyAfterDownload = true;
    [SerializeField] private long verifySizeThresholdBytes = 0;

    [Header("Warmup / Giải nén")]
    [SerializeField] private bool warmupKeyDataAfterDownload = true;
    [SerializeField] private bool warmupCachedKeyData = false;
    [SerializeField] private bool skipSceneWarmup = true;
    [SerializeField] private int warmupAssetBatchSize = 6;
    [SerializeField] private float warmupAssetBatchTimeoutSeconds = 240f;
    [SerializeField] private bool continueWhenWarmupAssetFailed = true;

    [Tooltip("Gọi Resources.UnloadUnusedAssets sau mỗi số batch warmup. 0 = tắt.")]
    [SerializeField] private int unloadUnusedAssetsEveryBatches = 8;

    // Chỉ dùng cho các bước chưa có số byte thật như probe/catalog/get-size.
    private float prepareProgressEnd = 0.05f;

    [Header("Progress Mapping")]
    [SerializeField] private float progressDownloadEnd = 0.80f;
    [SerializeField] private float progressVerifyEnd = 0.85f;
    [SerializeField] private float progressWarmupEnd = 0.99f;

    // Khi dữ liệu đã có sẵn trong cache, vẫn chạy thanh load tối thiểu bấy nhiêu giây thay vì nhảy thẳng 100%.
    private float cachedDataMinimumLoadSeconds = 0f;

    [Header("Retry / Timeout")]
    [SerializeField] private int maxCatalogRetries = 3;
    [SerializeField] private int maxDownloadRetries = 3;
    [SerializeField] private float stepTimeoutSeconds = 30f;
    [SerializeField] private float downloadTimeoutSeconds = 0f;
    [SerializeField] private float downloadStallTimeoutSeconds = 60f;
    [SerializeField] private float retryDelaySeconds = 1.5f;

    [Header("Cache")]
    [SerializeField] private bool clearCatalogCacheOnRetryOnly = true;

    [Header("Debug")]
    [SerializeField] private bool enableAddressablesRequestLog = true;
    [SerializeField] private bool verboseProgressLog = true;
    [SerializeField] private float progressLogInterval = 2f;

    private string remoteCatalogHashUrl = "";
    private string remoteCatalogJsonUrl = "";

    private Coroutine _catalogRunning;
    private Coroutine _prepareRunning;

    private bool _retryRequested;
    private bool _catalogUpdatedThisRun;

    private long _lastSpeedBytes;
    private float _lastSpeedTime;

    private readonly HashSet<string> _preparedKeys = new HashSet<string>();

    public bool IsPreparingKey { get; private set; }
    public string ActivePrepareKey { get; private set; } = "";
    public bool LastPrepareUsedCachedData { get; private set; }

    private float _prepareProgressStartRealtime;
    private bool _progressWindowActive;
    private float _progressWindowStart01;
    private float _progressWindowEnd01;

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

#if ADDRESSABLES
        ApplyUrlsFromRuntimeEnv();
        SetupAddressablesLogging();

        if (_catalogRunning == null)
            _catalogRunning = StartCoroutine(RunCatalogBootstrapFlow());
#endif
    }

    public void RequestRetry()
    {
#if ADDRESSABLES
        Debug.Log("[Preload] Retry requested.");

        _retryRequested = true;

        if (_catalogRunning == null)
            _catalogRunning = StartCoroutine(RunCatalogBootstrapFlow());
#endif
    }

#if ADDRESSABLES

    // ============================================================
    // PUBLIC API
    // ============================================================

    public IEnumerator IsAddressableKeyCachedRoutine(string key, Action<bool> onDone)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            onDone?.Invoke(false);
            yield break;
        }

        key = key.Trim();

        while (_catalogRunning != null)
            yield return null;

        if (!IsReady || HasFailed)
        {
            onDone?.Invoke(false);
            yield break;
        }

        AsyncOperationHandle<long> sizeHandle = default;

        try
        {
            sizeHandle = Addressables.GetDownloadSizeAsync(key);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Preload] Cache check threw exception. key={key}, error={e.Message}");
            onDone?.Invoke(false);
            yield break;
        }

        float timer = 0f;

        while (sizeHandle.IsValid() && !sizeHandle.IsDone)
        {
            if (stepTimeoutSeconds > 0f)
            {
                timer += Time.unscaledDeltaTime;

                if (timer >= stepTimeoutSeconds)
                {
                    Debug.LogWarning($"[Preload] Cache check timeout. key={key}");
                    SafeRelease(sizeHandle);
                    onDone?.Invoke(false);
                    yield break;
                }
            }

            yield return null;
        }

        bool cached =
            sizeHandle.IsValid() &&
            sizeHandle.Status == AsyncOperationStatus.Succeeded &&
            sizeHandle.Result <= verifySizeThresholdBytes;

        if (sizeHandle.IsValid() && sizeHandle.Status != AsyncOperationStatus.Succeeded)
        {
            string err = sizeHandle.OperationException != null
                ? sizeHandle.OperationException.Message
                : sizeHandle.Status.ToString();

            Debug.LogWarning($"[Preload] Cache check failed. key={key}, error={err}");
        }

        if (sizeHandle.IsValid())
            Debug.Log($"[Preload] Cache check. key={key}, cached={cached}, remain={FormatBytes(Math.Max(0L, sizeHandle.Result))}");

        SafeRelease(sizeHandle);
        onDone?.Invoke(cached);
    }

    public IEnumerator PrepareAddressableKeysRoutine(IEnumerable<string> keys)
    {
        List<string> keyList = BuildUniquePrepareKeyList(keys);

        if (keyList.Count == 0)
        {
            Fail("PrepareAddressableKeysRoutine failed: key list is empty.");
            yield break;
        }

        if (keyList.Count == 1)
        {
            yield return PrepareAddressableKeyRoutine(keyList[0]);
            yield break;
        }

        BeginNewLoadingSession(ShouldPreserveProgressForPrepareSession());

        while (_catalogRunning != null)
        {
            SetPrepareText(DownloadPercent01);
            yield return null;
        }

        if (HasFailed)
        {
            Debug.LogError($"[Preload] Cannot prepare key group because bootstrap failed. error={LastError}");
            yield break;
        }

        if (!IsReady)
        {
            Fail("Catalog is not ready for prepare key group.");
            yield break;
        }

        while (_prepareRunning != null)
            yield return null;

        if (checkCatalogBeforeEveryPrepare)
        {
            yield return CheckUpdateCatalogAndCleanOldBundles();

            if (HasFailed)
                yield break;
        }

        Debug.Log("[Preload] ===== Prepare key group started: " + string.Join(", ", keyList) + " =====");

        for (int i = 0; i < keyList.Count; i++)
        {
            string key = keyList[i];
            BeginProgressWindow(i, keyList.Count);

            bool alreadyPreparedInSession =
                rememberPreparedKeysInSession && _preparedKeys.Contains(key);

            if (alreadyPreparedInSession)
            {
                IsPreparingKey = true;
                ActivePrepareKey = key;
                LastPrepareUsedCachedData = true;
                _prepareProgressStartRealtime = Time.realtimeSinceStartup;

                SetStage(PreloadStage.Done);
                BytesToDownload = 0;
                BytesDownloadedApprox = 0;
                NetworkSpeedBytesPerSecond = 0;

                yield return WaitCachedPrepareMinimumIfNeeded();

                SetProgressExact(1f);
                SetCheckingResourceText();

                FinishPrepareKey();

                Debug.Log($"[Preload] Key already prepared in this session and data is latest: {key}");
                continue;
            }

            _prepareRunning = StartCoroutine(CoPrepareAddressableKeySafe(key, skipCatalogCheck: true));

            while (_prepareRunning != null)
                yield return null;

            if (HasFailed)
            {
                ClearProgressWindow();
                yield break;
            }
        }

        ClearProgressWindow();
        SetProgressExact(Mathf.Min(progressWarmupEnd, 0.99f));
        SetCheckingResourceText();
        SetStage(PreloadStage.Done);

        Debug.Log("[Preload] ===== Prepare key group DONE: " + string.Join(", ", keyList) + " =====");
    }

    public IEnumerator PrepareAddressableKeyRoutine(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            Fail("PrepareAddressableKeyRoutine failed: key is empty.");
            yield break;
        }

        key = key.Trim();

        BeginNewLoadingSession(ShouldPreserveProgressForPrepareSession());

        while (_catalogRunning != null)
        {
            SetPrepareText(DownloadPercent01);
            yield return null;
        }

        if (HasFailed)
        {
            Debug.LogError($"[Preload] Cannot prepare key because bootstrap failed. key={key}, error={LastError}");
            yield break;
        }

        if (!IsReady)
        {
            Fail($"Catalog is not ready. key={key}");
            yield break;
        }

        while (_prepareRunning != null)
            yield return null;

        bool alreadyPreparedInSession =
            rememberPreparedKeysInSession && _preparedKeys.Contains(key);

        if (checkCatalogBeforeEveryPrepare)
        {
            yield return CheckUpdateCatalogAndCleanOldBundles();

            if (HasFailed)
                yield break;

            if (_catalogUpdatedThisRun)
            {
                alreadyPreparedInSession = false;

                Debug.Log(
                    $"[Preload] Remote data changed. Force re-check/download key={key}"
                );
            }
        }

        if (alreadyPreparedInSession)
        {
            IsPreparingKey = true;
            ActivePrepareKey = key;
            LastPrepareUsedCachedData = true;
            _prepareProgressStartRealtime = Time.realtimeSinceStartup;

            SetStage(PreloadStage.Done);
            BytesToDownload = 0;
            BytesDownloadedApprox = 0;
            NetworkSpeedBytesPerSecond = 0;

            yield return WaitCachedPrepareMinimumIfNeeded();

            SetProgressExact(1f);
            SetCheckingResourceText();

            FinishPrepareKey();

            Debug.Log($"[Preload] Key already prepared in this session and data is latest: {key}");
            yield break;
        }

        _prepareRunning = StartCoroutine(CoPrepareAddressableKeySafe(key, skipCatalogCheck: true));

        while (_prepareRunning != null)
            yield return null;
    }

    // ============================================================
    // BOOT CATALOG FLOW
    // ============================================================

    private IEnumerator RunCatalogBootstrapFlow()
    {
        int attempt = 0;

        while (attempt < maxCatalogRetries)
        {
            attempt++;

            ResetStateForCatalogAttempt();

            Debug.Log($"[Preload] ===== Bootstrap attempt {attempt}/{maxCatalogRetries} started =====");

            yield return CoCatalogOnce(attempt);

            if (IsReady && !HasFailed)
            {
                SetStage(PreloadStage.CatalogReady);

                // Catalog-only boot không được set về 0%.
                SetProgressExact(catalogOnlyOnBoot ? prepareProgressEnd : 1f);
                SetCheckingResourceText();

                _catalogRunning = null;

                Debug.Log("[Preload] Bootstrap ready. On-demand mode enabled.");
                yield break;
            }

            if (_retryRequested)
            {
                Debug.Log("[Preload] Manual retry requested. Reset attempt counter.");
                _retryRequested = false;
                attempt = 0;
            }

            if (attempt < maxCatalogRetries)
            {
                Debug.LogWarning($"[Preload] Bootstrap attempt failed. Retry after {retryDelaySeconds}s. LastError={LastError}");

                if (clearCatalogCacheOnRetryOnly && attempt >= 1)
                    ClearAddressablesCatalogCache();

                yield return new WaitForSecondsRealtime(retryDelaySeconds);
            }
        }

        if (!IsReady && !HasFailed)
            Fail("Bootstrap failed after all retries.");

        _catalogRunning = null;
    }

    private IEnumerator CoCatalogOnce(int attempt)
    {
        if (string.IsNullOrWhiteSpace(remoteCatalogJsonUrl) || string.IsNullOrWhiteSpace(remoteCatalogHashUrl))
        {
            Fail("Remote catalog URL is empty.");
            yield break;
        }

        if (enableProbeRemoteCatalog)
        {
            SetStage(PreloadStage.Probe);
            SetPrepareText(0.01f);

            yield return HttpProbeGet(remoteCatalogHashUrl, probeReadBytes);
            if (HasFailed) yield break;

            yield return HttpProbeGet(remoteCatalogJsonUrl, probeReadBytes);
            if (HasFailed) yield break;
        }

        SetStage(PreloadStage.Initialize);
        SetPrepareText(0.02f);

        var init = Addressables.InitializeAsync(false);

        yield return WaitWithTimeout(init, stepTimeoutSeconds, $"InitializeAsync timeout. attempt={attempt}");

        if (HasFailed)
        {
            SafeRelease(init);
            yield break;
        }

        if (!init.IsValid() || init.Status != AsyncOperationStatus.Succeeded)
        {
            string err = init.IsValid() && init.OperationException != null
                ? init.OperationException.ToString()
                : "InitializeAsync failed.";

            SafeRelease(init);
            Fail(err);
            yield break;
        }

        SafeRelease(init);

        if (forceLoadRemoteCatalog)
        {
            SetStage(PreloadStage.ForceLoadCatalog);
            SetPrepareText(0.03f);

            var loadCat = Addressables.LoadContentCatalogAsync(remoteCatalogJsonUrl, false);

            yield return WaitWithTimeout(loadCat, stepTimeoutSeconds, $"LoadContentCatalogAsync timeout. attempt={attempt}");

            if (HasFailed)
            {
                SafeRelease(loadCat);
                yield break;
            }

            if (!loadCat.IsValid() || loadCat.Status != AsyncOperationStatus.Succeeded)
            {
                string err = loadCat.IsValid() && loadCat.OperationException != null
                    ? loadCat.OperationException.ToString()
                    : "LoadContentCatalogAsync failed.";

                SafeRelease(loadCat);
                Fail(err);
                yield break;
            }

            SafeRelease(loadCat);
        }

        yield return CheckUpdateCatalogAndCleanOldBundles();

        if (HasFailed)
            yield break;

        IsReady = true;
        HasFailed = false;
        LastError = "";
        IsCloudFullyDownloaded = false;

        SetStage(catalogOnlyOnBoot ? PreloadStage.CatalogReady : PreloadStage.Done);

        // Không set catalog-only về 0%, vì UI sẽ tưởng hoàn thành 0%.
        SetProgressExact(catalogOnlyOnBoot ? prepareProgressEnd : 1f);
        SetCheckingResourceText();

        if (!catalogOnlyOnBoot)
        {
            Debug.LogWarning("[Preload] catalogOnlyOnBoot is OFF, but this version is designed for on-demand. No global cloud download will run.");
        }
    }

    // ============================================================
    // PREPARE KEY FLOW
    // ============================================================

    private IEnumerator CoPrepareAddressableKeySafe(string key, bool skipCatalogCheck = false)
    {
        try
        {
            yield return CoPrepareAddressableKey(key, skipCatalogCheck);
        }
        finally
        {
            FinishPrepareKey();
        }
    }

    private IEnumerator CoPrepareAddressableKey(string key, bool skipCatalogCheck = false)
    {
        IsPreparingKey = true;
        ActivePrepareKey = key;
        LastPrepareUsedCachedData = false;
        _prepareProgressStartRealtime = Time.realtimeSinceStartup;

        HasFailed = false;
        LastError = "";

        BytesToDownload = 0;
        BytesDownloadedApprox = 0;
        NetworkSpeedBytesPerSecond = 0;

        _lastSpeedBytes = 0;
        _lastSpeedTime = 0f;

        Debug.Log($"[Preload] ===== Prepare key started: {key} =====");

        if (!skipCatalogCheck && checkCatalogBeforeEveryPrepare)
        {
            yield return CheckUpdateCatalogAndCleanOldBundles();

            if (HasFailed)
            {
                FinishPrepareKey();
                yield break;
            }
        }

        SetStage(PreloadStage.GetSize);
        SetPrepareText(prepareProgressEnd);

        var sizeHandle = Addressables.GetDownloadSizeAsync(key);

        yield return WaitWithTimeout(sizeHandle, stepTimeoutSeconds, $"GetDownloadSizeAsync timeout. key={key}");

        if (HasFailed)
        {
            SafeRelease(sizeHandle);
            FinishPrepareKey();
            yield break;
        }

        if (!sizeHandle.IsValid() || sizeHandle.Status != AsyncOperationStatus.Succeeded)
        {
            string err = sizeHandle.IsValid() && sizeHandle.OperationException != null
                ? sizeHandle.OperationException.ToString()
                : "GetDownloadSizeAsync failed.";

            SafeRelease(sizeHandle);
            Fail($"GetDownloadSizeAsync failed. key={key}, error={err}");
            FinishPrepareKey();
            yield break;
        }

        long totalBytes = sizeHandle.Result;
        SafeRelease(sizeHandle);

        BytesToDownload = totalBytes;

        Debug.Log($"[Preload] Prepare key size. key={key}, size={FormatBytes(totalBytes)}");

        if (totalBytes > 0)
        {
            bool downloadOk = false;

            BeginLoadingPhase(
                PreloadStage.Download,
                "",
                prepareProgressEnd
            );

            UpdateDownloadLoadingText(0f, 0, totalBytes);

            int retryCount = Mathf.Max(1, maxDownloadRetries);

            for (int attempt = 1; attempt <= retryCount; attempt++)
            {
                HasFailed = false;
                LastError = "";

                Debug.Log($"[Preload] Download attempt {attempt}/{retryCount}. key={key}");

                yield return DownloadSingleKeyWithTimeout(
                    key,
                    totalBytes,
                    () => downloadOk = true
                );

                if (downloadOk && !HasFailed)
                    break;

                if (attempt < retryCount)
                {
                    Debug.LogWarning($"[Preload] Download failed. Retry after {retryDelaySeconds}s. key={key}, error={LastError}");

                    NetworkSpeedBytesPerSecond = 0;
                    _lastSpeedBytes = 0;
                    _lastSpeedTime = 0f;

                    yield return new WaitForSecondsRealtime(retryDelaySeconds);
                }
            }

            if (!downloadOk || HasFailed)
            {
                FinishPrepareKey();
                yield break;
            }
        }
        else
        {
            LastPrepareUsedCachedData = true;
            BytesDownloadedApprox = 0;
            NetworkSpeedBytesPerSecond = 0;

            // Đã có cache thì không được kéo về 0%.
            SetProgressExact(progressDownloadEnd);
            SetCheckingResourceText();

            Debug.Log($"[Preload] Nothing to download. Key already cached according to current data: {key}");
        }

        if (verifyAfterDownload)
        {
            yield return VerifyKeyDownloaded(key);

            if (HasFailed)
            {
                FinishPrepareKey();
                yield break;
            }
        }
        else
        {
            SetProgressExact(progressVerifyEnd);
            SetCheckingResourceText();
        }

        if (warmupKeyDataAfterDownload && (totalBytes > 0 || warmupCachedKeyData))
        {
            BeginLoadingPhase(
                PreloadStage.WarmupKeyData,
                "",
                progressVerifyEnd
            );

            SetWarmupProgress(0.01f);

            yield return WarmupAddressableKeyData(key);

            if (HasFailed)
            {
                FinishPrepareKey();
                yield break;
            }
        }
        else
        {
            SetProgressExact(progressWarmupEnd);
        }

        yield return WaitCachedPrepareMinimumIfNeeded();

        SetProgressExact(1f);
        BytesDownloadedApprox = BytesToDownload;
        SetCheckingResourceText();
        SetStage(PreloadStage.Done);

        if (rememberPreparedKeysInSession)
            _preparedKeys.Add(key);

        Debug.Log($"[Preload] ===== Prepare key DONE: {key} =====");

        FinishPrepareKey();
    }

    private IEnumerator DownloadSingleKeyWithTimeout(string key, long totalBytes, Action onSuccess)
    {
        var dl = Addressables.DownloadDependenciesAsync(key, autoReleaseHandle: false);

        float totalTimer = 0f;
        float stallTimer = 0f;
        float logTimer = 0f;

        long lastDownloadedBytes = -1;
        float lastProgress = -1f;

        while (dl.IsValid() && !dl.IsDone)
        {
            totalTimer += Time.unscaledDeltaTime;
            stallTimer += Time.unscaledDeltaTime;
            logTimer += Time.unscaledDeltaTime;

            DownloadStatus status = default;
            bool hasStatus = false;

            try
            {
                status = dl.GetDownloadStatus();
                hasStatus = true;
            }
            catch
            {
                hasStatus = false;
            }

            long downloadedBytes;
            long realTotalBytes;

            if (hasStatus && status.TotalBytes > 0)
            {
                downloadedBytes = status.DownloadedBytes;
                realTotalBytes = status.TotalBytes;
            }
            else
            {
                float p = Mathf.Clamp01(dl.PercentComplete);
                downloadedBytes = (long)(totalBytes * p);
                realTotalBytes = totalBytes;
            }

            downloadedBytes = ClampLong(downloadedBytes, 0, realTotalBytes);

            BytesDownloadedApprox = downloadedBytes;

            float download01 = realTotalBytes > 0
                ? Mathf.Clamp01((float)downloadedBytes / realTotalBytes)
                : Mathf.Clamp01(dl.PercentComplete);

            float overall01 = Map01(download01, prepareProgressEnd, progressDownloadEnd);
            SetProgressExact(overall01);

            UpdateDownloadLoadingText(download01, downloadedBytes, realTotalBytes);

            bool progressedByBytes = downloadedBytes > lastDownloadedBytes;
            bool progressedByPercent = download01 > lastProgress + 0.0005f;

            if (progressedByBytes || progressedByPercent)
            {
                stallTimer = 0f;
                lastDownloadedBytes = downloadedBytes;
                lastProgress = download01;
            }

            if (verboseProgressLog && logTimer >= progressLogInterval)
            {
                logTimer = 0f;

                Debug.Log(
                    $"[Preload] Downloading key='{key}' " +
                    $"download={download01:P1} " +
                    $"ui={DownloadPercent01:P1} " +
                    $"bytes={FormatBytes(downloadedBytes)}/{FormatBytes(realTotalBytes)} " +
                    $"speed={FormatBytes(NetworkSpeedBytesPerSecond)}/s"
                );
            }

            if (downloadTimeoutSeconds > 0f && totalTimer >= downloadTimeoutSeconds)
            {
                SafeRelease(dl);
                Fail($"Download timeout. key={key}");
                yield break;
            }

            if (downloadStallTimeoutSeconds > 0f && stallTimer >= downloadStallTimeoutSeconds)
            {
                SafeRelease(dl);
                Fail($"Download stalled. key={key}");
                yield break;
            }

            yield return null;
        }

        if (!dl.IsValid())
        {
            Fail($"Download handle invalid. key={key}");
            yield break;
        }

        if (dl.Status != AsyncOperationStatus.Succeeded)
        {
            string err = dl.OperationException != null
                ? dl.OperationException.ToString()
                : dl.Status.ToString();

            SafeRelease(dl);
            Fail($"Download failed. key={key}, error={err}");
            yield break;
        }

        SafeRelease(dl);

        SetProgressExact(progressDownloadEnd);
        BytesDownloadedApprox = totalBytes;
        SetCheckingResourceText();

        Debug.Log($"[Preload] Download key DONE: {key}");

        onSuccess?.Invoke();
    }

    private IEnumerator VerifyKeyDownloaded(string key)
    {
        SetStage(PreloadStage.Verify);

        SetProgressExact(progressDownloadEnd);
        SetCheckingResourceText();

        var verifyHandle = Addressables.GetDownloadSizeAsync(key);

        yield return WaitWithTimeout(verifyHandle, stepTimeoutSeconds, $"Verify GetDownloadSizeAsync timeout. key={key}");

        if (HasFailed)
        {
            SafeRelease(verifyHandle);
            yield break;
        }

        if (!verifyHandle.IsValid() || verifyHandle.Status != AsyncOperationStatus.Succeeded)
        {
            string err = verifyHandle.IsValid() && verifyHandle.OperationException != null
                ? verifyHandle.OperationException.ToString()
                : "Verify GetDownloadSizeAsync failed.";

            SafeRelease(verifyHandle);
            Fail($"Verify failed. key={key}, error={err}");
            yield break;
        }

        long remain = verifyHandle.Result;
        SafeRelease(verifyHandle);

        Debug.Log($"[Preload] Verify key='{key}' remain={FormatBytes(remain)}");

        if (remain > verifySizeThresholdBytes)
        {
            BytesToDownload = remain;
            Fail($"Verify failed. key={key}, remain={FormatBytes(remain)}");
            yield break;
        }

        SetProgressExact(progressVerifyEnd);
        SetCheckingResourceText();
    }

    // ============================================================
    // UPDATE DATA + CLEAN OLD DATA
    // ============================================================

    private IEnumerator CheckUpdateCatalogAndCleanOldBundles()
    {
        _catalogUpdatedThisRun = false;

        SetStage(PreloadStage.CheckCatalog);
        SetPrepareText(Mathf.Max(0.01f, DownloadPercent01));

        var check = Addressables.CheckForCatalogUpdates(false);

        yield return WaitWithTimeout(check, stepTimeoutSeconds, "CheckForCatalogUpdates timeout.");

        if (HasFailed)
        {
            SafeRelease(check);
            yield break;
        }

        if (!check.IsValid() || check.Status != AsyncOperationStatus.Succeeded)
        {
            string err = check.IsValid() && check.OperationException != null
                ? check.OperationException.ToString()
                : "CheckForCatalogUpdates failed.";

            SafeRelease(check);
            Fail(err);
            yield break;
        }

        IList<string> catalogs = check.Result;
        SafeRelease(check);

        if (catalogs == null || catalogs.Count == 0)
        {
            Debug.Log("[Preload] Remote data is already latest.");
            yield break;
        }

        SetStage(PreloadStage.UpdateCatalog);
        SetPrepareText(Mathf.Max(0.02f, DownloadPercent01));

        Debug.Log($"[Preload] Remote data updates found: {catalogs.Count}");

        var update = Addressables.UpdateCatalogs(catalogs, false);

        yield return WaitWithTimeout(update, stepTimeoutSeconds, "UpdateCatalogs timeout.");

        if (HasFailed)
        {
            SafeRelease(update);
            yield break;
        }

        if (!update.IsValid() || update.Status != AsyncOperationStatus.Succeeded)
        {
            string err = update.IsValid() && update.OperationException != null
                ? update.OperationException.ToString()
                : "UpdateCatalogs failed.";

            SafeRelease(update);
            Fail(err);
            yield break;
        }

        SafeRelease(update);

        _catalogUpdatedThisRun = true;
        _preparedKeys.Clear();

        Debug.Log("[Preload] Remote data updated. Prepared key session cache cleared.");

        if (cleanOldBundleCacheAfterCatalogUpdate)
            yield return CleanOldBundleCache();
    }

    private IEnumerator CleanOldBundleCache()
    {
        SetStage(PreloadStage.CleanOldBundleCache);
        SetPrepareText(Mathf.Max(0.03f, DownloadPercent01));

        Debug.Log("[Preload] Clean old bundle cache started.");

        var clean = Addressables.CleanBundleCache();

        yield return WaitWithTimeout(clean, stepTimeoutSeconds, "CleanBundleCache timeout.");

        if (HasFailed)
        {
            SafeRelease(clean);
            yield break;
        }

        if (!clean.IsValid() || clean.Status != AsyncOperationStatus.Succeeded)
        {
            string err = clean.IsValid() && clean.OperationException != null
                ? clean.OperationException.ToString()
                : "CleanBundleCache failed.";

            SafeRelease(clean);
            Debug.LogWarning("[Preload] " + err);

            yield break;
        }

        bool result = clean.Result;
        SafeRelease(clean);

        Debug.Log($"[Preload] Clean old bundle cache completed. result={result}");
    }

    // ============================================================
    // WARMUP / GIẢI NÉN KEY
    // ============================================================

    private IEnumerator WarmupAddressableKeyData(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
            yield break;

        SetStage(PreloadStage.WarmupKeyData);
        SetWarmupProgress(0.01f);

        List<IResourceLocation> allLocations = new List<IResourceLocation>();

        var locHandle = Addressables.LoadResourceLocationsAsync(key);

        yield return WaitWithTimeout(locHandle, stepTimeoutSeconds, $"LoadResourceLocationsAsync timeout. key={key}");

        if (HasFailed)
        {
            SafeRelease(locHandle);
            yield break;
        }

        if (!locHandle.IsValid() || locHandle.Status != AsyncOperationStatus.Succeeded)
        {
            string err = locHandle.IsValid() && locHandle.OperationException != null
                ? locHandle.OperationException.ToString()
                : "LoadResourceLocationsAsync failed.";

            SafeRelease(locHandle);

            Debug.LogWarning($"[Preload] Warmup locations failed but continue. key={key}, error={err}");

            SetWarmupProgress(1f);
            yield break;
        }

        if (locHandle.Result != null)
        {
            HashSet<string> uniqueKeys = new HashSet<string>();

            foreach (IResourceLocation loc in locHandle.Result)
            {
                if (loc == null)
                    continue;

                string unique = BuildLocationUniqueKey(loc);

                if (uniqueKeys.Contains(unique))
                    continue;

                uniqueKeys.Add(unique);
                allLocations.Add(loc);
            }
        }

        SafeRelease(locHandle);

        if (allLocations.Count == 0)
        {
            Debug.LogWarning($"[Preload] No locations found for warmup. key={key}");
            SetWarmupProgress(1f);
            yield break;
        }

        List<IResourceLocation> sceneLocations = new List<IResourceLocation>();
        List<IResourceLocation> assetLocations = new List<IResourceLocation>();

        SplitLocations(allLocations, sceneLocations, assetLocations);

        Debug.Log(
            $"[Preload] Warmup key started: {key}\n" +
            $"Total locations={allLocations.Count}\n" +
            $"Scene locations={sceneLocations.Count}\n" +
            $"Asset locations={assetLocations.Count}"
        );

        if (sceneLocations.Count > 0 && skipSceneWarmup)
        {
            Debug.Log(
                $"[Preload] Skip scene warmup for key={key}. Scene will be loaded by LoadingScreenController."
            );
        }

        int totalWork = Mathf.Max(1, assetLocations.Count);
        int doneWork = 0;

        yield return WarmupAssetLocationsByBatch(
            assetLocations,
            doneWork,
            totalWork,
            addedDone =>
            {
                doneWork += addedDone;
            }
        );

        if (HasFailed)
            yield break;

        SetWarmupProgress(1f);

        Debug.Log($"[Preload] Warmup key DONE: {key}");
    }

    private IEnumerator WarmupAssetLocationsByBatch(
        List<IResourceLocation> assetLocations,
        int doneWorkAtStart,
        int totalWork,
        Action<int> onBatchDoneWork)
    {
        if (assetLocations == null || assetLocations.Count == 0)
        {
            SetWarmupProgress(1f);
            yield break;
        }

        int batchSize = Mathf.Max(1, warmupAssetBatchSize);
        int index = 0;
        int batchIndex = 0;

        while (index < assetLocations.Count)
        {
            int count = Mathf.Min(batchSize, assetLocations.Count - index);
            List<AsyncOperationHandle<UnityEngine.Object>> running =
                new List<AsyncOperationHandle<UnityEngine.Object>>(count);

            for (int i = 0; i < count; i++)
            {
                IResourceLocation loc = assetLocations[index + i];
                string locationKey = SafeLocationKey(loc);

                try
                {
                    var h = Addressables.LoadAssetAsync<UnityEngine.Object>(loc);
                    running.Add(h);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Preload] Warmup asset threw exception. key={locationKey}, error={e}");

                    if (!continueWhenWarmupAssetFailed)
                    {
                        Fail($"Warmup asset exception. key={locationKey}, error={e.Message}");
                        yield break;
                    }
                }
            }

            float timer = 0f;
            float logTimer = 0f;
            bool batchDone = false;

            while (!batchDone)
            {
                timer += Time.unscaledDeltaTime;
                logTimer += Time.unscaledDeltaTime;

                batchDone = true;

                float batchProgress = 0f;

                for (int i = 0; i < running.Count; i++)
                {
                    var h = running[i];

                    if (h.IsValid())
                    {
                        batchProgress += Mathf.Clamp01(h.PercentComplete);

                        if (!h.IsDone)
                            batchDone = false;
                    }
                    else
                    {
                        batchProgress += 1f;
                    }
                }

                if (running.Count > 0)
                    batchProgress /= running.Count;
                else
                    batchProgress = 1f;

                int currentDoneApprox = doneWorkAtStart + index;
                float warmup01 = Mathf.Clamp01(
                    (currentDoneApprox + batchProgress * count) / Mathf.Max(1f, totalWork)
                );

                SetWarmupProgress(Mathf.Max(0.01f, warmup01));

                if (verboseProgressLog && logTimer >= progressLogInterval)
                {
                    logTimer = 0f;
                    Debug.Log(
                        $"[Preload] Warming assets index={index}/{assetLocations.Count}, " +
                        $"batch={count}, batchProgress={batchProgress:P1}, ui={DownloadPercent01:P1}"
                    );
                }

                if (warmupAssetBatchTimeoutSeconds > 0f && timer >= warmupAssetBatchTimeoutSeconds)
                {
                    Debug.LogWarning($"[Preload] Warmup asset batch timeout. index={index}, count={count}");

                    for (int i = 0; i < running.Count; i++)
                        SafeRelease(running[i]);

                    if (!continueWhenWarmupAssetFailed)
                    {
                        Fail($"Warmup asset batch timeout. index={index}");
                        yield break;
                    }

                    break;
                }

                yield return null;
            }

            int successOrSkipped = 0;

            for (int i = 0; i < running.Count; i++)
            {
                var h = running[i];

                if (!h.IsValid())
                {
                    successOrSkipped++;
                    continue;
                }

                if (h.Status != AsyncOperationStatus.Succeeded)
                {
                    string err = h.OperationException != null
                        ? h.OperationException.ToString()
                        : h.Status.ToString();

                    Debug.LogWarning($"[Preload] Warmup asset failed but continue. error={err}");

                    if (!continueWhenWarmupAssetFailed)
                    {
                        SafeRelease(h);
                        Fail($"Warmup asset failed. error={err}");
                        yield break;
                    }
                }

                SafeRelease(h);
                successOrSkipped++;
            }

            index += count;
            batchIndex++;

            onBatchDoneWork?.Invoke(successOrSkipped);

            float done01 = Mathf.Clamp01((float)index / Mathf.Max(1, assetLocations.Count));
            SetWarmupProgress(done01);

            if (unloadUnusedAssetsEveryBatches > 0 &&
                batchIndex % unloadUnusedAssetsEveryBatches == 0)
            {
                Debug.Log("[Preload] Resources.UnloadUnusedAssets after warmup batches.");
                yield return Resources.UnloadUnusedAssets();
                GC.Collect();
            }

            yield return null;
        }
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private List<string> BuildUniquePrepareKeyList(IEnumerable<string> keys)
    {
        List<string> result = new List<string>();
        HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (keys == null)
            return result;

        foreach (string rawKey in keys)
        {
            if (string.IsNullOrWhiteSpace(rawKey))
                continue;

            string key = rawKey.Trim();

            if (seen.Add(key))
                result.Add(key);
        }

        return result;
    }

    private void BeginProgressWindow(int index, int totalCount)
    {
        totalCount = Mathf.Max(1, totalCount);
        index = Mathf.Clamp(index, 0, totalCount - 1);

        float groupProgressEnd01 = Mathf.Min(progressWarmupEnd, 0.99f);

        _progressWindowStart01 = Mathf.Clamp01(((float)index / totalCount) * groupProgressEnd01);
        _progressWindowEnd01 = Mathf.Clamp01(((float)(index + 1) / totalCount) * groupProgressEnd01);
        _progressWindowActive = true;
    }

    private void ClearProgressWindow()
    {
        _progressWindowActive = false;
        _progressWindowStart01 = 0f;
        _progressWindowEnd01 = 1f;
    }

    private void ApplyUrlsFromRuntimeEnv()
    {
        if (!AppBuildEnvRuntime.HasConfig)
        {
            Debug.LogWarning("[Preload] Missing AppBuildEnv.asset in Resources/AppBuildEnv. Fallback to inspector URLs.");
            return;
        }

        remoteCatalogJsonUrl = AppBuildEnvRuntime.RemoteCatalogJsonUrl;
        remoteCatalogHashUrl = AppBuildEnvRuntime.RemoteCatalogHashUrl;

        Debug.Log(
            "[Preload] Runtime ENV loaded:\n" +
            $"APP_ENV={AppBuildEnvRuntime.EnvironmentName}\n" +
            $"API_ENV={AppBuildEnvRuntime.ApiEnvironmentName}\n" +
            $"RELEASES={AppBuildEnvRuntime.ReleasesFolder}\n" +
            $"PLATFORM={AppBuildEnvRuntime.PlatformName}\n" +
            $"GCS_BUCKET={AppBuildEnvRuntime.GcsBucket}\n" +
            $"ROOT={AppBuildEnvRuntime.AddressablesRootFolder}\n" +
            $"CATALOG_JSON={remoteCatalogJsonUrl}\n" +
            $"CATALOG_HASH={remoteCatalogHashUrl}"
        );
    }

    private void SetupAddressablesLogging()
    {
        if (enableAddressablesRequestLog)
        {
            Addressables.WebRequestOverride = (req) =>
            {
                if (req == null || string.IsNullOrEmpty(req.url))
                    return;

                if (req.url.Contains("catalog") ||
                    req.url.EndsWith(".hash") ||
                    req.url.EndsWith(".json") ||
                    req.url.Contains(".bundle"))
                {
                    Debug.Log($"[ADDR REQ] {req.method} {req.url}");
                }
            };
        }

        ResourceManager.ExceptionHandler = (handle, ex) =>
        {
            Debug.LogError($"[ADDR EX] stage={Stage} name={handle.DebugName}\n{ex}");
        };
    }

    private void ResetStateForCatalogAttempt()
    {
        IsReady = false;
        HasFailed = false;
        LastError = "";

        BytesToDownload = 0;
        BytesDownloadedApprox = 0;
        IsCloudFullyDownloaded = false;
        LastPrepareUsedCachedData = false;

        // Reset thật sự cho lượt bootstrap mới.
        DownloadPercent01 = 0f;

        SetStage(PreloadStage.None);

        _lastSpeedBytes = 0;
        _lastSpeedTime = 0f;

        NetworkSpeedBytesPerSecond = 0;

        SetCheckingResourceText();
    }

    private IEnumerator HttpProbeGet(string url, int readBytes)
    {
        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = 15;
            req.downloadHandler = new DownloadHandlerBuffer();

            if (readBytes > 0)
                req.SetRequestHeader("Range", $"bytes=0-{readBytes - 1}");

            yield return req.SendWebRequest();

            bool ok =
                req.result == UnityWebRequest.Result.Success &&
                (req.responseCode == 200 || req.responseCode == 206);

            if (!ok)
            {
                Fail($"HTTP probe failed. code={req.responseCode}, url={url}, error={req.error}");
                yield break;
            }

            Debug.Log($"[Preload] HTTP probe OK: {url}, code={req.responseCode}");
        }
    }

    private IEnumerator WaitWithTimeout(AsyncOperationHandle handle, float timeoutSeconds, string timeoutMsg)
    {
        float t = 0f;

        while (handle.IsValid() && !handle.IsDone)
        {
            if (timeoutSeconds > 0f)
            {
                t += Time.unscaledDeltaTime;

                if (t >= timeoutSeconds)
                {
                    Fail(timeoutMsg);
                    yield break;
                }
            }

            yield return null;
        }

        if (!handle.IsValid())
        {
            Fail(timeoutMsg + " | handle invalid.");
        }
    }

    private void ClearAddressablesCatalogCache()
    {
        try
        {
            string dir = Path.Combine(Application.persistentDataPath, "com.unity.addressables");

            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
                Debug.Log($"[Preload] Deleted Addressables cache: {dir}");
            }
            else
            {
                Debug.Log($"[Preload] Addressables cache not found: {dir}");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Preload] ClearAddressablesCatalogCache failed: {e.Message}");
        }
    }

    private bool ShouldPreserveProgressForPrepareSession()
    {
        if (HasFailed || DownloadPercent01 <= 0f)
            return false;

        return _catalogRunning != null || Stage == PreloadStage.CatalogReady;
    }

    private void BeginNewLoadingSession(bool preserveCurrentProgress = false)
    {
        float startProgress01 = preserveCurrentProgress ? Mathf.Clamp01(DownloadPercent01) : 0f;

        int phaseBefore = LoadingPhaseId;
        SetStage(PreloadStage.None);

        // Nếu Stage đang là None rồi thì vẫn phải bump id để UI biết đây là lượt load mới.
        if (LoadingPhaseId == phaseBefore)
            LoadingPhaseId++;

        HasFailed = false;
        LastError = "";

        BytesToDownload = 0;
        BytesDownloadedApprox = 0;
        NetworkSpeedBytesPerSecond = 0;
        LastPrepareUsedCachedData = false;

        _lastSpeedBytes = 0;
        _lastSpeedTime = 0f;

        // Preserve bootstrap progress when a prepare session immediately follows catalog boot.
        DownloadPercent01 = startProgress01;

        SetCheckingResourceText();
    }

    private void SetStage(PreloadStage stage)
    {
        if (Stage == stage)
            return;

        Stage = stage;
        LoadingPhaseId++;
    }

    private void BeginLoadingPhase(PreloadStage stage, string text, float progress01)
    {
        SetStage(stage);
        SetProgressExact(progress01);
        SetCheckingResourceText();
    }

    private string CurrentOverallPercentText()
    {
        return FormatProgressPercent(DownloadPercent01);
    }

    private float Map01(float value01, float from, float to)
    {
        value01 = Mathf.Clamp01(value01);
        from = Mathf.Clamp01(from);
        to = Mathf.Clamp01(to);

        if (to < from)
            to = from;

        return Mathf.Lerp(from, to, value01);
    }

    private void SetProgressExact(float p01)
    {
        p01 = Mathf.Clamp01(p01);

        if (IsPreparingKey &&
            LastPrepareUsedCachedData &&
            cachedDataMinimumLoadSeconds > 0f)
        {
            float elapsed = Time.realtimeSinceStartup - _prepareProgressStartRealtime;
            float timeCap = Mathf.Clamp01(elapsed / cachedDataMinimumLoadSeconds);
            p01 = Mathf.Min(p01, timeCap);
        }

        if (_progressWindowActive)
            p01 = Mathf.Lerp(_progressWindowStart01, _progressWindowEnd01, p01);

        // Không cho progress tụt lùi trong cùng một lượt load.
        DownloadPercent01 = Mathf.Max(DownloadPercent01, p01);
    }

    private void SetPrepareText(float progress01)
    {
        float p = Mathf.Clamp01(progress01);
        SetProgressExact(Mathf.Min(prepareProgressEnd, p));

        SetCheckingResourceText();
    }

    private void SetWarmupProgress(float warmup01)
    {
        warmup01 = Mathf.Clamp01(warmup01);

        float overall01 = Map01(warmup01, progressVerifyEnd, progressWarmupEnd);
        SetProgressExact(overall01);

        SetCheckingResourceText();
    }

    private void UpdateDownloadLoadingText(float download01, long downloadedBytes, long totalBytes)
    {
        UpdateNetworkSpeed(downloadedBytes);

        SetCheckingResourceText();
    }

    private void UpdateNetworkSpeed(long downloadedBytes)
    {
        float now = Time.realtimeSinceStartup;

        if (_lastSpeedTime <= 0f)
        {
            _lastSpeedTime = now;
            _lastSpeedBytes = downloadedBytes;
            NetworkSpeedBytesPerSecond = 0;
            return;
        }

        float dt = now - _lastSpeedTime;

        if (dt < 0.5f)
            return;

        long deltaBytes = Math.Max(0L, downloadedBytes - _lastSpeedBytes);

        NetworkSpeedBytesPerSecond = (long)(deltaBytes / Math.Max(0.001f, dt));

        _lastSpeedBytes = downloadedBytes;
        _lastSpeedTime = now;
    }

    private void FinishPrepareKey()
    {
        IsPreparingKey = false;
        ActivePrepareKey = "";
        _prepareRunning = null;
    }

    private IEnumerator WaitCachedPrepareMinimumIfNeeded()
    {
        if (!LastPrepareUsedCachedData || cachedDataMinimumLoadSeconds <= 0f)
            yield break;

        while (Time.realtimeSinceStartup - _prepareProgressStartRealtime < cachedDataMinimumLoadSeconds)
        {
            SetProgressExact(1f);
            SetCheckingResourceText();

            yield return null;
        }
    }

    private void SetCheckingResourceText()
    {
        LoadingText = $"Đang kiểm tra tài nguyên: {CurrentOverallPercentText()}";
    }

    private static string FormatProgressPercent(float p01)
    {
        return (Mathf.Clamp01(p01) * 100f).ToString("0.00", CultureInfo.InvariantCulture) + "%";
    }

    private void Fail(string message)
    {
        HasFailed = true;
        LastError = message;
        SetStage(PreloadStage.Failed);
        SetCheckingResourceText();

        Debug.LogError("[Preload] " + message);
    }

    private string BuildLocationUniqueKey(IResourceLocation loc)
    {
        if (loc == null)
            return "null";

        string key = loc.PrimaryKey ?? "";
        string internalId = loc.InternalId ?? "";
        string provider = loc.ProviderId ?? "";
        string type = loc.ResourceType != null ? loc.ResourceType.FullName : "";

        return $"{key}|{internalId}|{provider}|{type}";
    }

    private string SafeLocationKey(IResourceLocation loc)
    {
        if (loc == null)
            return "null";

        if (!string.IsNullOrEmpty(loc.PrimaryKey))
            return loc.PrimaryKey;

        if (!string.IsNullOrEmpty(loc.InternalId))
            return loc.InternalId;

        return loc.ToString();
    }

    private void SplitLocations(
        List<IResourceLocation> allLocations,
        List<IResourceLocation> sceneLocations,
        List<IResourceLocation> assetLocations)
    {
        for (int i = 0; i < allLocations.Count; i++)
        {
            IResourceLocation loc = allLocations[i];

            if (loc == null)
                continue;

            if (IsSceneLocation(loc))
                sceneLocations.Add(loc);
            else
                assetLocations.Add(loc);
        }
    }

    private bool IsSceneLocation(IResourceLocation loc)
    {
        if (loc == null)
            return false;

        Type t = loc.ResourceType;

        if (t == typeof(SceneInstance))
            return true;

        string providerId = loc.ProviderId;
        if (!string.IsNullOrEmpty(providerId) &&
            providerId.IndexOf("Scene", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        string key = loc.PrimaryKey;
        if (!string.IsNullOrEmpty(key) &&
            key.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
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
        }
    }

    private static long ClampLong(long value, long min, long max)
    {
        if (value < min)
            return min;

        if (value > max)
            return max;

        return value;
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 1024L)
            return $"{bytes} B";

        double kb = bytes / 1024.0;
        if (kb < 1024.0)
            return $"{kb:0.##} KB";

        double mb = kb / 1024.0;
        if (mb < 1024.0)
            return $"{mb:0.##} MB";

        double gb = mb / 1024.0;
        return $"{gb:0.##} GB";
    }

#endif
}
