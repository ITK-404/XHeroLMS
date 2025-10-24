using UnityEngine;
using UnityEngine.UI;

public class SpriteToggleListenUI : MonoBehaviour
{
    private ToggleBaseUI ToggleBase;
    public Sprite onSprite;
    public Sprite offSprite;
    public Image image;

    private void OnValidate()
    {
        if(image == null)
        {
            image = GetComponent<Image>();
        }
    }

    private void Awake()
    {
        ToggleBase = GetComponent<ToggleBaseUI>();
        ToggleBase.OnToggleOff.AddListener(ChangeStateOn);
        ToggleBase.OnToggleOff.AddListener(ChangeStateOff);
    }
    [ContextMenu("Change State On")]
    private void ChangeStateOn()
    {
        image.sprite = onSprite;
    }

    [ContextMenu("Change State Off")]
    private void ChangeStateOff()
    {
        image.sprite = offSprite;
    }
}