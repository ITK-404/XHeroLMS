using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
using Cysharp.Threading.Tasks;

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

    [Header("API Paths")]
    public string deleteUserPath = "/users";
    public string logoutPath = "/users/logout";
    public string fromPlatform = "lms3d";

    public bool disableButtonsWhileDeleting = true;

    private bool _isDeleting = false;
    private bool _isLoggingOut = false;

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
        if (isLoaded || _isLoggingOut) return;

        if (!TokenStore.IsAuthenticated || string.IsNullOrEmpty(TokenStore.AccessToken))
        {
            Debug.LogWarning("[PlayerPanelUI] Cannot logout: not authenticated.");
            return;
        }

        StartCoroutine(CoLogout());
    }

    private IEnumerator CoLogout()
    {
        _isLoggingOut = true;

        if (logoutPopupUI != null)
            logoutPopupUI.SetInteractable(false);

        string baseUrl = LmsStore.Instance.baseUrl?.TrimEnd('/');
        string accessToken = TokenStore.AccessToken;

        if (string.IsNullOrEmpty(baseUrl))
        {
            Debug.LogError("[PlayerPanelUI] Logout failed: baseUrl is null or empty.");
            RestoreLogoutInteractable();
            yield break;
        }

        string url = $"{baseUrl}{logoutPath}?fromPlatform={UnityWebRequest.EscapeURL(fromPlatform)}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("authorization", $"Bearer {accessToken}");
            request.timeout = 20;
            // lưu trước khi logout
            yield return GameInitializer.Instance.GameSessionHandler.SaveSession().ToCoroutine();

            yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            bool success = request.result == UnityWebRequest.Result.Success;
#else
            bool success = !request.isNetworkError && !request.isHttpError;
#endif

            if (success)
            {
                Debug.Log("[PlayerPanelUI] Logout API success.");

                isLoaded = true;
                TokenStore.Clear();

                if (logoutPopupUI != null)
                    logoutPopupUI.Hide();
                
                EventHub.RaisePlayerLogout();
                LoadingTransition.Load_Scene(defaultLoadScene,false);
            }
            else
            {
                Debug.LogError($"[PlayerPanelUI] Logout API failed: {request.error}\nResponse: {request.downloadHandler.text}");

                RestoreLogoutInteractable();
            }
        }
    }

    private void RestoreLogoutInteractable()
    {
        _isLoggingOut = false;

        if (logoutPopupUI != null)
            logoutPopupUI.SetInteractable(true);
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
                EventHub.RaisePlayerDeleteAccount();
                TokenStore.Clear();
                LoadingTransition.Load_Scene(defaultLoadScene,false);
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
        _deleteAccountApi = new DeleteAccountApi(baseUrl: baseUrl, accessToken: accessToken, deleteUserPath: null);
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
}