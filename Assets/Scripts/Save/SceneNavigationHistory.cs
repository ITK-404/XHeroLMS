using System.Collections.Generic;
using UnityEngine;

public class SceneNavigationHistory : MonoBehaviour
{
    [SerializeField] private List<string> historyStack = new List<string>();
    [SerializeField] private bool isDebug = false;

    public void Record(string sceneLocation)
    {
        if (sceneLocation == null)
        {
            Debug.LogWarning("[SceneNavigationHistory] Cannot record null scene");
            return;
        }

        if (isDebug)
            Debug.Log($"[SceneNavigationHistory] Record {sceneLocation}");
        historyStack.Add(sceneLocation);
    }

    public string GetPrevious()
    {
        var previousScene = historyStack[historyStack.Count - 1];
        historyStack.RemoveAt(historyStack.Count - 1);
        if (isDebug)
            Debug.Log($"[SceneNavigationHistory] GetPrevious {previousScene}");
        return previousScene;
    }

    public bool HasHistory()
    {
        return historyStack.Count > 0;
    }

    public void ClearHistory()
    {
        if (isDebug)
            Debug.Log("[SceneNavigationHistory] Clear history");
        historyStack.Clear();
    }
}