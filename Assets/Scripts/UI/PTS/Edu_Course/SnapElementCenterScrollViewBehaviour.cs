using UnityEngine;
using UnityEngine.EventSystems;

public class SnapElementCenterScrollViewBehaviour : MonoBehaviour, IEndDragHandler, IBeginDragHandler
{
    [SerializeField] private SnapElementCenterScrollView customScrollView;
    [SerializeField] private float stopVelocity = 300f;
    
    private bool dragDone = false;
    private bool isAlign = false;
    
    private void OnValidate()
    {
        if (customScrollView == null)
        {
            customScrollView = GetComponent<SnapElementCenterScrollView>();
        }
    }
   
    private void Update()
    {
        if (customScrollView == null) return;
        if (dragDone == false)
        {
            return;
        }

        bool canStop = customScrollView.GetMagnitude() < stopVelocity;
        Debug.Log($"Horizontal normalize position: ");
        
        if (canStop && isAlign == false)
        {
            customScrollView.AlignCenterOfElement();
            
            isAlign = true;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        dragDone = true;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        dragDone = false;
        isAlign = false;
    }
}