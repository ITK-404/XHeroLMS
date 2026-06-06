using System;
using UnityEngine.Serialization;

[Serializable]
public class StepData
{
    public string stepId;
    [FormerlySerializedAs("guid")] public string stepGuid;
    public string stepDescription;
    public StepType stepType;
}