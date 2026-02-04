using System;
using UnityEngine;

public class AreaMapLocation : MonoBehaviour
{
    // hold world space
    // handle bound of space
    public virtual Vector3 GetItemWorldPosition()
    {
        return transform.position;
    }
}