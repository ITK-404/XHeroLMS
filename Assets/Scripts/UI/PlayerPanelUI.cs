using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;

#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
#endif
[DefaultExecutionOrder(-999)]
public class PlayerPanelUI : MonoBehaviour
{
    public static PlayerPanelUI Instance;
    public GameObject container;
    [Header("Containers")] public GameObject loginContainer;
    public GameObject unLogginContainer;
    public GameObject defaultContainer;
    public PlayerInformationUI playerInformation;


    public LogoutPopupUI logoutPopupUI;
    public DeleteAccountPopup deleteAccountPopup;
    public GameObject selectFunctionGroup;
    public Button blockingSelectFunction;
    public string defaultLoadScene = "New Scene";

    public GameObject pathfindPanel;
    public Button exitPathBtn;
    public Action OnClickTryExitAutoFindWay;

    public PlayerControllerUI controllerUI;
    // users 
    public string deleteUserPath = "/users"; // <-- chỉnh theo API thật
    public bool disableButtonsWhileDeleting = true;

    private bool _isDeleting = false;

    private DeleteAccountApi _deleteAccountApi;
    
    private void Awake()
    {
        Instance = this;
        defaultContainer.gameObject.SetActive(true);
        unLogginContainer.gameObject.SetActive(true);
        loginContainer.gameObject.SetActive(false);

        // LoginController.OnLoginComplete += Show;
        if (TokenStore.IsAuthenticated)
        {
            ShowLoginUI();
        }
        else
        {
            LoginController.OnLoginComplete += ShowLoginUI;
        }

        logoutPopupUI.gameObject.SetActive(false);
        DeleteAccountPopup.OnDeleteAccountAction = OnDeleteAccount;
        LogoutPopupUI.OnReturn += OnReturn;
        LogoutPopupUI.OnLogout += OnLogout;
        TryLogoutButton.OnTryLogout += TryLogoutButtonOnOnTryLogout;

        exitPathBtn.onClick.AddListener(TryExitAutoFinding);

        HidePathfinding();
    }

    public void TryExitAutoFinding()
    {
        OnClickTryExitAutoFindWay?.Invoke();
        HidePathfinding();
    }

    private void OnDestroy()
    {
        LoginController.OnLoginComplete -= ShowLoginUI;

        LogoutPopupUI.OnReturn -= OnReturn;
        LogoutPopupUI.OnLogout -= OnLogout;
        TryLogoutButton.OnTryLogout -= TryLogoutButtonOnOnTryLogout;
        exitPathBtn.onClick.RemoveListener(TryExitAutoFinding);
    }

    private bool isLoaded = false;

    private void OnLogout()
    {
        if (isLoaded) return;

        if (TokenStore.IsAuthenticated)
        {
            isLoaded = true;
            TokenStore.Clear();
            // LoadingTransition.Load(defaultLoadScene);
            StartCoroutine(CoLoadSceneSmart(defaultLoadScene));
        }
    }

    public void OnDeleteAccount()
    {
        if (_isDeleting || isLoaded) return;

        if (!TokenStore.IsAuthenticated || string.IsNullOrEmpty(TokenStore.AccessToken))
        {
            Debug.LogWarning("[PlayerPanelUI] Cannot delete account: not authenticated.");
            return;
        }

        _isDeleting = true;
        if (disableButtonsWhileDeleting && logoutPopupUI != null)
            logoutPopupUI.SetInteractable(false);

        StartCoroutine(_deleteAccountApi.DeleteAccountRoutine(
            onSuccess: () =>
            {
                _isDeleting = false;
                if (disableButtonsWhileDeleting && logoutPopupUI != null)
                    logoutPopupUI.SetInteractable(true);

                isLoaded = true;
                TokenStore.Clear();
                StartCoroutine(CoLoadSceneSmart(defaultLoadScene));
            },
            onFail: (err) =>
            {
                _isDeleting = false;
                if (disableButtonsWhileDeleting && logoutPopupUI != null)
                    logoutPopupUI.SetInteractable(true);

                Debug.LogError("[PlayerPanelUI] Delete account failed: " + err);
            }
        ));
    }

    private void TryLogoutButtonOnOnTryLogout()
    {
        selectFunctionGroup.gameObject.SetActive(true);
        blockingSelectFunction.gameObject.SetActive(true);
        // logoutPopupUI.Show();
    }

    private void OnReturn()
    {
        logoutPopupUI.Hide();
    }

    public void ShowLoginUI()
    {
        loginContainer.gameObject.SetActive(true);
        unLogginContainer.gameObject.SetActive(false);
        
        string baseUrl = LmsStore.Instance.baseUrl?.TrimEnd('/');
        string accessToken = TokenStore.AccessToken;
        _deleteAccountApi = new DeleteAccountApi(baseUrl: baseUrl, accessToken:accessToken, deleteUserPath: null);
    }

    public void HideAll()
    {
        container.gameObject.SetActive(false);
    }

    public void ShowAll()
    {
        container.gameObject.SetActive(true);
    }

    public void ShowUnLoginContainer(bool b)
    {
        // don't try to turn on or off this of is logged
        if (TokenStore.IsAuthenticated) return;
        unLogginContainer.gameObject.SetActive(b);
    }

    public void ShowPathfindingPanel()
    {
        pathfindPanel.gameObject.SetActive(true);
        playerInformation.gameObject.SetActive(false);
    }

    public void HidePathfinding()
    {
        pathfindPanel.gameObject.SetActive(false);
        playerInformation.gameObject.SetActive(true);
    }

    private IEnumerator CoLoadSceneSmart(string targetScene)
    {
#if ADDRESSABLES
        // Nếu scene là addressable (cloud) -> dùng LoadAssetBundle
        bool isCloud = false;
        yield return CoCheckIsCloudScene(targetScene, r => isCloud = r);

        if (isCloud)
            LoadingTransition.LoadAssetBundle(targetScene);
        else
            LoadingTransition.Load(targetScene);
#else
    LoadingTransition.Load(targetScene);
    yield break;
#endif
    }

#if ADDRESSABLES
// Check: sceneName có tồn tại như 1 addressable scene không?
    private IEnumerator CoCheckIsCloudScene(string sceneKeyOrName, System.Action<bool> result)
    {
        // var h = Addressables.LoadResourceLocationsAsync(sceneKeyOrName, typeof(SceneInstance));
        var h = Addressables.LoadResourceLocationsAsync(sceneKeyOrName);

        yield return h;

        bool ok = (h.Status == AsyncOperationStatus.Succeeded && h.Result != null && h.Result.Count > 0);

        // Release handle (tránh leak)
        Addressables.Release(h);

        result?.Invoke(ok);
    }
#endif
}