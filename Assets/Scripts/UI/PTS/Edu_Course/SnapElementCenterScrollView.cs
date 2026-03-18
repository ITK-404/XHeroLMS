using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class SnapElementCenterScrollView : MonoBehaviour, IEndDragHandler
{
    [SerializeField] private ScrollRect scrollRect;
    private Tween dragTween;
    public Action<int> OnUpdateCenterIndexEvent;
    private void OnValidate()
    {
        if (scrollRect == null)
        {
            scrollRect = GetComponent<ScrollRect>();
        }
    }

    [SerializeField] private int centerIndex;

    [ContextMenu("Align Center Of Element")]
    private void AlignCenterOfElement()
    {
        // lấy center của view port
        Vector3 center = Vector2.zero;
        Vector3[] array = new Vector3[4];
        scrollRect.GetComponent<RectTransform>().GetWorldCorners(array);
        center = (array[0] + array[2]) / 2;

        Debug.Log("Center pos: " + center);
        int shortestIndex = 0;
        float shortDistance = float.MaxValue;
        RectTransform targetItem = null;
        if (scrollRect.content.childCount == 0) return;
        
        foreach (RectTransform child in scrollRect.content.transform)
        {
            var distance = Vector3.Distance(child.position, center);
            if (distance < shortDistance)
            {
                shortestIndex = child.GetSiblingIndex();
                shortDistance = distance;
                targetItem = child;
            }
        }

        scrollRect.velocity = Vector2.zero;
        centerIndex = shortestIndex;

        Vector3 offset = center - targetItem.position;
        var targetPos = scrollRect.content.position + offset;
        var startPos = scrollRect.content.position;
        dragTween = DOVirtual.Vector3(startPos, targetPos, 0.25f, (pos) => { scrollRect.content.position = pos; })
            .SetEase(Ease.OutSine);
        
        OnUpdateCenterIndexEvent?.Invoke(shortestIndex);
    }


    public void OnEndDrag(PointerEventData eventData)
    {
        AlignCenterOfElement();
    }
}