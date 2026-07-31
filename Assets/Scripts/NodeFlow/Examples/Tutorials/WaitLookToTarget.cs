using DG.Tweening;
using UnityEngine;

public class WaitLookToTarget : TutorialStepBehaviour
{
    [SerializeField] private Transform target;

    private PointClickSystem pointClickSystem;
    private bool isDone = false;

    public override void Enter(CutsceneContext context = null)
    {
        base.Enter(context);
        pointClickSystem = FindFirstObjectByType<PointClickSystem>();
        RotateToPoint();
    }
    [ContextMenu("RotateToPoint")]
    private void RotateToPoint()
    {
        Debug.Log("[ClickCheckPointStep] rorate to check point");
        var direction = target.transform.position - pointClickSystem.transform.position;
        direction.y = 0;
        direction.Normalize();
        
        var targetRotation = Quaternion.LookRotation(direction);
        pointClickSystem.transform.DORotateQuaternion(targetRotation, 3f).OnComplete(() =>
        {
            isDone = true;
        });
    }
    public override bool IsCompleted()
    {
        return isDone;
    }
}