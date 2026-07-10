using UnityEngine;
using UnityEngine.UI;

public class KyMon_ToggleSwapImage : MonoBehaviour
{
    [SerializeField] private Sprite unToggleSprite;
    [SerializeField] private Sprite toggleSprite;
    [SerializeField] private Image targetImage;
    [SerializeField] private Toggle toggle;

    private void Awake()
    {
        toggle.onValueChanged.AddListener(ToggleValueChanged);
    }

    private void OnDestroy()
    {
        toggle.onValueChanged.RemoveListener(ToggleValueChanged);
    }

    private void Start()
    {
        ToggleValueChanged(toggle.isOn);
    }

    private void OnValidate()
    {
        if (toggle == null)
        {
            toggle = GetComponent<Toggle>();
        }
    }

    private void ToggleValueChanged(bool isOn)
    {
        if (toggleSprite == null || unToggleSprite == null)
        {
            return;
        }
        
        targetImage.sprite = isOn ? toggleSprite : unToggleSprite;
    }
}