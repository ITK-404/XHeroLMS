using System;
using UnityEngine;

public class CourseSceneReturnHandler : MonoBehaviour
{
    [SerializeField] private TabItemManagerUI tabItemManagerUI;

    private void Awake()
    {
        tabItemManagerUI.OnClickReturnBtnEvent += TabItemManagerUIOnOnClickReturnBtnEvent;
    }

    private void OnDestroy()
    {
        tabItemManagerUI.OnClickReturnBtnEvent -= TabItemManagerUIOnOnClickReturnBtnEvent;
    }

    private void TabItemManagerUIOnOnClickReturnBtnEvent()
    {
        LoadingTransition.LoadPreviousSceneOrDefault();
    }
}
