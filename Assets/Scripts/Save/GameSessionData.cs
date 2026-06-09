using System;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
public class GameSessionData
{
    public string UserID;
    public string SaveVersion;
    public static string CurrentVersion => Application.version;
    public DateTime SavedAt { get; set; } = DateTime.Now;
    public SceneLocation SceneLocation;
    public CourseData CourseData;

    public bool HasValidScene =>
        SceneLocation != null && !string.IsNullOrWhiteSpace(SceneLocation.SceneName);

    public bool HasCourseData =>
        CourseData != null && !string.IsNullOrWhiteSpace(CourseData.seoId);

    public static GameSessionData CaptureCurrentState(GameObject player)
    {
        return new GameSessionData
        {
            SaveVersion = Application.version,
            UserID   = TokenStore.UserID,
            SceneLocation = CaptureFromPlayer(player),
            CourseData = CaptureCourseData()
        };
    }

    private static CourseData CaptureCourseData()
    {
        var tracker = LessonProgressTracker.Instance;
        if (tracker != null && !string.IsNullOrEmpty(tracker.CourseID))
        {
            string seoId = SeoResolver.seoCourse;

            if (string.IsNullOrWhiteSpace(seoId))
                seoId = SeoResolver.GetSeoCourseByScene(SceneManager.GetActiveScene().name);

            if (!string.IsNullOrWhiteSpace(seoId))
                return new CourseData { seoId = seoId };
        }

        return null;
    }
    
    public static SceneLocation CaptureFromPlayer(GameObject player)
    {
        var sceneName = SceneManager.GetActiveScene().name;
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
public class CourseData
{
    // using this id to check
    public string seoId;
}