using System.Collections.Generic;
using UnityEngine;

public class SceneNavigationHistory 
{
    private readonly Stack<SceneLocation> historyStack = new Stack<SceneLocation>();
    private readonly bool isDebug = false;

    public SceneNavigationHistory(bool isDebug)
    {
        this.isDebug = isDebug;
    }
    
    public void Record(SceneLocation sceneLocation)
    {
        if (sceneLocation == null)
        {
            Debug.LogWarning("[SceneNavigationHistory] Cannot record null scene");
            return;
        }
        if(isDebug)
            Debug.Log($"[SceneNavigationHistory] Record {sceneLocation.Debug()}");
        historyStack.Push(sceneLocation);
    }

    public SceneLocation GetPrevious()
    {
        var previousScene = historyStack.Pop();
        if(isDebug)
            Debug.Log($"[SceneNavigationHistory] GetPrevious {previousScene.Debug()}");
        return previousScene;
    }

    public bool HasHistory()
    {
        return historyStack.Count > 0;
    }

    public void ClearHistory()
    {
        if(isDebug)
            Debug.Log("[SceneNavigationHistory] Clear history");
        historyStack.Clear();
    }
}