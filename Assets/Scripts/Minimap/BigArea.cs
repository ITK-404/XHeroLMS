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
    
    private void Awake()
    {
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
    }

    public void UnHighlight()
    {
        lineRenderer.gameObject.SetActive(false);
    }
}