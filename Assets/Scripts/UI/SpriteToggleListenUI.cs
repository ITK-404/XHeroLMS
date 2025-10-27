using UnityEngine;
using UnityEngine.UI;

public class SpriteToggleListenUI : MonoBehaviour
{
    private ToggleBaseUI ToggleBase;
    public Sprite onSprite;
    public Sprite offSprite;
    public Image image;
    public bool isActive;
    private void OnValidate()
    {
        if (image == null)
        {
            image = GetComponent<Image>();
        }
    }

    private void Awake()
    {
        ToggleBase = GetComponent<ToggleBaseUI>();
        if (ToggleBase != null)
            ToggleBase.OnValueChange += OnValueChange;
        else
            Debug.LogWarning($"ToggleBaseUI not found on {name}");
    }

    private void OnDestroy()
    {
        if (ToggleBase != null)
            ToggleBase.OnValueChange -= OnValueChange;
    }

    private void OnValueChange(ToggleBaseUI.State obj)
    {
        isActive = (obj == ToggleBaseUI.State.Active);
        if (image != null)
            image.sprite = isActive ? onSprite : offSprite;
    }
}