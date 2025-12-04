using UnityEngine;
using UnityEngine.EventSystems;

public class HoverCourseItemButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private TabUI tabUI;
    private void Awake()
    {
        tabUI = GetComponent<TabUI>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!tabUI.buttonState)
        {
            tabUI.SetGradientActive(true);
            tabUI.SetSpriteActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!tabUI.buttonState)
        {
            tabUI.SetGradientActive(false);
            tabUI.SetSpriteActive(false);
        }
    }
}
