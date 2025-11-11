using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class CertificatesToggle : MonoBehaviour
{
    [SerializeField] private Toggle toggle;
    [SerializeField] private Color deActiveColor;
    [SerializeField] private Color activeColor;
    [SerializeField] private TextMeshProUGUI textToggle;
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
        textToggle.color = state ? activeColor : deActiveColor;
    }
}