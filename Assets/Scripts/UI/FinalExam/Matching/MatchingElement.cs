using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class MatchingElement : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    public enum ElementSide
    {
        A,
        B
    }

    public ElementSide side;
    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("On Pointer Down");
    }

    public void OnDrag(PointerEventData eventData)
    {
        Debug.Log("On Drag");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("On Pointer Up at " + eventData.position);

        if (EventSystem.current == null)
        {
            Debug.LogWarning("No EventSystem found in scene.");
            return;
        }

        var pointer = new PointerEventData(EventSystem.current) { position = eventData.position };
        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, hits);

        foreach (var hit in hits)
        {
            var target = hit.gameObject.GetComponentInParent<MatchingElement>();
            if (target != null && target != this && target.side != side)
            {
                Debug.Log($"Dropped on MatchingElement: {target.name}");
                MatchingElementHandler.Instance?.OnDroppedOnto(this, target);
                return;
            }
        }

        Debug.Log("No MatchingElement found at release position");
    }
}