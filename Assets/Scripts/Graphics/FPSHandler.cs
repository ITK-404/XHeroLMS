using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class FPSHandler 
{
    private const string SAVE_FPS_KEY = "save_target_fps";
    private const int DEFAULT_FPS = 60;
    private const int LOWEST_FPS = 30;
    private const int LOADING_FPS = 15;

    public static int CurrentFPS { get; private set; }
    
    public static void SetFPS(int fps)
    {
        CurrentFPS = fps;
        ApplyFPS();
        Save();
    }

    public static void ApplyFPS()
    {
        Application.targetFrameRate = CurrentFPS;
        Debug.Log($"FPS Handler: Change fps to {CurrentFPS}");
    }

    public static void Save()
    {
        PlayerPrefs.SetInt(SAVE_FPS_KEY, CurrentFPS);
        PlayerPrefs.Save();
    }

    public static void Load()
    {
        CurrentFPS = PlayerPrefs.GetInt(SAVE_FPS_KEY, DEFAULT_FPS);
    }

    public static void SetLoadingFps()
    {
        Application.targetFrameRate = LOADING_FPS;
        Debug.Log($"FPS Handler: Change fps to {LOADING_FPS}");
    }

    public static void SetDefaultFrameRate()
    {
        Application.targetFrameRate = CurrentFPS;
        Debug.Log($"FPS Handler: Change fps to {CurrentFPS}");
    } 
    
    public static void SetLowestFrameRate()
    {
        Application.targetFrameRate = LOWEST_FPS;
        Debug.Log($"FPS Handler: Change fps to {LOWEST_FPS}");
    }
}

public class FPSSceneHandle : IDisposable
{
    // Đây là các scene loading
    private readonly HashSet<string> loadingScenes = new() { "IntroScene", "LoadingScene" };

    public FPSSceneHandle()
    {
        SceneManager.activeSceneChanged += CheckAfterLoad;
    }

    public void Dispose()
    {
        SceneManager.activeSceneChanged -= CheckAfterLoad;
    }

    private void CheckAfterLoad(Scene current, Scene next)
    {
        FpsLimitCheck(next.name);
    }

    public void Init()
    {
        FpsLimitCheck(SceneManager.GetActiveScene().name);
    }

    private void FpsLimitCheck(string sceneName)
    {
        if (loadingScenes.Contains(sceneName))
            FPSHandler.SetLoadingFps();
        else
            FPSHandler.SetDefaultFrameRate();
    }
}