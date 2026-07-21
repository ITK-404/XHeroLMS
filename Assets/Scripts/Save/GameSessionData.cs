using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

[Serializable]
public class GameSessionData
{
    public string UserID;
    public string SaveVersion;
    public static string CurrentVersion => Application.version;
    public DateTime SavedAt { get; set; } = DateTime.Now;
    public SceneLocation SceneLocation;
    [FormerlySerializedAs("CourseData")] public SaveCourseData saveCourseData;
    public static GameSessionData CaptureCurrentState(GameObject player)
    {
        return new GameSessionData
        {
            SaveVersion = Application.version,
            UserID   = TokenStore.UserID,
            SceneLocation = CaptureFromPlayer(player),
            saveCourseData = CaptureCourseData()
        };
    }

    private static SaveCourseData CaptureCourseData()
    {
        var tracker = LessonProgressTracker.Instance;
        if (tracker != null && !string.IsNullOrEmpty(tracker.CourseID))
        {
            SaveCourseData saveCourseData = new();
            saveCourseData.seoId = SeoResolver.seoCourse;
            return saveCourseData;
        }

        return new();
    }
    
    public static SceneLocation CaptureFromPlayer(GameObject player)
    {
        var sceneName = SceneNameAliases.ToSavedSceneName(SceneManager.GetActiveScene().name);
        var sceneLocation = new SceneLocation(sceneName: sceneName, position: player.transform.position,
            rotation: player.transform.rotation);

        return sceneLocation;
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

}

[Serializable]
public class SaveCourseData
{
    // using this id to check
    public string seoId;
}
