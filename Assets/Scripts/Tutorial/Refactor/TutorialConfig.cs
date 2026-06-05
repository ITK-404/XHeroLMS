using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "Tutorial Config",menuName = "SO/Tutorial Sequence Config")]
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

    public List<string> GetListStep()
    {
        List<string> validList = new();
        foreach (var step in StepDatas)
        {
            validList.Add(step.stepId);
        }
        return validList;
    }
}