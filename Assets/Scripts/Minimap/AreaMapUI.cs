using System;
using TMPro;
using UnityEngine;

public class AreaMapUI : MonoBehaviour
{
    [SerializeField] private AreaMapLocation areaMapLocation;
    Camera wrapperCamera;

    private RectTransform uiElement;
    private bool isWorldSpaceUI = false;

    private void Awake()
    {
        uiElement = GetComponent<RectTransform>();
    }

    private void LateUpdate()
    {
        // link with location to sync screen position
        if (wrapperCamera == null)
            return;

        if (areaMapLocation == null)
            return;
        
        var worldSpacePos = areaMapLocation.GetItemWorldPosition();
        
        if (isWorldSpaceUI)
        {
            uiElement.position = worldSpacePos;
        }
        else
        {
            var screenPosition = wrapperCamera.WorldToScreenPoint(worldSpacePos);

            uiElement.position = screenPosition;
        }
        
    }

    public void Setup(Camera cam, AreaMapLocation location,bool worldSpaceUI = false)
    {
        wrapperCamera = cam;
        areaMapLocation = location;

        this.isWorldSpaceUI = worldSpaceUI;
    }
}