using System;
using DG.Tweening;
using UnityEngine;


public class FrameEduCourseUI : PanelBaseUI
{
    private int currentIndex = -1;
    private Tween moveTween;

    private int ChildCount => (scrollView != null && scrollView.content != null) ? scrollView.content.childCount : 0;
    private bool firstInit = false;

    private void OnEnable()
    {
        PTS_FrameArrowNavigation.AssignCallback(PreviousIndex,NextIndex);
    }

    [ContextMenu("First Index")]
    public void FirstIndex()
    {
        if (ChildCount == 0) return;
        currentIndex = 0;
        CenterOnIndex(currentIndex,false);
    }

    [ContextMenu("Previous Index")]
    public void PreviousIndex()
    {
        if (ChildCount == 0) return;
        if (currentIndex < 0) currentIndex = 0;
        currentIndex = Mathf.Max(0, currentIndex - 1);
        CenterOnIndex(currentIndex);
    }

    [ContextMenu("Next Index")]
    public void NextIndex()
    {
        if (ChildCount == 0) return;
        if (currentIndex < 0) currentIndex = 0;
        currentIndex = Mathf.Min(ChildCount - 1, currentIndex + 1);
        CenterOnIndex(currentIndex);
    }

    public void SetIndex(int index)
    {
        if (ChildCount == 0) return;
        currentIndex = Mathf.Clamp(index, 0, ChildCount - 1);
        CenterOnIndex(currentIndex);
    }

    private void CenterOnIndex(int index, bool anim = true)
    {
        if (scrollView == null || scrollView.content == null) return;
        if (ChildCount == 0) return;
        index = Mathf.Clamp(index, 0, ChildCount - 1);
        var target = scrollView.content.GetChild(index);
        if (target == null) return;
        var rect = target.GetComponent<RectTransform>();
        if (rect == null) return;
        moveTween?.Kill();
        float duration = anim ? 0.5f : 0;
        // Use ForceCenterOnItem so the element is centered even if it requires moving content beyond normal bounds
        moveTween = scrollView.ForceCenterOnItem(rect, duration, false);
    }

    [ContextMenu("Force Center Current Index")]
    public void ForceCenterIndex()
    {
        if (ChildCount == 0) return;
        if (currentIndex < 0) currentIndex = 0;
        ForceCenterAtIndex(currentIndex);
    }

    public void ForceCenterAtIndex(int index, float duration = 1)
    {
        if (scrollView == null || scrollView.content == null) return;
        if (ChildCount == 0) return;
        index = Mathf.Clamp(index, 0, ChildCount - 1);
        var target = scrollView.content.GetChild(index);
        if (target == null) return;
        var rect = target.GetComponent<RectTransform>();
        if (rect == null) return;
        moveTween?.Kill();
        moveTween = scrollView.ForceCenterOnItem(rect, duration, true);
    }
}