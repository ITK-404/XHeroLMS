using UnityEngine;

public class SavePlayerData
{
    public string AccountID;
    public string LoginToken;

    public SceneLocation SceneLocation;

}

public class SceneLocation : MonoBehaviour
{
    public Vector3 Position;
    public Quaternion Rotation;
    public string SceneName;
}

public class RollbackHistory {

}