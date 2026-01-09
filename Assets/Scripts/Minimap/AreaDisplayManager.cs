using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Serialization;

public class AreaDisplayManager : MonoBehaviour
{
    public static AreaDisplayManager Instance;
    [Header("UI")]
    [SerializeField] private BigArea[] bigAreasLocation;
    [FormerlySerializedAs("bigMapUIPrefab")] 
    [SerializeField] private BigAreaUI bigAreaUIPrefab;
    [SerializeField] private RectTransform playerMarker;

    [Header("UI Plot Area")]
    [SerializeField]private PlotAreaUI plotAreaUIPrefab;
    [Header("Others")] 
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject uiAreaContainer;
    [SerializeField] private GameObject locationAreaContainer;
    [SerializeField] private Camera wrapperCamera;

    [Header("World Space")]
    [SerializeField] private GameObject worldSpaceContainer;
    
    public MinimapCameraHandler minimapCameraHandler;
    private List<BigAreaUI> bigMapUIList = new(); 

    private void Awake()
    {
        Instance = this;
        
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
            var bigMapUI = Instantiate(bigAreaUIPrefab, uiAreaContainer.transform);
            bigMapUI.SetData(bigMap.Data);
            
            var areaUI = bigMapUI.AreaMapUI;
            areaUI.Setup(wrapperCamera, bigMap.Location);
            
            bigMapUIList.Add(bigMapUI);
        }
    }

    public PlotAreaUI CreatePlotAreaUI(AreaMapLocation location)
    {
        var plotAreaUI = Instantiate(plotAreaUIPrefab, worldSpaceContainer.transform);
        plotAreaUI.AreaMapUI.Setup(wrapperCamera, location,false);
        return plotAreaUI;
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
            
            if (Physics.Raycast(ray, out var raycastHit,Mathf.Infinity, minimapLayerMask,QueryTriggerInteraction.Collide))
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
        // neu khong chon vung nao thi active ui len 
        ActiveBigAreaList(selectArea == null);
    }

    public void ResetArea()
    {
        HighlightSingleArea(null);
    }

    private void ActiveBigAreaList(bool isEnable)
    {
        foreach (var ui in bigMapUIList)
        {
            ui.gameObject.SetActive(isEnable);
        }
    }
}