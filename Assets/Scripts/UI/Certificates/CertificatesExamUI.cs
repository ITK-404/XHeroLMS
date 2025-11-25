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
        _certificate2DPreviewUI= GetComponent<Certificate2DPreviewUI>();
        toggle.onValueChanged.AddListener(OnValueChanged);
        closeButton.onClick.AddListener(Hide);
    }

    private void OnDestroy()
    {
        toggle.onValueChanged.RemoveListener(OnValueChanged);
        closeButton.onClick.RemoveListener(Hide);
    }

    private void OnValueChanged(bool state)
    {
        certificatesFrame.gameObject.SetActive(state);
    }

    public void Show()
    {
        // Bật container + bật luôn toggle để khung hiện ra
        containter.gameObject.SetActive(true);
        if (toggle != null)
            toggle.isOn = true;
        
        _certificate2DPreviewUI?.OnClickPreviewButton();
    }

    public void Hide()
    {
        Debug.Log("Ẩn UI Chứng nhận");
        containter.gameObject.SetActive(false);
    }
}
