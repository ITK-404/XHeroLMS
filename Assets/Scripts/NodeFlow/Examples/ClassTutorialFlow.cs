using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class ClassTutorialFlow : FlowBase
{
    [SerializeField] private List<TutorialStepBehaviour> tutorialList = new();

    protected override void Awake()
    {
        base.Awake();
        tutorialList = GetComponentsInChildren<TutorialStepBehaviour>().ToList();
    }

    private void Start()
    {
        RunFlow().Forget();
    }

    protected override FlowNode CreateFlow()
    {
        if (tutorialList == null || tutorialList.Count == 0)
        {
            Debug.LogWarning($"[{GetType().Name}] Tutorial list is empty.");
            return null;
        }

        Debug.Log($"[{GetType().Name}] Create tutorial flow. Total Steps: {tutorialList.Count}");

        FlowNode startNode = tutorialList[0].CreateTutorialNode();
        Debug.Log($"Start Node: {startNode.Name}");

        FlowNode currentNode = startNode;

        for (int i = 1; i < tutorialList.Count; i++)
        {
            FlowNode nextNode = tutorialList[i].CreateTutorialNode();

            Debug.Log(
                $"Link [{i - 1}] {currentNode.Name} -> [{i}] {nextNode.Name}"
            );

            currentNode.AddTransition(NodeResult.Completed, nextNode);
            currentNode = nextNode;
        }

        Debug.Log($"Tutorial flow created successfully.");

        return startNode;
    }
}