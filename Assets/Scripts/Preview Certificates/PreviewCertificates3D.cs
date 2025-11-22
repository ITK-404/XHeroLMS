using UnityEngine;
using UnityEngine.UI;

public class PreviewCertificates3D : MonoBehaviour
{
    [Header("Container")]
    public Transform mainContainer;
    public Transform previewWithFrame;
    public Transform previewNoneFrame;
    public Transform modelContainer;

    [Header("Button")]
    public Button previewButton;
    public Button dowloadCertificatesBtn;
    public Toggle showFrameToggle;

    [Header("Return button")]
    public Button returnBtnWithFrame;
    public Button returnBtnNoFrame;

    private void Awake()
    {
        if (previewButton != null)
            previewButton.onClick.AddListener(ShowPreviewUI);

        if (returnBtnWithFrame != null)
            returnBtnWithFrame.onClick.AddListener(ShowMainPreview);

        if (returnBtnNoFrame != null)
            returnBtnNoFrame.onClick.AddListener(ShowMainPreview);
    }

    private void OnDestroy()
    {
        if (previewButton != null)
            previewButton.onClick.RemoveListener(ShowPreviewUI);

        if (returnBtnWithFrame != null)
            returnBtnWithFrame.onClick.RemoveListener(ShowMainPreview);

        if (returnBtnNoFrame != null)
            returnBtnNoFrame.onClick.RemoveListener(ShowMainPreview);
    }

    private void Start()
    {
        ShowMainPreview();
    }

    public void ShowMainPreview()
    {
        if (previewWithFrame != null)
            previewWithFrame.gameObject.SetActive(false);

        if (previewNoneFrame != null)
            previewNoneFrame.gameObject.SetActive(false);

        if (mainContainer != null)
            mainContainer.gameObject.SetActive(true);

        if (modelContainer != null)
            modelContainer.gameObject.SetActive(true);
    }

    private void ShowPreviewUI()
    {
        if (mainContainer != null)
            mainContainer.gameObject.SetActive(false);

        if (modelContainer != null)
            modelContainer.gameObject.SetActive(false);

        bool hasFrame = showFrameToggle != null && showFrameToggle.isOn;

        if (previewWithFrame != null)
            previewWithFrame.gameObject.SetActive(hasFrame);

        if (previewNoneFrame != null)
            previewNoneFrame.gameObject.SetActive(!hasFrame);
    }
}
