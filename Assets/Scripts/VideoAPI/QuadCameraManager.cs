using DG.Tweening;
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
    private List<CinemachineCamera> cameraList = new();

    private void Awake()
    {
        Instance = this;

        cameraList.Add(playerCamera);
        cameraList.Add(localRoomCamera);
        cameraList.Add(sitdownCamera);

        ChangeToPlayerCamera();
    }

    private void SetPriorityToCamera(CinemachineCamera adjustCamera)
    {
        if (adjustCamera == null || cameraList == null || cameraList.Count == 0) return;

        const int activePriority = 10;
        const int inactivePriority = 0;

        // Ensure the target is tracked
        if (!cameraList.Contains(adjustCamera))
        {
            cameraList.Add(adjustCamera);
        }

        for (int i = 0; i < cameraList.Count; i++)
        {
            var cam = cameraList[i];
            if (cam == null) continue;
            cam.Priority = cam == adjustCamera ? activePriority : inactivePriority;
        }
    }


    public void SetupSitdownCameraByCheckPoint(Transform checkPoint)
    {
        Debug.Log($"Change to checkpoint: {checkPoint.transform.position} {checkPoint.transform.rotation}", checkPoint.gameObject);
        ChangeToSitdownCameraState(checkPoint.transform.position);
        Debug.Log($"{checkPoint.transform.rotation}");
        sitdownCamera.transform.DORotateQuaternion(checkPoint.transform.rotation, 1f);
    }

    public void ChangeToSitdownCameraState(Vector3 cameraPosition)
    {
        SetPriorityToCamera(sitdownCamera);
        if(sitdownCamera.Priority == 10)
        {
            sitdownCamera.transform.DOMove(cameraPosition, 1f);
        }
        else
        {
            sitdownCamera.transform.position = cameraPosition;
        }
    }

    public void ChangeToSitdownCameraState()
    {
        SetPriorityToCamera(sitdownCamera);
    }

    public void ChangeToPlayerCamera()
    {
        SetPriorityToCamera(playerCamera);
    }

    public void ChangeToLocalRoomCamera()
    {
        SetPriorityToCamera(localRoomCamera);
    }



}