using System;
using UnityEngine;

public class EventHub
{
    public static event Action<int> OnExamIndexClampChanged;
    public static void RaiseExamClampItem(int score)
    {
        OnExamIndexClampChanged?.Invoke(score);
    }
    public static event Action<int> OnExamIndexCenterChanged;

    public static void RaiseExamCenterItem(int score)
    {
        OnExamIndexCenterChanged?.Invoke(score);
    }

    public static Action<string> OnSendTutorialStep;

    public static void RaiseSendTutorialStep(string stepID)
    {
        if (OnSendTutorialStep != null)
        {
            OnSendTutorialStep.Invoke(stepID);
        }
    }

}
