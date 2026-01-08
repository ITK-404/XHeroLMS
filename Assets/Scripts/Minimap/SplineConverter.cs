using System.Linq;
using UnityEngine;
using UnityEngine.Splines;
public class SplineConverter : MonoBehaviour
{
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private LineRenderer lineRenderer;

    private void Awake()
    {
        UpdateLineRendererRaw();
    }
    
    private void UpdateLineRendererRaw()
    {
        if (splineContainer == null || lineRenderer == null) return;
        if (splineContainer.Splines.Count == 0) return;
        var knots = splineContainer.Spline.Knots.ToArray(); 
    
        int knotCount = knots.Length;
        lineRenderer.positionCount = knotCount;

        for (int i = 0; i < knotCount; i++)
        {
            Vector3 localPos = knots[i].Position;
            lineRenderer.SetPosition(i, splineContainer.transform.TransformPoint(localPos));
        }
    
        lineRenderer.loop = splineContainer.Spline.Closed;
    }
}