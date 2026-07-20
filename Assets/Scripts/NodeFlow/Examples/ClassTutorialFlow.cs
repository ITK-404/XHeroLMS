using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class ClassTutorialFlow : FlowBase
{
    [SerializeField] private Button chairButton;

    protected override FlowNode CreateFlow()
    {
        // block all
        var clickChair = new WaitForTutorialEventNode(TutorialStepId.ClickChair);
        var moveToChair = new WaitForTutorialEventNode(TutorialStepId.MoveToChairComplete);
        var sitDown = new WaitForTutorialEventNode(TutorialStepId.SitDown);
        var openCourse = new WaitForTutorialEventNode(TutorialStepId.OpenCourse);
        var wait15Seconds = new WaitForSecondsNode(15f);
        var closeCourse = new WaitForTutorialEventNode(TutorialStepId.CloseCourse);
        var standUp = new WaitForTutorialEventNode(TutorialStepId.StandUp);
    
        clickChair.AddTransition(NodeResult.Completed, moveToChair);
        moveToChair.AddTransition(NodeResult.Completed, sitDown);
        sitDown.AddTransition(NodeResult.Completed, openCourse);
        sitDown.AddTransition(NodeResult.Completed, openCourse);
        openCourse.AddTransition(NodeResult.Completed, wait15Seconds);
        wait15Seconds.AddTransition(NodeResult.Completed, closeCourse);
        closeCourse.AddTransition(NodeResult.Completed, standUp);

        return clickChair;
    }
}

public static class TutorialStepId
{
    public const string ClickChair = "ClickChair";
    public const string MoveToChairComplete = "MoveToChairComplete";
    public const string SitDown = "SitDown";
    public const string Wait15Seconds = "Wait15Seconds";
    public const string OpenCourse = "OpenCourse";
    public const string CloseCourse = "CloseCourse";
    public const string StandUp = "StandUp";
}

public static class TutorialEventBus
{
    public static event Action<string> OnEventRaised;

    public static void Raise(string eventId)
    {
        OnEventRaised?.Invoke(eventId);
    }
}

public class WaitForTutorialEventNode : FlowNode
{
    private readonly string eventId;

    public WaitForTutorialEventNode(string eventId)
        : base($"Wait Tutorial Event [{eventId}]")
    {
        this.eventId = eventId;
    }

    public override async UniTask<string> ExecuteAsync(
        CutsceneContext context,
        CancellationToken cancellationToken)
    {
        var tcs = new UniTaskCompletionSource();

        void OnEvent(string id)
        {
            if (id == eventId)
            {
                tcs.TrySetResult();
            }
        }

        TutorialEventBus.OnEventRaised += OnEvent;

        try
        {
            using (cancellationToken.Register(() => tcs.TrySetCanceled()))
            {
                await tcs.Task;
            }

            return NodeResult.Completed;
        }
        finally
        {
            TutorialEventBus.OnEventRaised -= OnEvent;
        }
    }
}

public class WaitForSecondsNode : FlowNode
{
    private readonly float duration;

    public WaitForSecondsNode(
        float duration)
        : base("WaitForSecondsNode")
    {
        this.duration = duration;
    }

    public override async UniTask<string> ExecuteAsync(
        CutsceneContext context,
        CancellationToken cancellationToken)
    {
        if (duration <= 0f)
        {
            return NodeResult.Completed;
        }

        await UniTask.Delay(
            TimeSpan.FromSeconds(duration),
            cancellationToken: cancellationToken
        );

        return NodeResult.Completed;
    }
}