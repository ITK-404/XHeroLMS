using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

using SFB;

public class CertificateDownloadCapture : MonoBehaviour
{
    [Header("Button tải về")]
    public Button downloadButton;

    [Header("Thông tin đặt tên file")]
    public TMP_Text nameText;              // để dùng làm tên file (vd: tên học viên)
    public string fileNamePrefix = "certificate_";

    [Header("Các object cần ẩn khi chụp")]
    public List<GameObject> objectsToHide = new List<GameObject>();

    private void Awake()
    {
        if (downloadButton != null)
            downloadButton.onClick.AddListener(OnClickDownload);
    }

    private void OnClickDownload()
    {
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("[CertificateDownloadCapture] GameObject chưa active, không thể chụp.");
            return;
        }

        StartCoroutine(CaptureAndSaveCoroutine());
    }

    /// <summary>
    /// Cho phép script khác (vd: CertificatePreviewButton) gọi capture trực tiếp,
    /// không cần bấm nút download.
    /// </summary>
    public void CaptureNow()
    {
        if (!gameObject.activeInHierarchy)
        {
            Debug.LogWarning("[CertificateDownloadCapture] GameObject chưa active, không thể chụp.");
            return;
        }

        StartCoroutine(CaptureAndSaveCoroutine());
    }

    private IEnumerator CaptureAndSaveCoroutine()
    {
        // 1) Lưu trạng thái active của các object, rồi ẩn đi
        var states = new List<bool>(objectsToHide.Count);
        for (int i = 0; i < objectsToHide.Count; i++)
        {
            GameObject go = objectsToHide[i];
            if (go == null)
            {
                states.Add(false);
                continue;
            }

            states.Add(go.activeSelf);
            go.SetActive(false);
        }

        // 2) Chờ đến cuối frame để UI render xong
        yield return new WaitForEndOfFrame();

        // 3) Kích thước màn hình hiện tại (Game View / Build)
        int texWidth  = Screen.width;
        int texHeight = Screen.height;

        if (texWidth <= 0 || texHeight <= 0)
        {
            Debug.LogWarning("[CertificateDownloadCapture] Screen size không hợp lệ.");
            RestoreObjects(states);
            yield break;
        }

        // 4) Chụp toàn bộ màn hình
        Texture2D tex = new Texture2D(texWidth, texHeight, TextureFormat.RGB24, false);
        Rect readRect = new Rect(0, 0, texWidth, texHeight);
        tex.ReadPixels(readRect, 0, 0);
        tex.Apply();

        // 5) Restore lại các object đã ẩn
        RestoreObjects(states);

        // 6) Encode PNG
        byte[] pngBytes = tex.EncodeToPNG();
        UnityEngine.Object.Destroy(tex);

        // 7) Đặt tên file mặc định: certificate_TenHocVien.png
        string baseName = nameText != null ? nameText.text : "certificate";
        string safeName = MakeSafeFileName(baseName);
        string defaultFileName = $"{fileNamePrefix}{safeName}.png";

        // 8) Lấy Desktop làm folder mặc định
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        // 9) Hiện cửa sổ Save As cho user chọn nơi lưu
        var extensions = new[]
        {
            new ExtensionFilter("PNG Image", "png")
        };

#if !UNITY_WEBGL
        string path = StandaloneFileBrowser.SaveFilePanel(
            "Lưu chứng chỉ",
            desktopPath,
            defaultFileName,
            extensions
        );

        // User bấm Cancel
        if (string.IsNullOrEmpty(path))
        {
            Debug.Log("[CertificateDownloadCapture] Người dùng huỷ lưu file.");
            yield break;
        }

        try
        {
            File.WriteAllBytes(path, pngBytes);
            Debug.Log("[CertificateDownloadCapture] Đã lưu bằng tại: " + path);
        }
        catch (Exception e)
        {
            Debug.LogError("[CertificateDownloadCapture] Lỗi khi ghi file: " + e);
        }
#else
        Debug.LogWarning("[CertificateDownloadCapture] SaveFilePanel không hỗ trợ trên WebGL/Platform này.");
#endif
    }

    private void RestoreObjects(List<bool> states)
    {
        for (int i = 0; i < objectsToHide.Count; i++)
        {
            if (i >= states.Count) break;
            GameObject go = objectsToHide[i];
            if (go == null) continue;
            go.SetActive(states[i]);
        }
    }

    private string MakeSafeFileName(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return "certificate";

        foreach (char c in Path.GetInvalidFileNameChars())
        {
            raw = raw.Replace(c.ToString(), "_");
        }

        raw = raw.Trim();
        if (raw.Length > 40)
            raw = raw.Substring(0, 40);

        return raw;
    }
}
