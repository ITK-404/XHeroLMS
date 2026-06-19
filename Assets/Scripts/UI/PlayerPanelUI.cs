using System;
using UnityEngine;
using UnityEngine.SceneManagement;
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
    private static bool triedRestoreSession;
    private const float LoginPanelGuardInterval = 0.25f;
    private bool hasAuthState;
    private bool lastAuthState;
    private float nextLoginPanelGuardTime;

    // ──────────────────────────────────────────────
    // Unity lifecycle
    // ──────────────────────────────────────────────
    private void Awake()
    {
        Instance = this;
        TryRestoreSessionOnce();

        if (defaultContainer != null)
            defaultContainer.SetActive(true);

        LoginController.OnLoginComplete -= ShowLoginUI;
        LoginController.OnLoginComplete += ShowLoginUI;

        RefreshAuthState(true);

        if (exitPathBtn != null)
            exitPathBtn.onClick.AddListener(TryExitAutoFinding);

        HidePathfinding();
    }

    private void OnEnable()
    {
        TryRestoreSessionOnce();
        RefreshAuthState(true);
    }

    private void LateUpdate()
    {
        RefreshAuthState(false);
        RunLoginPanelGuardIfNeeded();
    }

    private void OnDestroy()
    {
        LoginController.OnLoginComplete -= ShowLoginUI;

        if (exitPathBtn != null)
            exitPathBtn.onClick.RemoveListener(TryExitAutoFinding);
    }

    // ──────────────────────────────────────────────
    // Login UI
    // ──────────────────────────────────────────────

    public void ShowLoginUI()
    {
        hasAuthState = true;
        lastAuthState = true;
        SetAuthContainers(true);
        CloseBlockingLoginPanels();

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
        TryRestoreSessionOnce();

        if (TokenStore.IsAuthenticated) return;

        if (unLogginContainer != null)
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
        if (pathfindPanel != null)
            pathfindPanel.SetActive(false);

        if (playerInformation != null)
            playerInformation.gameObject.SetActive(true);
    }

    private void RefreshAuthState(bool force)
    {
        bool loggedIn = HasAuthenticatedSession();

        if (!force && hasAuthState && lastAuthState == loggedIn)
            return;

        hasAuthState = true;
        lastAuthState = loggedIn;
        SetAuthContainers(loggedIn);
    }

    private void SetAuthContainers(bool loggedIn)
    {
        if (loginContainer != null)
            loginContainer.SetActive(loggedIn);

        if (unLogginContainer != null)
            unLogginContainer.SetActive(!loggedIn);

        if (loggedIn)
            CloseBlockingLoginPanels();
    }

    private static void TryRestoreSessionOnce()
    {
        if (triedRestoreSession || TokenStore.IsAuthenticated)
            return;

        triedRestoreSession = true;
        TokenStore.TryRestoreFromDisk();
    }

    private void RunLoginPanelGuardIfNeeded()
    {
        if (!HasAuthenticatedSession())
            return;

        if (Time.unscaledTime < nextLoginPanelGuardTime)
            return;

        nextLoginPanelGuardTime = Time.unscaledTime + LoginPanelGuardInterval;
        CloseBlockingLoginPanels();
    }

    private static bool HasAuthenticatedSession()
    {
        return TokenStore.IsAuthenticated && !string.IsNullOrEmpty(TokenStore.AccessToken);
    }

    private static void CloseBlockingLoginPanels()
    {
        var panels = UnityEngine.Object.FindObjectsByType<OpenClosePanel>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < panels.Length; i++)
        {
            if (panels[i] != null)
                panels[i].CloseUI();
        }

        var transforms = UnityEngine.Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < transforms.Length; i++)
        {
            Transform root = transforms[i];
            if (root == null || root.name != "CanvasLogin" || !IsLoadedSceneObject(root.gameObject))
                continue;

            Transform overlay = root.Find("Image");
            if (overlay != null)
                overlay.gameObject.SetActive(false);

            SetDescendantsNamedActive(root, "UI", false);
        }
    }

    private static bool IsLoadedSceneObject(GameObject obj)
    {
        Scene scene = obj.scene;
        return scene.IsValid() && scene.isLoaded;
    }

    private static void SetDescendantsNamedActive(Transform root, string childName, bool active)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == childName)
                child.gameObject.SetActive(active);

            SetDescendantsNamedActive(child, childName, active);
        }
    }
}
