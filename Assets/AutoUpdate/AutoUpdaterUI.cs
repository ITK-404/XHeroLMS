using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using System.Net;                         // TLS
using System.Text.RegularExpressions;     // Regex m_BundleVersion
using UnityEngine;
using UnityEngine.Networking;
using System.Diagnostics;                 // Restart exe

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
    // [Tooltip("Tên file patch tạm")]
    // public string patchFileName = "patch.zip";
    [Tooltip("Số lần retry mạng")]
    public int maxRetries = 2;

    // ===== Runtime UI state =====
    Rect windowRect = new Rect(20, 20, 480, 210);
    bool showPopup = false;
    string uiTitle = "Auto Updater";
    string uiMessage = "";
    string remoteNotes = ""; // (không dùng với cách này, để dành)
    float progress01 = 0f;
    State state = State.Idle;

    enum State { Idle, Checking, UpToDate, Downloading, Applying, ReadyToRestart, Error }

    [Serializable]
    class VersionJson { public string version; } // dùng cho local version.json

    void Awake()
    {
        // Ép TLS 1.2 cho máy Windows cũ
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
    }

    void Start()
    {
        // Nếu Inspector để trống, tự điền default
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

            // 2) Suy ra URL patch theo quy ước patch_<version>.zip
            string packageUrl = CombineUrl(basePatchUrl, $"patch_{remoteVer}.zip");
            string packageUrlFallback = CombineUrl(basePatchUrlFallback, $"patch_{remoteVer}.zip");

            // 3) Tải patch (.zip)
            state = State.Downloading;
            uiTitle = $"Có bản mới {current} → {remoteVer}";
            uiMessage = "Đang tải bản cập nhật…";
            progress01 = 0f;

            // string patchPath = Path.Combine(Application.persistentDataPath, patchFolder, patchFileName);
            string patchPath = Path.Combine(Application.persistentDataPath, patchFolder, $"patch_{remoteVer}.zip");

            Directory.CreateDirectory(Path.GetDirectoryName(patchPath));

            bool ok = await DownloadFileWithRetry(packageUrl, patchPath, maxRetries);
            if (!ok && !string.IsNullOrEmpty(packageUrlFallback))
                ok = await DownloadFileWithRetry(packageUrlFallback, patchPath, maxRetries);

            if (!ok)
            {
                SetError("Tải bản cập nhật thất bại. Hãy kiểm tra internet/URL và thử lại.");
                return;
            }

            // 4) Áp dụng patch (giải nén đè)
            state = State.Applying;
            uiTitle = "Đang áp dụng bản cập nhật…";
            uiMessage = "Vui lòng chờ (không tắt game).";

            string gameDir = Path.GetDirectoryName(Application.dataPath); // thư mục chứa EXE
            try
            {
                using (var zip = ZipFile.OpenRead(patchPath))
                {
                    int i = 0;
                    int total = Mathf.Max(zip.Entries.Count, 1);
                    foreach (var entry in zip.Entries)
                    {
                        if (string.IsNullOrEmpty(entry.Name)) continue; // folder
                        string dest = Path.Combine(gameDir, entry.FullName);
                        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                        entry.ExtractToFile(dest, overwrite: true);
                        i++;
                        progress01 = Mathf.Clamp01(i / (float)total);
                    }
                }
                File.Delete(patchPath);

                // Ghi version local để lần sau khỏi tải lại
                File.WriteAllText(Path.Combine(gameDir, "version.json"),
                                  "{\"version\":\"" + remoteVer + "\"}");

                state = State.ReadyToRestart;
                uiTitle = $"Đã cập nhật lên {remoteVer}";
                uiMessage = "Nhấn Restart để khởi động lại game áp dụng bản mới.";
                progress01 = 1f;
                showPopup = true;
            }
            catch (Exception e)
            {
                state = State.ReadyToRestart;
                uiTitle = "Cần khởi động lại để hoàn tất";
                uiMessage = "Một số file đang được game sử dụng nên không thể ghi đè.\nNhấn Restart để áp dụng cập nhật.\n\nChi tiết: " + e.Message;
                progress01 = 1f;
                showPopup = true;
            }
        }
        catch (Exception ex)
        {
            SetError("Lỗi không xác định: " + ex.Message);
        }
    }

    // ===== UI IMGUI =====
    void OnGUI()
    {
        if (!showPopup) return;
        windowRect = GUI.ModalWindow(1927, windowRect, DrawWindow, "Auto Updater");
    }

    void DrawWindow(int id)
    {
        GUILayout.Label($"Trạng thái: {state}");
        if (!string.IsNullOrEmpty(uiMessage))
            GUILayout.Label(uiMessage, GUILayout.Width(450));

        if (state == State.Downloading || state == State.Applying || state == State.ReadyToRestart)
        {
            var rect = GUILayoutUtility.GetRect(450, 18);
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
                    UnityEngine.Debug.Log($"[Updater] Local version.json: {v.version}");
                    return v.version;
                }
                UnityEngine.Debug.LogWarning($"[Updater] version.json không hợp lệ: {txt}");
            }
            else
            {
                UnityEngine.Debug.LogWarning($"[Updater] Không tìm thấy version.json tại: {jsonPath}");
            }
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning("[Updater] Lỗi đọc version.json: " + e.Message);
        }

        if (!string.IsNullOrEmpty(Application.version))
            return Application.version; // từ PlayerSettings.bundleVersion

        return localVersionFallback;
    }

    async Task<string> GetRemoteVersionFromProjectSettings(string url)
    {
        string text = await DownloadText(url);
        if (string.IsNullOrEmpty(text)) return null;

        // Tìm m_BundleVersion: 1.2.3 hoặc bundleVersion: 1.2.3
        var m = Regex.Match(text, @"m_BundleVersion\s*:\s*([0-9]+(\.[0-9]+){1,3})");
        if (!m.Success)
            m = Regex.Match(text, @"bundleVersion\s*:\s*([0-9]+(\.[0-9]+){1,3})");
        if (m.Success)
        {
            string ver = m.Groups[1].Value;
            UnityEngine.Debug.Log($"[Updater] Remote version (ProjectSettings): {ver}");
            return ver;
        }

        UnityEngine.Debug.LogError("[Updater] Không tìm thấy m_BundleVersion trong ProjectSettings.asset");
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
                UnityEngine.Debug.LogError($"[Updater] GET {url}\nHTTP: {www.responseCode}\nError: {www.error}");
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
                UnityEngine.Debug.LogWarning("[Updater] Download lỗi: " + e.Message);
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
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.SetRequestHeader("User-Agent", "XHeroLMS-Updater/1.0");
            www.timeout = 600; // file lớn
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
        catch (Exception e)
        {
            SetError("Không thể khởi động lại: " + e.Message);
        }
    }
}
