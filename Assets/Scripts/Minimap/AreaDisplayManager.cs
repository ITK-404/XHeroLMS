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
        
        var ray = wrapperCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out var raycastHit, minimapLayerMask))
        {
            if (raycastHit.collider.TryGetComponent(out BigArea bigArea))
            {
                HighlightSingleArea(bigArea);
            }
        }
    }

    private void HighlightSingleArea(BigArea selectArea)
    {
        foreach (var area in bigAreasLocation)
        {
            if(area == selectArea)
            {
                area.Highlight();
            }
            else
            {
                area.UnHighlight();
            }     
        }
    }
}
