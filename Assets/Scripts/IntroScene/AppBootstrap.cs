using UnityEngine;

public class AppBootstrap : MonoBehaviour
{
    void Awake()
    {
#if UNITY_ANDROID || UNITY_IOS
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;
#endif
        DontDestroyOnLoad(gameObject);
    }
}
