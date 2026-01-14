using UnityEngine;
using UnityEngine.EventSystems;

public class CurvedScrollView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private float scrollAngle;
    [SerializeField] private float dragSensitivity;
    [SerializeField] private CircularScrollView scrollView;
    private bool isDrag = false;
    public void OnBeginDrag(PointerEventData eventData)
    {
        isDrag = true;
    }

    public void OnDrag(PointerEventData eventData)
    {
        float delta = eventData.delta.y; // hoặc y
        scrollAngle += delta * dragSensitivity;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDrag = false;
    }

    private void Update()
    {
        if(isDrag)
            scrollView.SetAngle(scrollAngle);
    }
}