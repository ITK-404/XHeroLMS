using UnityEngine;

public class SecurityEncodeRunner : MonoBehaviour
{
#if UNITY_EDITOR

    void Start()
    {
        EncodeBaseUrl();
    }
    [ContextMenu("Encode BaseUrl")]
    private void EncodeBaseUrl()
    {
        SecurityConfig.EncodeForCode("https://apis-dev.xheroapp.com");
    }
#endif
}
