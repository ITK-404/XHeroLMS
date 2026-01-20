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
    public string popupHeader = "Thông báo";
    public string popupMessage = "Hiện tại đã có phiên bản mới, vui lòng cập nhật.";

    private bool _shown;

#if UNITY_ANDROID
    private AppUpdateManager _appUpdateManager;
#endif

private void Start()
{
    Debug.Log("[StoreUpdateChecker] Start() called");

#if UNITY_ANDROID
    // Log package + versionName + versionCode (runtime)
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

        // nếu bạn muốn luôn thấy popup khi fail (debug), bật dòng dưới:
        // LoadingUI.ShowErrorPopup("GetAppUpdateInfo error: " + infoOp.Error, "DEBUG", null);

        yield break;
    }

    var info = infoOp.GetResult();

    // Log full state
    Debug.Log($"[StoreUpdateChecker] UpdateAvailability={info.UpdateAvailability} AppUpdateStatus={info.AppUpdateStatus}");

    bool updateAvailable = info.UpdateAvailability == UpdateAvailability.UpdateAvailable;

    int currentVC = GetCurrentVersionCodeSafe();
    int availableVC = TryGetAvailableVersionCode(info);

    Debug.Log($"[StoreUpdateChecker] currentVC={currentVC} availableVC={availableVC} updateAvailableFlag={updateAvailable}");

    bool canFlexible = info.IsUpdateTypeAllowed(AppUpdateOptions.FlexibleAppUpdateOptions());
    bool canImmediate = info.IsUpdateTypeAllowed(AppUpdateOptions.ImmediateAppUpdateOptions());
    Debug.Log($"[StoreUpdateChecker] allowed: flexible={canFlexible} immediate={canImmediate}");

    bool updateByVersionCode = (availableVC > 0 && currentVC > 0 && availableVC > currentVC);
    Debug.Log($"[StoreUpdateChecker] updateByVersionCode={updateByVersionCode}");

    if (!updateAvailable && !updateByVersionCode)
    {
        Debug.Log("[StoreUpdateChecker] No update detected -> return");
        yield break;
    }

    _shown = true;

    if (!canFlexible && !canImmediate)
    {
        Debug.LogWarning("[StoreUpdateChecker] Update available but no allowed update type -> fallback open store");
        ShowPopup_OpenStoreOrQuit();
        yield break;
    }

    Debug.Log("[StoreUpdateChecker] Update detected -> showing popup");
    UnityAction act = () =>
    {
        Debug.Log("[StoreUpdateChecker] Popup action clicked -> start update flow");
        StartCoroutine(CoStartAndroidUpdate(info, canFlexible, canImmediate));
    };

    LoadingUI.ShowErrorPopup(popupMessage, popupHeader, act);
}

// --- Try read AvailableVersionCode (plugin tùy version có/không)
private int TryGetAvailableVersionCode(AppUpdateInfo info)
{
    try
    {
        // Nhiều bản plugin có property này
        return info.AvailableVersionCode;
    }
    catch
    {
        // Không có property -> return -1
        return -1;
    }
}
#endif

    public void TriggerCheck() => StartCoroutine(CoCheck());

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

    private void ShowPopup_OpenStoreOrQuit()
    {
        UnityAction act = () =>
        {
            if (onReturn == ReturnAction.QuitApp) Application.Quit();
            else Application.OpenURL(androidStoreUrl);
        };

        LoadingUI.ShowErrorPopup(popupMessage, popupHeader, act);
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
            UnityAction act = () =>
            {
                if (onReturn == ReturnAction.QuitApp) Application.Quit();
                else Application.OpenURL(iosStoreUrl);
            };
            LoadingUI.ShowErrorPopup(popupMessage, popupHeader, act);
        }
    }

    private IEnumerator CoFetchIosStoreVersion(Action<string> onDone)
    {
        string url = $"https://itunes.apple.com/lookup?id={UnityWebRequest.EscapeURL(iosAppId)}&country={UnityWebRequest.EscapeURL(iosCountry)}";
        using (var req = UnityWebRequest.Get(url))
        {
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
