using System.Collections.Generic;
using UnityEngine;

public class TutorialConfig : ScriptableObject
{
    [SerializeField] private List<StepData> StepDatas = new();

    public const int NON_EXIT_INDEX = -1;
    
    public int GetStepCount() => StepDatas.Count;

    public int GetIndexOfStep(string stepId)
    {
        for (int index = 0; index < StepDatas.Count; index++)
        {
            // hard string compare
            if (stepId == StepDatas[index].stepId)
            {
                return index;
            }
        }

        return NON_EXIT_INDEX;
    }

}