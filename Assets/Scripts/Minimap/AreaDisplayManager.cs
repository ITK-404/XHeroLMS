using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public class AreaDisplayManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private BigArea[] bigAreasLocation;
    [SerializeField] private BigMapUI bigMapUIPrefab;
    [SerializeField] private RectTransform playerMarker;
    [Header("Others")] 
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject uiAreaContainer;
    [SerializeField] private GameObject locationAreaContainer;
    [SerializeField] private Camera wrapperCamera;
    
    [SerializeField] private MinimapCameraHandler minimapCameraHandler;
    private List<BigMapUI> bigMapUIList = new(); 

    private void Awake()
    {
        Hide();
    }

    private void Start()
    {
        SetupBigAreaUI();
        // hide all highlight area
        HighlightSingleArea(null);
    }

    private void SetupBigAreaUI()
    {
        bigAreasLocation = locationAreaContainer.GetComponentsInChildren<BigArea>();

        if (bigAreasLocation == null) return;

        foreach (var bigMap in bigAreasLocation)
        {
            var bigMapUI = Instantiate(bigMapUIPrefab, uiAreaContainer.transform);
            bigMapUI.SetData(bigMap.Data);
            
            var areaUI = bigMapUI.AreaMapUI;
            areaUI.Setup(wrapperCamera, bigMap.Location);
            
            bigMapUIList.Add(bigMapUI);
        }
    }

    [ContextMenu("Update Button Hitbox")]
    private void UpdateHitboxUI()
    {
        for (int i = 0; i < bigMapUIList.Count; i++)
        {
            var area = bigAreasLocation[i];
            bigMapUIList[i].CalculatorHitbox(area.GetSpline(),wrapperCamera);
        }
    }

    public void Show()
    {
        uiAreaContainer.gameObject.SetActive(true);   
    }

    public void Hide()
    {
        uiAreaContainer.gameObject.SetActive(false);
    }

    private void LateUpdate()
    {
        HandlerMarker();
        HandleAreaSelection();
    }

    private void HandlerMarker()
    {
        if (playerMarker && player && wrapperCamera)
        {
            var screenPosition = wrapperCamera.WorldToScreenPoint(player.transform.position);
            playerMarker.position = screenPosition;
        }
    }
    [SerializeField] private LayerMask minimapLayerMask;

    private void HandleAreaSelection()
    {
        if (!TeleMapController._mapActive)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            var ray = wrapperCamera.ScreenPointToRay(Input.mousePosition);
            
            if (Physics.Raycast(ray, out var raycastHit,Mathf.Infinity, minimapLayerMask))
            {
                Debug.Log("Hit something in highlight area");
                var bigArea = raycastHit.collider.GetComponentInParent<BigArea>();
                Debug.Log($"Hit Area: {raycastHit.collider.gameObject}",raycastHit.collider.gameObject);
                HighlightSingleArea(bigArea);
            }
        }
    }

    private void HighlightSingleArea(BigArea selectArea)
    {
        foreach (var area in bigAreasLocation)
        {
            if (area != selectArea)
            {
                area.UnHighlight();
            }
        }

        if (selectArea != null)
        {
            selectArea.Highlight();
            
            var focusCam = selectArea.GetFocusCamera();
            minimapCameraHandler.TryFocusCamera(focusCam);
        }
    }

    public void ResetArea()
    {
        HighlightSingleArea(null);
    }
}
