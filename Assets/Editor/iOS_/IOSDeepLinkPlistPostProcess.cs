#if UNITY_IOS
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

public static class iOSDeepLinkAndUniversalLinksPostProcess
{
    // ===== CONFIG =====
    private const string CustomScheme = "lms";        // lms://...
    private const string UniversalDomain = "lms.deeplink"; // https://lms.deeplink/...
    private const string EntitlementsFileName = "Unity-iPhone.entitlements";
    // ==================

    [PostProcessBuild(1100)]
    public static void OnPostProcessBuild(BuildTarget target, string path)
    {
        if (target != BuildTarget.iOS) return;

        AddCustomSchemeToInfoPlist(path, CustomScheme);
        AddAssociatedDomainsEntitlements(path, UniversalDomain, EntitlementsFileName);
    }

    // -------------------- Custom URL Scheme (Info.plist) --------------------
    private static void AddCustomSchemeToInfoPlist(string buildPath, string scheme)
    {
        string plistPath = Path.Combine(buildPath, "Info.plist");
        if (!File.Exists(plistPath))
        {
            UnityEngine.Debug.LogError("[iOSLink] Info.plist not found.");
            return;
        }

        var plist = new PlistDocument();
        plist.ReadFromFile(plistPath);
        var root = plist.root;

        var urlTypes = root.values.ContainsKey("CFBundleURLTypes")
            ? root["CFBundleURLTypes"].AsArray()
            : root.CreateArray("CFBundleURLTypes");

        // If any URLType already contains our scheme -> done
        foreach (var elem in urlTypes.values)
        {
            var dict = elem.AsDict();
            if (dict == null) continue;
            if (!dict.values.ContainsKey("CFBundleURLSchemes")) continue;

            var schemes = dict["CFBundleURLSchemes"].AsArray();
            if (schemes.values.Any(v => v.AsString() == scheme))
            {
                File.WriteAllText(plistPath, plist.WriteToString());
                UnityEngine.Debug.Log($"[iOSLink] Custom scheme already exists: {scheme}://");
                return;
            }
        }

        // Add new URL type
        var newDict = urlTypes.AddDict();
        newDict.SetString("CFBundleURLName", PlayerSettings.applicationIdentifier);

        var newSchemes = newDict.CreateArray("CFBundleURLSchemes");
        newSchemes.AddString(scheme);

        File.WriteAllText(plistPath, plist.WriteToString());
        UnityEngine.Debug.Log($"[iOSLink] Added custom scheme: {scheme}://");
    }

    // -------------------- 2) Universal Links (Entitlements + PBX) --------------------
    private static void AddAssociatedDomainsEntitlements(string buildPath, string domain, string entitlementsFileName)
    {
        // PBX
        string projPath = PBXProject.GetPBXProjectPath(buildPath);
        if (!File.Exists(projPath))
        {
            UnityEngine.Debug.LogError("[iOSLink] PBX project not found.");
            return;
        }

        var proj = new PBXProject();
        proj.ReadFromFile(projPath);

#if UNITY_2019_3_OR_NEWER
        string mainTarget = proj.GetUnityMainTargetGuid();
        string frameworkTarget = proj.GetUnityFrameworkTargetGuid();
#else
        string mainTarget = proj.TargetGuidByName("Unity-iPhone");
        string frameworkTarget = mainTarget;
#endif

        // Entitlements file path
        string entitlementsPath = Path.Combine(buildPath, entitlementsFileName);

        var ent = new PlistDocument();
        if (File.Exists(entitlementsPath))
            ent.ReadFromFile(entitlementsPath);

        var root = ent.root;

        var arr = root.values.ContainsKey("com.apple.developer.associated-domains")
            ? root["com.apple.developer.associated-domains"].AsArray()
            : root.CreateArray("com.apple.developer.associated-domains");

        string value = $"applinks:{domain}";

        bool exists = arr.values.Any(v => v.AsString() == value);
        if (!exists) arr.AddString(value);

        File.WriteAllText(entitlementsPath, ent.WriteToString());

        // Ensure file is in project & set CODE_SIGN_ENTITLEMENTS
        string fileGuid = proj.AddFile(entitlementsFileName, entitlementsFileName);
        proj.AddFileToBuild(mainTarget, fileGuid);

        proj.SetBuildProperty(mainTarget, "CODE_SIGN_ENTITLEMENTS", entitlementsFileName);
        proj.SetBuildProperty(frameworkTarget, "CODE_SIGN_ENTITLEMENTS", entitlementsFileName);

        proj.WriteToFile(projPath);

        UnityEngine.Debug.Log($"[iOSLink] Added Associated Domain: {value}");
    }
}
#endif

