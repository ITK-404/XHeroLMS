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

[InitializeOnLoad]
public static class AddressablesCloudAutoSetup
{
    private const string BucketName = "dlc-lms";
    private const string RootFolder = "addressables";
    private const string ProfileName = "GCS";

private static string GcloudExePath =>
    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Google", "Cloud SDK", "google-cloud-sdk", "bin", "gcloud.cmd");

private static string GsutilExePath =>
    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Google", "Cloud SDK", "google-cloud-sdk", "bin", "gsutil.cmd");


    // releases/<platform>/builds/<ver>/...
    // releases/<platform>/latest/...
    private const string ReleasesFolder = "releases";

    // KEY_MARKER: chỉ cần thay file json này khi đổi key
    private static readonly string ProjectKeyJsonPath =
        Path.Combine(Application.dataPath, "Editor", "GCS", "lms-3-479211-dc999d4c697c.json");

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
            // Writable config directory for gsutil/gcloud (avoid Program Files issues)
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Combine(home, ".config", "gcloud");
        }
    }

    private static string ProjectRoot
        => Directory.GetParent(Application.dataPath)?.FullName ?? "";

    static AddressablesCloudAutoSetup()
    {
        EnsureAddressablesSetup();
    }

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

        // --- Get or create profile id ---
        string profileId = ps.GetProfileId(ProfileName);
        if (string.IsNullOrEmpty(profileId))
        {
            string baseId = settings.activeProfileId;
            profileId = ps.AddProfile(ProfileName, baseId);
            UnityEngine.Debug.Log($"[AddressablesCloudAutoSetup] Created profile: {ProfileName} (base={baseId})");
        }

        // --- Variables ---
        EnsureVar(ps, profileId, "GCS_BUCKET", BucketName);
        EnsureVar(ps, profileId, "APP_VERSION", "0.0.0");
        EnsureVar(ps, profileId, "PLATFORM_NAME", "unknown");

        // Build path: always local folder ServerData/[BuildTarget]
        EnsureVar(ps, profileId, "RemoteBuildPath", "ServerData/[BuildTarget]");
        EnsureVar(ps, profileId, "Remote.BuildPath", "ServerData/[BuildTarget]");

        // Load path: ALWAYS latest (stable URL)
        string latestRemoteLoad =
            $"https://storage.googleapis.com/{{GCS_BUCKET}}/{RootFolder}/{ReleasesFolder}/{{PLATFORM_NAME}}/latest";
        EnsureVar(ps, profileId, "RemoteLoadPath", latestRemoteLoad);
        EnsureVar(ps, profileId, "Remote.LoadPath", latestRemoteLoad);

        settings.activeProfileId = profileId;

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();

        UnityEngine.Debug.Log("[AddressablesCloudAutoSetup] Done. Profile set to GCS.");
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

        string remoteBuildPath = ps.EvaluateString(profileId, ps.GetValueByName(profileId, "RemoteBuildPath"));
        string remoteLoadPath = ps.EvaluateString(profileId, ps.GetValueByName(profileId, "RemoteLoadPath"));

        string bucket = ps.EvaluateString(profileId, ps.GetValueByName(profileId, "GCS_BUCKET"));
        string ver = ps.EvaluateString(profileId, ps.GetValueByName(profileId, "APP_VERSION"));
        string plat = ps.EvaluateString(profileId, ps.GetValueByName(profileId, "PLATFORM_NAME"));

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

    // ======================================================
    // Build + Upload (builds/<ver> + sync to latest)
    // ======================================================
    [MenuItem("Tools/Cloud/Addressables/Build + Upload to GCS (builds + latest)")]
    public static void BuildAndUpload()
    {
        EnsureAddressablesSetup();

        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            UnityEngine.Debug.LogError("[AddressablesCloudAutoSetup] Addressable settings missing.");
            return;
        }

        // Version from PlayerSettings.bundleVersion
        string appVersion = PlayerSettings.bundleVersion;
        if (string.IsNullOrWhiteSpace(appVersion))
            appVersion = "0.0.0";

        // Platform from current editor build target
        BuildTarget bt = EditorUserBuildSettings.activeBuildTarget;
        string platformName = GetPlatformName(bt);

        var ps = settings.profileSettings;
        var profileId = settings.activeProfileId;

        // Update vars for build
        ps.SetValue(profileId, "APP_VERSION", appVersion);
        ps.SetValue(profileId, "PLATFORM_NAME", platformName);

        // Ensure latest RemoteLoadPath (stable)
        string latestRemoteLoad =
            $"https://storage.googleapis.com/{{GCS_BUCKET}}/{RootFolder}/{ReleasesFolder}/{{PLATFORM_NAME}}/latest";
        ps.SetValue(profileId, "RemoteLoadPath", latestRemoteLoad);
        ps.SetValue(profileId, "Remote.LoadPath", latestRemoteLoad);

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();

        UnityEngine.Debug.Log($"[AddressablesCloudAutoSetup] Building Addressables (PLATFORM={platformName}, APP_VERSION={appVersion}) ...");
        AddressableAssetSettings.BuildPlayerContent();

        // Local build output
        string serverDataRoot = Path.Combine(ProjectRoot, "ServerData");
        if (!Directory.Exists(serverDataRoot))
        {
            UnityEngine.Debug.LogError($"[AddressablesCloudAutoSetup] ServerData not found: {serverDataRoot}");
            return;
        }

        string localSrc = Path.Combine(serverDataRoot, bt.ToString());
        if (!Directory.Exists(localSrc))
        {
            UnityEngine.Debug.LogError($"[AddressablesCloudAutoSetup] Build output not found: {localSrc}");
            return;
        }

        // Credentials
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

        // Ensure child processes see the key
        Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", keyFileForCli);

        // Validate key (non-blocking)
        if (!RunCmd(GcloudExePath, $"auth activate-service-account --key-file=\"{keyFileForCli}\"", dumpLogFile: "gcloud_auth.txt"))
        {
            UnityEngine.Debug.LogWarning("[AddressablesCloudAutoSetup] gcloud auth failed (continuing anyway). gsutil will rely on GOOGLE_APPLICATION_CREDENTIALS.");
        }

        // IMPORTANT:
        // Do NOT use `gsutil ls -b gs://bucket` here.
        // `-b` needs storage.buckets.get and will fail for some service accounts even when uploads are allowed.
        // Let rsync be the real permission check.

        // 1) Upload to versioned builds/<ver> (history / rollback)
        string dstBuild = $"gs://{BucketName}/{RootFolder}/{ReleasesFolder}/{platformName}/builds/{appVersion}/";
        UnityEngine.Debug.Log($"[AddressablesCloudAutoSetup] Uploading (builds) {localSrc} -> {dstBuild}");
        if (!RunCmd(GsutilExePath, $"-m rsync -r \"{localSrc}\" \"{dstBuild}\"", dumpLogFile: "gsutil_rsync_builds.txt"))
        {
            UnityEngine.Debug.LogError("[AddressablesCloudAutoSetup] Upload failed (gsutil rsync) to builds/<ver>. Check gsutil_rsync_builds.txt for details.");
            return;
        }

        // 2) Sync builds/<ver> -> latest (stable URL for clients)
        string dstLatest = $"gs://{BucketName}/{RootFolder}/{ReleasesFolder}/{platformName}/latest/";
        UnityEngine.Debug.Log($"[AddressablesCloudAutoSetup] Syncing (latest) {dstBuild} -> {dstLatest}");
        if (!RunCmd(GsutilExePath, $"-m rsync -r \"{dstBuild}\" \"{dstLatest}\"", dumpLogFile: "gsutil_rsync_latest.txt"))
        {
            UnityEngine.Debug.LogError("[AddressablesCloudAutoSetup] Sync failed (gsutil rsync) builds/<ver> -> latest. Check gsutil_rsync_latest.txt for details.");
            return;
        }

        UnityEngine.Debug.Log(
            "[AddressablesCloudAutoSetup] Done.\n" +
            $"Versioned : https://storage.googleapis.com/{BucketName}/{RootFolder}/{ReleasesFolder}/{platformName}/builds/{appVersion}/\n" +
            $"Latest    : https://storage.googleapis.com/{BucketName}/{RootFolder}/{ReleasesFolder}/{platformName}/latest/\n" +
            "Client should always load from Latest (stable URL)."
        );
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

            // Ensure writable Cloud SDK config dir
            try { Directory.CreateDirectory(CloudSdkConfigDir); } catch { /* ignore */ }

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

            // Make gsutil/gcloud use a writable config directory
            psi.EnvironmentVariables["CLOUDSDK_CONFIG"] = CloudSdkConfigDir;

            // Force credential into child process
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

                if (!p.WaitForExit(timeoutMs))
                {
                    // Timeout -> kill (and children)
                    try
                    {
                        try { p.Kill(); } catch { /* ignore */ }

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
                        catch { /* ignore */ }
#endif
                    }
                    catch { /* ignore */ }

                    result.ExitCode = -2;
                    result.StdErr = $"TIMEOUT after {timeoutMs}ms";
                    DumpCmdLogIfNeeded(exe, args, result.ExitCode, sbOut.ToString(), result.StdErr, dumpLogFile, out result.LogPath);
                    return result;
                }

                // flush async buffers
                try { p.WaitForExit(1000); } catch { /* ignore */ }

                result.ExitCode = p.ExitCode;
                result.StdOut = sbOut.ToString();
                result.StdErr = sbErr.ToString();

                DumpCmdLogIfNeeded(exe, args, result.ExitCode, result.StdOut, result.StdErr, dumpLogFile, out result.LogPath);
                return result;
            }
        }
        catch (Exception e)
        {
            result.ExitCode = -3;
            result.StdErr = e.ToString();
            DumpCmdLogIfNeeded(exe, args, result.ExitCode, result.StdOut, result.StdErr, dumpLogFile, out result.LogPath);
            return result;
        }
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
