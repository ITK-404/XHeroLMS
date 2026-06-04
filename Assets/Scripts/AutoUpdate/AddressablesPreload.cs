using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
#endif

/// <summary>
/// Đặt trong BootstrapScene hoặc auto-create bởi BootFlow.
/// Preload Addressables remote content từ GCS.
/// 
/// Flow mới:
/// 1. Chuẩn bị Addressables.
/// 2. Download toàn bộ label cloud.
///    - % tải lấy theo overall thật.
///    - Không map 35%, 70%, 90%.
///    - Text có dung lượng đã tải / tổng + tốc độ mạng.
/// 3. Download xong 100% mới chuyển sang warmup/giải nén.
/// 4. Warmup toàn bộ ResourceLocation lấy được từ cloud.
///    - Scene: LoadSceneAsync Additive rồi Unload.
///    - Asset thường: LoadAssetAsync<Object> rồi Release.
/// 5. Warmup xong 100% mới IsReady = true.
/// </summary>
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
        GetSize,
        Download,
        Verify,
        WarmupAllCloudData,
        Done,
        Failed
    }

    [Header("State (Read-only)")]
    public PreloadStage Stage { get; private set; } = PreloadStage.None;

    public bool IsReady { get; private set; }
    public bool HasFailed { get; private set; }
    public string LastError { get; private set; } = "";

    /// <summary>
    /// Progress hiện tại của phase đang chạy.
    /// - Download phase: lấy theo overall thật.
    /// - Warmup phase: lấy theo số tài nguyên đã warmup / tổng.
    /// </summary>
    public float DownloadPercent01 { get; private set; }

    public long BytesToDownload { get; private set; }
    public long BytesDownloadedApprox { get; private set; }

    public bool IsCloudFullyDownloaded { get; private set; }

    /// <summary>
    /// Text cho UI loading.
    /// IntroManager nên ưu tiên lấy text này.
    /// </summary>
    public string LoadingText { get; private set; } = "Đang chuẩn bị tài nguyên";

    public long NetworkSpeedBytesPerSecond { get; private set; }

#if ADDRESSABLES

    [Header("Label to Preload")]
    [SerializeField] private List<string> preloadLabels = new List<string> { "cloud" };

    [Header("Retry / Timeout")]
    [SerializeField] private int maxRetries = 3;
    [SerializeField] private float stepTimeoutSeconds = 25f;
    [SerializeField] private float downloadTimeoutSeconds = 300f;
    [SerializeField] private float downloadStallTimeoutSeconds = 45f;
    [SerializeField] private float retryDelaySeconds = 1.5f;

    [Header("Verify downloaded")]
    [SerializeField] private bool verifyAfterDownload = true;
    [SerializeField] private long verifySizeThresholdBytes = 0;

    [Header("Probe remote catalog")]
    [SerializeField] private bool enableProbeRemoteCatalog = true;
    [SerializeField] private int probeReadBytes = 64;

    [Header("Force latest catalog")]
    [SerializeField] private bool forceLoadRemoteCatalog = true;

    [Header("Cache")]
    [SerializeField] private bool clearCatalogCacheOnRetryOnly = true;
    [SerializeField] private bool clearCatalogCacheAfterDownloadFail = true;

    [Header("Warmup all downloaded cloud data")]
    [SerializeField] private bool warmupAllCloudDataAfterDownload = true;

    [Tooltip("Số asset thường được warmup song song mỗi batch. Quá cao dễ spike RAM.")]
    [SerializeField] private int warmupAssetBatchSize = 6;

    [SerializeField] private float warmupAssetBatchTimeoutSeconds = 240f;
    [SerializeField] private float warmupSceneTimeoutSeconds = 300f;

    [SerializeField] private bool continueWhenWarmupAssetFailed = true;
    [SerializeField] private bool continueWhenWarmupSceneFailed = true;

    [Tooltip("Gọi Resources.UnloadUnusedAssets sau mỗi số batch nhất định. 0 = tắt.")]
    [SerializeField] private int unloadUnusedAssetsEveryBatches = 8;

    [Header("Debug")]
    [SerializeField] private bool enableAddressablesRequestLog = true;
    [SerializeField] private bool verboseProgressLog = true;
    [SerializeField] private float progressLogInterval = 2f;

    private string remoteCatalogHashUrl = "";
    private string remoteCatalogJsonUrl = "";

    private Coroutine _running;
    private bool _retryRequested;
    private bool _lastAttemptFailedDuringDownload;

    private long _lastSpeedBytes;
    private float _lastSpeedTime;

