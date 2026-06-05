using System.Collections.Generic;
using UnityEngine;

public class TutorialStepManager : MonoBehaviour
{
    [SerializeField] private TutorialConfig config;
    [SerializeField] private int currentStepIndex = 0;

    private List<TutorialStepObject> stepObjects = new();

   
    private int maxTutorialStepCount => config != null ? config.GetStepCount(): 0;


    private bool IsCurrentActiveStep(TutorialStepObject stepObject)
    {
        if (currentStepIndex < maxTutorialStepCount)
        {
            Debug.Log($"TutorialStepManager tutorial is complete, please reset", stepObject);
            return false;
        }
        
        int stepIndex = config.GetIndexOfStep(stepObject.GetStepId());
        if (stepIndex == TutorialConfig.NON_EXIT_INDEX)
        {
            Debug.Log($"TutorialStepManager cannot set step that does not exit", stepObject);
            return false;
        }

        if (stepIndex != currentStepIndex)
        {
            Debug.Log($"TutorialStepManager this step check not equal current step", stepObject);
            return false;
        }

        return true;
    }
    
    public bool TrySetStepComplete(TutorialStepObject stepObject)
    {
        if (!IsCurrentActiveStep(stepObject)) return false;

        currentStepIndex++;
        return true;
    }

    private void ShowNextStep(int currentStepIndex)
    {
        // info to view to show next step ma
    }

    public void SetTutorialConfig(TutorialConfig _config)
    {
        config = _config;
        currentStepIndex = 0;
    }
}