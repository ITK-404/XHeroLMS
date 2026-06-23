using System;
using UnityEngine;

public class CourseFindingManager : MonoBehaviour
{
    [SerializeField] private MinimapManager minimapManager;
    [SerializeField] private CourseMapBrowserUI courseMapBrowserUI;
    [SerializeField] private PlotArea defaultPlotArea;
    [SerializeField] private PointClickSystem pointClickSystem;
    private bool isAutoFindingWay;

    private void Start()
    {
        MinimapCourseDisplayUI.OnFindWayAction += OnClickFindWay;

        if (PlayerPanelUI.Instance != null)
            PlayerPanelUI.Instance.OnClickTryExitAutoFindWay += OnClickTryExitAutoFindWay;

        if (pointClickSystem != null)
            pointClickSystem.OnPathStopped += OnPointClickPathStopped;
    }

    private void OnClickTryExitAutoFindWay()
    {
        isAutoFindingWay = false;
        pointClickSystem?.StopMoving();
        InputBlocker.SetBlocked(false);
    }

    private void OnDestroy()
    {
        MinimapCourseDisplayUI.OnFindWayAction -= OnClickFindWay;

        if (PlayerPanelUI.Instance != null)
            PlayerPanelUI.Instance.OnClickTryExitAutoFindWay -= OnClickTryExitAutoFindWay;

        if (pointClickSystem != null)
            pointClickSystem.OnPathStopped -= OnPointClickPathStopped;
    }

    private void OnPointClickPathStopped(bool arrived)
    {
        if (!isAutoFindingWay)
            return;

        isAutoFindingWay = false;
        InputBlocker.SetBlocked(false);
        PlayerPanelUI.Instance?.HidePathfinding();
    }

    private void OnClickFindWay(string seoUrl)
    {
        Debug.Log("Thu tim kiem vi tri phu hop voi khoa hoc");
        if (seoUrl == null)
        {
            Debug.LogError("This data is null");
            return;
        }

        if (string.IsNullOrEmpty(seoUrl))
        {
            Debug.LogError("This seo url is null");
            return;
        }

        if (pointClickSystem == null)
        {
            Debug.LogError("PointClickSystem is null");
            return;
        }

        var findPlot = defaultPlotArea;
        foreach (BigArea bigArea in AreaDisplayManager.Instance.BigAreas)
        {
            foreach (var plotArea in bigArea.PlotAreas)
            {
                if (seoUrl == plotArea.seo_url)
                {
                    findPlot = plotArea;
                    break;
                }
            }
        }

        if (findPlot == null || findPlot.Location == null)
        {
            Debug.LogError($"Không tìm được vị trí khóa học hợp lệ cho seo url: {seoUrl}");
            return;
        }
        
        minimapManager?.ToggleOffMinimap();
        if (!pointClickSystem.MoveToPosition(findPlot.Location.GetItemWorldPosition()))
        {
            isAutoFindingWay = false;
            InputBlocker.SetBlocked(false);
            PlayerPanelUI.Instance?.HidePathfinding();
            return;
        }

        pointClickSystem.IsClickMoving = true;
        isAutoFindingWay = true;

        InputBlocker.SetBlocked(true);
        PlayerPanelUI.Instance?.ShowPathfindingPanel();
    }
}
