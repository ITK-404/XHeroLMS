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