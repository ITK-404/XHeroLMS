using System;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

#if UNITY_ANDROID
using Google.Play.AppUpdate;
using Google.Play.Common;
#endif

public class StoreUpdateChecker : MonoBehaviour
{
    // ===================== CONST / DISPLAY =====================
    private const string APP_DISPLAY_NAME = "HỌC VIỆN PHONG THỦY ĐẠI NAM";

    [Header("Store URLs")]
    public string iosStoreUrl = "https://apps.apple.com/vn/app/%C4%91%C3%A0o-t%E1%BA%A1o-phong-thu%E1%BB%B7-%C4%91%E1%BA%A1i-nam/id6756565267";
    public string androidStoreUrl = "https://play.google.com/store/apps/details?id=com.xherozone.xherolms&hl=vi&gl=VN";

    [Header("iOS lookup")]
    public string iosAppId = "6756565267";
    public string iosCountry = "vn";

    public bool enableCheckOnStart = true;

    public bool failSilentlyIfUnknown = true;

    public bool preferFlexibleOnAndroid = true;

    public bool forceImmediateOnAndroid = false;

    public enum ReturnAction { OpenStore, QuitApp }
    public ReturnAction onReturn = ReturnAction.OpenStore;

    [Header("Popup text")]
    [Tooltip("Sẽ được override bởi message động có ver mới/cũ nếu lấy được.")]
    public string popupMessage = "Hiện tại đã có phiên bản mới, vui lòng cập nhật.";

    private bool _shown;

#if UNITY_ANDROID
    private AppUpdateManager _appUpdateManager;
#endif

    private void Start()
    {
        Debug.Log("[StoreUpdateChecker] Start() called");

#if UNITY_ANDROID
        string pkg = Application.identifier;
        string vName = Application.version;
        int vCode = GetCurrentVersionCodeSafe();

        Debug.Log($"[StoreUpdateChecker] ANDROID pkg={pkg} versionName={vName} versionCode={vCode}");
#elif UNITY_IOS
        Debug.Log($"[StoreUpdateChecker] IOS versionName={Application.version}");
#else
        Debug.Log("[StoreUpdateChecker] OTHER PLATFORM: skip");
#endif

        if (enableCheckOnStart)
        {
            Debug.Log("[StoreUpdateChecker] enableCheckOnStart=true -> StartCoroutine(CoCheck)");
            StartCoroutine(CoCheck());
        }
        else
        {
            Debug.Log("[StoreUpdateChecker] enableCheckOnStart=false");
        }
    }

    private IEnumerator CoCheck()
    {
        Debug.Log($"[StoreUpdateChecker] CoCheck() enter. _shown={_shown}");
        if (_shown) yield break;

#if UNITY_ANDROID
        yield return CoCheckAndroid_UsingPlayCore();
#elif UNITY_IOS
        yield return CoCheckIos();
#else
        yield break;
#endif
    }

    // ===================== Message builder =====================
    private string BuildUpdateMessage(string storeVersion, string localVersion)
    {
        if (string.IsNullOrEmpty(storeVersion) || string.IsNullOrEmpty(localVersion))
            return popupMessage;

        return $"{APP_DISPLAY_NAME} đã có phiên bản mới.\n" +
               $"Phiên bản {storeVersion} đã sẵn sàng. Quý học viên đang dùng {localVersion}";
    }

