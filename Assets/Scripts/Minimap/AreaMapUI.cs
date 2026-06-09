using System;
using TMPro;
using UnityEngine;

public class WorldSpaceUI : MonoBehaviour
{
    protected RectTransform uiElement;
    [SerializeField] protected bool isWorldSpaceUI = false;
    [SerializeField] protected Camera wrapperCamera;

    protected virtual void Awake()
    {
        uiElement = GetComponent<RectTransform>();
    }

    protected void HandleFollowTarget()
    {
        var worldSpacePos = GetTargetPosition();

        if (!isWorldSpaceUI)
        {
            var screenPosition = wrapperCamera.WorldToScreenPoint(worldSpacePos);

            uiElement.position = screenPosition;
        }
        else
        {
            uiElement.position = worldSpacePos;
        }
    }

    public void SetCamera(Camera playerCamera)
    {
        wrapperCamera = playerCamera;
    }

    protected virtual Vector3 GetTargetPosition()
    {
        return transform.position;
    }
}

public class AreaMapUI : WorldSpaceUI
{
    [SerializeField] private AreaMapLocation areaMapLocation;

    private void LateUpdate()
    {
        // link with location to sync screen position
        if (wrapperCamera == null)
            return;

        if (areaMapLocation == null)
            return;

        HandleFollowTarget();
    }

    protected override Vector3 GetTargetPosition()
    {
        return areaMapLocation.GetItemWorldPosition();
    }

    public void Setup(Camera cam, AreaMapLocation location, bool worldSpaceUI = false)
    {
        wrapperCamera = cam;
        areaMapLocation = location;

        this.isWorldSpaceUI = worldSpaceUI;
        if (worldSpaceUI)
        {
            transform.position = areaMapLocation.GetItemWorldPosition();
        }
    }
}