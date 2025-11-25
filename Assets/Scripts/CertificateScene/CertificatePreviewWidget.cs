using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CertificatePreviewWidget : MonoBehaviour
{
    [Header("Refs")]
    public GameObject frameObject;
    public RawImage certificateImage;
    public TMP_Text nameText;
    public TMP_Text dateText;
    public Button btnCancel;

    [Header("Chụp hình")]
    [Tooltip("Canvas chứa vùng cần chụp (thường là canvas của preview).")]
    public Canvas captureCanvas;

    [Tooltip("Các object muốn ẩn khi chụp (nút Close, button, toggle, v.v.).")]
    public List<GameObject> hideWhenCapture = new List<GameObject>();

    [Header("Managers")]
    [HideInInspector] public CertificatesController certificatesController;
    private PreviewCertificates3D preview3D;

    private void Awake()
    {
        preview3D = FindAnyObjectByType<PreviewCertificates3D>();
        if (btnCancel != null)
            btnCancel.onClick.AddListener(OnClickCancel);
    }

    /// <summary>
    /// Gọi khi spawn widget: setup nội dung + tự chụp ra file.
    /// </summary>
    public void SetupAndCapture(CertificateItemUI source, Canvas canvas)
    {
        captureCanvas = canvas;
        // Với prefab riêng cho từng kiểu thì không cần showFrame nữa,
        // nếu bạn vẫn dùng 1 prefab chung thì có thể truyền bool.
        SetupFromItem(source);

        // Sau khi layout xong 1 frame thì chụp
        StartCoroutine(CaptureAndSaveToDesktop());
    }

    public void SetupFromItem(CertificateItemUI source, bool showFrame = true)
    {
        if (source == null) return;

        if (frameObject != null)
            frameObject.SetActive(showFrame);

        if (nameText) nameText.text = source.nameText.text;
        if (dateText) dateText.text = source.dateText.text;

        if (certificateImage != null)
        {
            certificateImage.texture = source.loadedTexture;
            certificateImage.color   = Color.white;
        }
    }

    private void OnClickCancel()
    {
        // Hiện lại 3D hiện tại (item đang chọn)
        if (certificatesController != null)
            certificatesController.SetCurrentCertificateVisible(true);

        // Hiện lại Container + 3D UI
        if (preview3D != null)
            preview3D.ShowMainPreview();
        else
            Debug.LogWarning("[CertificatePreviewWidget] preview3D chưa được gán.");

        Destroy(gameObject);
    }

    // ================== CAPTURE ==================

    private IEnumerator CaptureAndSaveToDesktop()
    {
        if (captureCanvas == null)
        {
            captureCanvas = GetComponentInParent<Canvas>();
        }

        if (captureCanvas == null)
        {
            Debug.LogError("[CertificatePreviewWidget] captureCanvas chưa được gán.");
            yield break;
        }

        // 1) Ẩn các object phụ
        List<bool> oldStates = new List<bool>(hideWhenCapture.Count);
        foreach (var go in hideWhenCapture)
        {
            if (go == null)
            {
                oldStates.Add(false);
                continue;
            }

            oldStates.Add(go.activeSelf);
            go.SetActive(false);
        }

        // 2) Chờ frame để UI render xong
        yield return new WaitForEndOfFrame();

        // 3) Lấy RectTransform của canvas (Screen Space Overlay/Camera)
        RectTransform canvasRect = captureCanvas.GetComponent<RectTransform>();
        if (canvasRect == null)
        {
            Debug.LogError("[CertificatePreviewWidget] Canvas không có RectTransform.");
            RestoreHidden(oldStates);
            yield break;
        }

        Vector3[] corners = new Vector3[4];
        canvasRect.GetWorldCorners(corners);

        // bottom-left & top-right in screen space
        Vector3 bl = RectTransformUtility.WorldToScreenPoint(null, corners[0]);
        Vector3 tr = RectTransformUtility.WorldToScreenPoint(null, corners[2]);

        float x = bl.x;
        float y = bl.y;
        float width  = tr.x - bl.x;
        float height = tr.y - bl.y;

        int texWidth  = Mathf.RoundToInt(width);
        int texHeight = Mathf.RoundToInt(height);

        if (texWidth <= 0 || texHeight <= 0)
        {
            Debug.LogWarning("[CertificatePreviewWidget] Kích thước capture không hợp lệ.");
            RestoreHidden(oldStates);
            yield break;
        }

        // 4) Chụp vùng canvas
        Texture2D tex = new Texture2D(texWidth, texHeight, TextureFormat.RGB24, false);
        Rect readRect = new Rect(x, y, width, height);
        tex.ReadPixels(readRect, 0, 0);
        tex.Apply();

        // 5) Hiện lại các object phụ
        RestoreHidden(oldStates);

        byte[] pngBytes = tex.EncodeToPNG();
        Destroy(tex);

        // 6) Tên file: Certificate_yyyyMMdd_HHmmss.png
        string timeStr = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"Certificate_{timeStr}.png";

        // Desktop mặc định
        string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        if (string.IsNullOrEmpty(desktopPath))
        {
            desktopPath = Application.persistentDataPath;
            Debug.LogWarning("[CertificatePreviewWidget] Không lấy được Desktop, dùng persistentDataPath.");
        }

        string fullPath = Path.Combine(desktopPath, fileName);

        try
        {
            File.WriteAllBytes(fullPath, pngBytes);
            Debug.Log("[CertificatePreviewWidget] Đã lưu bằng tại: " + fullPath);
        }
        catch (Exception e)
        {
            Debug.LogError("[CertificatePreviewWidget] Lỗi lưu file: " + e);
        }
    }

    private void RestoreHidden(List<bool> states)
    {
        for (int i = 0; i < hideWhenCapture.Count && i < states.Count; i++)
        {
            var go = hideWhenCapture[i];
            if (go == null) continue;
            go.SetActive(states[i]);
        }
    }
}
