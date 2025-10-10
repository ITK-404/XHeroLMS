// Assets/Scripts/AutoUpdaterUI.cs
using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Net;                         // TLS
using System.Text.RegularExpressions;     // Regex m_BundleVersion
using UnityEngine;
using UnityEngine.Networking;
using System.Diagnostics;                 // Restart exe
using Debug = UnityEngine.Debug;

[DisallowMultipleComponent]
public class AutoUpdaterUI : MonoBehaviour
{
    [Header("Remote (ProjectSettings.asset)")]
    [Tooltip("URL đọc ProjectSettings.asset để lấy m_BundleVersion")]
    public string projectSettingsUrl =
        "https://raw.githubusercontent.com/ITK-404/XHeroLMS/dev/ProjectSettings/ProjectSettings.asset";
    [Tooltip("URL dự phòng (jsDelivr)")]
    public string projectSettingsUrlFallback =
        "https://cdn.jsdelivr.net/gh/ITK-404/XHeroLMS@dev/ProjectSettings/ProjectSettings.asset";

    [Header("Base URL patch (kết thúc bằng /)")]
    [Tooltip("Thư mục chứa patch_#.zip (raw hoặc CDN)")]
    public string basePatchUrl =
        "https://raw.githubusercontent.com/ITK-404/XHeroLMS/dev/BuilderStore/";
    [Tooltip("Base URL dự phòng (jsDelivr), kết thúc bằng /")]
    public string basePatchUrlFallback =
        "https://cdn.jsdelivr.net/gh/ITK-404/XHeroLMS@dev/BuilderStore/";

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
        if (string.IsNullOrWhiteSpace(projectSettingsUrl))
            projectSettingsUrl = "https://raw.githubusercontent.com/ITK-404/XHeroLMS/dev/ProjectSettings/ProjectSettings.asset";
        if (string.IsNullOrWhiteSpace(projectSettingsUrlFallback))
            projectSettingsUrlFallback = "https://cdn.jsdelivr.net/gh/ITK-404/XHeroLMS@dev/ProjectSettings/ProjectSettings.asset";
        if (string.IsNullOrWhiteSpace(basePatchUrl))
            basePatchUrl = "https://raw.githubusercontent.com/ITK-404/XHeroLMS/dev/BuilderStore/";
        if (string.IsNullOrWhiteSpace(basePatchUrlFallback))
            basePatchUrlFallback = "https://cdn.jsdelivr.net/gh/ITK-404/XHeroLMS@dev/BuilderStore/";

