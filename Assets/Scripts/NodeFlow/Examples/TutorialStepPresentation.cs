using System;
using UnityEngine;

[Serializable]
public class TutorialStepPresentation
{
    [Dropdown(typeof(TutorialStepId))]
    public string StepId;
    public RectTransform Target;
    public bool ShowHighlight;
    public bool ShowArrow;
    public string Instruction;

    [HideInInspector] public Transform oldParent;
    [HideInInspector] public Transform newParent;
    
    private bool isChangedParent = false;
    public void ShowTutorial()
    {
        Target.transform.SetParent(newParent,true);
        isChangedParent = true;
    }

    public void HideTutorial()
    {
        if (!isChangedParent)
        {
            return;
        }

        isChangedParent = false;
        Target.transform.SetParent(oldParent,true);
    }
}