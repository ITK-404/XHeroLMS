using UnityEngine;
using UnityEngine.SceneManagement;

public static class LoadingTransition
{
    public static string TargetSceneName;

    /// <summary>
    /// Gọi hàm này để chuyển sang LoadingScene.
    /// LoadingScene sẽ đọc TargetSceneName và load async scene đích.
    /// </summary>
    public static void Load(string sceneName)
    {
        TargetSceneName = sceneName;
        SceneManager.LoadScene("LoadingScene");
    }
}
