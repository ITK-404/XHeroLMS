#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

[InitializeOnLoad]
public static class AddressablesCloudAutoSetup
{
    // ===================== CONFIG =====================
    private const string BucketName = "dlc-lms";
    private const string RootFolder = "addressables";
    private const string ReleasesFolderDev = "releases-dev";
    private const string ReleasesFolderProd = "releases-prod";
    private const string ProfileName = "GCS";

    private const string VarRemoteBuildPath = "RemoteBuildPath";
    private const string VarRemoteLoadPath = "RemoteLoadPath";

    private const string VarRemoteBuildPathSchema = "Remote.BuildPath";
    private const string VarRemoteLoadPathSchema = "Remote.LoadPath";

    private const string RemoteBuildPathValue = "ServerData/[BuildTarget]";

    private const bool AutoBumpPatch = false;

    private const string PrefEnvMode = "AddressablesCloudAutoSetup.EnvMode";
    private const string PrefProdAuthBlob = "AddressablesCloudAutoSetup.ProdAuthBlob";

    private const string DefaultProdIssuer = "LMS3D-PROD";
    private const string DefaultProdAccount = "LMS@XheroZone";

    private static readonly string ProjectKeyJsonPath =
        Path.Combine(Application.dataPath, "Editor", "GCS", "lms-3-479211-dc999d4c697c.json");

    private static string GcloudExePath => ResolveGcloudExe("gcloud");
    private static string GsutilExePath => ResolveGcloudExe("gsutil");

    private const string ProdAuthFolderRelative = "Assets/Editor/ProdAuth";
    private const string ProdAuthFileName = "lms3d_prod_auth.dat";

    private static string ProdAuthFolderAbsolute
        => Path.Combine(ProjectRoot, "Assets", "Editor", "ProdAuth");

    private static string ProdAuthFileAbsolutePath
        => Path.Combine(ProdAuthFolderAbsolute, ProdAuthFileName);

    private static string ProdAuthFileAssetPath
        => $"{ProdAuthFolderRelative}/{ProdAuthFileName}";
    
