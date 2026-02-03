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
/// </summary>
[DefaultExecutionOrder(-15000)]
public class AddressablesPreload : MonoBehaviour
{
    public static AddressablesPreload Instance { get; private set; }

    public enum PreloadStage
    {
        None, Probe, ClearCache, Initialize, CheckCatalog, UpdateCatalog, GetSize, Download, Verify, Done, Failed
    }

    [Header("State (Read-only)")]
    public PreloadStage Stage { get; private set; } = PreloadStage.None;

    /// <summary>
    /// Ready = flow hoàn tất và đã verify label preload không còn bytes cần tải.
    /// </summary>
    public bool IsReady { get; private set; }

    public bool HasFailed { get; private set; }
    public string LastError { get; private set; } = "";
    public float DownloadPercent01 { get; private set; }
    public long BytesToDownload { get; private set; }

    /// <summary>
    /// Cờ “đúng ý bạn”: chỉ true khi label "cloud" đã tải xong hết (size = 0 sau verify).
    /// BootFlow sẽ chờ cờ này.
    /// </summary>
    public bool IsCloudFullyDownloaded { get; private set; }

#if ADDRESSABLES
    [Header("Label to Preload")]
    private List<string> preloadLabels = new List<string> { "cloud" };

    [Header("Retry / Timeout")]
    [SerializeField] private int maxRetries = 3;
    [SerializeField] private float stepTimeoutSeconds = 25f;
    [SerializeField] private float downloadTimeoutSeconds = 300f;

    [Header("Verify downloaded (important)")]
    [Tooltip("Sau khi DownloadDependencies xong sẽ GetDownloadSize lại để confirm bytes=0. Nếu vẫn >0 -> fail để retry.")]
    [SerializeField] private bool verifyAfterDownload = true;

    [Tooltip("Cho phép chênh lệch nhỏ (đề phòng vài bytes báo sai). Thường để 0.")]
    [SerializeField] private long verifySizeThresholdBytes = 0;

    [Header("Probe remote catalog (optional)")]
    [SerializeField] private bool enableProbeRemoteCatalog = true;
    [SerializeField] private int probeReadBytes = 64;

    [SerializeField] private string remoteCatalogHashUrl =
        "https://storage.googleapis.com/dlc-lms/addressables/releases/android/latest/catalog.hash";

    [SerializeField] private string remoteCatalogJsonUrl =
        "https://storage.googleapis.com/dlc-lms/addressables/releases/android/latest/catalog.json";

    [Header("Cache")]
    [SerializeField] private bool clearCatalogCacheOnRetryOnly = true;

    [Header("Debug")]
    [SerializeField] private bool enableAddressablesRequestLog = true;

    private Coroutine _running;
    private bool _retryRequested;
#endif

