using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[CreateAssetMenu(fileName = "Tutorial Config", menuName = "SO/Tutorial Sequence Config")]
public class TutorialConfig : ScriptableObject
{
    [SerializeField] private List<StepData> StepDatas = new();

    public const int NON_EXIT_INDEX = -1;

    public int GetStepCount() => StepDatas.Count;

    public int GetIndexOfStep(string guid)
    {
        for (int index = 0; index < StepDatas.Count; index++)
        {
            if (guid == StepDatas[index].stepGuid)
                return index;
        }
        return NON_EXIT_INDEX;
    }

    public List<string> GetListStep()
    {
        return StepDatas.Select(s => s.stepId).ToList();
    }

    public string GetGuidAtIndex(int index)
    {
        if (index < 0 || index >= StepDatas.Count) return string.Empty;
        return StepDatas[index].stepGuid;
    }

    public int GetIndexOfGuid(string guid)
    {
        return StepDatas.FindIndex(s => s.stepGuid == guid);
    }

    private void OnValidate()
    {
        // Auto gen GUID nếu trống
        foreach (StepData step in StepDatas)
        {
            if (string.IsNullOrEmpty(step.stepGuid))
                step.stepGuid = System.Guid.NewGuid().ToString();
        }

        // Warn stepId trùng
        var duplicateIds = StepDatas
            .GroupBy(s => s.stepId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);
        foreach (var dup in duplicateIds)
            Debug.LogWarning($"TutorialConfig: StepId '{dup}' bị trùng!", this);

        // Warn stepId trống
        for (int i = 0; i < StepDatas.Count; i++)
        {
            if (string.IsNullOrEmpty(StepDatas[i].stepId))
                Debug.LogWarning($"TutorialConfig: StepData[{i}] chưa có stepId!", this);
        }
    }
}