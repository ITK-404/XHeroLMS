using System;
using UnityEngine;

public static class AndroidLocalVideoProxy
{
    public const int DefaultPort = 18080;
    private const string ProxyClass = "com.unity.localproxy.LocalVideoProxy";

    public static bool Start(int port = DefaultPort)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            Debug.Log("[LocalVideoProxy] Load class: " + ProxyClass);
            using (var jc = new AndroidJavaClass(ProxyClass))
            {
                bool ok = jc.CallStatic<bool>("startProxy", port);
                Debug.Log($"[LocalVideoProxy] startProxy({port}) => {ok}");
                return ok;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[LocalVideoProxy] Start failed: " + e);
            return false;
        }
#else
        return false;
#endif
    }

    public static void Stop()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (var jc = new AndroidJavaClass(ProxyClass))
                jc.CallStatic("stopProxy");
        }
        catch { }
#endif
    }

    public static string Wrap(string originUrl, int port = DefaultPort)
    {
        var escaped = Uri.EscapeDataString(originUrl);
        return $"http://127.0.0.1:{port}/video?u={escaped}";
    }
}
