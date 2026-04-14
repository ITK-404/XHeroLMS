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

    public static event Action OnPlayerLogout;
    
    public static void RaisePlayerLogout()
    {
        OnPlayerLogout?.Invoke();
    }
}
