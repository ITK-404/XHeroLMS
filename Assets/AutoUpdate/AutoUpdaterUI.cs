// Assets/Scripts/AutoUpdaterUI.cs
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Net;                         // TLS
using System.Text.RegularExpressions;     // Regex m_BundleVersion
using UnityEngine;
using UnityEngine.Networking;
using System.Diagnostics;                 // Restart exe / helper
using Debug = UnityEngine.Debug;

[DisallowMultipleComponent]
public class AutoUpdaterUI : MonoBehaviour
{
    [Header("Repo dùng cho GitHub Releases")]
    public string githubOwner = "ITK-404";
    public string githubRepo  = "XHeroLMS";
    [Tooltip("Prefix của tag release, ví dụ 'v' -> v1.2.3")]
    public string tagPrefix   = "v";

    [Header("Đọc version remote từ ProjectSettings.asset (nhánh dev)")]
    public string projectSettingsUrl =
        "https://raw.githubusercontent.com/ITK-404/XHeroLMS/dev/ProjectSettings/ProjectSettings.asset";
    public string projectSettingsUrlFallback =
        "https://cdn.jsdelivr.net/gh/ITK-404/XHeroLMS@dev/ProjectSettings/ProjectSettings.asset";

    [Header("Local")]
    [Tooltip("Phiên bản fallback nếu không đọc được version.json & Application.version")]
    public string localVersionFallback = "1.0.0";
    [Tooltip("Tên exe của game để restart")]
    public string exeName = "XHeroLMS.exe";

    [Header("Download")]
    [Tooltip("Thư mục tạm trong Application.persistentDataPath")]
    public string patchFolder = "Updates";
    [Tooltip("Số lần retry mạng")]
    public int maxRetries = 2;

    [SerializeField] bool helperDebug = true; // bật để xem console/log lần đầu
    // ===== Runtime UI state =====
    Rect windowRect = new Rect(20, 20, 520, 220);
    bool showPopup = false;
    string uiTitle = "Auto Updater";
    string uiMessage = "";
    float progress01 = 0f;
    State state = State.Idle;

    enum State { Idle, Checking, UpToDate, Downloading, Applying, ReadyToRestart, Error }
    [Serializable] class VersionJson { public string version; }

    void Awake()
    {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
    }

    void Start()
    {
#if UNITY_EDITOR
    Debug.Log("[Updater] Skipped in Editor (no version check)");
        return;
#else
        _ = FlowCheckAndUpdate();
#endif
    }

