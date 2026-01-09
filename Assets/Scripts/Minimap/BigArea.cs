using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Splines;

public class BigArea : MonoBehaviour
{
    [SerializeField] private AreaMapData mapData;
    [Header("Highlight")] 
    [SerializeField] private SplineContainer spline;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private CinemachineCamera focusCamera;
    [SerializeField] private Color normalColor;
    private Tween colorTween;
    private Material areaMaterial;

    [SerializeField] private GameObject plotContainer;
    private AreaMapLocation location;
    
    public AreaMapData Data => mapData;
    public AreaMapLocation Location => location;
    
    private void Awake()
    {
        areaMaterial = lineRenderer.sharedMaterial;
        
        location = GetComponent<AreaMapLocation>();
        LoadForDebug();
    }

    private void Start()
    {
        SetupPlots();
    }
    
    [ContextMenu("Load For Debug")]
    private void LoadForDebug()
    {
        if (mapData != null)
        {
            gameObject.name = "Big Area: " + mapData.displayName;
        }
    }

    public SplineContainer GetSpline()
    {
        return spline;
    }

    public void Highlight()
    {
        lineRenderer.gameObject.SetActive(true);
        HandleColor();
        ShowPlotArea();
    }

    public void UnHighlight()
    {
        lineRenderer.gameObject.SetActive(false);
        HidePlotArea();
    }

    private void HandleColor()
    {
        colorTween?.Kill();

        float factor = Mathf.Pow(2, 3);
        var baseColor = normalColor * factor;
    
        Color startColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0);
        Color finalColor = new Color(baseColor.r, baseColor.g, baseColor.b, 1);

        areaMaterial.SetColor("_Color", startColor);
    
        colorTween = DOVirtual.Color(startColor, finalColor, 1, value =>
            {
                areaMaterial.SetColor("_Color", value);
            })
            .SetLoops(2, LoopType.Yoyo);
    }
    
    public CinemachineCamera GetFocusCamera()
    {
        return focusCamera;
    }

    private List<PlotArea> plotAreaList = new();
    
    private void SetupPlots()
    {
        plotAreaList.Clear();
        plotAreaList = GetComponentsInChildren<PlotArea>().ToList();

        foreach (var plot in plotAreaList)
        {
            // setup UI
            var plotAreaUI = AreaDisplayManager.Instance.CreatePlotAreaUI(plot.Location);
            // using for API linking
            var plotData = mapData.GetPlotAreaData(plot.seo_url);
            plot.plotAreaUI = plotAreaUI;
            plot.plotAreaData = plotData;

            plot.Initialize();
        }
        
        HidePlotArea();
    }

    public void ShowPlotArea()
    {
        foreach (var plot in plotAreaList)
        {
            plot.Show(true);
        }
    }

    public void HidePlotArea()
    {
        foreach (var plot in plotAreaList)
        {
            plot.Show(false);
            
        }
    }
}