using UnityEngine.EventSystems;

public class LoginTabHover : HoverButtonBase
{
    private LoginTab loginTab;

    private void Awake()
    {
        loginTab = GetComponent<LoginTab>();
    }

    public override void OnPointerEnter(PointerEventData eventData)
    {
        if(!loginTab.isSelect)
            base.OnPointerEnter(eventData);
    }

    public override void OnPointerExit(PointerEventData eventData)
    {
        if (!loginTab.isSelect)
            base.OnPointerExit(eventData);
    }
}
  