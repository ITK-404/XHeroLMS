using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TutorialFlowBuilder : MonoBehaviour
{
    [Tooltip("This component will search all behaviour in side")]
    [SerializeField] private List<TutorialStepBehaviour> behaviours;
    [SerializeField] private bool autoFindAtAwake = true;
        
    private void Awake()
    {
        if (autoFindAtAwake)
        {
            behaviours = GetComponentsInChildren<TutorialStepBehaviour>().ToList();
        }
    }

    public FlowNode BuildFlowNode() => BuildNode(GetStepsBehaviour());

    private List<TutorialStepBehaviour> GetStepsBehaviour()
    {
        return behaviours;
    }

    private FlowNode BuildNode(List<TutorialStepBehaviour> tutorialList)
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