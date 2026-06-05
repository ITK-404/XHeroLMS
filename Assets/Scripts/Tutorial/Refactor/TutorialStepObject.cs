using UnityEngine;

public class TutorialStepObject : MonoBehaviour
{
    [SerializeField] private string stepId;

    public string GetStepId()
    {
        return stepId;
    }

    public bool IsStepCheckComplete()
    {
        return true;
    }
}