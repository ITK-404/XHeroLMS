using System;
using System.Threading;
using Cysharp.Threading.Tasks;

public class TutorialStepNode : FlowNode
{
    private readonly TutorialStepBehaviour stepBehaviour;
    private readonly float exitDelay;

    public TutorialStepNode(
        TutorialStepBehaviour stepBehaviour,
        float exitDelay = 0f)
        : base($"Tutorial Step [{stepBehaviour?.name}]")
    {
        this.stepBehaviour = stepBehaviour;
        this.exitDelay = exitDelay;
    }

    public override async UniTask<string> ExecuteAsync(
        CutsceneContext context,
        CancellationToken cancellationToken)
    {
        if (stepBehaviour == null)
        {
            throw new InvalidOperationException(
                $"{nameof(TutorialStepNode)} requires a TutorialStepBehaviour."
            );
        }

        stepBehaviour.Enter();

        try
        {
            while (!stepBehaviour.IsCompleted())
            {
                cancellationToken.ThrowIfCancellationRequested();

                stepBehaviour.Tick();

                await UniTask.Yield(
                    PlayerLoopTiming.Update,
                    cancellationToken
                );
            }

            if (exitDelay > 0f)
            {
                await UniTask.Delay(
                    TimeSpan.FromSeconds(exitDelay),
                    cancellationToken: cancellationToken
                );
            }

            return NodeResult.Completed;
        }
        finally
        {
            stepBehaviour.Exit();
        }
    }
}