using UnityEngine;
using UnityEngine.Video;

[DefaultExecutionOrder(-1000)]
public class LocalProxyAutoBoot : MonoBehaviour
{
    public bool enableProxyOnAndroid = true;
    public int port = AndroidLocalVideoProxy.DefaultPort;

    private bool started;

    void Awake()
    {
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
        // tùy bạn: nếu muốn tắt khi scene unload
        // AndroidLocalVideoProxy.Stop();\
#endif
    }

    public string GetPlayableUrl(string originUrl)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (enableProxyOnAndroid)
        {
            if (!started) started = AndroidLocalVideoProxy.Start(port);
            return AndroidLocalVideoProxy.Wrap(originUrl, port);
        }
#endif
        return originUrl;
    }
}
