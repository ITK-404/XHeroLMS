using System;
using UnityEngine;

public class CourseFindingManager : MonoBehaviour
{
    [SerializeField] private MinimapManager minimapManager;
    [SerializeField] private CourseMapBrowserUI courseMapBrowserUI;
    [SerializeField] private PlotArea defaultPlotArea;
    [SerializeField] private PointClickSystem pointClickSystem;
    private void Start()
    {
        MinimapCourseDisplayUI.OnFindWayAction += OnClickFindWay;
        PlayerPanelUI.Instance.OnClickTryExitAutoFindWay += OnClickTryExitAutoFindWay;
    }

    private void OnClickTryExitAutoFindWay()
    {
        pointClickSystem.StopMoving();
        InputBlocker.SetBlocked(false);
    }

    private void OnDestroy()
    {
        MinimapCourseDisplayUI.OnFindWayAction -= OnClickFindWay;
        PlayerPanelUI.Instance.OnClickTryExitAutoFindWay -= OnClickTryExitAutoFindWay;
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
        
        minimapManager.ToggleOffMinimap();
        pointClickSystem.MoveToPosition(findPlot.Location.GetItemWorldPosition());
        pointClickSystem.IsClickMoving = true;
        
        InputBlocker.SetBlocked(true);
        PlayerPanelUI.Instance.ShowPathfindingPanel();
    }
}