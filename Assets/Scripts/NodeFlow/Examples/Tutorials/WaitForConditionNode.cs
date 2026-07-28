using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class WaitForConditionNode : FlowNode
{
    private readonly Func<bool> condition;

    public WaitForConditionNode(
        string name,
        Func<bool> condition)
        : base(name)
    {
        this.condition = condition;
    }

    public override async UniTask<string> ExecuteAsync(
        CutsceneContext context,
        CancellationToken cancellationToken)
    {
        if (condition == null)
        {
            Debug.LogError($"[{Name}] Condition is null.");
            return NodeResult.Cancel;
        }

        await UniTask.WaitUntil(
            condition,
            cancellationToken: cancellationToken);

        return NodeResult.Completed;
    }
}