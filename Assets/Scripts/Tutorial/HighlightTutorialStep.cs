using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class HighlightTutorialStep : ListenTutorialStep
{
    [SerializeField] private Image highlightImg;

    private void OnValidate()
    {
        if (highlightImg == null)
        {
            highlightImg = GetComponent<Image>();
        }
    }

    protected override void OnSameStep()
    {
        base.OnSameStep();
        FadeImg(1);
        Debug.Log($"HighlightTutorialStep same step, turn on alpha");
    }

    protected override void OnDifferentStep()
    {
        base.OnDifferentStep();
        FadeImg(0);
        Debug.Log($"HighlightTutorialStep same step, turn off alpha");
    }

    private void FadeImg(float alpha,float duration = 0.2f)
    {
        if (highlightImg == null) return;
        highlightImg.DOFade(alpha, duration);
    }
}