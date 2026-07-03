using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;


public class FrameEduCourseUI : PanelBaseUI
{
    private SnapElementCenterScrollView snapElementCenterScrollView;
    
    private int currentIndex = -1;
    private Tween moveTween;
    private Coroutine restoreRoutine;

    private int ChildCount => (scrollView != null && scrollView.content != null) ? scrollView.content.childCount : 0;
    private bool firstInit = false;
    private bool hasSavedPosition;
    private int savedIndex = -1;
    private float savedHorizontalPosition = 1f;
    private float savedVerticalPosition = 1f;

    private void Awake()
    {
        snapElementCenterScrollView = GetComponent<SnapElementCenterScrollView>();
    }
    
    private void OnUpdateCenterIndexEvent(int newIndex)
    {
        currentIndex = newIndex;
    }

    private void OnEnable()
    {
        PTS_FrameArrowNavigation.AssignCallback(PreviousIndex,NextIndex);
        EduCourseElement.CourseOpenRequested += HandleCourseOpenRequested;
        
        if (snapElementCenterScrollView)
        {
            snapElementCenterScrollView.OnUpdateCenterIndexEvent += OnUpdateCenterIndexEvent;
        }

        FirstIndex();
    }

    private void OnDisable()
    {
        EduCourseElement.CourseOpenRequested -= HandleCourseOpenRequested;

        if (snapElementCenterScrollView)
        {
            snapElementCenterScrollView.OnUpdateCenterIndexEvent -= OnUpdateCenterIndexEvent;
        }

        if (restoreRoutine != null)
        {
            StopCoroutine(restoreRoutine);
            restoreRoutine = null;
        }
        
    }

    public override void Show()
    {
        base.Show();

        if (hasSavedPosition)
            RestoreSavedPosition();
    }

    [ContextMenu("First Index")]
    public void FirstIndex()
    {
        if (ChildCount == 0) return;

        if (hasSavedPosition)
        {
            RestoreSavedPosition();
            return;
        }

        currentIndex = 1;
        CenterOnIndex(currentIndex,false);
    }

    [ContextMenu("Previous Index")]
    public void PreviousIndex()
    {
        if (ChildCount == 0) return;
        hasSavedPosition = false;
        if (currentIndex < 0) currentIndex = 0;
        currentIndex = Mathf.Max(0, currentIndex - 1);
        CenterOnIndex(currentIndex);
    }

    [ContextMenu("Next Index")]
    public void NextIndex()
    {
        if (ChildCount == 0) return;
        hasSavedPosition = false;
        if (currentIndex < 0) currentIndex = 0;
        currentIndex = Mathf.Min(ChildCount - 1, currentIndex + 1);
        Debug.Log($"FrameEduCourseUI next {currentIndex}");
        CenterOnIndex(currentIndex);
    }

    public void SetIndex(int index)
    {
        if (ChildCount == 0) return;
        hasSavedPosition = false;
        currentIndex = Mathf.Clamp(index, 0, ChildCount - 1);
        // Debug.Log($"FrameEduCourseUI previous {currentIndex}");
        CenterOnIndex(currentIndex);
    }

    private void CenterOnIndex(int index, bool anim = true)
    {
        if (scrollView == null || scrollView.content == null) return;
        if (ChildCount == 0) return;
        index = Mathf.Clamp(index, 0, ChildCount - 1);
        
        // Debug.Log($"FrameEduCourseUI vị trí {index}");
        
        var target = scrollView.content.GetChild(index);
        if (target == null) return;
        var rect = target.GetComponent<RectTransform>();
        if (rect == null) return;

        StartCoroutine(DelayForUI(isAnim: anim, rect));
    }

    private IEnumerator DelayForUI(bool isAnim, RectTransform rect)
    {
        // delay one frame ?
        yield return new WaitForSeconds(0.1f);
        moveTween?.Kill();
        float duration = isAnim ? 0.5f : 0;
        // Use ForceCenterOnItem so the element is centered even if it requires moving content beyond normal bounds
        // Debug.Log($"FrameEduCourseUI di chuyen la",rect.gameObject);
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
        hasSavedPosition = false;
        index = Mathf.Clamp(index, 0, ChildCount - 1);
        var target = scrollView.content.GetChild(index);
        if (target == null) return;
        var rect = target.GetComponent<RectTransform>();
        if (rect == null) return;
        moveTween?.Kill();
        moveTween = scrollView.ForceCenterOnItem(rect, duration, true);
    }

    private void HandleCourseOpenRequested(string courseId)
    {
        SaveCurrentPosition();
    }

    private void SaveCurrentPosition()
    {
        if (scrollView == null)
            return;

        savedHorizontalPosition = scrollView.horizontalNormalizedPosition;
        savedVerticalPosition = scrollView.verticalNormalizedPosition;
        savedIndex = currentIndex >= 0 ? currentIndex : FindClosestIndexToCenter();
        hasSavedPosition = true;
    }

    private void RestoreSavedPosition()
    {
        if (scrollView == null)
            return;

        if (restoreRoutine != null)
            StopCoroutine(restoreRoutine);

        restoreRoutine = StartCoroutine(RestoreSavedPositionRoutine());
    }

    private IEnumerator RestoreSavedPositionRoutine()
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        if (scrollView == null)
        {
            restoreRoutine = null;
            yield break;
        }

        Canvas.ForceUpdateCanvases();
        moveTween?.Kill();
        scrollView.StopMovement();

        scrollView.horizontalNormalizedPosition = savedHorizontalPosition;
        scrollView.verticalNormalizedPosition = savedVerticalPosition;

        if (savedIndex >= 0)
            currentIndex = Mathf.Clamp(savedIndex, 0, Mathf.Max(0, ChildCount - 1));

        hasSavedPosition = false;
        restoreRoutine = null;
    }

    private int FindClosestIndexToCenter()
    {
        if (scrollView == null || scrollView.content == null || ChildCount == 0)
            return -1;

        var scrollRect = scrollView.GetComponent<RectTransform>();
        if (scrollRect == null)
            return currentIndex;

        Vector3[] corners = new Vector3[4];
        scrollRect.GetWorldCorners(corners);
        Vector3 center = (corners[0] + corners[2]) * 0.5f;

        int closestIndex = 0;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < scrollView.content.childCount; i++)
        {
            var child = scrollView.content.GetChild(i) as RectTransform;
            if (child == null)
                continue;

            float distance = Vector3.Distance(child.position, center);
            if (distance >= closestDistance)
                continue;

            closestDistance = distance;
            closestIndex = i;
        }

        return closestIndex;
    }
}