    async Task FlowCheckAndUpdate()
    {
        try
        {
            state = State.Checking; showPopup = true;
            uiTitle = "Đang kiểm tra cập nhật…";
            uiMessage = "Vui lòng chờ trong giây lát.";

            // Lấy version remote từ ProjectSettings.asset (raw -> fallback)
            string remoteVer = await GetRemoteVersionFromProjectSettings(projectSettingsUrl);
            if (string.IsNullOrEmpty(remoteVer) && !string.IsNullOrEmpty(projectSettingsUrlFallback))
                remoteVer = await GetRemoteVersionFromProjectSettings(projectSettingsUrlFallback);

            if (string.IsNullOrEmpty(remoteVer))
            {
                SetError("Không đọc được version từ ProjectSettings.asset trên Git.");
                return;
            }

            string current = GetLocalVersion();
            Debug.Log($"[Updater] Local version: {current} | Remote version: {remoteVer}");

            if (!IsNewer(remoteVer, current))
            {
                state = State.UpToDate;
                uiTitle = "Bạn đang dùng bản mới nhất";
                uiMessage = $"Phiên bản: {current}";
                await Task.Delay(2000);
                showPopup = false;
                return;
            }

            // Dựng URL từ GitHub Releases
            string tag = $"{tagPrefix}{remoteVer}";
            string patchName = $"patch_{remoteVer}.zip";
            string urlPatch = BuildReleaseUrl(githubOwner, githubRepo, tag, patchName);
            string urlBuild = BuildReleaseUrl(githubOwner, githubRepo, tag, "BUILD.zip"); // fallback

            // Tải patch (.zip)
            state = State.Downloading;
            uiTitle = $"Có bản mới {current} → {remoteVer}";
            uiMessage = "Đang tải bản cập nhật…";
            progress01 = 0f;

            string patchPath = Path.Combine(Application.persistentDataPath, patchFolder, patchName);
            Directory.CreateDirectory(Path.GetDirectoryName(patchPath));
            Debug.Log($"[Updater] persistentDataPath: {Application.persistentDataPath}");
            Debug.Log($"[Updater] patchPath: {patchPath}");

            bool ok =
                await DownloadFileWithRetry(urlPatch, patchPath, maxRetries) ||
                await DownloadFileWithRetry(urlBuild, patchPath, maxRetries);

            if (!ok)
            {
                SetError("Tải bản cập nhật thất bại. Hãy kiểm tra internet/URL và thử lại.");
                return;
            }

            // Verify file là ZIP (header)
            if (!LooksLikeZip(patchPath))
            {
                try { File.Delete(patchPath); } catch {}
                SetError("File tải về không phải .zip hợp lệ (có thể là HTML/404 hoặc tải chưa đủ).");
                return;
            }

            // Áp dụng patch — dùng helper để cập nhật được cả EXE
            state = State.Applying;
            uiTitle = "Đang áp dụng bản cập nhật…";
            uiMessage = "Game sẽ khởi động lại để hoàn tất cập nhật.";

            string gameDir = Path.GetDirectoryName(Application.dataPath);
            Debug.Log($"[Updater] gameDir: {gameDir}");

#if UNITY_STANDALONE_WIN
            StartHelperAndQuit(
                zipPath: patchPath,
                gameDir: gameDir,
                exePath: Path.Combine(gameDir, exeName),
                pid: Process.GetCurrentProcess().Id,
                newVersion: remoteVer
            );
#else
            // Non-Windows: giải nén ra thư mục tạm rồi copy sang gameDir (an toàn hơn)
            try
            {
                string tempBase = Path.Combine(Path.GetTempPath(), "lms_unpack_" + Guid.NewGuid().ToString("N"));
                string tempDir  = Path.Combine(tempBase, "payload");
                Directory.CreateDirectory(tempDir);

                using (var zip = ZipFile.OpenRead(patchPath))
                {
                    zip.ExtractToDirectory(tempDir);
                }

                // Unwrap nếu root là 1 thư mục
                string srcRoot = tempDir;
                var entries = Directory.GetFileSystemEntries(tempDir);
                if (entries.Length == 1 && Directory.Exists(entries[0]))
                {
                    srcRoot = entries[0];
                }

                CopyDirectoryRecursive(srcRoot, gameDir);
                try { File.Delete(patchPath); } catch {}

                // Ghi version.json
                File.WriteAllText(Path.Combine(gameDir, "version.json"), "{ \"version\": \"" + remoteVer + "\" }");

                state = State.ReadyToRestart; uiTitle = $"Đã cập nhật lên {remoteVer}";
                uiMessage = "Nhấn Restart để áp dụng.";
                showPopup = true;

                try { Directory.Delete(tempBase, true); } catch {}
            }
            catch (Exception e)
            {
                state = State.ReadyToRestart;
                uiTitle = "Cần khởi động lại để hoàn tất";
                uiMessage = "Không thể giải nén toàn bộ khi app đang chạy.\nChi tiết: " + e.Message;
                showPopup = true;
            }
#endif
        }
        catch (Exception ex)
        {
            SetError("Lỗi không xác định: " + ex.Message);
        }
    }

    // ===== UI =====
    void OnGUI()
    {
        if (!showPopup) return;
        windowRect = GUI.ModalWindow(1927, windowRect, DrawWindow, "Auto Updater");
    }

    void DrawWindow(int id)
    {
        GUILayout.Label($"Trạng thái: {state}");
        if (!string.IsNullOrEmpty(uiMessage))
            GUILayout.Label(uiMessage, GUILayout.Width(500));

        if (state == State.Downloading || state == State.Applying || state == State.ReadyToRestart)
        {
            var rect = GUILayoutUtility.GetRect(500, 18);
            GUI.Box(rect, GUIContent.none);
            var fill = new Rect(rect.x + 2, rect.y + 2, (rect.width - 4) * Mathf.Clamp01(progress01), rect.height - 4);
            GUI.Box(fill, GUIContent.none);
            GUILayout.Space(6);
        }

        GUILayout.BeginHorizontal();
        if (state == State.Error)
        {
            if (GUILayout.Button("Retry", GUILayout.Height(28)))
                _ = FlowCheckAndUpdate();
        }
        if (state == State.ReadyToRestart)
        {
            if (GUILayout.Button("Restart Now", GUILayout.Height(28)))
                RestartGame();
        }
        if (GUILayout.Button("Đóng", GUILayout.Height(28)))
            showPopup = false;
        GUILayout.EndHorizontal();

        GUI.DragWindow(new Rect(0, 0, 10000, 20));
    }

