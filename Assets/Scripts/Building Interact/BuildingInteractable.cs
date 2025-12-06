using System;
using Unity.Cinemachine;
using UnityEngine;

public class BuildingInteractable : MonoBehaviour
{
    public Transform standPosition;
    public CinemachineCamera virtualCamera;
    private Collider _collider;

    private void Awake()
    {
        _collider = GetComponent<Collider>();
    }

    public Transform GetStandTransform()
    {
        return standPosition;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawRay(standPosition.position, standPosition.forward);
    }

    public void OnSelect()
    {
        _collider.enabled = false;
    }

    public void DeSelect()
    {
        _collider.enabled = true;
    }
}
