using UnityEngine;

public class WaitForCourseUIStateTutorialStep : TutorialStepBehaviour
{
    [SerializeField] private LearnUI learnUI;
    [SerializeField] private bool targetActiveState = false;
    
    public override bool IsCompleted()
    {
        return learnUI.GetCourseToggleIsOn() == targetActiveState;
    }
}