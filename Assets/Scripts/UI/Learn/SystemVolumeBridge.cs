using UnityEngine;

public static class SystemVolumeBridge
{
#if UNITY_ANDROID && !UNITY_EDITOR
    static AndroidJavaObject _audioManager;
    static int _streamMusic;
#endif

    public static bool IsSupported =>
#if UNITY_ANDROID && !UNITY_EDITOR
        true;
#else
        false;
#endif

    static void Ensure()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (_audioManager != null) return;

        using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        _audioManager = activity.Call<AndroidJavaObject>("getSystemService", "audio");

        // AudioManager.STREAM_MUSIC = 3
        _streamMusic = 3;
#endif
    }

    /// <summary>0..1</summary>
    public static float GetNormalized()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Ensure();
        int cur = _audioManager.Call<int>("getStreamVolume", _streamMusic);
        int max = _audioManager.Call<int>("getStreamMaxVolume", _streamMusic);
        if (max <= 0) return 1f;
        return Mathf.Clamp01(cur / (float)max);
#else
        // Fallback: không có system volume => dùng volume nội bộ
        return AudioListener.volume;
#endif
    }

    /// <summary>0..1</summary>
    public static void SetNormalized(float normalized)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        Ensure();
        int max = _audioManager.Call<int>("getStreamMaxVolume", _streamMusic);
        int v = Mathf.Clamp(Mathf.RoundToInt(Mathf.Clamp01(normalized) * max), 0, max);

        // flags=0 (không bật UI volume của Android)
        _audioManager.Call("setStreamVolume", _streamMusic, v, 0);
#endif
        // Luôn set thêm nội bộ app để cảm giác đồng bộ
        AudioListener.volume = Mathf.Clamp01(normalized);
    }
}