    private static string DefaultKeyPath
    {
        get
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, ".gcp", "lms-3d-gcp.json");
        }
    }

    private static string CloudSdkConfigDir
    {
        get
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, ".config", "gcloud");
        }
    }

    private static string ProjectRoot
        => Directory.GetParent(Application.dataPath)?.FullName ?? "";

    static AddressablesCloudAutoSetup()
    {
        EnsureAddressablesSetup(GetSavedEnvironmentMode());
    }

    internal static bool IsAdminMachine()
    {
        return FindTotpSeedGeneratorType() != null;
    }

    private static Type FindTotpSeedGeneratorType()
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type t = asm.GetType("TotpSeedGenerator", false);
            if (t != null)
                return t;
        }

        return null;
    }

    private static bool TryCreateSecretFromAdminGenerator(out string secret)
    {
        secret = null;

        Type t = FindTotpSeedGeneratorType();
        if (t == null)
            return false;

        var method = t.GetMethod("CreateBase32Secret", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        if (method == null)
            return false;

        object result = method.Invoke(null, new object[] { 20 });
        secret = result as string;

        return !string.IsNullOrWhiteSpace(secret);
    }

    // ===================== MENU =====================

    [MenuItem("Tools/Cloud/Addressables/Ensure Setup")]
    public static void EnsureAddressablesSetupMenu()
    {
        EnsureAddressablesSetup(GetSavedEnvironmentMode());
    }
    [MenuItem("Tools/Cloud/Addressables/Prod Auth/Generate Auth File + Show QR", true)]
    public static bool ValidateGenerateAuthFileAndShowQrMenu()
    {
        return IsAdminMachine();
    }
    [MenuItem("Tools/Cloud/Addressables/Prod Auth/Generate Auth File + Show QR")]
    public static void GenerateAuthFileAndShowQrMenu()
    {
        GenerateAuthFileAndShowQr();
    }

    [MenuItem("Tools/Cloud/Addressables/Prod Auth/Import Auth File (.dat)")]
    public static void ImportAuthFileMenu()
    {
        ImportAuthFile();
    }

    [MenuItem("Tools/Cloud/Addressables/Prod Auth/Clear Imported Auth File")]
    public static void ClearImportedAuthFileMenu()
    {
        ClearProdAuthFile();
        EditorUtility.DisplayDialog("OK", "Đã xoá auth file local.", "OK");
    }
    [MenuItem("Tools/Cloud/Addressables/Print Resolved Paths")]
    public static void PrintResolvedPaths()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            UnityEngine.Debug.LogError("[AddressablesCloudAutoSetup] Addressable settings missing.");
            return;
        }

        var env = GetSavedEnvironmentMode();
        var ps = settings.profileSettings;
        var profileId = settings.activeProfileId;

        string bucket = Eval(ps, profileId, "GCS_BUCKET");
        string ver = Eval(ps, profileId, "APP_VERSION");
        string plat = Eval(ps, profileId, "PLATFORM_NAME");
        string remoteBuildPath = Eval(ps, profileId, VarRemoteBuildPath);
        string remoteLoadPath = Eval(ps, profileId, VarRemoteLoadPath);

        UnityEngine.Debug.Log(
            "[AddressablesCloudAutoSetup] Resolved Profile Paths:\n" +
            $"Env             : {env}\n" +
            $"ActiveProfileId : {profileId}\n" +
            $"GCS_BUCKET      : {bucket}\n" +
            $"APP_VERSION     : {ver}\n" +
            $"PLATFORM_NAME   : {plat}\n" +
            $"RemoteBuildPath : {remoteBuildPath}\n" +
            $"RemoteLoadPath  : {remoteLoadPath}\n"
        );
    }

    [MenuItem("Tools/Cloud/Addressables/Build + Upload to GCS (builds + latest)")]
    public static void OpenBuildWindow()
    {
        AddressablesBuildAndUploadWindow.Open();
    }

    internal static void EnsureAddressablesSetup(EnvironmentMode env)
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            UnityEngine.Debug.LogWarning("[AddressablesCloudAutoSetup] Addressable settings not found. Creating...");
            settings = AddressableAssetSettings.Create(
                AddressableAssetSettingsDefaultObject.kDefaultConfigFolder,
                AddressableAssetSettingsDefaultObject.kDefaultConfigAssetName,
                true, true
            );
            AddressableAssetSettingsDefaultObject.Settings = settings;
        }

        var ps = settings.profileSettings;

        string profileId = ps.GetProfileId(ProfileName);
        if (string.IsNullOrEmpty(profileId))
        {
            string baseId = settings.activeProfileId;
            profileId = ps.AddProfile(ProfileName, baseId);
            UnityEngine.Debug.Log($"[AddressablesCloudAutoSetup] Created profile: {ProfileName} (base={baseId})");
        }

        EnsureVar(ps, profileId, "GCS_BUCKET", BucketName);

        string appVersion = string.IsNullOrWhiteSpace(PlayerSettings.bundleVersion) ? "0.0.0" : PlayerSettings.bundleVersion;
        EnsureVar(ps, profileId, "APP_VERSION", appVersion);

        var platformName = GetPlatformName(EditorUserBuildSettings.activeBuildTarget);
        EnsureVar(ps, profileId, "PLATFORM_NAME", platformName);

        EnsureVar(ps, profileId, VarRemoteBuildPath, RemoteBuildPathValue);

        var resolvedLoad = LatestRemoteLoadResolved(platformName, env);
        EnsureVar(ps, profileId, VarRemoteLoadPath, resolvedLoad);

        SyncOptionalVarIfExists(ps, profileId, VarRemoteBuildPathSchema, RemoteBuildPathValue);
        SyncOptionalVarIfExists(ps, profileId, VarRemoteLoadPathSchema, resolvedLoad);

        settings.activeProfileId = profileId;

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();

        UnityEngine.Debug.Log($"[AddressablesCloudAutoSetup] Done. Profile set to GCS. Env={env}");
    }

    internal static void BuildAndUpload(EnvironmentMode env)
    {
        const bool RemoveVersionedCatalogsOnLatest = false;

        if (AutoBumpPatch)
        {
            PlayerSettings.bundleVersion = BumpPatch(PlayerSettings.bundleVersion);
            UnityEngine.Debug.Log($"[AddressablesCloudAutoSetup] Auto bumped version -> {PlayerSettings.bundleVersion}");
        }

        SaveEnvironmentMode(env);
        EnsureAddressablesSetup(env);

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            UnityEngine.Debug.LogError("[AddressablesCloudAutoSetup] Addressable settings missing.");
            return;
        }

        string appVersion = string.IsNullOrWhiteSpace(PlayerSettings.bundleVersion) ? "0.0.0" : PlayerSettings.bundleVersion;
        BuildTarget bt = EditorUserBuildSettings.activeBuildTarget;
        string platformName = GetPlatformName(bt);
        string releasesFolder = GetReleasesFolder(env);
        string resolvedLoad = LatestRemoteLoadResolved(platformName, env);

        var ps = settings.profileSettings;
        var profileId = settings.activeProfileId;

        ps.SetValue(profileId, "APP_VERSION", appVersion);
        ps.SetValue(profileId, "PLATFORM_NAME", platformName);
        ps.SetValue(profileId, VarRemoteBuildPath, RemoteBuildPathValue);
        ps.SetValue(profileId, VarRemoteLoadPath, resolvedLoad);

        SyncOptionalVarIfExists(ps, profileId, VarRemoteBuildPathSchema, RemoteBuildPathValue);
        SyncOptionalVarIfExists(ps, profileId, VarRemoteLoadPathSchema, resolvedLoad);

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();

        string expectedLocalOut = FindLocalBuildOutput(bt);
        CleanDirectorySafe(expectedLocalOut);

        UnityEngine.Debug.Log(
            "[AddressablesCloudAutoSetup] Building Addressables...\n" +
            $"ENV={env}\n" +
            $"BT={bt}\n" +
            $"PLATFORM={platformName}\n" +
            $"APP_VERSION={appVersion}\n" +
            $"RELEASES_FOLDER={releasesFolder}\n" +
            $"RemoteLoadPath={resolvedLoad}"
        );

        AddressableAssetSettings.BuildPlayerContent();

        string localSrc = FindLocalBuildOutput(bt);
        UnityEngine.Debug.Log($"[AddressablesCloudAutoSetup] LocalSrc = {localSrc}");

        if (!Directory.Exists(localSrc))
        {
            UnityEngine.Debug.LogError(
                "[AddressablesCloudAutoSetup] Build output not found.\n" +
                $"Expected something like: {Path.Combine(GetServerDataRoot(), bt.ToString())}\n" +
                $"Last tried: {localSrc}"
            );
            return;
        }

        string versionedJson = Directory.GetFiles(localSrc, "catalog_*.json", SearchOption.AllDirectories)
            .OrderByDescending(f => f)
            .FirstOrDefault();

        string versionedHash = Directory.GetFiles(localSrc, "catalog_*.hash", SearchOption.AllDirectories)
            .OrderByDescending(f => f)
            .FirstOrDefault();

        if (string.IsNullOrEmpty(versionedJson) || string.IsNullOrEmpty(versionedHash))
        {
            UnityEngine.Debug.LogError(
                "[AddressablesCloudAutoSetup] Catalog missing in local output.\n" +
                "Expected catalog_*.json and catalog_*.hash in:\n" + localSrc
            );
            return;
        }

        var catalogDir = Path.GetDirectoryName(versionedJson);
        string dstCatalogJson = Path.Combine(catalogDir, "catalog.json");
        string dstCatalogHash = Path.Combine(catalogDir, "catalog.hash");

        try
        {
            File.Copy(versionedJson, dstCatalogJson, true);
            File.Copy(versionedHash, dstCatalogHash, true);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("[AddressablesCloudAutoSetup] Failed to create catalog aliases:\n" + e);
            return;
        }

        UnityEngine.Debug.Log(
            "[AddressablesCloudAutoSetup] Created standard catalog aliases:\n" +
            $" - {Path.GetFileName(versionedJson)} -> catalog.json\n" +
            $" - {Path.GetFileName(versionedHash)} -> catalog.hash"
        );

        int bundleCount = Directory.GetFiles(localSrc, "*.bundle", SearchOption.AllDirectories).Length;
        UnityEngine.Debug.Log($"[AddressablesCloudAutoSetup] Bundles in output: {bundleCount}");

        string keyFileForCli = GetCredentialFileForCli(out string keySourceInfo);
        if (string.IsNullOrEmpty(keyFileForCli) || !File.Exists(keyFileForCli))
        {
            UnityEngine.Debug.LogError(
                "[AddressablesCloudAutoSetup] Missing GCP credential file.\n" +
                "Tried:\n" +
                " - ENV GOOGLE_APPLICATION_CREDENTIALS\n" +
                " - ENV GCP_SA_KEY_PATH\n" +
                $" - Project slot: {ProjectKeyJsonPath}\n" +
                $" - Fallback: {DefaultKeyPath}\n"
            );
            return;
        }

        UnityEngine.Debug.Log($"[AddressablesCloudAutoSetup] Using key source: {keySourceInfo}");
        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", keyFileForCli);

        if (!RunCmd(GcloudExePath, $"auth activate-service-account --key-file=\"{keyFileForCli}\"", dumpLogFile: "gcloud_auth.txt"))
        {
            UnityEngine.Debug.LogWarning("[AddressablesCloudAutoSetup] gcloud auth failed (continuing anyway). gsutil will rely on GOOGLE_APPLICATION_CREDENTIALS.");
        }

        string dstBuild = $"gs://{BucketName}/{RootFolder}/{releasesFolder}/{platformName}/builds/{appVersion}/";
        string dstLatest = $"gs://{BucketName}/{RootFolder}/{releasesFolder}/{platformName}/latest/";

        UnityEngine.Debug.Log($"[AddressablesCloudAutoSetup] Uploading (builds) {localSrc} -> {dstBuild}");
        if (!RunCmd(GsutilExePath, $"-m rsync -r \"{localSrc}\" \"{dstBuild}\"", dumpLogFile: "gsutil_rsync_builds.txt"))
            return;

        UnityEngine.Debug.Log($"[AddressablesCloudAutoSetup] Syncing (latest) {dstBuild} -> {dstLatest}");
        if (!RunCmd(GsutilExePath, $"-m rsync -r \"{dstBuild}\" \"{dstLatest}\"", dumpLogFile: "gsutil_rsync_latest.txt"))
            return;

        if (RemoveVersionedCatalogsOnLatest)
        {
            UnityEngine.Debug.Log("[AddressablesCloudAutoSetup] Removing catalog_*.json/hash on latest (keep catalog.json/hash) ...");
            RunCmd(GsutilExePath, $"-m rm \"{dstLatest}catalog_*.json\"", dumpLogFile: "gsutil_rm_catalog_ver_json_latest.txt");
            RunCmd(GsutilExePath, $"-m rm \"{dstLatest}catalog_*.hash\"", dumpLogFile: "gsutil_rm_catalog_ver_hash_latest.txt");
        }

        bool okLatestCatalog = VerifyLatestCatalogExists(dstLatest);
        if (!okLatestCatalog)
            UnityEngine.Debug.LogError("[AddressablesCloudAutoSetup] Remote latest/ missing catalog.json or catalog.hash. Check gsutil_verify_latest_catalog.txt");

        if (!okLatestCatalog)
            UnityEngine.Debug.LogError("[AddressablesCloudAutoSetup] Remote latest/ does NOT show catalog files. Check gsutil_verify_latest_catalog.txt");

        UnityEngine.Debug.Log(
            "[AddressablesCloudAutoSetup] Done.\n" +
            $"Env    : {env}\n" +
            $"Builds : https://storage.googleapis.com/{BucketName}/{RootFolder}/{releasesFolder}/{platformName}/builds/{appVersion}/\n" +
            $"Latest : https://storage.googleapis.com/{BucketName}/{RootFolder}/{releasesFolder}/{platformName}/latest/\n" +
            "Verify in browser:\n" +
            $" - https://storage.googleapis.com/{BucketName}/{RootFolder}/{releasesFolder}/{platformName}/latest/catalog.json\n" +
            $" - https://storage.googleapis.com/{BucketName}/{RootFolder}/{releasesFolder}/{platformName}/latest/catalog.hash\n"
        );
    }

    internal static EnvironmentMode GetSavedEnvironmentMode()
    {
        int raw = EditorPrefs.GetInt(PrefEnvMode, (int)EnvironmentMode.Dev);
        if (!Enum.IsDefined(typeof(EnvironmentMode), raw))
            raw = (int)EnvironmentMode.Dev;
        return (EnvironmentMode)raw;
    }

    internal static void SaveEnvironmentMode(EnvironmentMode env)
    {
        EditorPrefs.SetInt(PrefEnvMode, (int)env);
    }

