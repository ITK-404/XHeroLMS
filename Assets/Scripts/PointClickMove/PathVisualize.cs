using System;
using System.Collections.Generic;
using Pathfinding;
using UnityEngine;

public class PathVisualize : MonoBehaviour
{
    [SerializeField] private LineRenderer _lineRenderer;
    [SerializeField] private IAstarAI agent;

    private List<Vector3> paths = new();
    private bool state;

    private void Awake()
    {
        agent = GetComponent<IAstarAI>();
    }

    private void Update()
    {
        if (_lineRenderer == null)
            return;

        if (Input.GetKeyDown(KeyCode.J))
        {
            agent.GetRemainingPath(paths, out state);
            // agent.hasPath
            _lineRenderer.SetPositions(paths.ToArray());
        }
        
    }
    
    
}