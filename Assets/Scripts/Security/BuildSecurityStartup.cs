using UnityEngine;

public class BuildSilentSecurity : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);

#if !UNITY_EDITOR && !DEVELOPMENT_BUILD

        Debug.unityLogger.logEnabled = false;

        Application.logMessageReceived += OnLogIntercept;
#endif
    }

#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
    private void OnDestroy()
    {
        Application.logMessageReceived -= OnLogIntercept;
    }

    private void OnLogIntercept(string condition, string stackTrace, LogType type)
    {
    }
#endif
}
