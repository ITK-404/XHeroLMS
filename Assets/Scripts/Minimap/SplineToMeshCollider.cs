using System;
using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;

public class SplineToMeshCollider : MonoBehaviour
{
    [SerializeField] private SplineContainer splineContainer;

    [SerializeField] private BoxCollider boxCollider;

    private void Awake()
    {
        CalculatorBounds();
    }

    private void CalculatorBounds()
    {
        var spline = splineContainer.Spline;
        if (spline == null) return;
        var bounds = new Bounds();
        var pointCount = spline.Count;
        for (int i = 0; i < pointCount; i++)
        {
            var position = spline[i].Position;
            bounds.Encapsulate(position);
        }

        boxCollider.center = bounds.center;
        boxCollider.size = bounds.size;
    }
}