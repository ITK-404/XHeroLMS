using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Networking;
#endif

/// <summary>
/// Đặt trong BootstrapScene hoặc auto-create bởi BootFlow.
/// Preload Addressables remote content từ GCS.
/// Bản update:
/// - Không kẹt vô hạn ở DownloadDependenciesAsync.
/// - Detect download bị đứng progress.
/// - Retry rõ ràng.
/// - Dùng GetDownloadStatus để lấy byte thật.
/// - Expose HasFailed/LastError để Loading UI xử lý.
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

#if ADDRESSABLES
    [Header("Label to Preload")]
    [SerializeField] private List<string> preloadLabels = new List<string> { "cloud" };

    [Header("Retry / Timeout")]
    [SerializeField] private int maxRetries = 3;

    // Timeout cho các step nhỏ như Initialize, CheckCatalog, GetSize, Verify.
    [SerializeField] private float stepTimeoutSeconds = 25f;

    // Timeout tổng cho mỗi label download.
    [SerializeField] private float downloadTimeoutSeconds = 300f;

    // Nếu download không tăng byte/progress trong thời gian này thì coi như bị đứng và retry.
    [SerializeField] private float downloadStallTimeoutSeconds = 45f;

    // Delay giữa các lần retry.
    [SerializeField] private float retryDelaySeconds = 1.5f;

    [Header("Verify downloaded (important)")]
    [SerializeField] private bool verifyAfterDownload = true;

    // Nếu verify còn lại <= ngưỡng này thì vẫn coi là pass. Nên để 0 nếu muốn chặt.
    [SerializeField] private long verifySizeThresholdBytes = 0;

    [Header("Probe remote catalog (optional)")]
    [SerializeField] private bool enableProbeRemoteCatalog = true;
    [SerializeField] private int probeReadBytes = 64;

    [Header("Force latest catalog (Recommended)")]
    // Nếu bật: sau Initialize sẽ LoadContentCatalogAsync(remoteCatalogJsonUrl) để ép runtime dùng catalog latest.
    [SerializeField] private bool forceLoadRemoteCatalog = true;

    [Header("Cache")]
    // Retry lần 2 trở đi sẽ clear cache catalog Addressables.
    [SerializeField] private bool clearCatalogCacheOnRetryOnly = true;

    // Nếu download fail/stall thì clear catalog cache trước attempt sau.
    [SerializeField] private bool clearCatalogCacheAfterDownloadFail = true;

    [Header("Debug")]
    [SerializeField] private bool enableAddressablesRequestLog = true;
    [SerializeField] private bool verboseProgressLog = true;
    [SerializeField] private float progressLogInterval = 2f;

    private string remoteCatalogHashUrl = "";
    private string remoteCatalogJsonUrl = "";

    private Coroutine _running;
    private bool _retryRequested;
    private bool _lastAttemptFailedDuringDownload;
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

                // Không set req.timeout ở đây cho bundle lớn,
                // vì một số bundle tải lâu có thể bị cắt ngang sai.
                // Stall timeout được xử lý bằng coroutine bên dưới.
            };
        }

        ResourceManager.ExceptionHandler = (handle, ex) =>
        {
            Debug.LogError($"[ADDR EX] stage={Stage} name={handle.DebugName}\n{ex}");
        };

        if (_running == null)
            _running = StartCoroutine(RunPreloadFlow());
