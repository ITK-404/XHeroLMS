using System.Collections.Generic;
using UnityEngine;

public static class TravelContext
{
    public static string LastHouseKey = null;

    // Lưu return point theo houseKey
    private static readonly Dictionary<string, (Vector3 pos, Quaternion rot)> returnPoints = new();

    public static void SaveReturnPoint(string houseKey, Vector3 pos, Quaternion rot)
    {
        if (string.IsNullOrEmpty(houseKey)) return;
        returnPoints[houseKey] = (pos, rot);
    }

    public static bool TryGetReturnPoint(string houseKey, out Vector3 pos, out Quaternion rot)
    {
        if (!string.IsNullOrEmpty(houseKey) && returnPoints.TryGetValue(houseKey, out var data))
        {
            pos = data.pos; rot = data.rot; return true;
        }
        pos = default; rot = default; return false;
    }

    public static void ClearLast()
    {
        LastHouseKey = null;
    }
}
