using System;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class KyMon_ExpandablePanelElement : ExpandablePanel
{
    [SerializeField] private RectTransform container;
    [SerializeField] private RectTransform contentRect;
    [SerializeField] private Vector3 closePosition;
    [SerializeField] private Vector3 showPosition;
    [SerializeField] private float moveDuration = 0.2f;

    private const int ExpandLayoutHeight = 150;
    
    public override void Show()
    {
        base.Show();
        // container.gameObject.SetActive(true);
        // container.DOHeight(150, moveDuration);
        container.GetComponent<LayoutElement>().DOPreferredHeight(ExpandLayoutHeight, moveDuration);
        MoveContentUI(showPosition, moveDuration);
    }
    
    public override void Hide()
    {
        base.Hide();
        // container.DOHeight(0, moveDuration);
        container.GetComponent<LayoutElement>().DOPreferredHeight(0, moveDuration);
        MoveContentUI(closePosition, moveDuration, () =>
        {
            // container.gameObject.SetActive(false);
        });
    }

    private void MoveContentUI(Vector3 anchorPosition, float duration, TweenCallback onComplete = null)
    {
        contentRect.DOKill();
        contentRect.DOAnchorPos(anchorPosition, duration).OnComplete(onComplete).SetUpdate(UpdateType.Late);
    }
}

public static class LayoutElementExtensions
{
    public static Tween DOHeight(this RectTransform rectTransform, float targetHeight, float duration)
    {
        return DOTween.To(
            () => rectTransform.sizeDelta.y,
            value =>
            {
                var size = rectTransform.sizeDelta;
                size.y = value;
                rectTransform.sizeDelta = size;
            },
            targetHeight,
            duration).SetUpdate(UpdateType.Late);;
    }
    
    public static Tween DOPreferredHeight(
        this LayoutElement layoutElement,
        float endValue,
        float duration)
    {
        return DOTween.To(
            () => layoutElement.preferredHeight,
            value => layoutElement.preferredHeight = value,
            endValue,
            duration);
    }
}