#else
        Fail("ADDRESSABLES define OFF. Preload cannot run.");
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
                DownloadPercent01 = 1f;
                IsCloudFullyDownloaded = true;
                _running = null;

                Debug.Log("[Preload] DONE. Cloud content is ready.");
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

        Fail("[Preload] Max retries reached. Giving up.");
        _running = null;
    }

    private void ResetStateForAttempt()
    {
        IsReady = false;
        HasFailed = false;
        LastError = "";
        DownloadPercent01 = 0f;
        BytesToDownload = 0;
        BytesDownloadedApprox = 0;
        IsCloudFullyDownloaded = false;
        Stage = PreloadStage.None;
        _lastAttemptFailedDuringDownload = false;
    }

    private IEnumerator CoPreloadOnce(int attempt)
    {
        if (string.IsNullOrWhiteSpace(remoteCatalogJsonUrl) || string.IsNullOrWhiteSpace(remoteCatalogHashUrl))
        {
            Fail("[Preload] Remote catalog URLs are empty. Check AppBuildEnv.asset or inspector fallback values.");
            yield break;
        }

        if (enableProbeRemoteCatalog)
        {
            Stage = PreloadStage.Probe;
            SetStageProgress(0.02f);

            yield return HttpProbeGet(remoteCatalogHashUrl, probeReadBytes);
            if (HasFailed) yield break;

            yield return HttpProbeGet(remoteCatalogJsonUrl, probeReadBytes);
            if (HasFailed) yield break;
        }

        if (clearCatalogCacheOnRetryOnly && attempt >= 2)
        {
            Stage = PreloadStage.ClearCache;
            SetStageProgress(0.04f);
            ClearAddressablesCatalogCache();
        }

        Stage = PreloadStage.Initialize;
        SetStageProgress(0.05f);

        var init = Addressables.InitializeAsync();
        yield return WaitWithTimeout(init, stepTimeoutSeconds, $"InitializeAsync timeout (attempt {attempt})");

        if (HasFailed)
        {
            SafeRelease(init);
            yield break;
        }

        if (!init.IsValid())
        {
            Fail("[Preload] InitializeAsync handle invalid.");
            yield break;
        }

        if (init.Status != AsyncOperationStatus.Succeeded)
        {
            Fail("[Preload] Addressables init failed: " +
                 (init.OperationException != null ? init.OperationException.Message : init.Status.ToString()));
            SafeRelease(init);
            yield break;
        }

        SafeRelease(init);

        if (forceLoadRemoteCatalog)
        {
            Stage = PreloadStage.ForceLoadCatalog;
            SetStageProgress(0.08f);

            var loadCat = Addressables.LoadContentCatalogAsync(remoteCatalogJsonUrl, true);
            yield return WaitWithTimeout(loadCat, stepTimeoutSeconds, $"LoadContentCatalogAsync timeout (attempt {attempt})");

            if (HasFailed)
            {
                SafeRelease(loadCat);
                yield break;
            }

            if (!loadCat.IsValid() || loadCat.Status != AsyncOperationStatus.Succeeded)
            {
                Fail("[Preload] LoadContentCatalogAsync failed: " +
                     (loadCat.OperationException != null ? loadCat.OperationException.Message : loadCat.Status.ToString()));
                SafeRelease(loadCat);
                yield break;
            }

            SafeRelease(loadCat);
        }

        Stage = PreloadStage.CheckCatalog;
        SetStageProgress(0.10f);

        var check = Addressables.CheckForCatalogUpdates(false);
        yield return WaitWithTimeout(check, stepTimeoutSeconds, $"CheckForCatalogUpdates timeout (attempt {attempt})");

        if (HasFailed)
        {
            SafeRelease(check);
            yield break;
        }

        if (!check.IsValid())
        {
            Fail("[Preload] CheckForCatalogUpdates handle invalid.");
            yield break;
        }

        if (check.Status != AsyncOperationStatus.Succeeded)
        {
            Fail("[Preload] CheckForCatalogUpdates failed: " +
                 (check.OperationException != null ? check.OperationException.Message : check.Status.ToString()));
            SafeRelease(check);
            yield break;
        }

        IList<string> catalogs = check.Result;
        SafeRelease(check);

        if (catalogs != null && catalogs.Count > 0)
        {
            Stage = PreloadStage.UpdateCatalog;
            SetStageProgress(0.20f);

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
                Fail("[Preload] UpdateCatalogs handle invalid.");
                yield break;
            }

            if (update.Status != AsyncOperationStatus.Succeeded)
            {
                Fail("[Preload] UpdateCatalogs failed: " +
                     (update.OperationException != null ? update.OperationException.Message : update.Status.ToString()));
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
        SetStageProgress(0.30f);

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
                Fail($"[Preload] GetDownloadSizeAsync handle invalid (label={lb}).");
                yield break;
            }

            if (sizeHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Fail($"[Preload] GetDownloadSizeAsync failed (label={lb}): " +
                     (sizeHandle.OperationException != null ? sizeHandle.OperationException.Message : sizeHandle.Status.ToString()));
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
            Stage = PreloadStage.Download;
            SetStageProgress(0.35f);

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

                DownloadPercent01 = Mathf.Max(
                    DownloadPercent01,
                    Mathf.Lerp(0.35f, 0.95f, overall01)
                );
            }
        }
        else
        {
            Debug.Log("[Preload] Nothing to download. Cache is already complete.");
            SetStageProgress(0.95f);
        }

        if (verifyAfterDownload)
        {
            Stage = PreloadStage.Verify;
            SetStageProgress(0.98f);

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
                    Fail($"[Preload] Verify GetDownloadSizeAsync handle invalid (label={lb}).");
                    yield break;
                }

                if (verifyHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Fail($"[Preload] Verify GetDownloadSizeAsync failed (label={lb}): " +
                         (verifyHandle.OperationException != null ? verifyHandle.OperationException.Message : verifyHandle.Status.ToString()));
                    SafeRelease(verifyHandle);
                    yield break;
                }

                long remain = verifyHandle.Result;
                SafeRelease(verifyHandle);

                remainTotal += remain;

                Debug.Log($"[Preload] Verify label='{lb}' remain={FormatBytes(remain)}");
            }

            BytesToDownload = remainTotal;

            if (remainTotal > verifySizeThresholdBytes)
            {
                Fail($"[Preload] Verify failed: labels still have {FormatBytes(remainTotal)} to download. labels=({string.Join(",", labels)})");
                yield break;
            }
        }

        BytesDownloadedApprox = BytesToDownload;
        DownloadPercent01 = 1f;
        IsCloudFullyDownloaded = true;
        IsReady = true;
        HasFailed = false;
        LastError = "";
        Stage = PreloadStage.Done;
    }

    private List<string> BuildValidLabelList()
    {
        if (preloadLabels == null || preloadLabels.Count == 0)
        {
            Fail("[Preload] preloadLabels is empty. Please set at least 1 label, e.g. 'cloud'.");
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
            Fail("[Preload] preloadLabels has no valid label strings.");
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

            // currentLabelDownloaded = Mathf.Clamp((float)currentLabelDownloaded, 0f, currentLabelTotal);
            currentLabelDownloaded = (long)Mathf.Clamp(
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

            DownloadPercent01 = Mathf.Max(
                DownloadPercent01,
                Mathf.Lerp(0.35f, 0.95f, overall01)
            );

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
                    $"ui={DownloadPercent01:P1}"
                );
            }

            if (downloadTimeoutSeconds > 0f && totalTimer >= downloadTimeoutSeconds)
            {
                SafeRelease(dl);
                Fail($"[Preload] Download timeout. label={label}, timeout={downloadTimeoutSeconds}s");
                yield break;
            }

            if (downloadStallTimeoutSeconds > 0f && stallTimer >= downloadStallTimeoutSeconds)
            {
                SafeRelease(dl);
                Fail(
                    $"[Preload] Download stalled. label={label}, " +
                    $"no progress for {downloadStallTimeoutSeconds}s, " +
                    $"last={FormatBytes(lastDownloadedBytes)}/{FormatBytes(labelBytes)}"
                );
                yield break;
            }

            yield return null;
        }

        if (!dl.IsValid())
        {
            Fail($"[Preload] DownloadDependencies handle invalid. label={label}");
            yield break;
        }

        if (dl.Status != AsyncOperationStatus.Succeeded)
        {
            string err = dl.OperationException != null
                ? dl.OperationException.Message
                : dl.Status.ToString();

            SafeRelease(dl);
            Fail($"[Preload] DownloadDependencies failed. label={label}, error={err}");
            yield break;
        }

        SafeRelease(dl);

        Debug.Log($"[Preload] Download label DONE: '{label}'");
        onSuccess?.Invoke();
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
                Fail($"[HTTP PROBE GET FAILED] url={url} code={req.responseCode} err={req.error}");
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

    private void SetStageProgress(float p01)
    {
        DownloadPercent01 = Mathf.Max(DownloadPercent01, Mathf.Clamp01(p01));
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
                    Fail($"[Preload] {timeoutMsg} (timeout after {timeoutSeconds}s)");
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
            // Ignore release exception.
        }
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

    private void Fail(string msg)
    {
        HasFailed = true;
        IsReady = false;
        IsCloudFullyDownloaded = false;
        LastError = msg;
        Stage = PreloadStage.Failed;

        Debug.LogError(msg);
    }
}