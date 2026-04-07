using System.Collections.Generic;
using UnityEngine;

public class SavePlayerData
{
    public string AccountID;
    public string LoginToken;

    public SceneLocation SceneLocation;

}

public class SceneLocation 
{
    public Vector3 Position;
    public Quaternion Rotation;
    public string SceneName;

    public string Debug()
    {
        return $"SceneName {SceneName} Position: {Position} Rotation {Rotation}";
    }
}

public class RollbackHistory {

}


public class SceneHistory
{
    private Stack<SceneLocation> stack = new Stack<SceneLocation>();

    public void Push(SceneLocation sceneLocation)
    {
        if (sceneLocation == null)
        {
            Debug.Log($"[SceneHistory] Element is null");
            return;
        }
        
        Debug.Log($"[SceneHistory] Push {sceneLocation.Debug()}");
        stack.Push(sceneLocation);
    }

    public SceneLocation Pop()
    {
        var sceneLocation = stack.Pop();
        Debug.Log($"[SceneHistory] Pop {sceneLocation.Debug()}");
        return sceneLocation;
    }
    
    public bool CanGetScene()
    {
        return stack.Count > 0;
    }

    public void Clear()
    {
        Debug.Log("[SceneHistory] Is Clear");
        stack.Clear();
    }
}