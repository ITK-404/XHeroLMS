using System;
using System.Collections;
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
    
    // setting
    private bool isBleding;
    [SerializeField] private GameObject blockedImg;

    public Action<bool> OnMinimapActiveAction;
    private void Awake()
    {
        blockedImg.gameObject.SetActive(false);
        waitForTwoSecond = new WaitForSeconds(2);
        
        minimapUI.TurnOnMinimapAction += ToggleOnMinimap;
        minimapUI.TurnOffMinimapAction += ToggleOffMinimap;
        
        areaDisplayManager.OnShowFocusArea += OnShowFocusArea;
        
        plotHandlerUI.OnClickShowScrollViewAction += ToggleScrollViewArea;    
        plotHandlerUI.OnShowFindCourseAction += ShowFindCourseUI;

        findCourseHandler.OnCloseFindCourseAction += HideFindCourseUI;
    }
    
    private void OnDestroy()
    {
        minimapUI.TurnOnMinimapAction -= ToggleOnMinimap;
        minimapUI.TurnOffMinimapAction -= ToggleOffMinimap;
        
        areaDisplayManager.OnShowFocusArea -= OnShowFocusArea;

        plotHandlerUI.OnClickShowScrollViewAction -= ToggleScrollViewArea;
        plotHandlerUI.OnShowFindCourseAction -= ShowFindCourseUI;
        
        findCourseHandler.OnCloseFindCourseAction -= HideFindCourseUI;

    }

    private bool isOn = false;

    private void ShowFindCourseUI()
    {
        // make sure hide area course
        findCourseHandler.Show();
        circularScrollView.Hide();
    }

    public void HideFindCourseUI()
    {
        findCourseHandler.Hide();
        // circularScrollView.Show();
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


    private IEnumerator TryBlending(bool isEnable)
    {
        if (isBleding)
        {
            yield break;
        }

        isBleding = true;
        UpdateState(isEnable);
        blockedImg.gameObject.SetActive(true);
        yield return waitForTwoSecond;
        blockedImg.gameObject.SetActive(false);
        isBleding = false;
    }

    private YieldInstruction waitForTwoSecond;

    public void ToggleOffMinimap()
    {
        StartCoroutine(TryBlending(false));
        // UpdateState(false);
        minimapCameraHandler.FocusPlayerCamera();
        areaDisplayManager.ResetArea();
        Debug.Log($"Toggle vao player camera");
    }
    
    public void ToggleOnMinimap()
    {
        Debug.Log($"Toggle vao minimap camera");
        // UpdateState(true);
        player.transform.rotation = Quaternion.Euler(0, 180, 0);
        StartCoroutine(TryBlending(true));
        minimapCameraHandler.FocusMinimapCamera();
    }

    private void UpdateState(bool isEnable)
    {
        OnMinimapActiveAction?.Invoke(isEnable);
            InputBlocker.SetBlocked(isEnable);
            TeleMapController._mapActive = isEnable;
        
        player.GetComponent<PointClickSystem>().StopMoving();
        
        if (isEnable)
        {
            minimapUI.ShowTopViewUI();
            cameraZoomSlider.Show();
            areaDisplayManager.Show();
        }
        else
        {
            
            minimapUI.ShowBottomViewUI();
            
            cameraZoomSlider.Hide();
            areaDisplayManager.Hide();
            circularScrollView.Hide();
            findCourseHandler.Hide();
        }
    }
}

