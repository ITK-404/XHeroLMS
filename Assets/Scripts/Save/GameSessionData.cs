using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

[Serializable]
public class GameSessionData
{
    public string AccountID;
    public string LoginToken;

    public SceneLocation SceneLocation;
    
    public static GameSessionData CaptureCurrentState(GameObject player)
    {
        return new GameSessionData
        {
            AccountID   = TokenStore.UserID,
            LoginToken  = TokenStore.AccessToken,
            SceneLocation = SceneLocation.CaptureFromPlayer(player)
        };
    }
}
[Serializable]
public class SceneLocation 
{
    // PRIVATE
    public Vector3 Position;
    public Quaternion Rotation;
    public string SceneName;

    public SceneLocation(string sceneName, Vector3 position, Quaternion rotation)
    {
        this.SceneName = sceneName;
        this.Position = position;
        this.Rotation = rotation;
    }


    public string Debug()
    {
        return $"SceneName {SceneName} Position: {Position} Rotation {Rotation}";
    }

    public static SceneLocation CaptureFromPlayer(GameObject player)
    {
        var sceneName = SceneManager.GetActiveScene().name;
        var sceneLocation = new SceneLocation(sceneName: sceneName, position: player.transform.position,
            rotation: player.transform.rotation);

        return sceneLocation;
    }
}