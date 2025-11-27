using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using SFB;

public class CertificateDownloadCapture : MonoBehaviour
{
    [Header("Button tải về")]
    public Button downloadButton;

    [Header("Các object cần ẩn khi chụp")]
    public List<GameObject> objectsToHide = new List<GameObject>();

    [Header("Tùy chọn file")]
    public string fileNamePrefix = "certificate_";

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
        // Lưu + ẩn các UI không cần thiết
        var states = new List<bool>();
        foreach (var go in objectsToHide)
        {
            if (go == null) { states.Add(false); continue; }
            states.Add(go.activeSelf);
            go.SetActive(false);
        }

        // Chờ render UI xong
        yield return new WaitForEndOfFrame();

        // Chụp full màn hình
        int w = Screen.width;
        int h = Screen.height;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();

        // Restore UI lại
        for (int i = 0; i < objectsToHide.Count; i++)
        {
            if (objectsToHide[i] != null)
                objectsToHide[i].SetActive(states[i]);
        }

        // Encode PNG
        byte[] pngBytes = tex.EncodeToPNG();
        Destroy(tex);

        // Tạo tên file tự động theo thời gian
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string defaultFileName = $"{fileNamePrefix}{timestamp}.png";

        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

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

        if (string.IsNullOrEmpty(path))
        {
            Debug.Log("[CertificateDownloadCapture] Người dùng huỷ lưu.");
            yield break;
        }

        File.WriteAllBytes(path, pngBytes);
        Debug.Log("[CertificateDownloadCapture] Đã lưu: " + path);
#else
        Debug.LogWarning("[CertificateDownloadCapture] Không hỗ trợ SaveFilePanel trên WebGL.");
#endif
    }
}
