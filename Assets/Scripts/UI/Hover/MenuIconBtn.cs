using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MenuIconBtn : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image glowingImg;
    [SerializeField] private Color deActiveColorTxt;
    [SerializeField] private TextMeshProUGUI textColor;

    [Header("Timing & Ease")]
    [SerializeField] private float hoverDuration = 0.1f;
    [SerializeField] private float exitDuration = 0.1f;
    [SerializeField] private Ease hoverEase = Ease.OutSine;
    [SerializeField] private Ease exitEase = Ease.InSine;
    [SerializeField] private Transform container;
    public void OnPointerEnter(PointerEventData eventData)
    {
        Debug.Log("Hover Enter");
        textColor.DOKill();
        textColor.enableVertexGradient = true;

        container.DOKill();
        container.DOScale(Vector3.one * 1.1f, hoverDuration).SetEase(hoverEase);

        glowingImg.DOKill();
        glowingImg.DOFade(1, hoverDuration).SetEase(hoverEase);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        Debug.Log("Hover Exit");
        textColor.DOKill();
        textColor.enableVertexGradient = false;

        container.DOKill();
        container.DOScale(Vector3.one, exitDuration).SetEase(exitEase);

        glowingImg.DOKill();
        glowingImg.DOFade(0, exitDuration).SetEase(exitEase);
    }

    private void OnDestroy()
    {
        textColor.DOKill();
        container.DOKill();
        glowingImg.DOKill();
    }
}