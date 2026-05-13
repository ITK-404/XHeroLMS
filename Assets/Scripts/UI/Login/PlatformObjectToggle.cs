using System.Collections.Generic;
using UnityEngine;

public class PlatformObjectToggle : MonoBehaviour
{
    public List<GameObject> targetObjects = new List<GameObject>();

    private void Awake()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        SetActiveAll(true);
#elif UNITY_IOS && !UNITY_EDITOR
        SetActiveAll(true);
#else
        SetActiveAll(true);
#endif
    }

    private void SetActiveAll(bool value)
    {
        if (targetObjects == null) return;

        foreach (var go in targetObjects)
        {
            if (go != null)
                go.SetActive(value);
        }
    }
}