internal static bool HasProdAuthFile()
{
    return File.Exists(ProdAuthFileAbsolutePath);
}

internal static void ClearProdAuthFile()
{
    if (File.Exists(ProdAuthFileAbsolutePath))
    {
        File.Delete(ProdAuthFileAbsolutePath);
        AssetDatabase.Refresh();
    }
}

internal static bool TryGetProdAuthPayload(out ProdAuthPayload payload)
{
    payload = null;

    if (!File.Exists(ProdAuthFileAbsolutePath))
        return false;

    try
    {
        byte[] fileBytes = File.ReadAllBytes(ProdAuthFileAbsolutePath);
        return ProdAuthFileUtility.TryReadFileBytes(fileBytes, out payload, out _);
    }
    catch
    {
        return false;
    }
}

internal static bool GenerateAuthFileAndShowQr()
{
    if (!IsAdminMachine())
    {
        EditorUtility.DisplayDialog("Không có quyền", "Máy này không có TotpSeedGenerator nên không được tạo QR/auth file.", "OK");
        return false;
    }

    if (!TryCreateSecretFromAdminGenerator(out string secret))
    {
        EditorUtility.DisplayDialog("Lỗi", "Không tạo được secret từ TotpSeedGenerator.", "OK");
        return false;
    }

    var payload = new ProdAuthPayload
    {
        issuer = DefaultProdIssuer,
        account = DefaultProdAccount,
        secret = secret
    };

    byte[] fileBytes = ProdAuthFileUtility.CreateFileBytes(payload);

    try
    {
        Directory.CreateDirectory(ProdAuthFolderAbsolute);
        File.WriteAllBytes(ProdAuthFileAbsolutePath, fileBytes);
        AssetDatabase.Refresh();
    }
    catch (Exception ex)
    {
        EditorUtility.DisplayDialog("Lỗi", "Không ghi được file .dat\n" + ex.Message, "OK");
        return false;
    }

    string otpAuth = TotpUtility.BuildOtpAuthUri(
        payload.issuer,
        payload.account,
        payload.secret,
        6,
        30);

    string qrUrl = "https://api.qrserver.com/v1/create-qr-code/?size=300x300&data=" +
                   Uri.EscapeDataString(otpAuth);

    Application.OpenURL(qrUrl);

    EditorUtility.DisplayDialog(
        "OK",
        $"Đã tạo auth file tại:\n{ProdAuthFileAssetPath}\n\nĐã mở QR để quét authenticator.",
        "OK");

    return true;
}

