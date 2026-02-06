using System;
using UnityEngine;

public class DoorBlurControl : MonoBehaviour
{
    [SerializeField] private Material doorBlurMaterial;
    [SerializeField] private Transform target;
    [SerializeField] private float min = 5, max = 7;

    [SerializeField] private float distance;
    [SerializeField] private float normalize;
    private void Update()
    {
        if (target == null)
        {
            return;
        }

        if (doorBlurMaterial == null)
        {
            return;
        }
        
        distance = Vector3.Distance(transform.position, target.position);
        normalize = 1 - Mathf.InverseLerp(min, max, distance);
        doorBlurMaterial.SetFloat("_Normalize", normalize);
    }
}
