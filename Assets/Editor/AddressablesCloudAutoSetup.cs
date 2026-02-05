#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Networking;


[InitializeOnLoad]
public static class AddressablesCloudAutoSetup
{
    // ===================== CONFIG =====================
    private const string BucketName   = "dlc-lms";
    private const string RootFolder   = "addressables";
    private const string ReleasesFolder = "releases";
    private const string ProfileName  = "GCS";

    private const string VarRemoteBuildPath = "RemoteBuildPath";
    private const string VarRemoteLoadPath  = "RemoteLoadPath";

    private const string VarRemoteBuildPathSchema = "Remote.BuildPath";
    private const string VarRemoteLoadPathSchema  = "Remote.LoadPath";

    private const string RemoteBuildPathValue = "ServerData/[BuildTarget]";

    private const bool AutoBumpPatch = false;
    private static readonly string ProjectKeyJsonPath =
        Path.Combine(Application.dataPath, "Editor", "GCS", "lms-3-479211-dc999d4c697c.json");

    private static string GcloudExePath => ResolveGcloudExe("gcloud");
    private static string GsutilExePath => ResolveGcloudExe("gsutil");

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

    private static string ResolveGcloudExe(string toolName)
    {
#if UNITY_EDITOR_WIN
        // Windows install via Cloud SDK installer
        string win = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Google", "Cloud SDK", "google-cloud-sdk", "bin", toolName + ".cmd"
        );
        if (File.Exists(win)) return win;

        // fallback: rely on PATH
        return toolName + ".cmd";
#else
    string brew1 = Path.Combine("/opt/homebrew/bin", toolName);
    if (File.Exists(brew1)) return brew1;

    string brew2 = Path.Combine("/usr/local/bin", toolName);
    if (File.Exists(brew2)) return brew2;

    // official installer sometimes goes here (varies)
    string sdk1 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "google-cloud-sdk", "bin", toolName);
    if (File.Exists(sdk1)) return sdk1;

    // fallback: rely on PATH
    return toolName;