internal static bool ImportAuthFile()
{
    if (!File.Exists(ProdAuthFileAbsolutePath))
    {
        EditorUtility.DisplayDialog(
            "Thiếu file .dat",
            $"Không tìm thấy file:\n{ProdAuthFileAssetPath}\n\nHãy lấy file từ máy admin.",
            "OK");
        return false;
    }

    try
    {
        byte[] fileBytes = File.ReadAllBytes(ProdAuthFileAbsolutePath);

        if (!ProdAuthFileUtility.TryReadFileBytes(fileBytes, out var payload, out string error))
        {
            EditorUtility.DisplayDialog("Lỗi", "File auth không hợp lệ.\n" + error, "OK");
            return false;
        }

        EditorUtility.DisplayDialog(
            "OK",
            $"Đã đọc auth file.\nIssuer: {payload.issuer}\nAccount: {payload.account}",
            "OK");

        return true;
    }
    catch (Exception ex)
    {
        EditorUtility.DisplayDialog("Lỗi", ex.Message, "OK");
        return false;
    }
}

internal static bool VerifyProdCode(string code)
{
    if (!TryGetProdAuthPayload(out var payload))
        return false;

    return TotpUtility.VerifyCode(payload.secret, code, 1, 6, 30);
}

    private static string ResolveGcloudExe(string toolName)
    {
#if UNITY_EDITOR_WIN
        string win = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Google", "Cloud SDK", "google-cloud-sdk", "bin", toolName + ".cmd"
        );
        if (File.Exists(win)) return win;
        return toolName + ".cmd";
#else
        string brew1 = Path.Combine("/opt/homebrew/bin", toolName);
        if (File.Exists(brew1)) return brew1;

        string brew2 = Path.Combine("/usr/local/bin", toolName);
        if (File.Exists(brew2)) return brew2;

        string sdk1 = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "google-cloud-sdk", "bin", toolName);
        if (File.Exists(sdk1)) return sdk1;

        return toolName;
