using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class HoverNavigationUI : MonoBehaviour
{
    public Image normalImg;
    public Image hoverImg;

    public void SetHoverAndHideNormal(bool isHover)
    {
        //normalImg.DOKill();
        //hoverImg.DOKill();
        //normalImg.DOFade(isHover ? 0 : 1, 0.1f);
        //hoverImg.DOFade(isHover ? 1 : 0, 0.1f);
        normalImg.gameObject.SetActive(!isHover);
        hoverImg.gameObject.SetActive(isHover);
    }

}