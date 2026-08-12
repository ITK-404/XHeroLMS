using System;
using System.Threading;
using Cysharp.Threading.Tasks;

public class TutorialStepNode : FlowNode
{
    private readonly TutorialStepBehaviour stepBehaviour;

    public TutorialStepNode(
        TutorialStepBehaviour stepBehaviour)
        : base($"Tutorial Step [{stepBehaviour?.name}]")
    {
        this.stepBehaviour = stepBehaviour;
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

        await UniTask.WaitForSeconds(stepBehaviour.delayBeforeEnter, true, PlayerLoopTiming.LastUpdate,
            cancellationToken);
        stepBehaviour.Enter(context);

        try
        {
            while (!stepBehaviour.IsCompleted())
            {
                cancellationToken.ThrowIfCancellationRequested();

                stepBehaviour.Tick(context);

                await UniTask.Yield(
                    PlayerLoopTiming.Update,
                    cancellationToken
                );
            }

            return NodeResult.Completed;
        }
        finally
        {
            stepBehaviour.Exit(context);
        }
    }
}