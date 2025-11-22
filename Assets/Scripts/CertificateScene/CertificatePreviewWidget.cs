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

    [Header("Managers")]
    [HideInInspector] public CertificatesController certificatesController;
    private PreviewCertificates3D preview3D;

    private void Awake()
    {
        preview3D = FindAnyObjectByType<PreviewCertificates3D>();
        if (btnCancel != null)
            btnCancel.onClick.AddListener(OnClickCancel);
    }

    public void SetupFromItem(CertificateItemUI source, bool showFrame)
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
}
