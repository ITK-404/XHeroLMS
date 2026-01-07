using System;
using Unity.Cinemachine;
using UnityEngine;

public class MinimapManager : MonoBehaviour
{
    [Header("Others")]
    [SerializeField] private CinemachineCamera minimapCamera;
    [SerializeField] private GameObject player;
    [Header("UI")]
    [SerializeField] private MinimapUI minimapUI;
    [SerializeField] private CameraZoomSlider cameraZoomSlider;
    private void Awake()
    {
        minimapUI.turnOnBtn.onClick.AddListener(ToggleOn);
        minimapUI.turnOffBtn.onClick.AddListener(ToggleOff);
    }

    private void OnDestroy()
    {
        minimapUI.turnOnBtn.onClick.RemoveListener(ToggleOn);
        minimapUI.turnOffBtn.onClick.RemoveListener(ToggleOff);
    }
    

    private void ToggleOff()
    {
        minimapCamera.Priority.Value = 0;
        player.GetComponent<PlayerCamera>().playerCinemachineCamera.Priority.Value = 10;
        
        UpdateState(false);
    }
    
    private void ToggleOn()
    {
        minimapCamera.Priority.Value = 10;
        player.GetComponent<PlayerCamera>().playerCinemachineCamera.Priority.Value = 0;
        
        UpdateState(true);
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
        }
    }
}