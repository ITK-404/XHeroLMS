using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class WaitClickCheckPointStep : TutorialStepBehaviour
{
    [SerializeField] private ChairCheckPoint tutorialCheckPoint;
    [SerializeField] private Button chairCheckPoint;
    private PointClickSystem pointClickSystem;  

    private bool isClick = false;
    
    private void Awake()
    {
        chairCheckPoint.onClick.AddListener(TryClickToChairCheckPoint);
    }

    private void OnDestroy()
    {
        chairCheckPoint.onClick.AddListener(TryClickToChairCheckPoint);
    }

    private void Start()
    {
        chairCheckPoint.GetComponent<UIFollowFirstChairCheckPoint>().SetTarget(tutorialCheckPoint);
    }

    public override void Enter()
    {
        pointClickSystem = FindFirstObjectByType<PointClickSystem>();
        base.Enter();
        RotateToPoint();
    }

    private void RotateToPoint()
    {
        Debug.Log("[ClickCheckPointStep] rorate to check point");
        var direction = tutorialCheckPoint.transform.position - pointClickSystem.transform.position;
        direction.y = 0;
        direction.Normalize();
        
        var targetRotation = Quaternion.LookRotation(direction);
        pointClickSystem.transform.DORotateQuaternion(targetRotation, 3f);
    }

    private void TryClickToChairCheckPoint()
    {
        // find tutorial check point
        pointClickSystem.MoveToChairCheckPoint(tutorialCheckPoint);
        isClick = true;
    }

    public override bool IsCompleted()
    {
        return isClick;
    }
}