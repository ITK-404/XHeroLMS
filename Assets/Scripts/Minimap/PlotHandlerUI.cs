using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlotHandlerUI : MonoBehaviour
{
    [SerializeField] private GameObject container;
    [SerializeField] private TextMeshProUGUI bigAreaNameTmp;
    [SerializeField] private TextMeshProUGUI percentNameTmp;
    [Header("Function Btn")]
    [SerializeField] private Button showScrollViewBtn;
    [SerializeField] private Button findCourseBtn;
    [SerializeField] private Button showReviewBtn;

    public Action OnShowFindCourseAction;
    public Action OnClickShowScrollViewAction;
    public Action OnClickShowReviewBigArea;

    private void Awake()
    {
        findCourseBtn.onClick.AddListener(ClickShowFindCourse);
        showScrollViewBtn.onClick.AddListener(ClickShowScrollView);
        showReviewBtn.onClick.AddListener(ClickBigAreaInformation);
    }

    private void OnDestroy()
    {
        findCourseBtn.onClick.RemoveListener(ClickShowFindCourse);
        showScrollViewBtn.onClick.RemoveListener(ClickShowScrollView);
        showReviewBtn.onClick.RemoveListener(ClickBigAreaInformation);
    }

    private void ClickShowFindCourse() => OnShowFindCourseAction?.Invoke();
    private void ClickShowScrollView() => OnClickShowScrollViewAction?.Invoke();
    private void ClickBigAreaInformation() => OnClickShowReviewBigArea?.Invoke();
    public void Show()
    {
        container.gameObject.SetActive(true);
    }

    public void Hide()
    {
        container.gameObject.SetActive(false);
        
        bigAreaNameTmp.text = "";
    }

    public void ShowArea(BigArea bigArea)
    {
        bigAreaNameTmp.DOKill();
        bigAreaNameTmp.DOFade(0, 1).OnComplete(() =>
        {
            bigAreaNameTmp.text = bigArea.Data.displayName;
            bigAreaNameTmp.DOFade(1, 1);
        });
        // UpdatePercentComplete(2, 10);
    }

    public void UpdatePercentComplete(int current, int max)
    {
        percentNameTmp.text = $"Đã mở khóa <color=#F9DF99>{current}/{max} ({current/max * 100}%)</color>";
    }
}