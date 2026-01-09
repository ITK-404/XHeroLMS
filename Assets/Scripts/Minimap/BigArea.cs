using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Splines;
using Vector3 = UnityEngine.Vector3;

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
        StartCoroutine(WaitForDelay());
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

    private IEnumerator WaitForDelay()
    {
        yield return new WaitForSeconds(2);
        ShowPlotArea();
        RatioCalculation();
    }

    public void HidePlotArea()
    {
        foreach (var plot in plotAreaList)
        {
            plot.Show(false);
        }
    }

    private void RatioCalculation()
    {
        var startPosition = focusCamera.transform.position;
        var distanceForTest = 30;
        foreach (var item in plotAreaList)
        {
            var endPosition = item.transform.position;
            float distance = Vector3.Distance(startPosition, endPosition);

            float distancePoint = distance / distanceForTest;
            float minusRatio = distancePoint / 10;
            float ratio = 1 - minusRatio;
            
            ratio = Mathf.Clamp(ratio, 0.5f, 1);
            item.plotAreaUI.transform.localScale = Vector3.one * ratio;
        }
    }
}