#endif
    }

    private static string LatestRemoteLoadResolved(string platformName)
    => $"https://storage.googleapis.com/{BucketName}/{RootFolder}/{ReleasesFolder}/{platformName}/latest";


    static AddressablesCloudAutoSetup()
    {
        EnsureAddressablesSetup();
    }

    // ===================== MENU =====================

    [MenuItem("Tools/Cloud/Addressables/Ensure Setup")]
    public static void EnsureAddressablesSetup()
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

        // Get or create profile
        string profileId = ps.GetProfileId(ProfileName);
        if (string.IsNullOrEmpty(profileId))
        {
            string baseId = settings.activeProfileId;
            profileId = ps.AddProfile(ProfileName, baseId);
            UnityEngine.Debug.Log($"[AddressablesCloudAutoSetup] Created profile: {ProfileName} (base={baseId})");
        }

        // Core vars
        EnsureVar(ps, profileId, "GCS_BUCKET", BucketName);

        string appVersion = string.IsNullOrWhiteSpace(PlayerSettings.bundleVersion) ? "0.0.0" : PlayerSettings.bundleVersion;
        EnsureVar(ps, profileId, "APP_VERSION", appVersion);

        var platformName = GetPlatformName(EditorUserBuildSettings.activeBuildTarget);
        EnsureVar(ps, profileId, "PLATFORM_NAME", platformName);

        // Canonical vars
        EnsureVar(ps, profileId, VarRemoteBuildPath, RemoteBuildPathValue);
        var resolvedLoad = LatestRemoteLoadResolved(platformName);
        EnsureVar(ps, profileId, VarRemoteLoadPath, resolvedLoad);

        SyncOptionalVarIfExists(ps, profileId, VarRemoteBuildPathSchema, RemoteBuildPathValue);
        SyncOptionalVarIfExists(ps, profileId, VarRemoteLoadPathSchema, resolvedLoad);

        settings.activeProfileId = profileId;

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();

        UnityEngine.Debug.Log("[AddressablesCloudAutoSetup] Done. Profile set to GCS.");
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

        var ps = settings.profileSettings;
        var profileId = settings.activeProfileId;

        string bucket = Eval(ps, profileId, "GCS_BUCKET");
        string ver    = Eval(ps, profileId, "APP_VERSION");
        string plat   = Eval(ps, profileId, "PLATFORM_NAME");

        string remoteBuildPath = Eval(ps, profileId, VarRemoteBuildPath);
        string remoteLoadPath  = Eval(ps, profileId, VarRemoteLoadPath);

        UnityEngine.Debug.Log(
            "[AddressablesCloudAutoSetup] Resolved Profile Paths:\n" +
            $"ActiveProfileId : {profileId}\n" +
            $"GCS_BUCKET      : {bucket}\n" +
            $"APP_VERSION     : {ver}\n" +
            $"PLATFORM_NAME   : {plat}\n" +
            $"RemoteBuildPath : {remoteBuildPath}\n" +
            $"RemoteLoadPath  : {remoteLoadPath}\n"
        );
    }

    [MenuItem("Tools/Cloud/Addressables/Build + Upload to GCS (builds + latest)")]
    public static void BuildAndUpload()
    {
        // ====== OPTIONS ======
        // Nếu bạn muốn tự xoá catalog_*.json/hash trên latest sau khi sync -> bật true
        const bool RemoveVersionedCatalogsOnLatest = false;

        if (AutoBumpPatch)
        {
            PlayerSettings.bundleVersion = BumpPatch(PlayerSettings.bundleVersion);
            UnityEngine.Debug.Log($"[AddressablesCloudAutoSetup] Auto bumped version -> {PlayerSettings.bundleVersion}");
        }

        EnsureAddressablesSetup();

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            UnityEngine.Debug.LogError("[AddressablesCloudAutoSetup] Addressable settings missing.");
            return;
        }

        string appVersion = string.IsNullOrWhiteSpace(PlayerSettings.bundleVersion) ? "0.0.0" : PlayerSettings.bundleVersion;
        BuildTarget bt = EditorUserBuildSettings.activeBuildTarget;
        string platformName = GetPlatformName(bt);
        string resolvedLoad = LatestRemoteLoadResolved(platformName);

        var ps = settings.profileSettings;
        var profileId = settings.activeProfileId;

        // Refresh profile vars for this run
        ps.SetValue(profileId, "APP_VERSION", appVersion);
        ps.SetValue(profileId, "PLATFORM_NAME", platformName);
        ps.SetValue(profileId, VarRemoteBuildPath, RemoteBuildPathValue);
        ps.SetValue(profileId, VarRemoteLoadPath, resolvedLoad);

        SyncOptionalVarIfExists(ps, profileId, VarRemoteBuildPathSchema, RemoteBuildPathValue);
        SyncOptionalVarIfExists(ps, profileId, VarRemoteLoadPathSchema, resolvedLoad);

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();

        // Clean output folder to avoid mixing old files
        string expectedLocalOut = FindLocalBuildOutput(bt);
        CleanDirectorySafe(expectedLocalOut);

        UnityEngine.Debug.Log(
            "[AddressablesCloudAutoSetup] Building Addressables...\n" +
            $"BT={bt}\nPLATFORM={platformName}\nAPP_VERSION={appVersion}\nRemoteLoadPath={resolvedLoad}"
        );

        AddressableAssetSettings.BuildPlayerContent();

        // Local output
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

        // ======= Ensure standard catalog.json + catalog.hash exist =======
        string versionedJson = Directory.GetFiles(localSrc, "catalog_*.json").OrderByDescending(f => f).FirstOrDefault();
        string versionedHash = Directory.GetFiles(localSrc, "catalog_*.hash").OrderByDescending(f => f).FirstOrDefault();

        if (string.IsNullOrEmpty(versionedJson) || string.IsNullOrEmpty(versionedHash))
        {
            UnityEngine.Debug.LogError(
                "[AddressablesCloudAutoSetup] Catalog missing in local output.\n" +
                "Expected catalog_*.json and catalog_*.hash in:\n" + localSrc
            );
            return;
        }

        string dstCatalogJson = Path.Combine(localSrc, "catalog.json");
        string dstCatalogHash = Path.Combine(localSrc, "catalog.hash");

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

        // (Optional) quick sanity about bundles
        int bundleCount = Directory.GetFiles(localSrc, "*.bundle", SearchOption.TopDirectoryOnly).Length;
        UnityEngine.Debug.Log($"[AddressablesCloudAutoSetup] Bundles in output: {bundleCount}");

        // ======= Credentials =======
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

        // Optional: validate auth (non-blocking)
        if (!RunCmd(GcloudExePath, $"auth activate-service-account --key-file=\"{keyFileForCli}\"", dumpLogFile: "gcloud_auth.txt"))
        {
            UnityEngine.Debug.LogWarning("[AddressablesCloudAutoSetup] gcloud auth failed (continuing anyway). gsutil will rely on GOOGLE_APPLICATION_CREDENTIALS.");
        }

        // ======= Upload =======
        string dstBuild = $"gs://{BucketName}/{RootFolder}/{ReleasesFolder}/{platformName}/builds/{appVersion}/";
        string dstLatest = $"gs://{BucketName}/{RootFolder}/{ReleasesFolder}/{platformName}/latest/";

        UnityEngine.Debug.Log($"[AddressablesCloudAutoSetup] Uploading (builds) {localSrc} -> {dstBuild}");
        if (!RunCmd(GsutilExePath, $"-m rsync -r \"{localSrc}\" \"{dstBuild}\"", dumpLogFile: "gsutil_rsync_builds.txt"))
            return;

        UnityEngine.Debug.Log($"[AddressablesCloudAutoSetup] Syncing (latest) {dstBuild} -> {dstLatest}");
        if (!RunCmd(GsutilExePath, $"-m rsync -r \"{dstBuild}\" \"{dstLatest}\"", dumpLogFile: "gsutil_rsync_latest.txt"))
            return;

        // ======= Optional: remove versioned catalogs on latest (keep only catalog.json/hash) =======
        if (RemoveVersionedCatalogsOnLatest)
        {
            UnityEngine.Debug.Log("[AddressablesCloudAutoSetup] Removing catalog_*.json/hash on latest (keep catalog.json/hash) ...");
            RunCmd(GsutilExePath, $"-m rm \"{dstLatest}catalog_*.json\"", dumpLogFile: "gsutil_rm_catalog_ver_json_latest.txt");
            RunCmd(GsutilExePath, $"-m rm \"{dstLatest}catalog_*.hash\"", dumpLogFile: "gsutil_rm_catalog_ver_hash_latest.txt");
        }

        // Verify catalog exists remotely
        // bool okLatestCatalog = RunCmd(GsutilExePath, $"ls \"{dstLatest}\" | findstr /i catalog", dumpLogFile: "gsutil_verify_latest_catalog.txt");
        string verifyCmd;
