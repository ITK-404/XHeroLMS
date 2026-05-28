using System;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(-999)]
public class PlayerPanelUI : MonoBehaviour
{
    public static PlayerPanelUI Instance;

    // ──────────────────────────────────────────────
    // Inspector fields
    // ──────────────────────────────────────────────
    public GameObject container;

    [Header("Containers")]
    public GameObject loginContainer;
    public GameObject unLogginContainer;
    public GameObject defaultContainer;

    public PlayerInformationUI playerInformation;
    public PlayerControllerUI  controllerUI;

    [Header("Auth")]
    public AuthView authView;           // ← reference tới AuthView component

    [Header("Scene & API")]
    public string defaultLoadScene = "New Scene";
    public string deleteUserPath   = "/users";
    public string logoutPath       = "/users/logout";
    public string fromPlatform     = "lms3d";

    [Header("Pathfinding")]
    public GameObject pathfindPanel;
    public Button     exitPathBtn;
    public Action     OnClickTryExitAutoFindWay;

    // ──────────────────────────────────────────────
    // Unity lifecycle
    // ──────────────────────────────────────────────
    private void Awake()
    {
        Instance = this;

        defaultContainer.SetActive(true);
        unLogginContainer.SetActive(true);
        loginContainer.SetActive(false);

        if (TokenStore.IsAuthenticated)
            ShowLoginUI();
        else
            LoginController.OnLoginComplete += ShowLoginUI;

        exitPathBtn.onClick.AddListener(TryExitAutoFinding);
        HidePathfinding();
    }

    private void OnDestroy()
    {
        LoginController.OnLoginComplete -= ShowLoginUI;
        exitPathBtn.onClick.RemoveListener(TryExitAutoFinding);
    }

    // ──────────────────────────────────────────────
    // Login UI
    // ──────────────────────────────────────────────

    public void ShowLoginUI()
    {
        loginContainer.SetActive(true);
        unLogginContainer.SetActive(false);

        // string baseUrl     = LmsStore.Instance.baseUrl?.TrimEnd('/');
        // string accessToken = TokenStore.AccessToken;
        //
        // var deleteApi = new DeleteAccountApi(
        //     baseUrl:        baseUrl,
        //     accessToken:    accessToken,
        //     deleteUserPath: deleteUserPath
        // );
        //
        // var authHandler = new AuthHandler(
        //     coroutineRunner:  this,
        //     baseUrl:          baseUrl,
        //     logoutPath:       logoutPath,
        //     fromPlatform:     fromPlatform,
        //     defaultLoadScene: defaultLoadScene,
        //     deleteAccountApi: deleteApi
        // );
        //
        // // Kết nối AuthHandler với AuthView
        // authView.Bind(authHandler);
    }

    // ──────────────────────────────────────────────
    // Visibility helpers
    // ──────────────────────────────────────────────

    public void HideAll() => container.SetActive(false);
    public void ShowAll() => container.SetActive(true);

    public void ShowUnLoginContainer(bool b)
    {
        if (TokenStore.IsAuthenticated) return;
        unLogginContainer.SetActive(b);
    }

    // ──────────────────────────────────────────────
    // Pathfinding
    // ──────────────────────────────────────────────

    public void TryExitAutoFinding()
    {
        OnClickTryExitAutoFindWay?.Invoke();
        HidePathfinding();
    }

    public void ShowPathfindingPanel()
    {
        pathfindPanel.SetActive(true);
        playerInformation.gameObject.SetActive(false);
    }

    public void HidePathfinding()
    {
        pathfindPanel.SetActive(false);
        playerInformation.gameObject.SetActive(true);
    }
}