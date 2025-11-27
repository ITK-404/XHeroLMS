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

    [Header("Downloader")]
    [Tooltip("Script capture để chụp scene và lưu PNG")]
    public CertificateDownloadCapture downloadCapture;

    private CertificatePreviewWidget _currentPreview;
    private CertificateItemUI        _currentSource;   // nhớ lại certificate hiện tại

    private void Awake()
    {
        if (btnPreview != null)
            btnPreview.onClick.AddListener(OnClickPreview);

        if (toggleWithFrame != null)
            toggleWithFrame.onValueChanged.AddListener(OnToggleWithFrameChanged);

        if (toggleWithoutFrame != null)
            toggleWithoutFrame.onValueChanged.AddListener(OnToggleWithoutFrameChanged);
    }

    private CertificateItemUI GetSourceItem()
    {
        if (certificatesController == null) return null;
        return certificatesController.GetCurrentCertificateUI();
    }

    // CHỈ gọi khi bấm nút "Xem trước"
    private void ShowPreview(bool showFrame)
    {
        var src = GetSourceItem();
        if (src == null)
        {
            Debug.LogWarning("[CertificatePreviewButton] Không có certificate hiện tại.");
            return;
        }

        _currentSource = src;

        // Nếu muốn giữ 3D (đế + khung) để chụp full scene thì KHÔNG ẩn:
        // certificatesController.SetCurrentCertificateVisible(false);

        // Cập nhật đế + khung 3D theo toggle
        _currentSource.SetBaseAndFrameVisible(showFrame);

        Transform parent = previewParent != null ? previewParent : transform.parent;

        if (_currentPreview == null)
        {
            _currentPreview = Instantiate(previewPrefab, parent);
            _currentPreview.certificatesController = certificatesController;
        }

        _currentPreview.gameObject.SetActive(true);
        _currentPreview.SetupFromItem(_currentSource, showFrame);

        if (downloadCapture != null && _currentPreview.nameText != null)
        {
            downloadCapture.nameText = _currentPreview.nameText;
        }
    }

    // CHỈ đổi khung trên preview đã mở + 3D base/frame, KHÔNG tạo mới preview
    private void ApplyFrameState(bool showFrame)
    {
        // cập nhật 3D (đế + khung)
        if (_currentSource == null)
            _currentSource = GetSourceItem();

        if (_currentSource != null)
            _currentSource.SetBaseAndFrameVisible(showFrame);

        // cập nhật 2D preview nếu đang mở
        if (_currentPreview != null && _currentPreview.gameObject.activeSelf)
        {
            _currentPreview.SetupFromItem(_currentSource, showFrame);
        }
    }

    private void OnClickPreview()
    {
        bool showFrame = toggleWithFrame != null && toggleWithFrame.isOn;
        ShowPreview(showFrame);
    }

    private void OnToggleWithFrameChanged(bool isOn)
    {
        if (!isOn) return;
        ApplyFrameState(true);   // có khung
    }

    private void OnToggleWithoutFrameChanged(bool isOn)
    {
        if (!isOn) return;
        ApplyFrameState(false);  // không khung
    }
}