    // ===================== Android =====================
#if UNITY_ANDROID
    private IEnumerator CoCheckAndroid_UsingPlayCore()
    {
        Debug.Log("[StoreUpdateChecker] CoCheckAndroid_UsingPlayCore() enter");

        if (_appUpdateManager == null)
        {
            _appUpdateManager = new AppUpdateManager();
            Debug.Log("[StoreUpdateChecker] AppUpdateManager created");
        }

        var infoOp = _appUpdateManager.GetAppUpdateInfo();
        Debug.Log("[StoreUpdateChecker] GetAppUpdateInfo() requested");
        yield return infoOp;

        Debug.Log("[StoreUpdateChecker] GetAppUpdateInfo() finished. error=" + infoOp.Error);

        if (infoOp.Error != AppUpdateErrorCode.NoError)
        {
            Debug.LogWarning("[StoreUpdateChecker] Android GetAppUpdateInfo error: " + infoOp.Error);
            yield break;
        }

        var info = infoOp.GetResult();

        Debug.Log($"[StoreUpdateChecker] UpdateAvailability={info.UpdateAvailability} AppUpdateStatus={info.AppUpdateStatus}");

        bool updateAvailableFlag = info.UpdateAvailability == UpdateAvailability.UpdateAvailable;

        int currentVC = GetCurrentVersionCodeSafe();
        int availableVC = TryGetAvailableVersionCode(info);

        Debug.Log($"[StoreUpdateChecker] currentVC={currentVC} availableVC={availableVC} updateAvailableFlag={updateAvailableFlag}");

        bool canFlexible = info.IsUpdateTypeAllowed(AppUpdateOptions.FlexibleAppUpdateOptions());
        bool canImmediate = info.IsUpdateTypeAllowed(AppUpdateOptions.ImmediateAppUpdateOptions());
        Debug.Log($"[StoreUpdateChecker] allowed: flexible={canFlexible} immediate={canImmediate}");

        bool updateByVersionCode = (availableVC > 0 && currentVC > 0 && availableVC > currentVC);
        Debug.Log($"[StoreUpdateChecker] updateByVersionCode={updateByVersionCode}");

        if (!updateAvailableFlag && !updateByVersionCode)
        {
            Debug.Log("[StoreUpdateChecker] No update detected -> return");
            yield break;
        }

        _shown = true;

        // Build dynamic message: storeVerName (preferred) -> fallback versionCode
        string localVer = Application.version;

        string storeVerName = null;
        yield return CoFetchAndroidStoreVersionName(v => storeVerName = v);

        string storeVerDisplay = !string.IsNullOrEmpty(storeVerName) ? storeVerName :
                                 (availableVC > 0 ? $"(code {availableVC})" : "mới hơn");

        string msg = BuildUpdateMessage(storeVerDisplay, localVer);

        if (!canFlexible && !canImmediate)
        {
            Debug.LogWarning("[StoreUpdateChecker] Update available but no allowed update type -> fallback open store");
            LoadingUI.ShowUpdatePopup(msg, () =>
            {
                if (onReturn == ReturnAction.QuitApp) Application.Quit();
                else Application.OpenURL(androidStoreUrl);
            });
            yield break;
        }

        Debug.Log("[StoreUpdateChecker] Update detected -> showing popup");
        UnityAction act = () =>
        {
            Debug.Log("[StoreUpdateChecker] Popup action clicked -> start update flow");
            StartCoroutine(CoStartAndroidUpdate(info, canFlexible, canImmediate));
        };

        LoadingUI.ShowUpdatePopup(msg, act);
    }

// --- Try read AvailableVersionCode (plugin tùy version có/không)
    private int TryGetAvailableVersionCode(AppUpdateInfo info)
    {
        try { return info.AvailableVersionCode; }
        catch { return -1; }
    }

    // Fetch versionName từ Play Store HTML (itemprop="softwareVersion")
    private IEnumerator CoFetchAndroidStoreVersionName(Action<string> onDone)
    {
        if (string.IsNullOrEmpty(androidStoreUrl))
        {
            onDone?.Invoke(null);
            yield break;
        }

        using (var req = UnityWebRequest.Get(androidStoreUrl))
        {
            req.timeout = 10;
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool err = req.result != UnityWebRequest.Result.Success;
#else
            bool err = req.isNetworkError || req.isHttpError;
#endif
            if (err)
            {
                if (!failSilentlyIfUnknown)
                    Debug.LogWarning("[StoreUpdateChecker] Android store page fetch failed: " + req.error);
                onDone?.Invoke(null);
                yield break;
            }

            string html = req.downloadHandler.text;

            // itemprop="softwareVersion">1.0.4</span>
            var m = Regex.Match(html, "itemprop=\"softwareVersion\"[^>]*>\\s*([^<\\s]+)\\s*<",
                RegexOptions.IgnoreCase);

            onDone?.Invoke(m.Success ? m.Groups[1].Value.Trim() : null);
        }
    }

    private IEnumerator CoStartAndroidUpdate(AppUpdateInfo info, bool canFlexible, bool canImmediate)
    {
        if (_appUpdateManager == null)
            _appUpdateManager = new AppUpdateManager();

        AppUpdateOptions opt = null;

        if (forceImmediateOnAndroid)
        {
            if (canImmediate) opt = AppUpdateOptions.ImmediateAppUpdateOptions();
            else if (canFlexible) opt = AppUpdateOptions.FlexibleAppUpdateOptions();
        }
        else if (preferFlexibleOnAndroid)
        {
            if (canFlexible) opt = AppUpdateOptions.FlexibleAppUpdateOptions();
            else if (canImmediate) opt = AppUpdateOptions.ImmediateAppUpdateOptions();
        }
        else
        {
            if (canImmediate) opt = AppUpdateOptions.ImmediateAppUpdateOptions();
            else if (canFlexible) opt = AppUpdateOptions.FlexibleAppUpdateOptions();
        }

        if (opt == null)
        {
            Application.OpenURL(androidStoreUrl);
            yield break;
        }

        var startOp = _appUpdateManager.StartUpdate(info, opt);
        yield return startOp;

        if (startOp.Error != AppUpdateErrorCode.NoError)
        {
            if (!failSilentlyIfUnknown)
                Debug.LogWarning("[StoreUpdateChecker] Android StartUpdate error: " + startOp.Error);

            // fallback: open store
            Application.OpenURL(androidStoreUrl);
            yield break;
        }

        if (opt.AppUpdateType == AppUpdateType.Flexible)
        {
            while (true)
            {
                var pollOp = _appUpdateManager.GetAppUpdateInfo();
                yield return pollOp;

                if (pollOp.Error != AppUpdateErrorCode.NoError)
                {
                    if (!failSilentlyIfUnknown)
                        Debug.LogWarning("[StoreUpdateChecker] Android poll GetAppUpdateInfo error: " + pollOp.Error);
                    yield break;
                }

                var latest = pollOp.GetResult();
                if (latest.AppUpdateStatus == AppUpdateStatus.Downloaded)
                    break;

                yield return new WaitForSeconds(0.5f);
            }

            _appUpdateManager.CompleteUpdate();
        }
    }
#endif

