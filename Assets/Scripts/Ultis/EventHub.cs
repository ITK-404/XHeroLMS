using System;
using System.Collections.Generic;
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

    public static event Action OnPlayerDeleteAccount;
    
    public static void RaisePlayerDeleteAccount()
    {
        OnPlayerLogout?.Invoke();
    }
}


public interface ICommand { }

public static class SignalBus
{
    private static readonly Dictionary<Type, List<object>> handlers = new();

    public static void Subscribe<T>(Action<T> handler) where T : ICommand
    {
        var type = typeof(T);
        if (!handlers.ContainsKey(type))
            handlers[type] = new List<object>();
        handlers[type].Add(handler);
    }

    public static void Unsubscribe<T>(Action<T> handler) where T : ICommand
    {
        var type = typeof(T);
        if (handlers.ContainsKey(type))
            handlers[type].Remove(handler);
    }

    public static void Send<T>(T command) where T : ICommand
    {
        var type = typeof(T);
        if (!handlers.ContainsKey(type)) return;
        foreach (var h in handlers[type])
            (h as Action<T>)?.Invoke(command);
    }

    public static void Clear() => handlers.Clear();
}