using System;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public InputCanvas InputCanvas;
    public PlayerPanelUI PlayerPanelUI;
    public CourseMenuButtons CourseMenuButtons;
    
    // binding event
    public MinimapManager minimapManager;
    
    private void Awake()
    {
        Instance = this;
        minimapManager.OnMinimapActiveAction += OnMinimapActiveAction;
    }

    private void OnDestroy()
    {
        minimapManager.OnMinimapActiveAction -= OnMinimapActiveAction;
    }

    private void OnMinimapActiveAction(bool isEnable)
    {
        if (isEnable)
        {
            InputCanvas.Hide();
            PlayerPanelUI.HideAll();
            CourseMenuButtons.Hide();
        }
        else
        {
            InputCanvas.Show();
            PlayerPanelUI.ShowAll();
            CourseMenuButtons.Show();
        }
    }
}
