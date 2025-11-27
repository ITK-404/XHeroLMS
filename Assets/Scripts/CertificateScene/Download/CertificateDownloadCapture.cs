using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using SFB;

public class CertificateDownloadCapture : MonoBehaviour
{
    [Header("Button tải về")]
    public Button downloadButton;

    [Header("Share Buttons")]
    public Button shareFacebookButton;
    public Button shareZaloButton;

    [Header("Các object cần ẩn khi chụp")]
    public List<GameObject> objectsToHide = new List<GameObject>();

    public string fileNamePrefix = "certificate_";

    [Header("Nội dung share")]
    public string shareMessage = "Chúc mừng đã nhận bằng tốt nghiệp!";

    private void Awake()
    {
        if (downloadButton != null)
            downloadButton.onClick.AddListener(OnClickDownload);

        if (shareFacebookButton != null)
            shareFacebookButton.onClick.AddListener(OnClickShareFacebook);

        if (shareZaloButton != null)
            shareZaloButton.onClick.AddListener(OnClickShareZalo);
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

    /// <summary>Cho script khác gọi chụp mà không cần bấm nút.</summary>
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
            if (go == null)
            {
                states.Add(false);
                continue;
            }

            states.Add(go.activeSelf);
            go.SetActive(false);
        }

        // Chờ render UI xong
        yield return new WaitForEndOfFrame();

        // Chụp full màn hình
        int w = Screen.width;
        int h = Screen.height;

        if (w <= 0 || h <= 0)
        {
            Debug.LogWarning("[CertificateDownloadCapture] Kích thước màn hình không hợp lệ.");
            RestoreObjects(states);
            yield break;
        }

        Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        tex.Apply();

        // Restore UI lại
        RestoreObjects(states);

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

    private void RestoreObjects(List<bool> states)
    {
        for (int i = 0; i < objectsToHide.Count; i++)
        {
            if (i >= states.Count) break;
            if (objectsToHide[i] == null) continue;
            objectsToHide[i].SetActive(states[i]);
        }
    }

    // ================== COMMON: COPY CLIPBOARD ==================
    private void CopyShareMessageToClipboard()
    {
        string msg = string.IsNullOrEmpty(shareMessage)
            ? "Chúc mừng đã nhận bằng tốt nghiệp!"
            : shareMessage;

        GUIUtility.systemCopyBuffer = msg;
        Debug.Log("[CertificateDownloadCapture] Copied share message to clipboard: " + msg);
    }

    // ================== SHARE FACEBOOK: mở trang cá nhân ==================
    private void OnClickShareFacebook()
    {
        CopyShareMessageToClipboard();

        string fbProfileUrl = "https://www.facebook.com/me/";
        Application.OpenURL(fbProfileUrl);

        Debug.Log("[CertificateDownloadCapture] Open Facebook profile: " + fbProfileUrl);
    }

    // ================== SHARE ZALO: ưu tiên app, fallback web ==================
    private void OnClickShareZalo()
    {
        // Copy sẵn nội dung
        CopyShareMessageToClipboard();
        Application.OpenURL("zalo://");

        Debug.Log("[CertificateDownloadCapture] Tried open Zalo app via zalo://");
    }
}
