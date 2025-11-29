using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HoverButtonBase : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image normalImg;
    [SerializeField] private Image hoverImg;
    [SerializeField] private float fadeTime = 0.1f;
    private Button btn;
    private void Awake()
    {
        btn = GetComponent<Button>();
        //normalImg.DOFade(1, 0);
        hoverImg.DOFade(0, 0);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hoverImg.DOFade(1, fadeTime);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hoverImg.DOFade(0, fadeTime);
    }
}