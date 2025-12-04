using UnityEngine;
using UnityEngine.UI;

public class HoverNavigationUI : MonoBehaviour
{
    public Image normalImg;
    public Image hoverImg;

    public void SetHoverAndHideNormal(bool isHover)
    {
        normalImg.gameObject.SetActive(!isHover);
        hoverImg.gameObject.SetActive(isHover);
    }

}