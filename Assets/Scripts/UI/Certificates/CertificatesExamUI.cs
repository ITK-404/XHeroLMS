using UnityEngine;
using UnityEngine.UI;

public class CertificatesExamUI : MonoBehaviour
{
    [SerializeField] private Toggle toggle;
    [SerializeField] private Transform containter;
    [SerializeField] private Transform certificatesFrame;

    private void Awake()
    {
        toggle.onValueChanged.AddListener(OnValueChanged);
    }

    private void OnDestroy()
    {
        toggle.onValueChanged.RemoveListener(OnValueChanged);
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
        containter.gameObject.SetActive(false);
    }
}