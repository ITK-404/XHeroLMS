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
    public CertificatePreviewWidget previewPrefab;
    public Transform previewParent;

    [Header("Button xem trước")]
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
        if (src == null) return;

        // Ẩn 3D (KHÔNG destroy)
        certificatesController.SetCurrentCertificateVisible(false);

        Transform parent = previewParent != null ? previewParent : transform.parent;

        // Nếu đã có instance → reuse
        if (_currentPreview == null)
        {
            _currentPreview = Instantiate(previewPrefab, parent);
            _currentPreview.certificatesController = certificatesController;
        }

        _currentPreview.gameObject.SetActive(true);

        bool showFrame = toggleWithFrame != null && toggleWithFrame.isOn;

        _currentPreview.SetupFromItem(src, showFrame);
    }
}
