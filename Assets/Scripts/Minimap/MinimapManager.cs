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
    private bool minimapBlockedByBoxLoad;
    private bool ownsGameplayLock;
    private bool hasStarted;

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
        AddressableAdditiveSceneLoader.BoxLoadVisibilityChanged += OnBoxLoadVisibilityChanged;
        BootFlow.InitialBootLoadingChanged += OnInitialBootLoadingChanged;
        LoadingTransition.LoadingStateChanged += OnLoadingStateChanged;

        bool loadingNow = BootFlow.IsInitialBootLoading || LoadingTransition.IsLoading;
        SetMinimapBlockedByBoxLoad(
            loadingNow && AddressableAdditiveSceneLoader.IsAnyBoxLoadVisible);
    }

    private void Start()
    {
        hasStarted = true;
        RefreshMinimapVisibility();
    }

    private void OnEnable()
    {
        if (hasStarted)
            RefreshMinimapVisibility();
    }
    
    private void OnDestroy()
    {
        minimapUI.TurnOnMinimapAction -= ToggleOnMinimap;
        minimapUI.TurnOffMinimapAction -= ToggleOffMinimap;
        
        areaDisplayManager.OnShowFocusArea -= OnShowFocusArea;

        plotHandlerUI.OnClickShowScrollViewAction -= ToggleScrollViewArea;
        plotHandlerUI.OnShowFindCourseAction -= ShowFindCourseUI;
        
        findCourseHandler.OnCloseFindCourseAction -= HideFindCourseUI;
        AddressableAdditiveSceneLoader.BoxLoadVisibilityChanged -= OnBoxLoadVisibilityChanged;
        BootFlow.InitialBootLoadingChanged -= OnInitialBootLoadingChanged;
        LoadingTransition.LoadingStateChanged -= OnLoadingStateChanged;

        SetGameplayLock(false);

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
        if (IsSceneLoading() || minimapBlockedByBoxLoad)
        {
            Debug.Log("[Minimap] Minimap is blocked while the scene is loading.");
            return;
        }

        Debug.Log($"Toggle vao minimap camera");
        // UpdateState(true);
        player.transform.rotation = Quaternion.Euler(0, 180, 0);
        StartCoroutine(TryBlending(true));
        minimapCameraHandler.FocusMinimapCamera();
    }

    private void UpdateState(bool isEnable)
    {
        OnMinimapActiveAction?.Invoke(isEnable);
        SetGameplayLock(isEnable);
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

    private void OnBoxLoadVisibilityChanged(bool isVisible)
    {
        SetMinimapBlockedByBoxLoad(isVisible);

        if (isVisible)
            minimapUI.Hide();
        else
            RefreshMinimapVisibility();
    }

    private void OnInitialBootLoadingChanged(bool isLoading)
    {
        if (isLoading)
        {
            HideMinimapForLoading();
            return;
        }

        // BootFlow chỉ phát ready sau khi late loader và intro đã kết thúc.
        SetMinimapBlockedByBoxLoad(false);
        RefreshMinimapVisibility();
    }

    private void OnLoadingStateChanged(bool isLoading)
    {
        if (isLoading)
        {
            HideMinimapForLoading();
            return;
        }

        // LoadingScreenController chỉ gọi complete sau khi target và late content sẵn sàng.
        SetMinimapBlockedByBoxLoad(false);
        RefreshMinimapVisibility();
    }

    private void HideMinimapForLoading()
    {
        TeleMapController._mapActive = false;
        SetGameplayLock(false);

        if (minimapUI != null)
            minimapUI.Hide();
    }

    private void RefreshMinimapVisibility()
    {
        if (minimapUI == null)
            return;

        if (IsSceneLoading() || minimapBlockedByBoxLoad)
        {
            minimapUI.Hide();
            return;
        }

        minimapUI.ShowBottomViewUI();
        minimapUI.Show();
    }

    private static bool IsSceneLoading()
    {
        return BootFlow.IsInitialBootLoading || LoadingTransition.IsLoading;
    }

    private void SetMinimapBlockedByBoxLoad(bool blocked)
    {
        minimapBlockedByBoxLoad = blocked;

        if (minimapUI != null)
            minimapUI.SetTurnOnInteractable(!blocked);

        if (blocked)
        {
            minimapUI.Hide();

            if (TeleMapController._mapActive)
                ToggleOffMinimap();
        }
    }

    private void SetGameplayLock(bool locked)
    {
        if (ownsGameplayLock == locked)
            return;

        if (locked)
        {
            GameplayLock.Lock(GameplayLockReason.UI, GameplayLockTarget.All);
        }
        else
        {
            GameplayLock.Unlock(GameplayLockReason.UI);
        }

        ownsGameplayLock = locked;
    }
}

