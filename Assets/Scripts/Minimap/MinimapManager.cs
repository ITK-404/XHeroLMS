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
    private void Awake()
    {
        minimapUI.turnOnBtn.onClick.AddListener(ToggleOn);
        minimapUI.turnOffBtn.onClick.AddListener(ToggleOff);
        
        areaDisplayManager.OnShowFocusArea += OnShowFocusArea;
    }
    

    private void OnShowFocusArea(BigArea selectArea)
    {
        if (selectArea != null)
        {
            plotHandlerUI.Show();
            plotHandlerUI.ShowArea(selectArea);
            
            cameraZoomSlider.Hide();
        }
        else
        {
            plotHandlerUI.Hide();
        }
    }

    private void OnDestroy()
    {
        minimapUI.turnOnBtn.onClick.RemoveListener(ToggleOn);
        minimapUI.turnOffBtn.onClick.RemoveListener(ToggleOff);
        areaDisplayManager.OnShowFocusArea -= OnShowFocusArea;
        
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
            if(TokenStore.IsAuthenticated)
                UIManager.Instance.CourseMenuButtons.Show();
            
            InputBlocker.SetBlocked(false);
            TeleMapController._mapActive = false;
            cameraZoomSlider.Hide();
            areaDisplayManager.Hide();
        }
    }
}

