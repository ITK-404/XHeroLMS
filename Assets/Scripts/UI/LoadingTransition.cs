using UnityEngine;
using UnityEngine.SceneManagement;

public static class LoadingTransition
{
    public static string TargetSceneName;
    public static string PreviousSceneName;
    /// <summary>
    /// Gọi hàm này để chuyển sang LoadingScene.
    /// LoadingScene sẽ đọc TargetSceneName và load async scene đích.
    /// </summary>
    public static void Load(string sceneName)
    {
        PreviousSceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"Previous scene name: " + PreviousSceneName);
        TargetSceneName = sceneName;
        SceneManager.LoadScene("LoadingScene", LoadSceneMode.Additive);
    }
}
