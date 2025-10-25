using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class QuadCameraManager : MonoBehaviour
{
    public static QuadCameraManager Instance;
    
    public CinemachineCamera playerCamera;
    public CinemachineCamera localRoomCamera;
    public CinemachineCamera sitdownCamera;
    public ViewState playerViewState;
    private List<CinemachineCamera> cameraList = new();

    private void Awake()
    {
        Instance = this;
        
        cameraList.Add(playerCamera);
        cameraList.Add(localRoomCamera);
        cameraList.Add(sitdownCamera);
        
        ChangeCameraState(ViewState.Player);
    }

    private void SetPriorityToCamera(CinemachineCamera adjustCamera)
    {
        foreach (var camera in cameraList)
        {
            if (camera == adjustCamera)
            {
                camera.Priority = 10;
            }
            else
            {
                camera.Priority = 0;
            }
        }
    }

    public void ChangeCameraState(ViewState viewState)
    {
        switch (viewState)
        {
            case ViewState.Player:
                SetPriorityToCamera(playerCamera);
                break;
            case ViewState.Default:
                SetPriorityToCamera(localRoomCamera);
                break;
            case ViewState.Sitdown:
                SetPriorityToCamera(sitdownCamera);
                break;
            case ViewState.External:
                SetPriorityToCamera(localRoomCamera);
                break;
            case ViewState.FullScreen:
                // turn off UI 
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(viewState), viewState, null);
        }
    }

    public void ChangeToSitdownCameraState(Vector3 cameraPosition)
    {
        ChangeCameraState(ViewState.Sitdown);
        sitdownCamera.transform.position = cameraPosition;
    }
    
}