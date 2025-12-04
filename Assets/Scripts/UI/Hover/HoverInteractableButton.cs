using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

public class HoverInteractableButton : HoverButtonBase
{
    private Color color = new Color(0.7f, 0.7f, 0.7f, 0.5f);
    private bool interactable = false;

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if (btn != null && btn.interactable)
        {
            base.OnPointerEnter(eventData);
        }
        else
        {
            isPointerOver = true;
        }
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        base.OnPointerExit(eventData);
    }

    private void Update()
    {
        if (btn == null) return;

        if (interactable != btn.interactable)
        {
            interactable = btn.interactable;
            if (!interactable)
            {
                normalImg.DOKill();
                normalImg.DOColor(color, 0.1f);

                TriggerHoverExit();
            }
            else
            {
                normalImg.DOKill();
                normalImg.DOColor(Color.white, 0.1f);

                if (isPointerOver)
                {
                    TriggerHoverEnter();
                }
                else
                {
                    TriggerHoverExit();
                }
            }
        }
    }
}
