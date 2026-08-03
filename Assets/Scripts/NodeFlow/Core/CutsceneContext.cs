using System.Collections.Generic;
using UnityEngine;

public class CutsceneContext
{
    private readonly Dictionary<string, object> values = new();

    public void Set<T>(string key, T value)
    {
        Debug.Log($"[CutsceneContext] Set Value {key}");
        values[key] = value;
    }

    public bool TryGet<T>(string key, out T value)
    {
        Debug.Log($"[CutsceneContext] Try Get Value {key}");
        
        if (values.TryGetValue(key, out object rawValue)
            && rawValue is T typedValue)
        {
            value = typedValue;
        Debug.Log($"[CutsceneContext] Try Get Value {key} complete");
            
            return true;
        }
        Debug.Log($"[CutsceneContext] Try Get Value {key} fall");

        value = default;
        return false;
    }

    public T Get<T>(string key, T defaultValue = default)
    {
        return TryGet<T>(key, out T value)
            ? value
            : defaultValue;
    }
}