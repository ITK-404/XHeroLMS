using UnityEngine;

public class WaitForActiveFirstLessonStep : TutorialStepBehaviour
{
    [SerializeField] private bool isActive = false;
    public override bool IsCompleted()
    {
        return isActive;
    }
}