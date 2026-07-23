using System;
using UnityEngine;

public class WaitMoveToTargetCheckPoint : TutorialStepBehaviour
{
    [SerializeField] private ChairCheckPoint targetCheckPoint;
    [SerializeField] private PointClickSystem pointClickSystem;
    [SerializeField] private bool isDoneTest = false;
    [SerializeField] private float stoppingDistance = 2f;

    public override bool IsCompleted()
    {
        return IsNearTarget() && SameWithCurrentNearestChair();
    }

    private bool SameWithCurrentNearestChair()
    {
        var currentCheckPoint = PlayerChairManager.Instance.currentCheckPoint;
        return currentCheckPoint == targetCheckPoint;
    }

    private bool IsNearTarget()
    {
        var targetPos = targetCheckPoint.transform.position;
        var currentPos = pointClickSystem.transform.position;
        return Vector3.Distance(targetPos, currentPos) < stoppingDistance;
    }
}