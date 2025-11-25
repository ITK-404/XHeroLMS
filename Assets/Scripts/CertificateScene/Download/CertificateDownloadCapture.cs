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

    [Header("Vùng cần chụp (UI)")]
    [Tooltip("RawImage hoặc panel gốc chứa cả phôi + text tên, ngày, v.v.")]
    public RectTransform captureRect;      // nên là RectTransform root của preview widget
    public Canvas captureCanvas;           // Canvas chứa preview (để lấy scaleFactor)

    [Header("Thông tin đặt tên file")]
    public TMP_Text nameText;              // để dùng làm tên file
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
        if (captureRect == null)
        {
            Debug.LogWarning("[CertificateDownloadCapture] Chưa gán captureRect.");
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

        // 3) Lấy scaleFactor của canvas
        float scaleFactor = 1f;
        if (captureCanvas == null)
            captureCanvas = captureRect.GetComponentInParent<Canvas>();

        if (captureCanvas != null)
            scaleFactor = captureCanvas.scaleFactor;

        // 4) Lấy toạ độ màn hình của vùng cần chụp
        Vector3[] corners = new Vector3[4];
        captureRect.GetWorldCorners(corners);

        float x = corners[0].x;
        float y = corners[0].y;
        float width = corners[2].x - corners[0].x;
        float height = corners[2].y - corners[0].y;

        int texWidth  = Mathf.RoundToInt(width  * scaleFactor);
        int texHeight = Mathf.RoundToInt(height * scaleFactor);

        if (texWidth <= 0 || texHeight <= 0)
        {
            Debug.LogWarning("[CertificateDownloadCapture] Kích thước capture không hợp lệ.");
            // Restore lại object rồi thoát
            RestoreObjects(states);
            yield break;
        }

        // 5) Chụp màn hình vùng đó
        Texture2D tex = new Texture2D(texWidth, texHeight, TextureFormat.RGB24, false);
        Rect readRect = new Rect(x, y, width, height); // gốc (0,0) là bottom-left màn hình
        tex.ReadPixels(readRect, 0, 0);
        tex.Apply();

        // 6) Restore lại các object đã ẩn
        RestoreObjects(states);

        // 7) Encode PNG
        byte[] pngBytes = tex.EncodeToPNG();
        Destroy(tex);

        // 8) Đặt tên file mặc định: certificate_TenHocVien.png
        string baseName = nameText != null ? nameText.text : "certificate";
        string safeName = MakeSafeFileName(baseName);
        string defaultFileName = $"{fileNamePrefix}{safeName}.png";

        // 9) Lấy Desktop làm folder mặc định
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

        // 10) Hiện cửa sổ Save As cho user chọn nơi lưu
        var extensions = new[]
        {
            new ExtensionFilter("PNG Image", "png")
        };

        // Chỉ hoạt động trên Standalone (Windows/Mac/Linux)
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
