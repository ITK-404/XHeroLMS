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
            {
                jc.CallStatic("stopProxy");
            }

            Debug.Log("[LocalVideoProxy] stopProxy()");
        }
        catch (Exception e)
        {
            Debug.LogWarning("[LocalVideoProxy] Stop failed: " + e.Message);
        }
#endif
    }

    public static string Wrap(string originUrl, int port = DefaultPort)
    {
        if (string.IsNullOrEmpty(originUrl))
        {
            return originUrl;
        }

        string localPrefix = $"http://127.0.0.1:{port}/video?u=";

        // Tránh wrap trùng 2 lần.
        if (originUrl.StartsWith(localPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return originUrl;
        }

        string escaped = Uri.EscapeDataString(originUrl);
        return $"{localPrefix}{escaped}";
    }

    public static string WrapNoCache(string originUrl, int port = DefaultPort)
    {
        if (string.IsNullOrEmpty(originUrl))
        {
            return originUrl;
        }

        string localPrefix = $"http://127.0.0.1:{port}/stream?u=";

        if (originUrl.StartsWith(localPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return originUrl;
        }

        string escaped = Uri.EscapeDataString(originUrl);
        return $"{localPrefix}{escaped}";
    }

    public static bool Preload(string originUrl, long start = 0)
{
#if UNITY_ANDROID && !UNITY_EDITOR
    try
    {
        using (var jc = new AndroidJavaClass(ProxyClass))
        {
            return jc.CallStatic<bool>("preload", originUrl, start);
        }
    }
    catch (Exception e)
    {
        Debug.LogWarning("[LocalVideoProxy] Preload failed: " + e.Message);
        return false;
    }
#else
    return false;
#endif
}

public static long GetCachedUntil(string originUrl)
{
#if UNITY_ANDROID && !UNITY_EDITOR
    try
    {
        using (var jc = new AndroidJavaClass(ProxyClass))
        {
            return jc.CallStatic<long>("getCachedUntil", originUrl);
        }
    }
    catch
    {
        return -1;
    }
#else
    return -1;
#endif
}

public static long GetTotalBytes(string originUrl)
{
#if UNITY_ANDROID && !UNITY_EDITOR
    try
    {
        using (var jc = new AndroidJavaClass(ProxyClass))
        {
            return jc.CallStatic<long>("getTotalBytes", originUrl);
        }
    }
    catch
    {
        return -1;
    }
#else
    return -1;
#endif
}
}