        _ = FlowCheckAndUpdate();
    }

    async Task FlowCheckAndUpdate()
    {
        try
        {
            state = State.Checking; showPopup = true;
            uiTitle = "Đang kiểm tra cập nhật…";
            uiMessage = "Vui lòng chờ trong giây lát.";

            // 1) Lấy version remote từ ProjectSettings.asset (raw -> fallback)
            string remoteVer = await GetRemoteVersionFromProjectSettings(projectSettingsUrl);
            if (string.IsNullOrEmpty(remoteVer) && !string.IsNullOrEmpty(projectSettingsUrlFallback))
                remoteVer = await GetRemoteVersionFromProjectSettings(projectSettingsUrlFallback);

            if (string.IsNullOrEmpty(remoteVer))
            {
                SetError("Không đọc được version từ ProjectSettings.asset trên Git.");
                return;
            }

            string current = GetLocalVersion();
            if (!IsNewer(remoteVer, current))
            {
                state = State.UpToDate;
                uiTitle = "Bạn đang dùng bản mới nhất";
                uiMessage = $"Phiên bản: {current}";
                await Task.Delay(2000);
                showPopup = false;
                return;
            }

            // 2) Dựng URL patch theo quy ước patch_<version>.zip + fallback BUILD.zip
            string patchName = $"patch_{remoteVer}.zip";
            string packageUrlPrimary   = CombineUrl(basePatchUrl, patchName);
            string packageUrlPrimaryCDN = CombineUrl(basePatchUrlFallback, patchName);
            string packageUrlBuild     = CombineUrl(basePatchUrl, "BUILD.zip");
            string packageUrlBuildCDN  = CombineUrl(basePatchUrlFallback, "BUILD.zip");

            // 3) Tải patch (.zip)
            state = State.Downloading;
            uiTitle = $"Có bản mới {current} → {remoteVer}";
            uiMessage = "Đang tải bản cập nhật…";
            progress01 = 0f;

            string patchPath = Path.Combine(Application.persistentDataPath, patchFolder, patchName);
            Directory.CreateDirectory(Path.GetDirectoryName(patchPath));

            bool ok =
                await DownloadFileWithRetry(packageUrlPrimary, patchPath, maxRetries) ||
                await DownloadFileWithRetry(packageUrlPrimaryCDN, patchPath, maxRetries) ||
                await DownloadFileWithRetry(packageUrlBuild, patchPath, maxRetries) ||
                await DownloadFileWithRetry(packageUrlBuildCDN, patchPath, maxRetries);

            if (!ok)
            {
                SetError("Tải bản cập nhật thất bại. Hãy kiểm tra internet/URL và thử lại.");
                return;
            }

            // 3.5) Verify file có vẻ là ZIP thật
            if (!LooksLikeZip(patchPath))
            {
                try { File.Delete(patchPath); } catch {}
                SetError("File tải về không phải .zip hợp lệ (có thể là 404/HTML hoặc tải chưa đủ).");
                return;
            }

            // 4) Áp dụng patch — dùng helper để cập nhật được cả EXE
            state = State.Applying;
            uiTitle = "Đang áp dụng bản cập nhật…";
            uiMessage = "Game sẽ khởi động lại để hoàn tất cập nhật.";

#if UNITY_STANDALONE_WIN
            string gameDir = Path.GetDirectoryName(Application.dataPath);
            StartHelperAndQuit(patchPath, gameDir, Path.Combine(gameDir, exeName), Process.GetCurrentProcess().Id, remoteVer);
            // NOTE: helper sẽ:
            //  - chờ game thoát
            //  - giải nén đè
            //  - ghi version.json
            //  - xóa file zip
            //  - khởi động lại game
#else
            // Non-Windows fallback: giải nén ngay (không cập nhật được .app đang chạy)
            try
            {
                using (var zip = ZipFile.OpenRead(patchPath))
                {
                    foreach (var entry in zip.Entries)
                    {
                        if (string.IsNullOrEmpty(entry.Name)) continue;
                        string dest = Path.Combine(Path.GetDirectoryName(Application.dataPath), entry.FullName);
                        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                        entry.ExtractToFile(dest, overwrite: true);
                    }
                }
                File.Delete(patchPath);
                string gameDir2 = Path.GetDirectoryName(Application.dataPath);
                File.WriteAllText(Path.Combine(gameDir2, "version.json"), "{\"version\":\"" + remoteVer + "\"}");
                state = State.ReadyToRestart; uiTitle = $"Đã cập nhật lên {remoteVer}";
                uiMessage = "Nhấn Restart để áp dụng.";
                showPopup = true;
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

    // ===== Helpers =====
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

    string CombineUrl(string baseUrl, string fileName)
    {
        if (string.IsNullOrEmpty(baseUrl)) return fileName;
        if (!baseUrl.EndsWith("/")) baseUrl += "/";
        return baseUrl + fileName;
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
        if (string.IsNullOrWhiteSpace(url)) throw new Exception("URL rỗng");
        Directory.CreateDirectory(Path.GetDirectoryName(dest));
        Debug.Log($"[Updater] Downloading from: {url} -> {dest}");

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.SetRequestHeader("User-Agent", "XHeroLMS-Updater/1.0");
            www.timeout = 600;
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

    // ZIP sanity check (tránh file HTML 404)
    bool LooksLikeZip(string path)
    {
        try
        {
            using (var fs = File.OpenRead(path))
            {
                if (fs.Length < 22) return false;
                byte[] head = new byte[4]; fs.Read(head, 0, 4);
                bool headOk = head[0]==0x50 && head[1]==0x4B && head[2]==0x03 && head[3]==0x04;
                fs.Seek(-22, SeekOrigin.End);
                byte[] tail = new byte[4]; fs.Read(tail, 0, 4);
                bool tailOk = tail[0]==0x50 && tail[1]==0x4B && tail[2]==0x05 && tail[3]==0x06;
                return headOk && tailOk;
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
    // Tạo helper PowerShell để cập nhật sau khi game thoát (cập nhật được .exe)
    void StartHelperAndQuit(string zipPath, string gameDir, string exePath, int pid, string newVersion)
    {
        try
        {
            string ps1 = Path.Combine(Path.GetTempPath(), $"lms_update_{Guid.NewGuid():N}.ps1");
            string script = $@"
param([int]$pid,[string]$zip,[string]$dest,[string]$exe,[string]$ver)
# đợi process thoát
while (Get-Process -Id $pid -ErrorAction SilentlyContinue) {{ Start-Sleep -Milliseconds 300 }}
try {{
  Add-Type -AssemblyName System.IO.Compression.FileSystem
  [System.IO.Compression.ZipFile]::ExtractToDirectory($zip,$dest,$true)
  Remove-Item $zip -ErrorAction SilentlyContinue
  Set-Content -Path (Join-Path $dest 'version.json') -Value ('{{""version"":""' + $ver + '""}}') -Encoding UTF8
}} catch {{
  Write-Host $_
}}
Start-Process -FilePath $exe -WorkingDirectory (Split-Path $exe)
";
            File.WriteAllText(ps1, script);

            var psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"{ps1}\" {pid} \"{zipPath}\" \"{gameDir}\" \"{exePath}\" \"{newVersion}\"",
                UseShellExecute = true,
                CreateNoWindow = true,
                WorkingDirectory = gameDir
            };
            Process.Start(psi);
            // Thoát game → helper tiếp tục công việc rồi mở lại game
            Application.Quit();
        }
        catch (Exception e)
        {
            SetError("Không tạo được helper update: " + e.Message);
        }
    }
#endif
}