    public void TriggerCheck() => StartCoroutine(CoCheck());

    

    private int GetCurrentVersionCodeSafe()
    {
        // Unity 2019+ thường có PlayerSettings.Android.bundleVersionCode nhưng runtime không đọc được.
        // Runtime cách chắc nhất là PackageInfo từ AndroidJavaObject:
        try
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            using (var pm = activity.Call<AndroidJavaObject>("getPackageManager"))
            {
                string pkg = activity.Call<string>("getPackageName");
                var pi = pm.Call<AndroidJavaObject>("getPackageInfo", pkg, 0);

                // versionCode (API < 28) / longVersionCode (API >= 28)
                int sdk = new AndroidJavaClass("android.os.Build$VERSION").GetStatic<int>("SDK_INT");
                if (sdk >= 28)
                {
                    long lvc = pi.Call<long>("getLongVersionCode");
                    return (int)Mathf.Clamp(lvc, 0, int.MaxValue);
                }
                else
                {
                    return pi.Get<int>("versionCode");
                }
            }
        }
        catch
        {
            return -1;
        }
    }

    private void ShowPopup_OpenStoreOrQuit(string msg)
    {
        UnityAction act = () =>
        {
            if (onReturn == ReturnAction.QuitApp) Application.Quit();
#if UNITY_ANDROID
            else Application.OpenURL(androidStoreUrl);
#elif UNITY_IOS
            else Application.OpenURL(iosStoreUrl);
#else
            else { }
#endif
        };

        LoadingUI.ShowUpdatePopup(msg, act);
    }

    // ===================== iOS =====================
#if UNITY_IOS
    private IEnumerator CoCheckIos()
    {
        string local = Application.version;
        string store = null;

        yield return CoFetchIosStoreVersion(v => store = v);

        if (string.IsNullOrEmpty(store))
        {
            if (!failSilentlyIfUnknown)
                Debug.LogWarning("[StoreUpdateChecker] iOS store version unknown -> could not check update.");
            yield break;
        }

        int cmp = CompareVersions(local, store);
        Debug.Log($"[StoreUpdateChecker] iOS Local={local} | Store={store} | cmp={cmp}");

        if (cmp < 0)
        {
            _shown = true;

            string msg = BuildUpdateMessage(store, local);

            UnityAction act = () =>
            {
                if (onReturn == ReturnAction.QuitApp) Application.Quit();
                else Application.OpenURL(iosStoreUrl);
            };

            LoadingUI.ShowUpdatePopup(msg, act);
        }
    }

    private IEnumerator CoFetchIosStoreVersion(Action<string> onDone)
    {
        string url = $"https://itunes.apple.com/lookup?id={UnityWebRequest.EscapeURL(iosAppId)}&country={UnityWebRequest.EscapeURL(iosCountry)}";
        using (var req = UnityWebRequest.Get(url))
        {
            req.timeout = 10;
            yield return req.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
            bool err = req.result != UnityWebRequest.Result.Success;
#else
            bool err = req.isNetworkError || req.isHttpError;
#endif
            if (err) { onDone?.Invoke(null); yield break; }

            string json = req.downloadHandler.text;
            var m = Regex.Match(json, "\"version\"\\s*:\\s*\"([^\"]+)\"");
            onDone?.Invoke(m.Success ? m.Groups[1].Value.Trim() : null);
        }
    }
#endif

    // ===================== Compare versions (iOS) =====================
    public static int CompareVersions(string local, string store)
    {
        var a = ExtractVersionParts(local);
        var b = ExtractVersionParts(store);

        int n = Mathf.Max(a.Length, b.Length);
        for (int i = 0; i < n; i++)
        {
            int ai = i < a.Length ? a[i] : 0;
            int bi = i < b.Length ? b[i] : 0;
            if (ai != bi) return ai.CompareTo(bi);
        }
        return 0;
    }

    private static int[] ExtractVersionParts(string v)
    {
        if (string.IsNullOrEmpty(v)) return Array.Empty<int>();
        var nums = new System.Collections.Generic.List<int>(4);
        var matches = Regex.Matches(v, "\\d+");
        foreach (Match m in matches)
            if (int.TryParse(m.Value, out int x)) nums.Add(x);
        return nums.ToArray();
    }
}
