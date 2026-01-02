using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoverButtonBase : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] protected Image normalImg;
    [SerializeField] protected Image hoverImg;
    [SerializeField] protected float fadeTime = 0.1f;
    protected Button btn;

    protected bool isPointerOver;

    private void Awake()
    {
        if (btn == null)
        {
            btn = GetComponent<Button>();
        }
        hoverImg.DOFade(0, 0);
    }

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        isPointerOver = true;
        TriggerHoverEnter();
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        isPointerOver = false;
        TriggerHoverExit();
    }

    protected void TriggerHoverEnter()
    {
        if (hoverImg == null) return;
        hoverImg.DOKill();
        hoverImg.DOFade(1, fadeTime);
    }

    protected void TriggerHoverExit()
    {
        if (hoverImg == null) return;
        hoverImg.DOKill();
        hoverImg.DOFade(0, fadeTime);
    }

    [ContextMenu("Finding")]
    private void Finding()
    {
        normalImg = transform.Find("Normal").GetComponent<Image>();
        hoverImg = transform.Find("Hover").GetComponent<Image>();
    }

    public virtual void OnPointerClick(PointerEventData eventData)
    {
        ResetHoverState();
    }
    
    protected void ResetHoverState()
    {
        isPointerOver = false;
        if (hoverImg != null)
        {
            hoverImg.DOKill();
            hoverImg.DOFade(0, fadeTime);
        }
    }
}
