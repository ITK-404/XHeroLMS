using System;
using DG.Tweening;
using Pathfinding;
using UnityEngine;

public class InteractionPoint : MonoBehaviour
{
    public Transform standPosition;
    public void Interact(PointClickSystem pointClickSystem)
    {
        pointClickSystem.MoveToPosition(transform.position,false);
        pointClickSystem.transform.DORotateQuaternion(standPosition.rotation,2f);
    }
    
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawRay(standPosition.position, standPosition.forward);
    }
}