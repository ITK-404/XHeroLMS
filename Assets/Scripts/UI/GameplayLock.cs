using System.Collections.Generic;
using System.Text;
using UnityEngine;

[System.Flags]
public enum GameplayLockTarget
{
    None      = 0,
    Movement  = 1 << 0,
    Interact  = 1 << 1,
    Camera    = 1 << 2,

    All = Movement | Interact | Camera
}

public enum GameplayLockReason
{
    Dialog,
    Cutscene,
    UI,
    Animation,
    Loading
}

public static class GameplayLock
{
    private static readonly Dictionary<GameplayLockReason, GameplayLockTarget> locks = new();

    public static void Lock(GameplayLockReason reason, GameplayLockTarget target)
    {
        locks[reason] = target;
    }

    public static void Unlock(GameplayLockReason reason)
    {
        locks.Remove(reason);
    }

    public static void Clear()
    {
        locks.Clear();
    }

    public static bool IsLocked(GameplayLockTarget target)
    {
        foreach (var pair in locks)
        {
            if ((pair.Value & target) != 0)
                return true;
        }

        return false;
    }

    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    public static void LogState()
    {
        if (locks.Count == 0)
        {
            Debug.Log("[GameplayLock] No active locks.");
            return;
        }

        var builder = new StringBuilder();
        builder.AppendLine("[GameplayLock] Active Locks:");

        foreach (var pair in locks)
        {
            builder.AppendLine($"- {pair.Key} : {pair.Value}");
        }

        Debug.Log(builder.ToString());
    }
}
