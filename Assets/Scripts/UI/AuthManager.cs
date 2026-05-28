using UnityEngine;

public class AuthManager : MonoBehaviour
{
    [SerializeField] private AuthView view;
    public string defaultLoadScene = "New Scene";
    public string deleteUserPath   = "/users";
    public string logoutPath       = "/users/logout";
    public string fromPlatform     = "lms3d";

    private void Awake()
    {
        Bind();
    }

    public void Bind()
    {
        string baseUrl     = LmsStore.Instance.baseUrl?.TrimEnd('/');
        string accessToken = TokenStore.AccessToken;

        var deleteApi = new DeleteAccountApi(
            baseUrl:        baseUrl,
            accessToken:    accessToken,
            deleteUserPath: deleteUserPath
        );

        var authHandler = new AuthHandler(
            coroutineRunner:  GameInitializer.Instance,
            baseUrl:          baseUrl,
            logoutPath:       logoutPath,
            fromPlatform:     fromPlatform,
            defaultLoadScene: defaultLoadScene,
            deleteAccountApi: deleteApi
        );

        // Kết nối AuthHandler với AuthView
        view.Bind(authHandler);
    }
}