#endif
    }

    private static string LatestRemoteLoadResolved(string platformName, EnvironmentMode env)
        => $"https://storage.googleapis.com/{BucketName}/{RootFolder}/{GetReleasesFolder(env)}/{platformName}/latest";

    private static string GetReleasesFolder(EnvironmentMode env)
        => env == EnvironmentMode.Prod ? ReleasesFolderProd : ReleasesFolderDev;

    private static bool VerifyLatestCatalogExists(string dstLatest)
    {
        var r = RunCmdWithResult(GsutilExePath, $"ls \"{dstLatest}\"", 5 * 60 * 1000, "gsutil_verify_latest_catalog.txt");

        if (r.ExitCode != 0) return false;

        var s = (r.StdOut ?? "") + "\n" + (r.StdErr ?? "");
        s = s.ToLowerInvariant();

        return s.Contains("catalog.json") && s.Contains("catalog.hash");
    }

    // ===================== HELPERS =====================

    private static string GetServerDataRoot()
    {
        return Path.Combine(ProjectRoot, "ServerData");
    }

    private static string FindLocalBuildOutput(BuildTarget bt)
    {
        string root = GetServerDataRoot();

        var candidates = new List<string>
        {
            Path.Combine(root, bt.ToString()),
            Path.Combine(root, bt.ToString().ToLowerInvariant()),
            Path.Combine(root, FirstCharUpper(GetPlatformName(bt))),
            Path.Combine(root, GetPlatformName(bt)),
        };

        foreach (var c in candidates)
        {
            if (Directory.Exists(c))
                return c;
        }

        return candidates[0];
    }

    private static void CleanDirectorySafe(string dir)
    {
        if (!Directory.Exists(dir)) return;

        try
        {
            Directory.Delete(dir, true);
            UnityEngine.Debug.Log($"[AddressablesCloudAutoSetup] Cleaned: {dir}");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning($"[AddressablesCloudAutoSetup] Failed to clean {dir}\n{e}");
        }
    }

    private static string FirstCharUpper(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        if (s.Length == 1) return s.ToUpperInvariant();
        return char.ToUpperInvariant(s[0]) + s.Substring(1);
    }

    private static void EnsureVar(AddressableAssetProfileSettings ps, string profileId, string name, string value)
    {
        if (!ps.GetVariableNames().Contains(name))
        {
            ps.CreateValue(name, value);
            UnityEngine.Debug.Log($"[AddressablesCloudAutoSetup] Created profile var: {name} = {value}");
        }
        ps.SetValue(profileId, name, value);
    }

    private static void SyncOptionalVarIfExists(AddressableAssetProfileSettings ps, string profileId, string name, string value)
    {
        if (ps.GetVariableNames().Contains(name))
            ps.SetValue(profileId, name, value);
    }

    private static string Eval(AddressableAssetProfileSettings ps, string profileId, string varName)
    {
        var raw = ps.GetValueByName(profileId, varName);
        return ps.EvaluateString(profileId, raw);
    }

    private static string GetPlatformName(BuildTarget t)
    {
        switch (t)
        {
            case BuildTarget.Android: return "android";
            case BuildTarget.iOS: return "ios";
            case BuildTarget.StandaloneOSX: return "mac";
            case BuildTarget.StandaloneWindows:
            case BuildTarget.StandaloneWindows64: return "pc";
            case BuildTarget.StandaloneLinux64: return "linux";
            default: return t.ToString().ToLowerInvariant();
        }
    }

    private static string BumpPatch(string v)
    {
        if (string.IsNullOrWhiteSpace(v)) return "0.0.1";

        var parts = v.Split('.');
        if (parts.Length < 3) return v + ".1";

        if (!int.TryParse(parts[2], out var patch)) patch = 0;
        parts[2] = (patch + 1).ToString();
        return string.Join(".", parts);
    }

    private static string GetCredentialFileForCli(out string sourceInfo)
    {
        var p1 = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        if (!string.IsNullOrWhiteSpace(p1) && File.Exists(p1))
        {
            sourceInfo = "ENV:GOOGLE_APPLICATION_CREDENTIALS";
            return p1;
        }

        var p2 = Environment.GetEnvironmentVariable("GCP_SA_KEY_PATH");
        if (!string.IsNullOrWhiteSpace(p2) && File.Exists(p2))
        {
            sourceInfo = "ENV:GCP_SA_KEY_PATH";
            return p2;
        }

        if (File.Exists(ProjectKeyJsonPath))
        {
            string tmp = Path.Combine(Path.GetTempPath(), "gcs_sa_key_tmp.json");
            File.Copy(ProjectKeyJsonPath, tmp, true);
            sourceInfo = $"PROJECT:{ProjectKeyJsonPath} -> TEMP:{tmp}";
            return tmp;
        }

        if (File.Exists(DefaultKeyPath))
        {
            sourceInfo = $"FALLBACK:{DefaultKeyPath}";
            return DefaultKeyPath;
        }

        sourceInfo = "NONE";
        return null;
    }

    private struct CmdResult
    {
        public int ExitCode;
        public string StdOut;
        public string StdErr;
        public string LogPath;
    }

    private static bool RunCmd(string exe, string args, int timeoutMs = 10 * 60 * 1000, string dumpLogFile = null)
    {
        var r = RunCmdWithResult(exe, args, timeoutMs, dumpLogFile);

        if (!string.IsNullOrWhiteSpace(r.StdOut))
            UnityEngine.Debug.Log($"[CMD] {exe} {args}\n{r.StdOut}");

        if (r.ExitCode != 0)
        {
            UnityEngine.Debug.LogError(
                $"[CMD] {exe} {args}\nExitCode={r.ExitCode}\n{r.StdErr}\n" +
                (string.IsNullOrWhiteSpace(r.LogPath) ? "" : $"(See log: {r.LogPath})"));
            return false;
        }

        if (!string.IsNullOrWhiteSpace(r.StdErr))
            UnityEngine.Debug.LogWarning($"[CMD] {exe} {args}\n{r.StdErr}");

        return true;
    }

    private static CmdResult RunCmdWithResult(string exe, string args, int timeoutMs, string dumpLogFile)
    {
        var result = new CmdResult { ExitCode = -1, StdOut = "", StdErr = "", LogPath = "" };

        try
        {
            bool isCmd = exe.EndsWith(".cmd", StringComparison.OrdinalIgnoreCase)
                      || exe.EndsWith(".bat", StringComparison.OrdinalIgnoreCase);

            try { Directory.CreateDirectory(CloudSdkConfigDir); } catch { }

            var psi = new ProcessStartInfo
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            if (isCmd)
            {
                psi.FileName = "cmd.exe";
                psi.Arguments = $"/d /s /c \"\"{exe}\" {args}\"";
            }
            else
            {
                psi.FileName = exe;
                psi.Arguments = args;
            }

            psi.EnvironmentVariables["CLOUDSDK_CONFIG"] = CloudSdkConfigDir;

            var cred = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
            if (!string.IsNullOrWhiteSpace(cred) && File.Exists(cred))
                psi.EnvironmentVariables["GOOGLE_APPLICATION_CREDENTIALS"] = cred;

            var sbOut = new StringBuilder();
            var sbErr = new StringBuilder();

            using (var p = new Process())
            {
                p.StartInfo = psi;
                p.OutputDataReceived += (_, e) => { if (e.Data != null) sbOut.AppendLine(e.Data); };
                p.ErrorDataReceived += (_, e) => { if (e.Data != null) sbErr.AppendLine(e.Data); };

                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();

                bool exited = p.WaitForExit(timeoutMs);
                if (!exited)
                {
                    try { p.Kill(); } catch { }

#if UNITY_EDITOR_WIN
                    try
                    {
                        var killer = new Process();
                        killer.StartInfo.FileName = "taskkill";
                        killer.StartInfo.Arguments = $"/PID {p.Id} /T /F";
                        killer.StartInfo.UseShellExecute = false;
                        killer.StartInfo.CreateNoWindow = true;
                        killer.Start();
                        killer.WaitForExit(5000);
                    }
                    catch { }
#endif

                    result.ExitCode = -2;
                    result.StdErr = $"TIMEOUT after {timeoutMs}ms";
                }
                else
                {
                    try { p.WaitForExit(1000); } catch { }
                    result.ExitCode = p.ExitCode;
                    result.StdOut = sbOut.ToString();
                    result.StdErr = sbErr.ToString();
                }
            }
        }
        catch (Exception e)
        {
            result.ExitCode = -3;
            result.StdErr = e.ToString();
        }

        DumpCmdLogIfNeeded(exe, args, result.ExitCode, result.StdOut, result.StdErr, dumpLogFile, out result.LogPath);
        return result;
    }

    private static void DumpCmdLogIfNeeded(string exe, string args, int exitCode, string stdout, string stderr, string dumpLogFile, out string logPath)
    {
        logPath = "";
        if (string.IsNullOrWhiteSpace(dumpLogFile)) return;

        try
        {
            logPath = Path.Combine(ProjectRoot, dumpLogFile);
            File.WriteAllText(logPath,
                $"[CMD] {exe} {args}\nExitCode={exitCode}\n\n--- STDOUT ---\n{stdout}\n\n--- STDERR ---\n{stderr}\n");
            UnityEngine.Debug.Log($"[AddressablesCloudAutoSetup] Wrote log: {logPath}");
        }
        catch
        {
            logPath = "";
        }
    }
}

