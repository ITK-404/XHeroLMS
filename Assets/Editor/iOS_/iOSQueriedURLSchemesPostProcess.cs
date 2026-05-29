#if UNITY_IOS
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

public static class iOSQueriedURLSchemesPostProcess
{
    #region DeepLinkConfig

    private static readonly string[] QueriedSchemes =
    {
        "xhero",
        "tel"
        // Thêm scheme khác vào đây
        // "facebook",
        // "instagram",
    };
    private static readonly string[] BankQueriedSchemes =
    {
        // Techcombank
        "tcb",
        "techcombank",

        // Vietcombank
        "vcb",
        "vietcombank",
        "vietcombankmobile",

        // BIDV
        "bidv",
        "bidvsmartbanking",
        "bidvapp",

        // MB Bank
        "mbbank",
        "mb",
        "mbbankpay",

        // ACB
        "acb",
        "acbapp",
        "acbbiz",

        // Vietinbank
        "vietinbank",
        "vietinbankipay",
        "vietinbankmobile",
        "icb",

        // VPBank
        "vpbank",
        "vpbankneo",

        // TPBank
        "tpbank",
        "tpbankmobile",
        "tpb-pay",

        // HDBank
        "hdbank",
        "dihdbank",

        // SHB
        "shb",
        "shbmobile",
        "shbvn",

        // OCB
        "ocb",
        "ocbomni",

        // MSB
        "msb",
        "msbmbanking",
        "msbmbank",
        "msbmobile",

        // SeABank
        "seabank",
        "seabankconnect",

        // Agribank
        "agribank",
        "agribankemobile",
        "vba",

        // Sacombank
        "sacombank",
        "sacombankmobile",
        "sacombankpay",

        // Vietbank
        "vietbank",
        "vietbankdigital",

        // VIB
        "vib",
        "myvib",
        "vib-2",

        // LPBank
        "lpbank",
        "lpb",

        // Kienlongbank
        "kienlongbank",
        "klb",

        // PVcombank
        "pvcombank",
        "pvcb",

        // Cake by VPBank
        "cakebyvpbank",
        "cake",

        // Timo
        "timo",

        // Các ngân hàng khác
        "sgbmobile",
        "ncbizimobile",
        "vabmobilebanking",
        "newomni-app",
        "acbone",
        "lv24h",
        "seamobile",

        // Zalo
        "zalo",
    };

    #endregion

    [PostProcessBuild(1101)] // Chạy sau class kia (1100)
    public static void OnPostProcessBuild(BuildTarget target, string path)
    {
        if (target != BuildTarget.iOS) return;

        AddQueriedURLSchemes(path, QueriedSchemes);
        AddQueriedURLSchemes(path, BankQueriedSchemes);
    }

    private static void AddQueriedURLSchemes(string buildPath, string[] schemes)
    {
        string plistPath = Path.Combine(buildPath, "Info.plist");
        if (!File.Exists(plistPath))
        {
            UnityEngine.Debug.LogError("[iOSQueried] Info.plist not found.");
            return;
        }

        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);

        var arr = plist.root.values.ContainsKey("LSApplicationQueriesSchemes")
            ? plist.root["LSApplicationQueriesSchemes"].AsArray()
            : plist.root.CreateArray("LSApplicationQueriesSchemes");

        foreach (var scheme in schemes)
        {
            if (arr.values.Any(v => v.AsString() == scheme))
            {
                UnityEngine.Debug.Log($"[iOSQueried] Scheme already exists: {scheme}");
                continue;
            }

            arr.AddString(scheme);
            UnityEngine.Debug.Log($"[iOSQueried] Added queried scheme: {scheme}");
        }

        File.WriteAllText(plistPath, plist.WriteToString());
    }
}
#endif