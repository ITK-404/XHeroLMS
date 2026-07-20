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
}