internal enum EnvironmentMode
{
    Dev = 0,
    Prod = 1
}

internal sealed class AddressablesBuildAndUploadWindow : EditorWindow
{
    private EnvironmentMode envMode;
    private string prodIssuer;
    private string prodAccount;
    private string prodSecret;
    private string manualCode = "";

    public static void Open()
    {
        var window = GetWindow<AddressablesBuildAndUploadWindow>("Addressables GCS Build");
        window.minSize = new Vector2(520, 430);
        window.Show();
    }

    private void OnEnable()
    {
        envMode = AddressablesCloudAutoSetup.GetSavedEnvironmentMode();
    }

    private void OnGUI()
    {
        GUILayout.Space(8);
        EditorGUILayout.LabelField("Addressables Build + Upload to GCS", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Dev sẽ upload vào releases-dev. Prod sẽ upload vào releases-prod và bắt buộc xác thực bằng mã authenticator trước khi build.", MessageType.Info);

        EditorGUI.BeginChangeCheck();
        envMode = (EnvironmentMode)EditorGUILayout.EnumPopup("Environment", envMode);
        if (EditorGUI.EndChangeCheck())
        {
            AddressablesCloudAutoSetup.SaveEnvironmentMode(envMode);
            GUI.FocusControl(null);
        }

        GUILayout.Space(8);

        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Current target info", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Build Target", EditorUserBuildSettings.activeBuildTarget.ToString());
            EditorGUILayout.LabelField("Bundle Version", string.IsNullOrWhiteSpace(PlayerSettings.bundleVersion) ? "0.0.0" : PlayerSettings.bundleVersion);
            EditorGUILayout.LabelField("Remote Root",
                envMode == EnvironmentMode.Prod
                    ? "gs://dlc-lms/addressables/releases-prod/"
                    : "gs://dlc-lms/addressables/releases-dev/");
        }

        GUILayout.Space(8);

        if (envMode == EnvironmentMode.Prod)
        {
            DrawProdSecuritySection();
        }
        else
        {
            EditorGUILayout.HelpBox("Dev mode giữ flow build hiện tại, chỉ đổi folder từ releases thành releases-dev.", MessageType.None);
        }

        GUILayout.FlexibleSpace();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Ensure Setup", GUILayout.Height(32)))
            {
                AddressablesCloudAutoSetup.EnsureAddressablesSetup(envMode);
            }

            GUI.backgroundColor = envMode == EnvironmentMode.Prod ? new Color(1f, 0.85f, 0.3f) : Color.green;
            if (GUILayout.Button("Build + Upload", GUILayout.Height(36)))
            {
                TryBuild();
            }
            GUI.backgroundColor = Color.white;
        }
    }

    private void DrawProdSecuritySection()
    {
        using (new EditorGUILayout.VerticalScope("box"))
        {
            EditorGUILayout.LabelField("Prod Auth File", EditorStyles.boldLabel);

            bool hasAuth = AddressablesCloudAutoSetup.HasProdAuthFile();
            bool isAdmin = AddressablesCloudAutoSetup.IsAdminMachine();

            EditorGUILayout.HelpBox(
                hasAuth
                    ? $"Đã có auth file tại Assets/Editor/ProdAuth/lms3d_prod_auth.dat\nMáy này chỉ verify mã để build prod."
                    : "Chưa có auth file .dat trong Assets/Editor/ProdAuth.",
                hasAuth ? MessageType.Info : MessageType.Warning);

            if (isAdmin)
            {
                if (GUILayout.Button("Generate Auth File + Show QR", GUILayout.Height(34)))
                {
                    AddressablesCloudAutoSetup.GenerateAuthFileAndShowQr();
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Máy này không có TotpSeedGenerator nên không có quyền tạo QR/auth file.", MessageType.None);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Check Auth File", GUILayout.Height(28)))
                {
                    AddressablesCloudAutoSetup.ImportAuthFile();
                }

                if (GUILayout.Button("Clear Auth File", GUILayout.Height(28)))
                {
                    AddressablesCloudAutoSetup.ClearProdAuthFile();
                }
            }

            GUILayout.Space(10);

            manualCode = EditorGUILayout.TextField("Verify Code", manualCode);

            if (GUILayout.Button("Verify Code", GUILayout.Height(30)))
            {
                bool ok = AddressablesCloudAutoSetup.VerifyProdCode(manualCode);

                EditorUtility.DisplayDialog(
                    ok ? "OK" : "Sai mã",
                    ok ? "Mã hợp lệ." : "Sai mã.",
                    "OK");
            }
        }
    }

    private void TryBuild()
    {
        if (envMode == EnvironmentMode.Dev)
        {
            AddressablesCloudAutoSetup.BuildAndUpload(EnvironmentMode.Dev);
            return;
        }

        if (!AddressablesCloudAutoSetup.HasProdAuthFile())
        {
            EditorUtility.DisplayDialog(
                "Thiếu auth file",
                "Không tìm thấy file prod auth .dat trong Assets/Editor/ProdAuth.\nHãy lấy file từ máy admin.",
                "OK");
            return;
        }

        string code = PromptForCode();
        if (string.IsNullOrWhiteSpace(code))
            return;

        if (!AddressablesCloudAutoSetup.VerifyProdCode(code))
        {
            EditorUtility.DisplayDialog("Sai mã", "Mã authenticator không đúng. Build prod bị chặn.", "OK");
            return;
        }

        AddressablesCloudAutoSetup.BuildAndUpload(EnvironmentMode.Prod);
    }

    private string PromptForCode()
    {
        return TotpCodePromptWindow.ShowDialog();
    }
}

