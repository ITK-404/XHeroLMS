using System.Runtime.InteropServices;
using UnityEngine;
public class IOSUrlChecker
{
#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern bool _CanOpenURL(string url);
#endif

    public static bool CanOpen(string url)
    {
#if UNITY_IOS && !UNITY_EDITOR
        return _CanOpenURL(url);
#else
        return false;
#endif
    }
}