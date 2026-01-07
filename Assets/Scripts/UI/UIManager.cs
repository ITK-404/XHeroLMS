using System;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    public InputCanvas InputCanvas;
    public PlayerPanelUI PlayerPanelUI;
    public CourseMenuButtons CourseMenuButtons;

    private void Awake()
    {
        Instance = this;
    }
    
    
}
