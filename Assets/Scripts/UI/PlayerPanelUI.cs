using System;
using UnityEngine;
using UnityEngine.UI;

public enum PlayerState
{
    Unlogged,
    LoggedIn,
    Learning,
    Examining,
}

public class PlayerPanelUI : MonoBehaviour
{
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

    private void Awake()
    {
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
}

