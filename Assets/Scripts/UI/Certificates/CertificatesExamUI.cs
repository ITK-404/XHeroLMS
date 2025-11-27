using UnityEngine;
using UnityEngine.UI;

public class CertificatesExamUI : MonoBehaviour
{
    [SerializeField] private Toggle toggle;
    [SerializeField] private Transform containter;
    [SerializeField] private Transform certificatesFrame;
    [SerializeField] private Button closeButton;

    private Certificate2DPreviewUI _certificate2DPreviewUI;

    private void Awake()
    {
        _certificate2DPreviewUI = GetComponent<Certificate2DPreviewUI>();

        if (toggle != null)
        {
            toggle.onValueChanged.AddListener(OnValueChanged);
        }
        else
        {
            Debug.LogError("[CertificatesExamUI] Toggle chưa được gán trong Inspector.");
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Hide);
        }
        else
        {
            Debug.LogError("[CertificatesExamUI] CloseButton chưa được gán trong Inspector.");
        }

        if (certificatesFrame == null)
        {
            Debug.LogError("[CertificatesExamUI] certificatesFrame chưa được gán trong Inspector!");
        }
    }

    private void OnDestroy()
    {
        if (toggle != null)
            toggle.onValueChanged.RemoveListener(OnValueChanged);

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Hide);
    }

    private void OnValueChanged(bool state)
    {
        if (certificatesFrame == null)
        {
            Debug.LogError("[CertificatesExamUI] OnValueChanged nhưng certificatesFrame = null. Hãy kéo object khung vào field này trong Inspector.");
            return;
        }

        certificatesFrame.gameObject.SetActive(state);
    }

    public void Show()
    {
        // Bật container
        if (containter != null)
            containter.gameObject.SetActive(true);

        // Bật toggle (KHÔNG gọi callback 2 lần)
        if (toggle != null)
        {
            toggle.SetIsOnWithoutNotify(true);
            OnValueChanged(true); // áp dụng trạng thái cho certificatesFrame
        }

        // Gọi preview
        _certificate2DPreviewUI?.OnClickPreviewButton();
    }

    public void Hide()
    {
        Debug.Log("Ẩn UI Chứng nhận");
        if (containter != null)
            containter.gameObject.SetActive(false);
    }
}
