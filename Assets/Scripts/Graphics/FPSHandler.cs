using UnityEngine;

public static class FPSHandler 
{
    private const string SAVE_FPS_KEY = "save_target_fps";
    private const int DEFAULT_FPS = 60;

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
}