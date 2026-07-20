using System.Collections.Generic;

public class CutsceneContext
{
    private readonly Dictionary<string, object> values = new();

    public void Set<T>(string key, T value)
    {
        values[key] = value;
    }

    public bool TryGet<T>(string key, out T value)
    {
        if (values.TryGetValue(key, out object rawValue)
            && rawValue is T typedValue)
        {
            value = typedValue;
            return true;
        }

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