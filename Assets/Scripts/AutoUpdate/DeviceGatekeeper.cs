using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeviceGatekeeper : MonoBehaviour
{
    public bool enforceBlock = true;
    public bool verboseLog = true;

    public bool showPopupWhenBlocked = true;
    public float autoQuitAfterSeconds = 5f;
    public string blockedMessage =
        "Thiết bị của bạn không đạt yêu cầu để sử dụng ứng dụng.\nVui lòng dùng thiết bị mạnh hơn hoặc liên hệ hỗ trợ.";

    public string blockedHeader = "Không hỗ trợ thiết bị";

    private bool _exitScheduledOrDone = false;
    private bool _quitCalled = false;
    private Coroutine _autoQuitCo;

    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

        var result = CheckDevice();

        if (verboseLog)
            Debug.Log($"[Gate] Allowed={result.allowed} Reason={result.reason}\n{result.detail}");

        if (result.allowed || !enforceBlock)
            return;

        HandleBlocked(result);
    }

    private void HandleBlocked((bool allowed, string reason, string detail) result)
    {
        if (_exitScheduledOrDone) return;
        _exitScheduledOrDone = true;

        LoadingUI.Hide();

        void QuitNow()
        {
            if (_quitCalled) return;
            _quitCalled = true;

            if (_autoQuitCo != null)
            {
                StopCoroutine(_autoQuitCo);
                _autoQuitCo = null;
            }

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        if (showPopupWhenBlocked)
        {
            LoadingUI.ShowErrorPopup(blockedMessage, blockedHeader, QuitNow);
        }
        else
        {
            QuitNow();
            return;
        }

        if (autoQuitAfterSeconds > 0f)
        {
            _autoQuitCo = StartCoroutine(AutoQuitRoutine(autoQuitAfterSeconds, QuitNow));
        }
    }

    private IEnumerator AutoQuitRoutine(float seconds, Action quitNow)
    {
        yield return new WaitForSecondsRealtime(seconds);

        if (_quitCalled) yield break; 
        quitNow?.Invoke();
    }

    private (bool allowed, string reason, string detail) CheckDevice()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return CheckAndroid();
#elif UNITY_IOS && !UNITY_EDITOR
   return (true,"null","null");  
   // return CheckiOS();
#else
        return (true, "Editor/Other platform", $"platform={Application.platform}");
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private (bool allowed, string reason, string detail) CheckAndroid()
    {
        string socModel = null;
        string hardware = null;
        string board = null;
        string device = null;
        string model = null;
        string proc = SystemInfo.processorType;

        try
        {
            using var build = new AndroidJavaClass("android.os.Build");
            model = build.GetStatic<string>("MODEL");
            device = build.GetStatic<string>("DEVICE");
            hardware = build.GetStatic<string>("HARDWARE");
            board = build.GetStatic<string>("BOARD");

            try { socModel = build.GetStatic<string>("SOC_MODEL"); } catch { }
        }
        catch (Exception e)
        {
            return (true, "Android info read failed -> allow", e.ToString());
        }

        string haystack = $"{socModel} {proc} {hardware} {board} {device} {model}".ToLowerInvariant();

        var blockedTokens = new List<string>
        {
            // ===== MediaTek =====
            "mt6580","mt6582","mt6592",
            "mt6735","mt6737","mt6739",
            "mt6750","mt6752","mt6753","mt6755","mt6757",
            "mt6761","mt6762","mt6763","mt6765","mt6768",
            "helio a22","helio p10","helio p20","helio p22","helio p23","helio p35",

            // ===== Snapdragon =====
            "snapdragon 210","snapdragon 212","snapdragon 215",
            "snapdragon 410","snapdragon 425","snapdragon 427",
            "snapdragon 429","snapdragon 430","snapdragon 435",
            "snapdragon 439","snapdragon 450",

            // ===== Unisoc / Spreadtrum =====
            "sc7731","sc8830","sc9832","sc9863","sc9863a",
            "t310","t606","t610","t618",

            // ===== Exynos =====
            "exynos 3475","exynos 7570","exynos 7870","exynos 7880","exynos 7884"
        };

        foreach (var t in blockedTokens)
        {
            if (haystack.Contains(t))
            {
                string detail =
                    $"socModel={socModel}\nproc={proc}\nhardware={hardware}\nboard={board}\ndevice={device}\nmodel={model}\nmatch={t}\nhaystack={haystack}";
                return (false, "Blocked: low-end SoC", detail);
            }
        }

        return (true, "Android OK",
            $"socModel={socModel}\nproc={proc}\nhardware={hardware}\nboard={board}\ndevice={device}\nmodel={model}");
    }
#endif

#if UNITY_IOS && !UNITY_EDITOR
    private (bool allowed, string reason, string detail) CheckiOS()
    {
        string hw = IOSHardwareMachine();
        string iosVer = UnityEngine.iOS.Device.systemVersion;

        int major = ParseIPhoneMajor(hw);

        if (major <= 0)
            return (true, "iOS unknown model -> allow", $"hw.machine={hw} ios={iosVer}");

        if (major <= 11)
            return (false, "Blocked: iPhone XS Max or below", $"hw.machine={hw} (major={major}) ios={iosVer}");

        return (true, "iOS OK", $"hw.machine={hw} (major={major}) ios={iosVer}");
    }

    // [System.Runtime.InteropServices.DllImport("__Internal")]
    private static extern IntPtr _GetHWMachine();

    private static string IOSHardwareMachine()
    {
        try
        {
            var ptr = _GetHWMachine();
            return System.Runtime.InteropServices.Marshal.PtrToStringAnsi(ptr);
        }
        catch { return "unknown"; }
    }

    private static int ParseIPhoneMajor(string hwMachine)
    {
        if (string.IsNullOrEmpty(hwMachine)) return -1;
        if (!hwMachine.StartsWith("iPhone", StringComparison.OrdinalIgnoreCase)) return -1;

        var s = hwMachine.Substring("iPhone".Length);
        int comma = s.IndexOf(',');
        if (comma <= 0) return -1;

        return int.TryParse(s.Substring(0, comma), out int major) ? major : -1;
    }
#endif
}
