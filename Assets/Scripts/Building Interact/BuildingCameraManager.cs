using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;

public class BuildingCameraManager : MonoBehaviour
{
    public static BuildingCameraManager Instance;
    private BuildingInteractable buildingInteractable;
    public CinemachineBrain brain;
    private bool isTeleport;
    public Transform player;
    private void Awake()
    {
        Instance = this;

    }

    private void Update()
    {
        if (brain.IsBlending && brain.ActiveBlend != null)
        {
            return;
        }

        if (buildingInteractable != null && Input.anyKeyDown && isTeleport)
        {
            ResetInteract();
        }
    }

    private void ResetInteract()
    {
        buildingInteractable.virtualCamera.Priority = 0;
        buildingInteractable.DeSelect();
        buildingInteractable = null;
    }

    public bool IsSameTarget(BuildingInteractable buildingInteractable)
    {
        if (this.buildingInteractable == null) return false;
        return buildingInteractable == this.buildingInteractable;
    }

    private IEnumerator WaitForBlend()
    {
        yield return new WaitForSeconds(0.1f);
        while (brain.IsBlending || brain.ActiveBlend != null)
        {
            yield return null;
        }
        player.GetComponent<PointClickSystem>().TeleportDelay(buildingInteractable.standPosition);
        player.rotation = buildingInteractable.GetStandTransform().rotation;
        isTeleport = true;
    }

    public void FocusOnBuilding(BuildingInteractable interactable)
    {
        
        Debug.Log("Assign interactable");
        if(buildingInteractable != null && buildingInteractable != interactable)
        {
            ResetInteract();
            return;
        }
        if (interactable == null)
        {
            Debug.Log("Interact building is null");
        }
        this.buildingInteractable = interactable;

        buildingInteractable.virtualCamera.Priority = 10;
        buildingInteractable.OnSelect();
        isTeleport = false;

        StartCoroutine(WaitForBlend());
    }

    public bool IsFocus()
    {
        return buildingInteractable != null;
    }
}
