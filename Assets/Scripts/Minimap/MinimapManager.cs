using System;
using UnityEngine;

public class MinimapManager : MonoBehaviour
{
    
    [Header("Others")]
    [SerializeField] private GameObject player;
    [Header("UI")]
    [SerializeField] private MinimapUI minimapUI;
    [SerializeField] private CameraZoomSlider cameraZoomSlider;
    [SerializeField] private AreaDisplayManager areaDisplayManager;
    [SerializeField] private MinimapCameraHandler minimapCameraHandler;
    [SerializeField] private PlotHandlerUI plotHandlerUI;
    [SerializeField] private CircularScrollView circularScrollView;
    [SerializeField] private FindCourseHandler findCourseHandler;
    private void Awake()
    {
        minimapUI.turnOnBtn.onClick.AddListener(ToggleOn);
        minimapUI.turnOffBtn.onClick.AddListener(ToggleOff);
        areaDisplayManager.OnShowFocusArea += OnShowFocusArea;
        
        plotHandlerUI.showScrollViewBtn.onClick.AddListener(ToggleScrollViewArea);
        plotHandlerUI.findCourseBtn.onClick.AddListener(ShowFindCourseUI);
    }
    
    private void OnDestroy()
    {
        minimapUI.turnOnBtn.onClick.RemoveListener(ToggleOn);
        minimapUI.turnOffBtn.onClick.RemoveListener(ToggleOff);
        areaDisplayManager.OnShowFocusArea -= OnShowFocusArea;
        
        plotHandlerUI.showScrollViewBtn.onClick.RemoveListener(ToggleScrollViewArea);
        plotHandlerUI.findCourseBtn.onClick.RemoveListener(ShowFindCourseUI);
    }

    private bool isOn = false;

    private void ShowFindCourseUI()
    {
        // make sure hide area course
        findCourseHandler.Show();
        circularScrollView.Hide();
    }
    
    private void ToggleScrollViewArea()
    {
        isOn = !isOn;
        OnScrollViewAreaUpdate(isOn);
    }

    private void OnScrollViewAreaUpdate(bool state)
    {
        if (state)
        {
            circularScrollView.Show();
            AreaDisplayManager.Instance.SelectArea?.HidePlotArea();
            findCourseHandler.Hide();
        }
        else
        {
            circularScrollView.Hide();
            AreaDisplayManager.Instance.SelectArea?.ShowPlotArea();
        }
    }

    private void OnShowFocusArea(BigArea selectArea)
    {
        if (selectArea != null)
        {
            // hien thi khu vuc nho ben trong khu vuc lon
            plotHandlerUI.Show();
            plotHandlerUI.ShowArea(selectArea);
            // an thanh slider
            cameraZoomSlider.Hide();
            // an scroll view hien thi danh sach khu vuc
            isOn = false;
            circularScrollView.Hide();
        }
        else
        {
            plotHandlerUI.Hide();
        }
    }

  
    

    private void ToggleOff()
    {
        UpdateState(false);
        minimapCameraHandler.FocusPlayerCamera();
        areaDisplayManager.ResetArea();
        Debug.Log($"Toggle vao player camera");
    }
    
    private void ToggleOn()
    {
        Debug.Log($"Toggle vao minimap camera");
        UpdateState(true);
        minimapCameraHandler.FocusMinimapCamera();
    }

    private void UpdateState(bool isEnable)
    {
        if (isEnable)
        {
            minimapUI.ShowTopViewUI();
            UIManager.Instance.PlayerPanelUI.HideAll();
            UIManager.Instance.InputCanvas.Hide();
            InputBlocker.SetBlocked(true);
            UIManager.Instance.CourseMenuButtons.Hide();
            TeleMapController._mapActive = true;
            cameraZoomSlider.Show();
            areaDisplayManager.Show();
        }
        else
        {
            minimapUI.ShowBottomViewUI();
            UIManager.Instance.PlayerPanelUI.ShowAll();
            UIManager.Instance.InputCanvas.Show();
            
            UIManager.Instance.CourseMenuButtons.Show();
            
            InputBlocker.SetBlocked(false);
            TeleMapController._mapActive = false;
            cameraZoomSlider.Hide();
            areaDisplayManager.Hide();
        }
    }
}

