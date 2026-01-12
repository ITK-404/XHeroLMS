using UnityEngine;
using UnityEngine.EventSystems;

public class CurvedScrollView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private float scrollAngle;
    [SerializeField] private float dragSensitivity;
    [SerializeField] private CircularScrollView scrollView;
    public void OnBeginDrag(PointerEventData eventData)
    {
        
    }

    public void OnDrag(PointerEventData eventData)
    {
        float delta = eventData.delta.y; // hoặc y
        scrollAngle += delta * dragSensitivity;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
    }

    private void Update()
    {
        scrollView.SetAngle(scrollAngle);
    }
}