    string GetLocalVersion()
    {
        try
        {
            string gameDir = Path.GetDirectoryName(Application.dataPath);
            string jsonPath = Path.Combine(gameDir, "version.json");
            if (File.Exists(jsonPath))
            {
                var txt = File.ReadAllText(jsonPath);
                var v = JsonUtility.FromJson<VersionJson>(txt);
                if (v != null && !string.IsNullOrEmpty(v.version))
                {
                    Debug.Log($"[Updater] Local version.json: {v.version}");
                    return v.version;
                }
                Debug.LogWarning($"[Updater] version.json không hợp lệ: {txt}");
            }
            else
            {
                Debug.LogWarning($"[Updater] Không tìm thấy version.json tại: {jsonPath}");
            }
        }
        catch (Exception e) { Debug.LogWarning("[Updater] Lỗi đọc version.json: " + e.Message); }

        if (!string.IsNullOrEmpty(Application.version))
            return Application.version;

        return localVersionFallback;
    }

    async Task<string> GetRemoteVersionFromProjectSettings(string url)
    {
        string text = await DownloadText(url);
        if (string.IsNullOrEmpty(text)) return null;

        var m = Regex.Match(text, @"m_BundleVersion\s*:\s*([0-9]+(\.[0-9]+){1,3})");
        if (!m.Success) m = Regex.Match(text, @"bundleVersion\s*:\s*([0-9]+(\.[0-9]+){1,3})");
        if (m.Success)
        {
            string ver = m.Groups[1].Value;
            Debug.Log($"[Updater] Remote version (ProjectSettings): {ver}");
            return ver;
        }
        Debug.LogError("[Updater] Không tìm thấy m_BundleVersion trong ProjectSettings.asset");
        return null;
    }

    bool IsNewer(string remote, string local)
    {
        Version v1, v2;
        if (Version.TryParse(remote, out v1) && Version.TryParse(local, out v2))
            return v1 > v2;
        return false;
    }

    string BuildReleaseUrl(string owner, string repo, string tag, string filename)
    {
        // https://github.com/{owner}/{repo}/releases/download/{tag}/{filename}
        return $"https://github.com/{owner}/{repo}/releases/download/{tag}/{filename}";
    }

    async Task<string> DownloadText(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.SetRequestHeader("User-Agent", "XHeroLMS-Updater/1.0");
            www.timeout = 20;
            var op = www.SendWebRequest();
            while (!op.isDone) await Task.Yield();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[Updater] GET {url}\nHTTP: {www.responseCode}\nError: {www.error}");
                return null;
            }
            return www.downloadHandler.text;
        }
    }

    async Task<bool> DownloadFileWithRetry(string url, string dest, int retries)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        for (int i = 0; i <= retries; i++)
        {
            try
            {
                await DownloadFile(url, dest);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Updater] Download lỗi: " + e.Message);
                if (i == retries) return false;
                await Task.Delay(1000 * (i + 1));
            }
        }
        return false;
    }

    async Task DownloadFile(string url, string dest)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dest));
        Debug.Log($"[Updater] Downloading from: {url} -> {dest}");

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.SetRequestHeader("User-Agent", "XHeroLMS-Updater/1.0");
            www.timeout = 3600; // 1 giờ cho file to
            www.downloadHandler = new DownloadHandlerFile(dest) { removeFileOnAbort = true };

            var op = www.SendWebRequest();
            while (!op.isDone)
            {
                progress01 = www.downloadProgress;
                await Task.Yield();
            }
            if (www.result != UnityWebRequest.Result.Success)
                throw new Exception($"HTTP {www.responseCode} - {www.error}");
        }
        LogFileInfo(dest);
    }

    // ZIP sanity check (đơn giản, tránh false-negative do comment ở EOCD)
    bool LooksLikeZip(string path)
    {
        try
        {
            using (var fs = File.OpenRead(path))
            {
                if (fs.Length < 4) return false;
                byte[] head = new byte[4];
                fs.Read(head, 0, 4);
                return head[0]==0x50 && head[1]==0x4B && head[2]==0x03 && head[3]==0x04;
            }
        }
        catch { return false; }
    }

    void LogFileInfo(string path)
    {
        try
        {
            var fi = new FileInfo(path);
            Debug.Log($"[Updater] Saved file: {path} ({fi.Length} bytes)");
        }
        catch { }
    }

    void SetError(string msg)
    {
        state = State.Error;
        uiTitle = "Lỗi cập nhật";
        uiMessage = msg;
        showPopup = true;
    }

    void RestartGame()
    {
        try
        {
#if UNITY_STANDALONE_WIN
            string exePath = Path.Combine(Path.GetDirectoryName(Application.dataPath), exeName);
            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = Path.GetDirectoryName(exePath),
                UseShellExecute = true
            });
#endif
            Application.Quit();
        }
        catch (Exception e) { SetError("Không thể khởi động lại: " + e.Message); }
    }