#if UNITY_EDITOR_WIN
        verifyCmd = $"ls \"{dstLatest}\" | findstr /i catalog";
#else
    verifyCmd = $"ls \"{dstLatest}\" | grep -i catalog";
#endif

        // bool okLatestCatalog = RunCmd(GsutilExePath, verifyCmd, dumpLogFile: "gsutil_verify_latest_catalog.txt");
        bool okLatestCatalog = VerifyLatestCatalogExists(dstLatest);
        if (!okLatestCatalog)
            UnityEngine.Debug.LogError("[AddressablesCloudAutoSetup] Remote latest/ missing catalog.json or catalog.hash. Check gsutil_verify_latest_catalog.txt");


        if (!okLatestCatalog)
            UnityEngine.Debug.LogError("[AddressablesCloudAutoSetup] Remote latest/ does NOT show catalog files. Check gsutil_verify_latest_catalog.txt");

        UnityEngine.Debug.Log(
            "[AddressablesCloudAutoSetup] Done.\n" +
            $"Builds : https://storage.googleapis.com/{BucketName}/{RootFolder}/{ReleasesFolder}/{platformName}/builds/{appVersion}/\n" +
            $"Latest : https://storage.googleapis.com/{BucketName}/{RootFolder}/{ReleasesFolder}/{platformName}/latest/\n" +
            "Verify in browser:\n" +
            $" - https://storage.googleapis.com/{BucketName}/{RootFolder}/{ReleasesFolder}/{platformName}/latest/catalog.json\n" +
            $" - https://storage.googleapis.com/{BucketName}/{RootFolder}/{ReleasesFolder}/{platformName}/latest/catalog.hash\n"
        );
    }

    private static bool VerifyLatestCatalogExists(string dstLatest)
    {
        var r = RunCmdWithResult(GsutilExePath, $"ls \"{dstLatest}\"", 5 * 60 * 1000, "gsutil_verify_latest_catalog.txt");

        if (r.ExitCode != 0) return false;

        var s = (r.StdOut ?? "") + "\n" + (r.StdErr ?? "");
        s = s.ToLowerInvariant();

        // cần ít nhất catalog.json và catalog.hash
        return s.Contains("catalog.json") && s.Contains("catalog.hash");
    }

    // ===================== HELPERS =====================

    private static string GetServerDataRoot()
    {
        return Path.Combine(ProjectRoot, "ServerData");
    }

    private static string FindLocalBuildOutput(BuildTarget bt)
    {
        // Try common names: ServerData/Android, ServerData/android, ServerData/<BuildTarget>
        string root = GetServerDataRoot();

        var candidates = new List<string>
        {
            Path.Combine(root, bt.ToString()),                 // "Android"
            Path.Combine(root, bt.ToString().ToLowerInvariant()), // "android" (rare)
            Path.Combine(root, FirstCharUpper(GetPlatformName(bt))), // "Android" from "android"
            Path.Combine(root, GetPlatformName(bt)),           // "android"
        };

        foreach (var c in candidates)
        {
            if (Directory.Exists(c))
                return c;
        }

        return candidates[0]; // fallback for logging
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
        // Very simple x.y.z bump
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
            // Copy to temp to avoid locked/ACL oddities in project folder
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
            UnityEngine.Debug.LogError($"[CMD] {exe} {args}\nExitCode={r.ExitCode}\n{r.StdErr}\n" +
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
#endif
