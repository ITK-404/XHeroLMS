using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class WaitClickCheckPointStep : TutorialStepBehaviour
{
    [SerializeField] private ChairCheckPoint tutorialCheckPoint;
    [SerializeField] private Button chairCheckPoint;
    [SerializeField] private FocusTutorialTest focusTutorialTest;
    private PointClickSystem pointClickSystem;  

    private bool isClick = false;
    
    private void Awake()
    {
    }

    private void OnDestroy()
    {
    }

    private void Start()
    {
        chairCheckPoint.GetComponent<UIFollowFirstChairCheckPoint>().SetTarget(tutorialCheckPoint);
    }

    public override void Enter(CutsceneContext context = null)
    {
        base.Enter(context);
        if (context != null && context.TryGet(nameof(ClassTutorialFlow), out ClassTutorialFlow value))
        {
            value.ShowBlockPanel(false, 0);
        }
        chairCheckPoint.onClick.AddListener(TryClickToChairCheckPoint);
        if(pointClickSystem == null)
            pointClickSystem = FindFirstObjectByType<PointClickSystem>();
        RotateToPoint();
        focusTutorialTest?.Enable();
    }

    public override void Exit(CutsceneContext context = null)
    {
        base.Exit(context);
        if (context != null && context.TryGet(nameof(ClassTutorialFlow), out ClassTutorialFlow value))
        {
            value.ShowBlockPanel(true, 0);
        }
        chairCheckPoint.onClick.RemoveListener(TryClickToChairCheckPoint);
        focusTutorialTest?.Disable();
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