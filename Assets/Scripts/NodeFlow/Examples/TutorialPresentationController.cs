using System;
using System.Collections.Generic;
using UnityEngine;

public class TutorialPresentationController : MonoBehaviour
{
    [SerializeField]
    private List<TutorialStepPresentation> presentations;

    [SerializeField] private Transform newParent;

    private void Start()
    {
        foreach (var item in presentations)
        {
            // assign parent
            item.oldParent = item.Target.transform.parent;
            item.newParent = newParent;
        }
        
        TutorialEventBus.OnEventRaised += TutorialEventBusOnOnEventRaised;
    }

    private void OnDestroy()
    {
        TutorialEventBus.OnEventRaised -= TutorialEventBusOnOnEventRaised;
    }

    private void TutorialEventBusOnOnEventRaised(string eventName)
    {
        Show(eventName);
    }

    private void Init()
    {
        presentations.Clear();  
    }
    
    public void Show(string tutorialStepID)
    {
        foreach (var item in presentations)
        {
            if (item.StepId == tutorialStepID)
            {
                item.ShowTutorial();
            }
            else
            {
                item.HideTutorial();
            }
        }
    }
}