    public void RequestRetry()
    {
#if ADDRESSABLES
        _retryRequested = true;
        Debug.Log("[Preload] Retry requested");
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
        if (enableAddressablesRequestLog)
        {
            Addressables.WebRequestOverride = (req) =>
            {
                if (req.url.Contains("catalog") || req.url.EndsWith(".hash") || req.url.EndsWith(".json") || req.url.Contains(".bundle"))
                    Debug.Log($"[ADDR REQ] {req.method} {req.url}");
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

    private IEnumerator RunPreloadFlow()
    {
        int attempt = 0;

        while (attempt < maxRetries)
        {
            attempt++;

            ResetStateForAttempt();

            yield return CoPreloadOnce(attempt);

            if (IsReady && !HasFailed)
            {
                Stage = PreloadStage.Done;
                _running = null;
                yield break;
            }

            if (_retryRequested)
            {
                _retryRequested = false;
                attempt = 0; // reset attempts
            }

            yield return new WaitForSecondsRealtime(1f);
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
        IsCloudFullyDownloaded = false;
        Stage = PreloadStage.None;
    }

private IEnumerator CoPreloadOnce(int attempt)
{
    // 0) PROBE
    if (enableProbeRemoteCatalog &&
        !string.IsNullOrWhiteSpace(remoteCatalogHashUrl) &&
        !string.IsNullOrWhiteSpace(remoteCatalogJsonUrl))
    {
        Stage = PreloadStage.Probe;
        SetStageProgress(0.02f);

        yield return HttpProbeGet(remoteCatalogHashUrl, probeReadBytes);
        if (HasFailed) yield break;

        yield return HttpProbeGet(remoteCatalogJsonUrl, probeReadBytes);
        if (HasFailed) yield break;
    }

    // 0.5) Clear cache on retry
    if (clearCatalogCacheOnRetryOnly && attempt >= 2)
    {
        Stage = PreloadStage.ClearCache;
        SetStageProgress(0.04f);
        ClearAddressablesCatalogCache();
    }

    // 1) Initialize
    Stage = PreloadStage.Initialize;
    SetStageProgress(0.05f);

var init = Addressables.InitializeAsync();
yield return init;

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

    // 2) Check catalog updates
    Stage = PreloadStage.CheckCatalog;
    SetStageProgress(0.10f);

    var check = Addressables.CheckForCatalogUpdates(false);
    yield return WaitWithTimeout(check, stepTimeoutSeconds, $"CheckForCatalogUpdates timeout (attempt {attempt})");

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

    // 3) Update catalogs
    if (catalogs != null && catalogs.Count > 0)
    {
        Stage = PreloadStage.UpdateCatalog;
        SetStageProgress(0.20f);

        var update = Addressables.UpdateCatalogs(catalogs, false);
        yield return WaitWithTimeout(update, stepTimeoutSeconds, $"UpdateCatalogs timeout (attempt {attempt})");

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

    // --- sanitize labels ---
    if (preloadLabels == null || preloadLabels.Count == 0)
    {
        Fail("[Preload] preloadLabels is empty. Please set at least 1 label (e.g. 'cloud').");
        yield break;
    }

    // remove null/empty + distinct
    List<string> labels = new List<string>();
    for (int i = 0; i < preloadLabels.Count; i++)
    {
        string lb = preloadLabels[i];
        if (string.IsNullOrWhiteSpace(lb)) continue;
        if (!labels.Contains(lb)) labels.Add(lb);
    }

    if (labels.Count == 0)
    {
        Fail("[Preload] preloadLabels has no valid label strings.");
        yield break;
    }

    // 4) Get total size (trước download) - SUM all labels
    Stage = PreloadStage.GetSize;
    SetStageProgress(0.30f);

    long totalBytes = 0;
    var perLabelBytes = new Dictionary<string, long>(labels.Count);

    for (int i = 0; i < labels.Count; i++)
    {
        string lb = labels[i];

        var sizeHandle = Addressables.GetDownloadSizeAsync(lb);
        yield return WaitWithTimeout(sizeHandle, stepTimeoutSeconds, $"GetDownloadSizeAsync timeout ({lb}) (attempt {attempt})");

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
    }

    BytesToDownload = totalBytes;
    // Nếu totalBytes = 0 thì vẫn đi verify (để chắc chắn) nếu bạn muốn.

    // 5) Download deps (ALL labels)
    if (BytesToDownload > 0)
    {
        Stage = PreloadStage.Download;
        SetStageProgress(0.35f);

        long downloadedBytesApprox = 0;

        for (int i = 0; i < labels.Count; i++)
        {
            string lb = labels[i];
            long thisLabelBytes = perLabelBytes.TryGetValue(lb, out var bb) ? bb : 0;
            if (thisLabelBytes <= 0) continue;

            Debug.Log($"[Preload] Download label='{lb}' bytes={thisLabelBytes}");

            var dl = Addressables.DownloadDependenciesAsync(lb, autoReleaseHandle: false);

            float t = 0f;
            while (!dl.IsDone)
            {
                // approx overall progress: base 0.35 -> 1.0
                float labelProgress01 = Mathf.Clamp01(dl.PercentComplete);
                long labelDownloadedApprox = (long)(thisLabelBytes * labelProgress01);

                long overallDownloadedApprox = downloadedBytesApprox + labelDownloadedApprox;
                float overall01 = (totalBytes <= 0) ? 1f : Mathf.Clamp01((float)overallDownloadedApprox / (float)totalBytes);

                DownloadPercent01 = Mathf.Max(DownloadPercent01, Mathf.Lerp(0.35f, 1f, overall01));

                t += Time.unscaledDeltaTime;
                if (downloadTimeoutSeconds > 0f && t >= downloadTimeoutSeconds)
                {
                    SafeRelease(dl);
                    Fail($"[Preload] Download timeout (label={lb}).");
                    yield break;
                }
                yield return null;
            }

            if (dl.Status != AsyncOperationStatus.Succeeded)
            {
                Fail($"[Preload] DownloadDependencies failed (label={lb}): " +
                     (dl.OperationException != null ? dl.OperationException.Message : dl.Status.ToString()));
                SafeRelease(dl);
                yield break;
            }

            SafeRelease(dl);

            // after done label
            downloadedBytesApprox += thisLabelBytes;
            DownloadPercent01 = Mathf.Max(DownloadPercent01, Mathf.Lerp(0.35f, 1f, (totalBytes <= 0 ? 1f : (float)downloadedBytesApprox / totalBytes)));
        }
    }

    // 6) Verify (TỔNG remain của ALL labels phải = 0)
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
        }

        // Cập nhật để UI/Log nhìn được (remain tổng)
        BytesToDownload = remainTotal;

        if (remainTotal > verifySizeThresholdBytes)
        {
            Fail($"[Preload] Verify failed: labels still have {remainTotal} bytes to download. labels=({string.Join(",", labels)})");
            yield break;
        }
    }

    // DONE
    DownloadPercent01 = 1f;
    IsCloudFullyDownloaded = true; // “coi như cloud pack” của bạn đã xong (theo danh sách labels)
    IsReady = true;
    Stage = PreloadStage.Done;
}

    private IEnumerator HttpProbeGet(string url, int readBytes)
    {
        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = 15;
            req.downloadHandler = new DownloadHandlerBuffer();
            if (readBytes > 0) req.SetRequestHeader("Range", $"bytes=0-{readBytes - 1}");

            yield return req.SendWebRequest();

            bool ok = (req.result == UnityWebRequest.Result.Success) &&
                      (req.responseCode == 200 || req.responseCode == 206);

            if (!ok)
                Fail($"[HTTP PROBE GET FAILED] url={url} code={req.responseCode} err={req.error}");
        }
    }

    private void ClearAddressablesCatalogCache()
    {
        try
        {
            string dir = Path.Combine(Application.persistentDataPath, "com.unity.addressables");
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
        }
        catch { }
    }

    private void SetStageProgress(float p01)
    {
        DownloadPercent01 = Mathf.Max(DownloadPercent01, Mathf.Clamp01(p01));
    }

private IEnumerator WaitWithTimeout(AsyncOperationHandle handle, float timeoutSeconds, string timeoutMsg)
{
    while (!handle.IsDone)
        yield return null;
}

    private void SafeRelease(AsyncOperationHandle handle)
    {
        try
        {
            if (handle.IsValid())
                Addressables.Release(handle);
        }
        catch { }
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