internal sealed class TotpCodePromptWindow : EditorWindow
{
    private string code = "";
    private bool submitted;
    private static string result;

    public static string ShowDialog()
    {
        result = null;
        var window = CreateInstance<TotpCodePromptWindow>();
        window.titleContent = new GUIContent("Nhập mã Authenticator");
        window.minSize = new Vector2(360, 120);
        window.maxSize = new Vector2(360, 120);
        window.ShowModalUtility();
        return result;
    }

    private void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Nhập mã 6 số từ app authenticator", EditorStyles.boldLabel);
        GUI.SetNextControlName("TotpCodeField");
        code = EditorGUILayout.TextField("Mã", code);

        GUILayout.FlexibleSpace();
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Huỷ", GUILayout.Height(28)))
            {
                result = null;
                Close();
            }

            if (GUILayout.Button("Xác nhận", GUILayout.Height(28)))
            {
                submitted = true;
                result = (code ?? string.Empty).Trim();
                Close();
            }
        }

        EditorGUI.FocusTextInControl("TotpCodeField");
    }

    private void OnLostFocus()
    {
        if (!submitted)
            return;
    }
}

internal static class TotpUtility
{
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static string CreateBase32Secret(int numBytes = 20)
    {
        byte[] data = new byte[numBytes];
        RandomNumberGenerator.Fill(data);
        return ToBase32(data);
    }

    public static string BuildOtpAuthUri(string issuer, string accountName, string base32Secret, int digits = 6, int period = 30)
    {
        string safeIssuer = string.IsNullOrWhiteSpace(issuer) ? "LMS3D-PROD" : issuer.Trim();
        // string safeAccount = string.IsNullOrWhiteSpace(accountName) ? "internal@team" : accountName.Trim();
        string safeAccount = string.IsNullOrWhiteSpace(accountName) ? "LMS@XheroZone" : accountName.Trim();

        string label = $"{safeIssuer}:{safeAccount}";
        return $"otpauth://totp/{Uri.EscapeDataString(label)}" +
               $"?secret={Uri.EscapeDataString(base32Secret)}" +
               $"&issuer={Uri.EscapeDataString(safeIssuer)}" +
               $"&digits={digits}" +
               $"&period={period}";
    }

    public static bool VerifyCode(string base32Secret, string code, int allowedTimeStepDrift = 1, int digits = 6, int period = 30)
    {
        if (string.IsNullOrWhiteSpace(base32Secret) || string.IsNullOrWhiteSpace(code))
            return false;

        string normalizedCode = new string(code.Where(char.IsDigit).ToArray());
        if (normalizedCode.Length != digits)
            return false;

        byte[] key = FromBase32(base32Secret);
        long timestep = GetCurrentTimeStepNumber(period);

        for (long i = -allowedTimeStepDrift; i <= allowedTimeStepDrift; i++)
        {
            string expected = ComputeTotp(key, timestep + i, digits);
            if (FixedTimeEquals(expected, normalizedCode))
                return true;
        }

        return false;
    }

    private static long GetCurrentTimeStepNumber(int period)
    {
        long unix = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return unix / period;
    }

    private static string ComputeTotp(byte[] key, long timestepNumber, int digits)
    {
        byte[] timestepBytes = BitConverter.GetBytes(timestepNumber);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(timestepBytes);

        using (var hmac = new HMACSHA1(key))
        {
            byte[] hash = hmac.ComputeHash(timestepBytes);
            int offset = hash[hash.Length - 1] & 0x0F;

            int binaryCode =
                ((hash[offset] & 0x7F) << 24) |
                ((hash[offset + 1] & 0xFF) << 16) |
                ((hash[offset + 2] & 0xFF) << 8) |
                (hash[offset + 3] & 0xFF);

            int otp = binaryCode % (int)Math.Pow(10, digits);
            return otp.ToString(new string('0', digits), CultureInfo.InvariantCulture);
        }
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        if (a == null || b == null || a.Length != b.Length)
            return false;

        int diff = 0;
        for (int i = 0; i < a.Length; i++)
            diff |= a[i] ^ b[i];
        return diff == 0;
    }

    private static string ToBase32(byte[] data)
    {
        if (data == null || data.Length == 0)
            return string.Empty;

        StringBuilder result = new StringBuilder((data.Length + 7) * 8 / 5);

        int buffer = data[0];
        int next = 1;
        int bitsLeft = 8;

        while (bitsLeft > 0 || next < data.Length)
        {
            if (bitsLeft < 5)
            {
                if (next < data.Length)
                {
                    buffer <<= 8;
                    buffer |= data[next++] & 0xFF;
                    bitsLeft += 8;
                }
                else
                {
                    int pad = 5 - bitsLeft;
                    buffer <<= pad;
                    bitsLeft += pad;
                }
            }

            int index = (buffer >> (bitsLeft - 5)) & 0x1F;
            bitsLeft -= 5;
            result.Append(Base32Alphabet[index]);
        }

        return result.ToString();
    }

