using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class JoystickUI : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public Image background;
    public Image handle;

    private bool isStickActive = false;

    private void Update()
    {
        float targetAlpha = isStickActive ? .8f : 0.3f;
        Color bgColor = background.color;
        Color handleColor = handle.color;
        bgColor.a = Mathf.Lerp(bgColor.a, targetAlpha, Time.deltaTime * 10f);
        handleColor.a = Mathf.Lerp(bgColor.a, targetAlpha, Time.deltaTime * 10f);
        
        background.color = bgColor;
        handle.color = handleColor;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        isStickActive = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isStickActive = false;
    }
}
