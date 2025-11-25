using UnityEngine;
using UnityEngine.UI;

public class CertificatePreviewButton : MonoBehaviour
{
    [Header("Source")]
    public CertificatesController certificatesController;

    [Header("Toggle chọn khung")]
    public Toggle toggleWithFrame;
    public Toggle toggleWithoutFrame;

    [Header("Prefab preview 2D")]
    public CertificatePreviewWidget prefabWithFrame;
    public CertificatePreviewWidget prefabWithoutFrame;
    public Transform previewParent;

    [Header("Canvas cần chụp")]
    public Canvas captureCanvas;

    [Header("Button xem trước + chụp")]
    public Button btnPreview;

    private CertificatePreviewWidget _currentPreview;

    private void Awake()
    {
        if (btnPreview != null)
            btnPreview.onClick.AddListener(OnClickPreview);
    }

    private CertificateItemUI GetSourceItem()
    {
        if (certificatesController == null) return null;
        return certificatesController.GetCurrentCertificateUI();
    }

    private void OnClickPreview()
    {
        var src = GetSourceItem();
        if (src == null)
        {
            Debug.LogWarning("[CertificatePreviewButton] Không có certificate hiện tại.");
            return;
        }

        if (captureCanvas == null)
        {
            Debug.LogWarning("[CertificatePreviewButton] Chưa gán captureCanvas.");
            return;
        }

        // Ẩn 3D (KHÔNG destroy)
        certificatesController.SetCurrentCertificateVisible(false);

        Transform parent = previewParent != null ? previewParent : transform.parent;

        // Chọn prefab theo toggle
        CertificatePreviewWidget prefab = null;

        bool withFrameOn  = (toggleWithFrame != null && toggleWithFrame.isOn);
        bool noFrameOn    = (toggleWithoutFrame != null && toggleWithoutFrame.isOn);

        if (withFrameOn && prefabWithFrame != null)
            prefab = prefabWithFrame;
        else if (noFrameOn && prefabWithoutFrame != null)
            prefab = prefabWithoutFrame;
        else if (prefabWithFrame != null)
            prefab = prefabWithFrame; // fallback

        if (prefab == null)
        {
            Debug.LogError("[CertificatePreviewButton] Chưa gán prefab preview (with/without frame).");
            return;
        }

        // Nếu đã có instance → xoá, mình luôn tạo mới để layout chuẩn trước khi chụp
        if (_currentPreview != null)
        {
            Destroy(_currentPreview.gameObject);
            _currentPreview = null;
        }

        _currentPreview = Instantiate(prefab, parent);
        _currentPreview.certificatesController = certificatesController;

        // Setup + tự chụp theo canvas
        _currentPreview.SetupAndCapture(src, captureCanvas);
    }
}
