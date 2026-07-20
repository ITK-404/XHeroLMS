using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;


public abstract class FlowNode
{
    private readonly Dictionary<string, FlowNode> transitions = new();

    public string Name { get; }

    protected FlowNode(string name)
    {
        Name = name;
    }

    public void AddTransition(
        string result,
        FlowNode nextNode
    )
    {
        transitions[result] = nextNode;
    }

    public bool TryGetNextNode(
        string result,
        out FlowNode nextNode
    )
    {
        return transitions.TryGetValue(
            result,
            out nextNode
        );
    }

    public abstract UniTask<string> ExecuteAsync(
        CutsceneContext context,
        CancellationToken cancellationToken
    );
}
