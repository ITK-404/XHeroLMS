using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GraphicsSettingElementUI : MonoBehaviour
{
    [Header("References")] 
    [SerializeField] private Image panelImg;
    [SerializeField] private TextMeshProUGUI descriptionTmp;
    
    [Header("Sprites")]
    [SerializeField] private Sprite activePanelSprite;
    [SerializeField] private Sprite deActivePanelSprite;

    [SerializeField] private Color deActiveColor;
    [SerializeField] private Color activeColor;
    
    private ToggleSwitch toggleSwitch;

    private void Awake()
    {
        toggleSwitch = GetComponent<ToggleSwitch>();
        
        if (toggleSwitch != null)
        {
            toggleSwitch.onToggleOn.AddListener(OnActive);
            toggleSwitch.onToggleOff.AddListener(OnDeActive);
        }
    }

    private void OnDestroy()
    {
        if (toggleSwitch)
        {
            toggleSwitch.onToggleOn.RemoveListener(OnActive);
            toggleSwitch.onToggleOff.RemoveListener(OnDeActive);   
        }
    }

    private void OnActive()
    {
        UpdateDescriptionColor(true);
        UpdatePanelSprite(true);
    }

    private void OnDeActive()
    {
        UpdateDescriptionColor(false);
        UpdatePanelSprite(false);
    }

    private void UpdateDescriptionColor(bool isActive)
    {
        descriptionTmp.color = isActive ? activeColor : deActiveColor;
    }

    private void UpdatePanelSprite(bool isActive)
    {
        panelImg.sprite = isActive ? activePanelSprite : deActivePanelSprite;
    }
    
}