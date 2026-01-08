using DG.Tweening;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Splines;

public class BigArea : MonoBehaviour
{
    private AreaMapLocation location;
    public AreaMapLocation Location => location;
    [SerializeField] private AreaMapData mapData;
    public AreaMapData Data => mapData;
    [Header("Highlight")] 
    [SerializeField] private SplineContainer spline;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private CinemachineCamera focusCamera;

    
    
    private void Awake()
    {
        areaMaterial = lineRenderer.sharedMaterial;
        
        location = GetComponent<AreaMapLocation>();
        LoadForDebug();
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
    }

    public void UnHighlight()
    {
        lineRenderer.gameObject.SetActive(false);
    }

    private Material areaMaterial;
    [SerializeField] private Color normalColor;
    private Tween colorTween;
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
}