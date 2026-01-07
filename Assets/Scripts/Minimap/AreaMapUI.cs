using System;
using TMPro;
using UnityEngine;

public class AreaMapUI : MonoBehaviour
{
    [SerializeField] private AreaMapLocation areaMapLocation;
    Camera wrapperCamera;

    private RectTransform uiElement;

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
        var screenPosition = wrapperCamera.WorldToScreenPoint(worldSpacePos);

        uiElement.position = screenPosition;
    }

    public void Setup(Camera cam, AreaMapLocation location)
    {
        wrapperCamera = cam;
        areaMapLocation = location;
    }
}