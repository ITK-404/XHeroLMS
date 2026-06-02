using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class LocalProxyAutoBoot : MonoBehaviour
{
    public bool enableProxyOnAndroid = true;
    public int port = AndroidLocalVideoProxy.DefaultPort;

    [Header("Lifecycle")]
    public bool dontDestroyOnLoad = true;

    private static bool started;

    void Awake()
    {
        if (dontDestroyOnLoad)
        {
            DontDestroyOnLoad(gameObject);
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (enableProxyOnAndroid && !started)
        {
            started = AndroidLocalVideoProxy.Start(port);
        }
#endif
    }

    void OnDestroy()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        // Không tự Stop khi unload scene để tránh video đang stream bị ngắt.
        // Nếu muốn tắt thật sự khi thoát app, dùng OnApplicationQuit().
#endif
    }

    void OnApplicationQuit()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (started)
        {
            AndroidLocalVideoProxy.Stop();
            started = false;
        }
#endif
    }

    public string GetPlayableUrl(string originUrl)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (enableProxyOnAndroid)
        {
            if (!started)
            {
                started = AndroidLocalVideoProxy.Start(port);
            }

            if (started)
            {
                return AndroidLocalVideoProxy.Wrap(originUrl, port);
            }
        }
#endif

        return originUrl;
    }
    public bool EnsureStarted()
{
#if UNITY_ANDROID && !UNITY_EDITOR
    if (!enableProxyOnAndroid)
        return false;

    if (!started)
        started = AndroidLocalVideoProxy.Start(port);

    return started;
#else
    return false;
#endif
}

public bool Preload(string originUrl, long start = 0)
{
#if UNITY_ANDROID && !UNITY_EDITOR
    if (!enableProxyOnAndroid)
        return false;

    if (!EnsureStarted())
        return false;

    return AndroidLocalVideoProxy.Preload(originUrl, start);
#else
    return false;
#endif
}

public long GetCachedUntil(string originUrl)
{
#if UNITY_ANDROID && !UNITY_EDITOR
    if (!enableProxyOnAndroid)
        return -1;

    return AndroidLocalVideoProxy.GetCachedUntil(originUrl);
#else
    return -1;
#endif
}

public long GetTotalBytes(string originUrl)
{
#if UNITY_ANDROID && !UNITY_EDITOR
    if (!enableProxyOnAndroid)
        return -1;

    return AndroidLocalVideoProxy.GetTotalBytes(originUrl);
#else
    return -1;
#endif
}
}