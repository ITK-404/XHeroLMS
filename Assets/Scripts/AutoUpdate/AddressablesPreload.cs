using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class AddressablesPreload : MonoBehaviour
{
    public static AddressablesPreload Instance { get; private set; }

    // Public status for IntroManager
    public bool IsReady { get; private set; } = false;          // true when dependencies downloaded (or nothing)
    public float DownloadPercent01 { get; private set; } = 0f;  // 0..1 while downloading
    public long BytesToDownload { get; private set; } = 0;      // last computed download bytes
public bool HasFailed { get; private set; } = false;
public string LastError { get; private set; } = "";

public void RequestRetry()
{
    retryRequested = true;
    Debug.Log("[Preload] Retry requested");
}


    [Header("Download all content tagged by this label")]
    [SerializeField] private string preloadLabel = "cloud";

    [Header("Retry / Timeout")]
    [SerializeField] private int maxRetries = 3;
    [SerializeField] private float stepTimeoutSeconds = 25f;   // cho init/check/update/size
    [SerializeField] private float downloadTimeoutSeconds = 300f;

    [Header("Optional progress log")]
    [SerializeField] private bool showProgressLog = false;

    // =========================
    // CLEAN POLICY
    // =========================
    [Header("Clean policy")]
    [SerializeField] private long cleanIfCacheExceedsBytes = 5L * 1024 * 1024 * 1024; // 5GB
    [SerializeField] private long reserveFreeSpaceBytes = 800L * 1024 * 1024;         // 800MB
    [SerializeField] private bool cleanOnMajorUpgrade = true;
    [SerializeField] private string majorVersionPrefsKey = "ADDR_LAST_MAJOR";
    [SerializeField] private bool cleanWhenLowDiskSpace = true;

    private bool retryRequested = false;
    private Coroutine running;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // Optionally keep across scene
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        if (running == null)
            running = StartCoroutine(RunPreloadFlow());
    }

    private IEnumerator RunPreloadFlow()
    {
        int attempt = 0;

        while (attempt <= maxRetries)
        {
            attempt++;

            IsReady = false;
            HasFailed = false;
            LastError = "";
            DownloadPercent01 = 0f;
            BytesToDownload = 0;

            yield return StartCoroutine(CoPreloadOnce(attempt));

            if (IsReady && !HasFailed)
            {
                Debug.Log("[Preload] READY");
                running = null;
                yield break;
            }

            // failed
            Debug.LogWarning($"[Preload] Attempt {attempt} failed. HasFailed={HasFailed}. RetryRequested={retryRequested}");

            if (retryRequested)
            {
                retryRequested = false;
                attempt = 0; // reset attempts when user explicitly retries
                continue;
            }

            if (attempt > maxRetries)
            {
                Debug.LogError("[Preload] Max retries reached. Giving up.");
                running = null;
                yield break;
            }

            // backoff small
            yield return new WaitForSecondsRealtime(1.5f);
        }

        running = null;
    }

    private IEnumerator CoPreloadOnce(int attempt)
    {
        // 1) Init
        var init = Addressables.InitializeAsync();
        yield return WaitWithTimeout(init, stepTimeoutSeconds, $"InitializeAsync timeout (attempt {attempt})");

        if (!CheckSucceeded(init, "[Preload] Addressables init failed"))
        {
            Addressables.Release(init);
            yield break;
        }

        // 2) Check catalog updates
        var check = Addressables.CheckForCatalogUpdates(false);
        yield return WaitWithTimeout(check, stepTimeoutSeconds, $"CheckForCatalogUpdates timeout (attempt {attempt})");

        if (!CheckSucceeded(check, "[Preload] CheckForCatalogUpdates failed"))
        {
            Addressables.Release(check);
            yield break;
        }

        var catalogs = check.Result;

        bool catalogUpdated = false;

        // 3) Update catalogs if needed
        if (catalogs != null && catalogs.Count > 0)
        {
            var update = Addressables.UpdateCatalogs(catalogs, false);
            yield return WaitWithTimeout(update, stepTimeoutSeconds, $"UpdateCatalogs timeout (attempt {attempt})");

            if (!CheckSucceeded(update, "[Preload] UpdateCatalogs failed"))
            {
                Addressables.Release(update);
                Addressables.Release(check);
                yield break;
            }

            Addressables.Release(update);
            catalogUpdated = true;
        }

        Addressables.Release(check);

        // 4) Download size
        var sizeHandle = Addressables.GetDownloadSizeAsync(preloadLabel);
        yield return WaitWithTimeout(sizeHandle, stepTimeoutSeconds, $"GetDownloadSizeAsync timeout (attempt {attempt})");

        if (!CheckSucceeded(sizeHandle, "[Preload] GetDownloadSizeAsync failed"))
        {
            Addressables.Release(sizeHandle);
            yield break;
        }

        long bytesToDownload = sizeHandle.Result;
        BytesToDownload = bytesToDownload;
        Addressables.Release(sizeHandle);

        Debug.Log($"[Preload] Label='{preloadLabel}', Need download: {bytesToDownload / (1024f * 1024f):0.00} MB");

        // 5) Decide clean policy
        bool shouldClean = false;
        string cleanReason = "";

        if (cleanOnMajorUpgrade)
        {
            int currentMajor = GetCurrentMajorVersion();
            int lastMajor = PlayerPrefs.GetInt(majorVersionPrefsKey, -1);

            if (lastMajor >= 0 && currentMajor >= 0 && currentMajor != lastMajor)
            {
                shouldClean = true;
                cleanReason = $"Major version changed ({lastMajor} -> {currentMajor})";
            }

            if (currentMajor >= 0)
            {
                PlayerPrefs.SetInt(majorVersionPrefsKey, currentMajor);
                PlayerPrefs.Save();
            }
        }

        if (!shouldClean && cleanIfCacheExceedsBytes > 0)
        {
            long cacheBytes = EstimateUnityCacheBytes();
            if (cacheBytes >= 0 && cacheBytes > cleanIfCacheExceedsBytes)
            {
                shouldClean = true;
                cleanReason = $"Cache exceeds threshold ({FormatBytes(cacheBytes)} > {FormatBytes(cleanIfCacheExceedsBytes)})";
            }
        }

        if (!shouldClean && cleanWhenLowDiskSpace && bytesToDownload > 0)
        {
            long freeBytes = GetFreeDiskSpaceBestEffort();
            if (freeBytes >= 0)
            {
                long required = bytesToDownload + reserveFreeSpaceBytes;
                if (freeBytes < required)
                {
                    shouldClean = true;
                    cleanReason = $"Low disk space (free={FormatBytes(freeBytes)}, required~={FormatBytes(required)})";
                }
            }
        }

        if (shouldClean)
        {
            Debug.Log($"[Preload] Cleaning bundle cache... Reason: {cleanReason}");
            var clean = Addressables.CleanBundleCache();
            yield return WaitWithTimeout(clean, stepTimeoutSeconds, $"CleanBundleCache timeout (attempt {attempt})");

            if (clean.Status != AsyncOperationStatus.Succeeded)
                Debug.LogWarning("[Preload] CleanBundleCache failed: " + clean.OperationException);
            else
                Debug.Log("[Preload] CleanBundleCache done.");

            Addressables.Release(clean);

            // recalc download size
            var size2 = Addressables.GetDownloadSizeAsync(preloadLabel);
            yield return WaitWithTimeout(size2, stepTimeoutSeconds, $"GetDownloadSizeAsync(after clean) timeout (attempt {attempt})");

            if (size2.Status == AsyncOperationStatus.Succeeded)
            {
                bytesToDownload = size2.Result;
                BytesToDownload = bytesToDownload;
                Debug.Log($"[Preload] Need download after clean: {bytesToDownload / (1024f * 1024f):0.00} MB");
            }
            else
            {
                Debug.LogWarning("[Preload] GetDownloadSizeAsync(after clean) failed: " + size2.OperationException);
            }

            Addressables.Release(size2);
        }
        else
        {
            if (catalogUpdated)
                Debug.Log("[Preload] Catalog updated, cache clean skipped (policy not triggered).");
        }

        // 6) Download
        if (bytesToDownload > 0)
        {
            var dl = Addressables.DownloadDependenciesAsync(preloadLabel, true);

            float t = 0f;
            while (!dl.IsDone)
            {
                DownloadPercent01 = Mathf.Clamp01(dl.PercentComplete);

                if (showProgressLog)
                    Debug.Log($"[Preload] Downloading... {DownloadPercent01 * 100f:0.0}%");

                t += Time.unscaledDeltaTime;
                if (downloadTimeoutSeconds > 0 && t >= downloadTimeoutSeconds)
                {
                    Fail("[Preload] Download timeout.");
                    yield break;
                }

                yield return null;
            }

            if (dl.Status != AsyncOperationStatus.Succeeded)
            {
                Fail("[Preload] Download failed: " + (dl.OperationException?.ToString() ?? "Unknown"));
                yield break;
            }

            DownloadPercent01 = 1f;
            IsReady = true;
            Debug.Log("[Preload] Download done! (Only changed/missing bundles were fetched)");
        }
        else
        {
            DownloadPercent01 = 1f;
            IsReady = true;
            Debug.Log("[Preload] Nothing to download. Cache already up to date. (or label has no entries)");
        }
    }

    // ========================= Helpers =========================

    private IEnumerator WaitWithTimeout(AsyncOperationHandle handle, float timeout, string timeoutMsg)
    {
        if (timeout <= 0f)
        {
            yield return handle;
            yield break;
        }

        float t = 0f;
        while (!handle.IsDone)
        {
            t += Time.unscaledDeltaTime;
            if (t >= timeout)
            {
                Fail(timeoutMsg);
                yield break;
            }
            yield return null;
        }
    }

    private bool CheckSucceeded(AsyncOperationHandle handle, string prefix)
    {
        if (HasFailed) return false;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Fail(prefix + ": " + (handle.OperationException?.ToString() ?? "Unknown"));
            return false;
        }

        return true;
    }

    private void Fail(string msg)
    {
        HasFailed = true;
        IsReady = false;
        LastError = msg;
        Debug.LogError(msg);
    }

    private int GetCurrentMajorVersion()
    {
        string v = Application.version;
        if (string.IsNullOrWhiteSpace(v)) return -1;
        var parts = v.Split('.');
        if (parts.Length == 0) return -1;
        if (int.TryParse(parts[0], out int major)) return major;
        return -1;
    }

    private long EstimateUnityCacheBytes()
    {
        try
        {
            string cachePath = Application.temporaryCachePath;
            if (string.IsNullOrEmpty(cachePath) || !Directory.Exists(cachePath))
                return -1;
            return GetDirectorySizeSafe(cachePath);
        }
        catch { return -1; }
    }

    private long GetFreeDiskSpaceBestEffort()
    {
        try
        {
            string p = Application.persistentDataPath;
            if (string.IsNullOrEmpty(p)) return -1;
            string root = Path.GetPathRoot(p);
            if (string.IsNullOrEmpty(root)) return -1;
            var drive = new DriveInfo(root);
            if (!drive.IsReady) return -1;
            return drive.AvailableFreeSpace;
        }
        catch { return -1; }
    }

    private static long GetDirectorySizeSafe(string folder)
    {
        long size = 0;
        try
        {
            var di = new DirectoryInfo(folder);
            foreach (var fi in di.EnumerateFiles("*", SearchOption.AllDirectories))
            {
                try { size += fi.Length; } catch { }
            }
        }
        catch { }
        return size;
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 0) return "unknown";
        float mb = bytes / (1024f * 1024f);
        if (mb < 1024f) return $"{mb:0.##} MB";
        float gb = mb / 1024f;
        return $"{gb:0.##} GB";
    }
}
