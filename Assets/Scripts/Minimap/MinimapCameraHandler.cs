using System.Collections.Generic;
using System.Linq;
using Unity.Cinemachine;
using UnityEngine;

public class MinimapCameraHandler : MonoBehaviour
{
    [SerializeField] private CinemachineCamera minimapCamera;
    [SerializeField] private CinemachineCamera playerCmCamera;

    private List<CinemachineCamera> focusCameraGroup = new();
    [SerializeField] private GameObject areaContainer;

    public CinemachineCamera GetActiveCamera()
    {
        return minimapCamera;
    }
    
    private void Start()
    {
        CatchAllCamera();
    }
    private void CatchAllCamera()
    {
        if (areaContainer == null)
        {
            Debug.LogError($"Area container to find cinemachine camera is null",gameObject);
            return;
        }
        focusCameraGroup.Clear();
        
        focusCameraGroup = areaContainer.gameObject.GetComponentsInChildren<CinemachineCamera>().ToList();

        focusCameraGroup.Add(minimapCamera);
        focusCameraGroup.Add(playerCmCamera);
    }

    public void FocusMinimapCamera()
    {
        SetPriorityTo(minimapCamera);
    }

    public void FocusPlayerCamera()
    {
        SetPriorityTo(playerCmCamera);
    }

    public void TryFocusCamera(CinemachineCamera focusCamera)
    {
        SetPriorityTo(focusCamera);
    }

    private void SetPriorityTo(CinemachineCamera focusCamera)
    {
        if (focusCamera == null)
        {
            Debug.LogError($"Focus camera is null, please checkout",gameObject);
            return;
        }
        
        foreach (var cam in focusCameraGroup)
        {
            if (cam != focusCamera)
            {
                cam.Priority.Value = 0;
            }
        }
        focusCamera.Priority.Value = 10;
    }
}