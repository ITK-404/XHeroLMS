using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class ClassTutorialFlow : FlowBase
{
    [SerializeField] private List<TutorialStepBehaviour> tutorialList = new();    
    protected override FlowNode CreateFlow()
    {
        if (tutorialList == null || tutorialList.Count == 0)
        {
            return null;
        }

        FlowNode startNode = tutorialList[0].CreateTutorialNode();
        FlowNode currentNode = startNode;

        for (int i = 1; i < tutorialList.Count; i++)
        {
            FlowNode nextNode = tutorialList[i].CreateTutorialNode();

            currentNode.AddTransition(NodeResult.Completed, nextNode);

            currentNode = nextNode;
        }

        return startNode;
    }
}
