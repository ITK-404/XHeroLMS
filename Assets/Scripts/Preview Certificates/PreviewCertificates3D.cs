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
        previewButton.onClick.AddListener(ShowPreviewUI);
        returnBtnWithFrame.onClick.AddListener(ShowMainPreview);
        returnBtnNoFrame.onClick.AddListener(ShowMainPreview);
    }

    private void OnDestroy()
    {
        previewButton.onClick.RemoveListener(ShowPreviewUI);
        returnBtnWithFrame.onClick.RemoveListener(ShowMainPreview);
        returnBtnNoFrame.onClick.RemoveListener(ShowMainPreview);
    }

    private void Start()
    {
        ShowMainPreview();
    }

    private void ShowMainPreview()
    {
        previewWithFrame.gameObject.SetActive(false);
        previewNoneFrame.gameObject.SetActive(false);
        mainContainer.gameObject.SetActive(true);
        modelContainer.gameObject.SetActive(true);
    }

    private void ShowPreviewUI()
    {
        if (showFrameToggle)
        {
            previewWithFrame.gameObject.SetActive(true);
        }
        else
        {
            previewNoneFrame.gameObject.SetActive(true);
        }
        modelContainer.gameObject.SetActive(false);
    }
    
}