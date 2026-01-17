using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using System.Collections;
public enum PlayerState
{
    Unlogged,
    LoggedIn,
    Learning,
    Examining,
}

public class PlayerPanelUI : MonoBehaviour
{
    public static PlayerPanelUI Instance;
    public GameObject container;
    [Header("Containers")]
    public GameObject loginContainer;
    public GameObject unLogginContainer;
    public GameObject defaultContainer;
    public PlayerInformationUI playerInformation;


    public LogoutPopupUI logoutPopupUI;
    public DeleteAccountPopup deleteAccountPopup;
    public GameObject selectFunctionGroup;
    public Button blockingSelectFunction;
    public string defaultLoadScene = "New Scene";

    
    // users 
    public string deleteUserPath = "/users"; // <-- chỉnh theo API thật
    public bool disableButtonsWhileDeleting = true;

    private bool _isDeleting = false;
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
    }

    private bool isLoaded = false;

    private void OnLogout()
    {
        if (isLoaded) return;

        if (TokenStore.IsAuthenticated)
        {
            isLoaded = true;
            TokenStore.Clear();
            LoadingTransition.Load(defaultLoadScene);
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

        StartCoroutine(DeleteAccountRoutine(
            onSuccess: () =>
            {
                // Xóa local token + load lại scene
                isLoaded = true;
                TokenStore.Clear();
                LoadingTransition.Load(defaultLoadScene);
            },
            onFail: (err) =>
            {
                Debug.LogError("[PlayerPanelUI] Delete account failed: " + err);
            }
        ));
    }

    private IEnumerator DeleteAccountRoutine(Action onSuccess, Action<string> onFail)
    {
        _isDeleting = true;

        if (disableButtonsWhileDeleting)
        {
            if (logoutPopupUI != null) logoutPopupUI.SetInteractable(false);
        }

        string baseUrl = LmsStore.Instance.baseUrl?.TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl))
        {
            _isDeleting = false;
            onFail?.Invoke("BaseUrl empty");
            yield break;
        }

        string path = (deleteUserPath ?? "/users").Trim();
        if (!path.StartsWith("/")) path = "/" + path;

        string url = baseUrl + path;

        using (UnityWebRequest www = UnityWebRequest.Delete(url))
        {
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Accept", "application/json");

            // Bearer token
            string token = TokenStore.AccessToken?.Trim();
            if (!string.IsNullOrEmpty(token))
            {
                if (!token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    token = "Bearer " + token;
                www.SetRequestHeader("Authorization", token);
            }

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[PlayerPanelUI] Delete account success: " + www.downloadHandler.text);
                _isDeleting = false;
                onSuccess?.Invoke();
                yield break;
            }

            long code = www.responseCode;
            string body = www.downloadHandler != null ? www.downloadHandler.text : "";
            string err = $"HTTP {code} | {www.error} | {body}";

            _isDeleting = false;

            if (disableButtonsWhileDeleting)
            {
                if (logoutPopupUI != null) logoutPopupUI.SetInteractable(true);
            }

            onFail?.Invoke(err);
        }
    }

    private void OnDestroy()
    {
        LoginController.OnLoginComplete -= ShowLoginUI;

        LogoutPopupUI.OnReturn -= OnReturn;
        LogoutPopupUI.OnLogout -= OnLogout;
        TryLogoutButton.OnTryLogout -= TryLogoutButtonOnOnTryLogout;
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
        unLogginContainer.gameObject.SetActive(b);
    }
}
