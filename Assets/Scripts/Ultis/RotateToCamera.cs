using System;
using UnityEngine;

public class RotateToCamera : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform item;
    [SerializeField] private bool ignoreY = false;
    [SerializeField] private bool flipFoward = false;
    private void LateUpdate()
    {
        if (playerCamera == null) return;
        if (item == null) return;

        var direction = playerCamera.transform.position - item.transform.position;
        if (ignoreY)
        {
            direction.y = 0;
        }
        if(flipFoward)
        {
            direction = -direction;
        }
        item.transform.forward = direction;
    }

    public void SetCamera(Camera playerCamera)
    {
        this.playerCamera = playerCamera;
    }
}