#endif

    public void RequestRetry()
    {
#if ADDRESSABLES
        Debug.Log("[Preload] Retry requested by UI/user.");

        _retryRequested = true;

        if (_running == null)
            _running = StartCoroutine(RunPreloadFlow());
#endif
    }

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

        if (_running == null)
            _running = StartCoroutine(RunPreloadFlow());
#endif
    }

#if ADDRESSABLES

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

    private IEnumerator RunPreloadFlow()
    {
        int attempt = 0;

        while (attempt < maxRetries)
        {
            attempt++;

            ResetStateForAttempt();

            Debug.Log($"[Preload] ===== Attempt {attempt}/{maxRetries} started =====");

            yield return CoPreloadOnce(attempt);

            if (IsReady && !HasFailed)
            {
                Stage = PreloadStage.Done;
                SetProgressExact(1f);
                LoadingText = "Hoàn tất";
                IsCloudFullyDownloaded = true;
                _running = null;

                Debug.Log("[Preload] DONE. Cloud content is downloaded + warmed up.");
                yield break;
            }

            if (_retryRequested)
            {
                Debug.Log("[Preload] Manual retry requested. Reset attempt counter.");
                _retryRequested = false;
                attempt = 0;
            }

            if (attempt < maxRetries)
            {
                Debug.LogWarning($"[Preload] Attempt failed. Retry after {retryDelaySeconds}s. LastError={LastError}");

                if (clearCatalogCacheAfterDownloadFail && _lastAttemptFailedDuringDownload)
                {
                    Debug.LogWarning("[Preload] Last attempt failed during download. Clearing Addressables catalog cache before retry.");
                    ClearAddressablesCatalogCache();
                }

                yield return new WaitForSecondsRealtime(retryDelaySeconds);
            }
        }

        _running = null;
    }

    private void ResetStateForAttempt()
    {
        IsReady = false;
        HasFailed = false;
        LastError = "";
        SetProgressExact(0.01f);
        BytesToDownload = 0;
        BytesDownloadedApprox = 0;
        IsCloudFullyDownloaded = false;
        Stage = PreloadStage.None;
        _lastAttemptFailedDuringDownload = false;
        _lastSpeedBytes = 0;
        _lastSpeedTime = 0f;
        NetworkSpeedBytesPerSecond = 0;
        LoadingText = "Đang chuẩn bị tài nguyên";
    }

    private IEnumerator CoPreloadOnce(int attempt)
    {
        if (string.IsNullOrWhiteSpace(remoteCatalogJsonUrl) || string.IsNullOrWhiteSpace(remoteCatalogHashUrl))
        {
            yield break;
        }

        if (enableProbeRemoteCatalog)
        {
            Stage = PreloadStage.Probe;
            SetProgressExact(0.01f);
            LoadingText = "Đang kiểm tra tài nguyên cloud";

            yield return HttpProbeGet(remoteCatalogHashUrl, probeReadBytes);
            if (HasFailed) yield break;

            yield return HttpProbeGet(remoteCatalogJsonUrl, probeReadBytes);
            if (HasFailed) yield break;
        }

        if (clearCatalogCacheOnRetryOnly && attempt >= 2)
        {
            Stage = PreloadStage.ClearCache;
            SetProgressExact(0.01f);
            LoadingText = "Đang làm mới cache tài nguyên";
            ClearAddressablesCatalogCache();
        }

        Stage = PreloadStage.Initialize;
        SetProgressExact(0.01f);
        LoadingText = "Đang khởi tạo tài nguyên";

        var init = Addressables.InitializeAsync(false);
        yield return WaitWithTimeout(init, stepTimeoutSeconds, $"InitializeAsync timeout (attempt {attempt})");

        if (HasFailed)
        {
            SafeRelease(init);
            yield break;
        }

        if (!init.IsValid())
        {
            yield break;
        }

        if (init.Status != AsyncOperationStatus.Succeeded)
        {
            SafeRelease(init);
            yield break;
        }

        SafeRelease(init);

        if (forceLoadRemoteCatalog)
        {
            Stage = PreloadStage.ForceLoadCatalog;
            SetProgressExact(0.01f);
            LoadingText = "Đang tải catalog tài nguyên";

            var loadCat = Addressables.LoadContentCatalogAsync(remoteCatalogJsonUrl, false);
            yield return WaitWithTimeout(loadCat, stepTimeoutSeconds, $"LoadContentCatalogAsync timeout (attempt {attempt})");

            if (HasFailed)
            {
                SafeRelease(loadCat);
                yield break;
            }

            if (!loadCat.IsValid() || loadCat.Status != AsyncOperationStatus.Succeeded)
            {
                SafeRelease(loadCat);
                yield break;
            }

            SafeRelease(loadCat);
        }

        Stage = PreloadStage.CheckCatalog;
        SetProgressExact(0.01f);
        LoadingText = "Đang kiểm tra cập nhật tài nguyên";

        var check = Addressables.CheckForCatalogUpdates(false);
        yield return WaitWithTimeout(check, stepTimeoutSeconds, $"CheckForCatalogUpdates timeout (attempt {attempt})");

        if (HasFailed)
        {
            SafeRelease(check);
            yield break;
        }

        if (!check.IsValid())
        {
            yield break;
        }

        if (check.Status != AsyncOperationStatus.Succeeded)
        {
            SafeRelease(check);
            yield break;
        }

        IList<string> catalogs = check.Result;
        SafeRelease(check);

        if (catalogs != null && catalogs.Count > 0)
        {
            Stage = PreloadStage.UpdateCatalog;
            SetProgressExact(0.01f);
            LoadingText = "Đang cập nhật catalog tài nguyên";

            Debug.Log($"[Preload] Catalog updates found: {catalogs.Count}");

            var update = Addressables.UpdateCatalogs(catalogs, false);
            yield return WaitWithTimeout(update, stepTimeoutSeconds, $"UpdateCatalogs timeout (attempt {attempt})");

            if (HasFailed)
            {
                SafeRelease(update);
                yield break;
            }

            if (!update.IsValid())
            {
                yield break;
            }

            if (update.Status != AsyncOperationStatus.Succeeded)
            {
                SafeRelease(update);
                yield break;
            }

            SafeRelease(update);
        }
        else
        {
            Debug.Log("[Preload] No catalog updates.");
        }

        List<string> labels = BuildValidLabelList();
        if (labels == null || labels.Count == 0)
            yield break;

        Stage = PreloadStage.GetSize;
        SetProgressExact(0.01f);
        LoadingText = "Đang tính dung lượng tài nguyên";

        long totalBytes = 0;
        var perLabelBytes = new Dictionary<string, long>(labels.Count);

        for (int i = 0; i < labels.Count; i++)
        {
            string lb = labels[i];

            var sizeHandle = Addressables.GetDownloadSizeAsync(lb);
            yield return WaitWithTimeout(sizeHandle, stepTimeoutSeconds, $"GetDownloadSizeAsync timeout ({lb}) (attempt {attempt})");

            if (HasFailed)
            {
                SafeRelease(sizeHandle);
                yield break;
            }

            if (!sizeHandle.IsValid())
            {
                yield break;
            }

            if (sizeHandle.Status != AsyncOperationStatus.Succeeded)
            {
                SafeRelease(sizeHandle);
                yield break;
            }

            long b = sizeHandle.Result;
            SafeRelease(sizeHandle);

            perLabelBytes[lb] = b;
            totalBytes += b;

            Debug.Log($"[Preload] Label size: '{lb}' = {FormatBytes(b)}");
        }

        BytesToDownload = totalBytes;

        Debug.Log($"[Preload] Total bytes to download = {FormatBytes(BytesToDownload)}");

        if (BytesToDownload > 0)
        {
BeginLoadingPhase(
    PreloadStage.Download,
    $"Đang tải tài nguyên (1%+({FormatBytes(0)}/{FormatBytes(totalBytes)})+({FormatBytes(0)}/s))",
    0.01f
);

UpdateDownloadLoadingText(0.01f, 0, totalBytes);

            long downloadedBeforeCurrentLabel = 0;

            for (int i = 0; i < labels.Count; i++)
            {
                string lb = labels[i];

                long thisLabelBytes = perLabelBytes.TryGetValue(lb, out var bb) ? bb : 0;
                if (thisLabelBytes <= 0)
                {
                    Debug.Log($"[Preload] Skip label='{lb}' because size is 0.");
                    continue;
                }

                Debug.Log($"[Preload] Download label='{lb}' bytes={FormatBytes(thisLabelBytes)}");

                bool labelOk = false;

                yield return DownloadLabelWithTimeout(
                    label: lb,
                    labelBytes: thisLabelBytes,
                    downloadedBeforeCurrentLabel: downloadedBeforeCurrentLabel,
                    totalBytes: totalBytes,
                    onSuccess: () => labelOk = true
                );

                if (!labelOk || HasFailed)
                {
                    _lastAttemptFailedDuringDownload = true;
                    yield break;
                }

                downloadedBeforeCurrentLabel += thisLabelBytes;
                BytesDownloadedApprox = downloadedBeforeCurrentLabel;

                float overall01 = totalBytes <= 0
                    ? 1f
                    : Mathf.Clamp01((float)downloadedBeforeCurrentLabel / totalBytes);

                SetProgressExact(Mathf.Max(0.01f, overall01));
                UpdateDownloadLoadingText(DownloadPercent01, downloadedBeforeCurrentLabel, totalBytes);
            }
        }
        else
        {
            Debug.Log("[Preload] Nothing to download. Cache is already complete.");
            SetProgressExact(1f);
            LoadingText = "Tài nguyên đã có sẵn trong cache (100%)";
        }

        if (verifyAfterDownload)
        {
            Stage = PreloadStage.Verify;
            LoadingText = "Đang xác minh tài nguyên đã tải (100%)";

            long remainTotal = 0;

            for (int i = 0; i < labels.Count; i++)
            {
                string lb = labels[i];

                var verifyHandle = Addressables.GetDownloadSizeAsync(lb);
                yield return WaitWithTimeout(verifyHandle, stepTimeoutSeconds, $"Verify GetDownloadSizeAsync timeout ({lb}) (attempt {attempt})");

                if (HasFailed)
                {
                    SafeRelease(verifyHandle);
                    yield break;
                }

                if (!verifyHandle.IsValid())
                {
                    yield break;
                }

                if (verifyHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    SafeRelease(verifyHandle);
                    yield break;
                }

                long remain = verifyHandle.Result;
                SafeRelease(verifyHandle);

                remainTotal += remain;

                Debug.Log($"[Preload] Verify label='{lb}' remain={FormatBytes(remain)}");
            }

            if (remainTotal > verifySizeThresholdBytes)
            {
                BytesToDownload = remainTotal;

                yield break;
            }
        }

        IsCloudFullyDownloaded = true;

BeginLoadingPhase(
    PreloadStage.WarmupAllCloudData,
    "Đang giải nén tài nguyên",
    0.01f
);

yield return WarmupAllCloudData(labels);

        if (HasFailed)
            yield break;

        SetProgressExact(1f);
        BytesDownloadedApprox = BytesToDownload;
        IsReady = true;
        HasFailed = false;
        LastError = "";
        LoadingText = "Hoàn tất";
        Stage = PreloadStage.Done;
    }

    private List<string> BuildValidLabelList()
    {
        if (preloadLabels == null || preloadLabels.Count == 0)
        {
            return null;
        }

        List<string> labels = new List<string>();

        for (int i = 0; i < preloadLabels.Count; i++)
        {
            string lb = preloadLabels[i];

            if (string.IsNullOrWhiteSpace(lb))
                continue;

            lb = lb.Trim();

            if (!labels.Contains(lb))
                labels.Add(lb);
        }

        if (labels.Count == 0)
        {
            return null;
        }

        return labels;
    }

    private IEnumerator DownloadLabelWithTimeout(
        string label,
        long labelBytes,
        long downloadedBeforeCurrentLabel,
        long totalBytes,
        Action onSuccess)
    {
        var dl = Addressables.DownloadDependenciesAsync(label, autoReleaseHandle: false);

        float totalTimer = 0f;
        float stallTimer = 0f;
        float logTimer = 0f;

        long lastDownloadedBytes = -1;
        float lastProgress = -1f;

        while (!dl.IsDone)
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

            long currentLabelDownloaded;
            long currentLabelTotal;

            if (hasStatus && status.TotalBytes > 0)
            {
                currentLabelDownloaded = status.DownloadedBytes;
                currentLabelTotal = status.TotalBytes;
            }
            else
            {
                float p = Mathf.Clamp01(dl.PercentComplete);
                currentLabelDownloaded = (long)(labelBytes * p);
                currentLabelTotal = labelBytes;
            }

            currentLabelDownloaded = ClampLong(
                currentLabelDownloaded,
                0,
                currentLabelTotal
            );

            long overallDownloaded = downloadedBeforeCurrentLabel + currentLabelDownloaded;

            BytesDownloadedApprox = overallDownloaded;

            float labelProgress01 = currentLabelTotal > 0
                ? Mathf.Clamp01((float)currentLabelDownloaded / currentLabelTotal)
                : Mathf.Clamp01(dl.PercentComplete);

            float overall01 = totalBytes > 0
                ? Mathf.Clamp01((float)overallDownloaded / totalBytes)
                : labelProgress01;

            float ui01 = Mathf.Max(0.01f, overall01);

            SetProgressExact(ui01);
            UpdateDownloadLoadingText(ui01, overallDownloaded, totalBytes);

            bool progressedByBytes = currentLabelDownloaded > lastDownloadedBytes;
            bool progressedByPercent = labelProgress01 > lastProgress + 0.0005f;

            if (progressedByBytes || progressedByPercent)
            {
                stallTimer = 0f;
                lastDownloadedBytes = currentLabelDownloaded;
                lastProgress = labelProgress01;
            }

            if (verboseProgressLog && logTimer >= progressLogInterval)
            {
                logTimer = 0f;

                Debug.Log(
                    $"[Preload] Downloading '{label}' " +
                    $"label={labelProgress01:P1} " +
                    $"overall={overall01:P1} " +
                    $"bytes={FormatBytes(currentLabelDownloaded)}/{FormatBytes(currentLabelTotal)} " +
                    $"ui={DownloadPercent01:P1} " +
                    $"speed={FormatBytes(NetworkSpeedBytesPerSecond)}/s"
                );
            }

            if (downloadTimeoutSeconds > 0f && totalTimer >= downloadTimeoutSeconds)
            {
                SafeRelease(dl);
                yield break;
            }

            if (downloadStallTimeoutSeconds > 0f && stallTimer >= downloadStallTimeoutSeconds)
            {
                SafeRelease(dl);
                yield break;
            }

            yield return null;
        }

        if (!dl.IsValid())
        {
            yield break;
        }

        if (dl.Status != AsyncOperationStatus.Succeeded)
        {
            string err = dl.OperationException != null
                ? dl.OperationException.Message
                : dl.Status.ToString();

            SafeRelease(dl);
            yield break;
        }

        SafeRelease(dl);

        Debug.Log($"[Preload] Download label DONE: '{label}'");
        onSuccess?.Invoke();
    }

    // ============================================================
    // Warmup / giải nén toàn bộ cloud data theo label
    // ============================================================

    private IEnumerator WarmupAllCloudData(List<string> labels)
    {
        if (!warmupAllCloudDataAfterDownload)
        {
            Debug.Log("[Preload] Warmup all cloud data disabled.");
            SetProgressExact(1f);
            LoadingText = "Hoàn tất giải nén tài nguyên (100%)";
            yield break;
        }

        if (labels == null || labels.Count == 0)
        {
            Debug.LogWarning("[Preload] Warmup skipped because labels empty.");
            SetProgressExact(1f);
            LoadingText = "Hoàn tất giải nén tài nguyên (100%)";
            yield break;
        }

Stage = PreloadStage.WarmupAllCloudData;
SetProgressExact(0.01f);
LoadingText = "Đang giải nén tài nguyên";

        List<IResourceLocation> allLocations = new List<IResourceLocation>();

        yield return CollectAllCloudResourceLocations(labels, allLocations);

        if (HasFailed)
            yield break;

        if (allLocations.Count == 0)
        {
            Debug.LogWarning("[Preload] No resource locations found for cloud warmup.");
            SetProgressExact(1f);
            LoadingText = "Hoàn tất giải nén tài nguyên (100%)";
            yield break;
        }

        List<IResourceLocation> sceneLocations = new List<IResourceLocation>();
        List<IResourceLocation> assetLocations = new List<IResourceLocation>();

        SplitLocations(allLocations, sceneLocations, assetLocations);

        Debug.Log(
            "[Preload] Warmup all cloud data started.\n" +
            $"Total locations={allLocations.Count}\n" +
            $"Scene locations={sceneLocations.Count}\n" +
            $"Asset locations={assetLocations.Count}"
        );

// Không load scene thật trong phase giải nén nữa,
// vì LoadSceneAsync Additive activateOnLoad=true sẽ làm scene nhấp nháy / bị activate.
// Scene dependencies đã được DownloadDependenciesAsync(label cloud) tải trước đó.
// Phase này chỉ warmup asset thường để không làm thay đổi scene hiện tại.
int totalWork = Mathf.Max(1, assetLocations.Count);
int doneWork = 0;

if (sceneLocations.Count > 0)
{
    Debug.Log($"[Preload] Skip visual scene warmup. Scene locations={sceneLocations.Count}. Scenes will not be activated during preload.");
}

yield return WarmupAssetLocationsByBatch(assetLocations, doneWork, totalWork, addedDone =>
{
    doneWork += addedDone;
});

        if (HasFailed)
            yield break;

        SetProgressExact(1f);
        // LoadingText = $"Hoàn tất giải nén tài nguyên (100% - {totalWork}/{totalWork})";
        LoadingText = "Hoàn tất giải nén tài nguyên (100%)";

        Debug.Log("[Preload] Warmup all cloud data DONE.");
    }

    private IEnumerator CollectAllCloudResourceLocations(List<string> labels, List<IResourceLocation> output)
    {
        HashSet<string> uniqueKeys = new HashSet<string>();

        for (int i = 0; i < labels.Count; i++)
        {
            string label = labels[i];

            if (string.IsNullOrWhiteSpace(label))
                continue;

            Debug.Log($"[Preload] LoadResourceLocationsAsync label='{label}'");

            var locHandle = Addressables.LoadResourceLocationsAsync(label);

            yield return WaitWithTimeout(
                locHandle,
                stepTimeoutSeconds,
                $"LoadResourceLocationsAsync timeout. label={label}"
            );

            if (HasFailed)
            {
                SafeRelease(locHandle);
                yield break;
            }

            if (!locHandle.IsValid())
            {
                yield break;
            }

            if (locHandle.Status != AsyncOperationStatus.Succeeded)
            {
                string err = locHandle.OperationException != null
                    ? locHandle.OperationException.ToString()
                    : locHandle.Status.ToString();

                SafeRelease(locHandle);
                yield break;
            }

            if (locHandle.Result != null)
            {
                foreach (IResourceLocation loc in locHandle.Result)
                {
                    if (loc == null)
                        continue;

                    string key = BuildLocationUniqueKey(loc);

                    if (uniqueKeys.Contains(key))
                        continue;

                    uniqueKeys.Add(key);
                    output.Add(loc);
                }
            }

            Debug.Log($"[Preload] Collected locations after label='{label}': {output.Count}");

            SafeRelease(locHandle);
        }
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

        if (t == typeof(Scene))
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

    private IEnumerator WarmupSingleSceneLocation(
        IResourceLocation loc,
        int index,
        int total,
        Action<bool> onDone)
    {
        string key = SafeLocationKey(loc);

        Debug.Log($"[Preload] Warmup scene {index + 1}/{total}: {key}");

        AsyncOperationHandle<SceneInstance> handle = default;

        try
        {
            handle = Addressables.LoadSceneAsync(
                loc,
                LoadSceneMode.Additive,
                activateOnLoad: true
            );
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Preload] Warmup scene threw exception. key={key}, error={e}");

            onDone?.Invoke(false);
            yield break;
        }

        float timer = 0f;
        float logTimer = 0f;

        while (handle.IsValid() && !handle.IsDone)
        {
            timer += Time.unscaledDeltaTime;
            logTimer += Time.unscaledDeltaTime;

            if (logTimer >= progressLogInterval)
            {
                logTimer = 0f;
                Debug.Log($"[Preload] Warming scene='{key}', progress={handle.PercentComplete:P1}");
            }

            if (warmupSceneTimeoutSeconds > 0f && timer >= warmupSceneTimeoutSeconds)
            {
                Debug.LogWarning($"[Preload] Warmup scene timeout. key={key}, timeout={warmupSceneTimeoutSeconds}s");

                SafeRelease(handle);

                onDone?.Invoke(false);
                yield break;
            }

            yield return null;
        }

        if (!handle.IsValid())
        {
            Debug.LogWarning($"[Preload] Warmup scene handle invalid. key={key}");

            onDone?.Invoke(false);
            yield break;
        }

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            string err = handle.OperationException != null
                ? handle.OperationException.ToString()
                : handle.Status.ToString();

            Debug.LogWarning($"[Preload] Warmup scene failed. key={key}, error={err}");

            SafeRelease(handle);

            onDone?.Invoke(false);
            yield break;
        }

        yield return null;

        var unload = Addressables.UnloadSceneAsync(handle, autoReleaseHandle: true);

        float unloadTimer = 0f;

        while (unload.IsValid() && !unload.IsDone)
        {
            unloadTimer += Time.unscaledDeltaTime;

            if (warmupSceneTimeoutSeconds > 0f && unloadTimer >= warmupSceneTimeoutSeconds)
            {
                Debug.LogWarning($"[Preload] Warmup unload scene timeout. key={key}");

                if (!continueWhenWarmupSceneFailed)

                onDone?.Invoke(false);
                yield break;
            }

            yield return null;
        }

        Debug.Log($"[Preload] Warmup scene DONE: {key}");
        onDone?.Invoke(true);
    }

    private IEnumerator WarmupAssetLocationsByBatch(
        List<IResourceLocation> assetLocations,
        int doneWorkAtStart,
        int totalWork,
        Action<int> onBatchDoneWork)
    {
        if (assetLocations == null || assetLocations.Count == 0)
            yield break;

        int batchSize = Mathf.Max(1, warmupAssetBatchSize);
        int index = 0;
        int batchIndex = 0;

        while (index < assetLocations.Count)
        {
            int count = Mathf.Min(batchSize, assetLocations.Count - index);
            List<AsyncOperationHandle<UnityEngine.Object>> running = new List<AsyncOperationHandle<UnityEngine.Object>>(count);

            for (int i = 0; i < count; i++)
            {
                IResourceLocation loc = assetLocations[index + i];
                string key = SafeLocationKey(loc);

                try
                {
                    var h = Addressables.LoadAssetAsync<UnityEngine.Object>(loc);
                    running.Add(h);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Preload] Warmup asset threw exception. key={key}, error={e}");

                    if (!continueWhenWarmupAssetFailed)
                    {
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
                float totalProgress = Mathf.Clamp01((currentDoneApprox + batchProgress * count) / Mathf.Max(1f, totalWork));

                SetProgressExact(Mathf.Max(0.01f, totalProgress));
                UpdateWarmupLoadingText(currentDoneApprox, totalWork, DownloadPercent01);

                if (logTimer >= progressLogInterval)
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
                        yield break;
                    }
                }

                SafeRelease(h);
                successOrSkipped++;
            }

            index += count;
            batchIndex++;

            onBatchDoneWork?.Invoke(successOrSkipped);

            UpdateWarmupProgress(doneWorkAtStart + index, totalWork);

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

    private void UpdateWarmupProgress(int doneWork, int totalWork)
    {
        float p = Mathf.Clamp01((float)doneWork / Mathf.Max(1, totalWork));
        SetProgressExact(Mathf.Max(0.01f, p));
        UpdateWarmupLoadingText(doneWork, totalWork, DownloadPercent01);
    }

private void UpdateDownloadLoadingText(float t01, long downloadedBytes, long totalBytes)
{
    UpdateNetworkSpeed(downloadedBytes);

    int percent = Mathf.Clamp(Mathf.FloorToInt(t01 * 100f), 1, 100);

    LoadingText =
        $"Đang tải tài nguyên: {percent}% | {FormatBytes(NetworkSpeedBytesPerSecond)}/s | {FormatBytes(downloadedBytes)}/{FormatBytes(totalBytes)}";
}

private void UpdateWarmupLoadingText(int doneWork, int totalWork, float progress01)
{
    int percent = Mathf.Clamp(Mathf.FloorToInt(progress01 * 100f), 1, 100);
    LoadingText = $"Đang giải nén tài nguyên ({percent}%)";
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
                yield break;
            }

            Debug.Log($"[Preload] HTTP probe OK: {url}, code={req.responseCode}");
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
                Debug.Log($"[Preload] Deleted Addressables catalog cache: {dir}");
            }
            else
            {
                Debug.Log($"[Preload] Addressables catalog cache not found: {dir}");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Preload] ClearAddressablesCatalogCache failed: {e.Message}");
        }
    }

    private void SetProgressExact(float p01)
    {
        DownloadPercent01 = Mathf.Clamp01(p01);
    }

    private IEnumerator WaitWithTimeout(AsyncOperationHandle handle, float timeoutSeconds, string timeoutMsg)
    {
        float t = 0f;

        while (!handle.IsDone)
        {
            if (timeoutSeconds > 0f)
            {
                t += Time.unscaledDeltaTime;

                if (t >= timeoutSeconds)
                {
                    yield break;
                }
            }

            yield return null;
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

    public int LoadingPhaseId { get; private set; }
    private void BeginLoadingPhase(PreloadStage stage, string text, float progress01 = 0.01f)
{
    Stage = stage;
    LoadingPhaseId++;
    SetProgressExact(progress01);
    LoadingText = text;
}
}