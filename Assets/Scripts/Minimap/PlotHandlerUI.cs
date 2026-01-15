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

    [Header("Data Sources")]
    private AreaDisplayManager areaDisplayManager;     
    private CourseMapBrowserUI courseMapBrowserUI;     

    public Action OnShowFindCourseAction;
    public Action OnClickShowScrollViewAction;
    public Action OnClickShowReviewBigArea;

    private void Awake()
    {
        findCourseBtn.onClick.AddListener(ClickShowFindCourse);
        showScrollViewBtn.onClick.AddListener(ClickShowScrollView);
        showReviewBtn.onClick.AddListener(ClickBigAreaInformation);

        if (areaDisplayManager == null) areaDisplayManager = FindAnyObjectByType<AreaDisplayManager>();
        if (courseMapBrowserUI == null) courseMapBrowserUI = FindAnyObjectByType<CourseMapBrowserUI>();
    }

    private void OnEnable()
    {
        if (courseMapBrowserUI != null)
            courseMapBrowserUI.OnCoursesChanged += RefreshPercentFromData;
    }

    private void OnDisable()
    {
        if (courseMapBrowserUI != null)
            courseMapBrowserUI.OnCoursesChanged -= RefreshPercentFromData;
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
        RefreshPercentFromData(); // bật UI là update luôn
    }

    public void Hide()
    {
        container.gameObject.SetActive(false);
        bigAreaNameTmp.text = "";
    }

    public void ShowArea(BigArea bigArea)
    {
        bigAreaNameTmp.DOKill();
        bigAreaNameTmp.DOFade(0, 0.25f).OnComplete(() =>
        {
            bigAreaNameTmp.text = bigArea != null && bigArea.Data != null ? bigArea.Data.displayName : "";
            bigAreaNameTmp.DOFade(1, 0.25f);
        });

        RefreshPercentFromData();
    }

    private void RefreshPercentFromData()
    {
        if (percentNameTmp == null) return;

        if (areaDisplayManager == null) areaDisplayManager = AreaDisplayManager.Instance;
        if (areaDisplayManager == null || areaDisplayManager.BigAreas == null || areaDisplayManager.BigAreas.Length == 0)
        {
            UpdatePercentComplete(0, 0);
            return;
        }

        int max = areaDisplayManager.BigAreas.Length;

        // Nếu chưa có CourseMapBrowserUI hoặc chưa load course thì current = 0
        int current = 0;

        if (courseMapBrowserUI != null)
        {
            foreach (var area in areaDisplayManager.BigAreas)
            {
                float ownedPercent = courseMapBrowserUI.GetBigAreaOwnedPercent(area);
                if (ownedPercent > 0f) current++;
            }
        }

        UpdatePercentComplete(current, max);
    }

    public void UpdatePercentComplete(int current, int max)
    {
        max = Mathf.Max(1, max);
        current = Mathf.Clamp(current, 0, max);

        float percent = (current * 100f) / max;
        int percentRound = Mathf.RoundToInt(percent);

        percentNameTmp.text =
            $"Đã mở khóa <color=#F9DF99>{current}/{max} ({percentRound}%)</color>";
    }
}
