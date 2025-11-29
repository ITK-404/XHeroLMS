using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using Unity.Cinemachine;

public enum ViewState
{
    Player,
    Default,
    Sitdown,
    External,
    FullScreen,
    Exam
}

[DisallowMultipleComponent]
public class QuadCinemachineController : MonoBehaviour
{
    public static QuadCinemachineController Instance;

    [Header("Scene Refs")]
    public GameObject quad;

    public Camera mainRenderCamera;

    // VCam bám theo targetCamera.transform
    public Button btnDef;
    public Button btnEx;
    public Button btnFull;

    [Header("UI (tuỳ chọn)")]
    public Canvas[] worldSpaceCanvases;

    [Header("Priority")]
    private Vector3 originalQuadPos;

    private bool targetWasEnabled;
    private float targetOriginalDepth;
    private ViewState currentState = ViewState.Player;

    VideoPlayerControllerPro videoPlayerController;
    LearnUI learnUI;

    private QuadCameraManager quadCamManager;
    public PlayerStandUI playerStandUI;
    [SerializeField] private List<ToggleVideoType> toggleVideoList = new();

    void Awake()
    {
        Instance = this;
        
        learnUI = FindAnyObjectByType<LearnUI>();
        videoPlayerController = FindAnyObjectByType<VideoPlayerControllerPro>();
        playerStandUI = FindAnyObjectByType<PlayerStandUI>();

        if (!mainRenderCamera) mainRenderCamera = Camera.main;
        if (quad) originalQuadPos = quad.transform.position;
        // assign event
        foreach (var item in toggleVideoList)
        {
            item.OnClickVideoAction += ChangeState;
        }
    }

    private void OnDestroy()
    {
        foreach (var item in toggleVideoList)
        {
            item.OnClickVideoAction -= ChangeState;
        }
    }

    void Start()
    {
        quadCamManager = QuadCameraManager.Instance;

        // World/ScreenSpace-Camera canvases raycast qua Main Camera
        if (worldSpaceCanvases != null && mainRenderCamera != null)
        {
            foreach (var cvs in worldSpaceCanvases)
            {
                if (!cvs) continue;
                if (cvs.renderMode == RenderMode.WorldSpace || cvs.renderMode == RenderMode.ScreenSpaceCamera)
                    cvs.worldCamera = mainRenderCamera;
            }
        }

        if (EventSystem.current == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        currentState = ViewState.Player;
    }

    public void ChangeState(ViewState newState)
    {
        
        Debug.Log("Thay đổi sang view state: " + newState);
        if (newState == currentState)
        {
            // change to player camera
            // turn off all
            currentState = ViewState.Sitdown;
        }
        else
        {
            currentState = newState;
        }


        switch (currentState)
        {
            case ViewState.Player:
                quadCamManager.ChangeToPlayerCamera();
                SetQuadZ(originalQuadPos.z);
                break;
            case ViewState.Sitdown:
                quadCamManager.ChangeToSitdownCameraState();
                playerStandUI.ShowStandUpButton();
                SetQuadZ(originalQuadPos.z);
                videoPlayerController.EnterDefaultMode();
                break;
            case ViewState.External:
                // quadCamManager.ChangeToLocalRoomCamera();
                // SetQuadZ(-1.2f);
                videoPlayerController.EnterSecondMode();
                
                break;
            case ViewState.FullScreen:
                videoPlayerController.EnterFullScreenMode();
                break;
            case ViewState.Exam:
                videoPlayerController.EnterFullScreenMode();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }

        if (currentState == ViewState.Player)
        {
            playerStandUI.ShowSitdownButton();
        }
        else if (currentState == ViewState.Sitdown)
        {
            playerStandUI.ShowStandUpButton();
        }
        else
        {
            playerStandUI.HideButtons();
        }

        ClearUISelection();
        
        foreach (var item in toggleVideoList)
        {
            item.ChangeState(item.watchVideoState == currentState
                ? ToggleBaseUI.State.Active
                : ToggleBaseUI.State.DeActive);
        }

    }

    void SetQuadZ(float z)
    {
        if (!quad) return;
        var p = quad.transform.position;
        p.z = z;
        quad.transform.position = p;
    }

    void ClearUISelection()
    {
        if (EventSystem.current)
            EventSystem.current.SetSelectedGameObject(null);
    }
}