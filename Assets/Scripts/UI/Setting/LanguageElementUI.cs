using System;
using UnityEngine;
using UnityEngine.UI;

public class LanguageElementUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image iconLanguageImg;
    [SerializeField] private Image backgroundImg;
    [SerializeField] private Image toggleImg;
    [SerializeField] private ToggleSwitch toggleSwitch; // cùng GO → GetComponent

    [Header("Sprites")]
    [SerializeField] private Sprite activePanelSprite;
    [SerializeField] private Sprite deActivePanelSprite;
    [SerializeField] private Sprite activeSpriteBtn;
    [SerializeField] private Sprite deActiveSpriteBtn;

    private string languageId;

    private void Awake()
    {
        if (toggleSwitch == null)
            toggleSwitch = GetComponentInChildren<ToggleSwitch>();
    }

    // LanguageGroupManagerUI gọi sau khi spawn
    public void Init(LanguageElementData data)
    {
        languageId = data.languageId;
        iconLanguageImg.sprite = data.icon;
        
        UpdateVisual(false);
    }

    // Gán vào UnityEvent onToggleOn của ToggleSwitch trong Inspector (hoặc qua Init)
    public void OnActivated() => UpdateVisual(true);
    public void OnDeactivated() => UpdateVisual(false);

    private void UpdateVisual(bool isOn)
    {
        backgroundImg.sprite = isOn ? activePanelSprite : deActivePanelSprite;
        toggleImg.sprite = isOn ? activeSpriteBtn : deActiveSpriteBtn;
    }

    public string LanguageId => languageId;
    public ToggleSwitch ToggleSwitch => toggleSwitch;
}


[Serializable]
public class LanguageElementData 
{
    public Sprite icon;
    public string languageId;
    public string description;
}
