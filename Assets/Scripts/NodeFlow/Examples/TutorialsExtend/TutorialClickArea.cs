using UnityEngine;

using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class TutorialClickArea : MonoBehaviour, IPointerClickHandler
{
    public event Action Clicked;
    [SerializeField] private Image blockImg;
    private void Awake()
    {
        // Image vẫn nhận raycast dù hoàn toàn trong suốt.
        if(!blockImg)
            blockImg = GetComponent<Image>();
        blockImg.raycastTarget = true;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Clicked?.Invoke();
        Debug.Log("Player đã nhấn đúng vùng tutorial");
    }

    public void Active()
    {
        blockImg.raycastTarget = true;
    }

    public void DeActive()
    {
        blockImg.raycastTarget = false;
    }
}