    private static byte[] FromBase32(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return Array.Empty<byte>();

        string s = input.Trim().TrimEnd('=').Replace(" ", "").ToUpperInvariant();

        List<byte> bytes = new List<byte>();
        int bitBuffer = 0;
        int bitsInBuffer = 0;

        foreach (char c in s)
        {
            int val = Base32Alphabet.IndexOf(c);
            if (val < 0)
                throw new FormatException($"Invalid Base32 character: {c}");

            bitBuffer = (bitBuffer << 5) | val;
            bitsInBuffer += 5;

            if (bitsInBuffer >= 8)
            {
                bitsInBuffer -= 8;
                bytes.Add((byte)((bitBuffer >> bitsInBuffer) & 0xFF));
            }
        }

        return bytes.ToArray();
    }
}
#endif
[Serializable]
internal sealed class ProdAuthPayload
{
    public string issuer;
    public string account;
    public string secret;
}

internal static class ProdAuthFileUtility
{
    private const string Magic = "LMS3DAUTH1";

    private static readonly byte[] EncKey = Sha256("XheroZone::LMS3D::ProdAuth::EncKey::v1");
    private static readonly byte[] MacKey = Sha256("XheroZone::LMS3D::ProdAuth::MacKey::v1");

    public static byte[] CreateFileBytes(ProdAuthPayload payload)
    {
        string json = JsonUtility.ToJson(payload);
        byte[] plain = Encoding.UTF8.GetBytes(json);

        byte[] iv = new byte[16];
        RandomNumberGenerator.Fill(iv);

        byte[] cipher = EncryptAesCbc(plain, EncKey, iv);

        byte[] magicBytes = Encoding.UTF8.GetBytes(Magic);
        byte[] macInput = Combine(magicBytes, iv, cipher);
        byte[] mac = ComputeHmac(macInput, MacKey);

        return Combine(magicBytes, iv, cipher, mac);
    }

    public static bool TryReadFileBytes(byte[] fileBytes, out ProdAuthPayload payload, out string error)
    {
        payload = null;
        error = null;

        try
        {
            byte[] magicBytes = Encoding.UTF8.GetBytes(Magic);

            if (fileBytes == null || fileBytes.Length < magicBytes.Length + 16 + 32)
            {
                error = "File quá ngắn.";
                return false;
            }

            byte[] fileMagic = new byte[magicBytes.Length];
            Buffer.BlockCopy(fileBytes, 0, fileMagic, 0, magicBytes.Length);

            if (!fileMagic.SequenceEqual(magicBytes))
            {
                error = "Magic header không đúng.";
                return false;
            }

            int ivOffset = magicBytes.Length;
            int cipherOffset = ivOffset + 16;
            int macOffset = fileBytes.Length - 32;
            int cipherLength = macOffset - cipherOffset;

            if (cipherLength <= 0)
            {
                error = "Cipher length không hợp lệ.";
                return false;
            }

            byte[] iv = new byte[16];
            Buffer.BlockCopy(fileBytes, ivOffset, iv, 0, 16);

            byte[] cipher = new byte[cipherLength];
            Buffer.BlockCopy(fileBytes, cipherOffset, cipher, 0, cipherLength);

            byte[] mac = new byte[32];
            Buffer.BlockCopy(fileBytes, macOffset, mac, 0, 32);

            byte[] macInput = Combine(magicBytes, iv, cipher);
            byte[] expectedMac = ComputeHmac(macInput, MacKey);

            if (!FixedTimeEquals(mac, expectedMac))
            {
                error = "MAC không hợp lệ.";
                return false;
            }

            byte[] plain = DecryptAesCbc(cipher, EncKey, iv);
            string json = Encoding.UTF8.GetString(plain);

            payload = JsonUtility.FromJson<ProdAuthPayload>(json);

            if (payload == null ||
                string.IsNullOrWhiteSpace(payload.issuer) ||
                string.IsNullOrWhiteSpace(payload.account) ||
                string.IsNullOrWhiteSpace(payload.secret))
            {
                error = "Payload không hợp lệ.";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static byte[] EncryptAesCbc(byte[] plain, byte[] key, byte[] iv)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using (ICryptoTransform enc = aes.CreateEncryptor())
            {
                return enc.TransformFinalBlock(plain, 0, plain.Length);
            }
        }
    }

    private static byte[] DecryptAesCbc(byte[] cipher, byte[] key, byte[] iv)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using (ICryptoTransform dec = aes.CreateDecryptor())
            {
                return dec.TransformFinalBlock(cipher, 0, cipher.Length);
            }
        }
    }

    private static byte[] ComputeHmac(byte[] data, byte[] key)
    {
        using (var hmac = new HMACSHA256(key))
        {
            return hmac.ComputeHash(data);
        }
    }

    private static byte[] Sha256(string s)
    {
        using (var sha = SHA256.Create())
        {
            return sha.ComputeHash(Encoding.UTF8.GetBytes(s));
        }
    }

    private static byte[] Combine(params byte[][] arrays)
    {
        int length = 0;
        foreach (var arr in arrays)
            length += arr.Length;

        byte[] result = new byte[length];
        int offset = 0;

        foreach (var arr in arrays)
        {
            Buffer.BlockCopy(arr, 0, result, offset, arr.Length);
            offset += arr.Length;
        }

        return result;
    }

    private static bool FixedTimeEquals(byte[] a, byte[] b)
    {
        if (a == null || b == null || a.Length != b.Length)
            return false;

        int diff = 0;
        for (int i = 0; i < a.Length; i++)
            diff |= a[i] ^ b[i];
        return diff == 0;
    }
}