#if UNITY_STANDALONE_WIN
    void StartHelperAndQuit(string zipPath, string gameDir, string exePath, int pid, string newVersion)
    {
        try
        {
            string ps1 = Path.Combine(Path.GetTempPath(), $"lms_update_{Guid.NewGuid():N}.ps1");
            string logPath = Path.Combine(Path.GetTempPath(), $"lms_update_{Guid.NewGuid():N}.log");

            string script = @"
param([int]$gamePid,[string]$zip,[string]$dest,[string]$exe,[string]$ver,[string]$logPath)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Test-Admin {
  $u=[Security.Principal.WindowsIdentity]::GetCurrent()
  (New-Object Security.Principal.WindowsPrincipal($u)).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}
function Needs-Elevation($p) {
  $pf=$env:ProgramFiles; $pf86=${env:ProgramFiles(x86)}
  $p.StartsWith($pf,[System.StringComparison]::InvariantCultureIgnoreCase) -or
  ($pf86 -and $p.StartsWith($pf86,[System.StringComparison]::InvariantCultureIgnoreCase))
}

# Logging
$global:LOG = if ([string]::IsNullOrWhiteSpace($logPath)) { Join-Path $env:TEMP ('lms_update_' + [Guid]::NewGuid().ToString('N') + '.log') } else { $logPath }
function Log($m){ ('[{0}] {1}' -f (Get-Date), $m) | Out-File -FilePath $global:LOG -Append -Encoding UTF8 }

# Elevate when needed
if (Needs-Elevation $dest -and -not (Test-Admin)) {
  Log 'Re-launching elevated...'
  Start-Process -FilePath 'powershell.exe' `
    -ArgumentList @('-NoProfile','-ExecutionPolicy','Bypass','-File', $PSCommandPath, $gamePid, $zip, $dest, $exe, $ver, $global:LOG) `
    -Verb RunAs
  exit
}

Log 'Helper started'
Log ""zip=$zip""; Log ""dest=$dest""; Log ""exe=$exe""; Log ""ver=$ver""; Log ""log=$global:LOG""

# Wait game exit
while (Get-Process -Id $gamePid -ErrorAction SilentlyContinue) { Start-Sleep -Milliseconds 300 }
Log 'Game exited.'

# Unpack to temp
$tempBase = Join-Path $env:TEMP ('lms_unpack_' + [Guid]::NewGuid().ToString('N'))
$tempDir  = Join-Path $tempBase 'payload'
New-Item -ItemType Directory -Force -Path $tempDir | Out-Null
try {
  if (Get-Command Expand-Archive -ErrorAction SilentlyContinue) {
    Log 'Using Expand-Archive'
    Expand-Archive -Path $zip -DestinationPath $tempDir -Force
  } else {
    Log 'Using ZipFile API'
    Add-Type -AssemblyName System.IO.Compression.FileSystem
    [System.IO.Compression.ZipFile]::ExtractToDirectory($zip, $tempDir)
  }

  $entries = Get-ChildItem -Path $tempDir -Force
  $src = if ($entries.Count -eq 1 -and $entries[0].PSIsContainer) { $entries[0].FullName } else { $tempDir }
  Log ('Copy from: ' + $src)

  $null = robocopy $src $dest /MIR /IS /IT /R:1 /W:1 /FFT /NFL /NDL /NP /NJH /NJS /MT:8
  Log ('Robocopy exit=' + $LASTEXITCODE)

  Set-Content -Path (Join-Path $dest 'version.json') -Value ('{ ""version"": ""' + $ver + '"" }') -Encoding UTF8
  Remove-Item $zip -ErrorAction SilentlyContinue
} catch { Log ('Update failed: ' + $_) } finally {
  try { Remove-Item $tempBase -Recurse -Force -ErrorAction SilentlyContinue } catch {}
}

Log 'Starting game...'
Start-Process -FilePath $exe -WorkingDirectory (Split-Path $exe)
Log 'Done.'
";

            File.WriteAllText(ps1, script);

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = helperDebug
                    ? $"-NoProfile -ExecutionPolicy Bypass -NoExit -File \"{ps1}\" {pid} \"{zipPath}\" \"{gameDir}\" \"{exePath}\" \"{newVersion}\" \"{logPath}\""
                    : $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{ps1}\" {pid} \"{zipPath}\" \"{gameDir}\" \"{exePath}\" \"{newVersion}\" \"{logPath}\"",
                UseShellExecute = true,
                CreateNoWindow = !helperDebug,
                WorkingDirectory = gameDir,
                WindowStyle = helperDebug ? ProcessWindowStyle.Normal : ProcessWindowStyle.Hidden
            };

            Debug.Log($"[Updater] Helper log: {logPath}");
            var p = Process.Start(psi);
            if (p == null) { SetError("Không khởi chạy được helper."); return; }

            System.Threading.Thread.Sleep(300); // cho helper spawn xong
            Application.Quit();
        }
        catch (Exception e)
        {
            SetError("Không tạo được helper update: " + e.Message);
        }
    }
#endif
}
