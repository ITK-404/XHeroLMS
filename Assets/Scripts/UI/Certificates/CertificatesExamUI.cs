using UnityEngine;
using UnityEngine.UI;

public class CertificatesExamUI : MonoBehaviour
{
    [SerializeField] private Toggle toggle;
    [SerializeField] private Transform containter;
    [SerializeField] private Transform certificatesFrame;
    [SerializeField] private Button closeButton;
    private void Awake()
    {
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
        containter.gameObject.SetActive(true);
    }

    public void Hide()
    {
        Debug.Log("Ẩn UI Chứng nhận");
        containter.gameObject.SetActive(false);
    }
}