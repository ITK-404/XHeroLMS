using UnityEngine;

public class TutorialFocusRaycastFilter : MonoBehaviour, ICanvasRaycastFilter
{
    [SerializeField] private RectTransform targetRect;
    [SerializeField] private Canvas canvas;
    [SerializeField] private Vector2 padding;

    public void SetTarget(RectTransform target)
    {
        targetRect = target;
    }

    public void ClearTarget()
    {
        targetRect = null;
    }
    
    public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
    {
        if (targetRect == null) return false;

        bool isInsideFocus = RectTransformUtility.RectangleContainsScreenPoint(targetRect, sp, eventCamera);

        return !isInsideFocus;
    }
}