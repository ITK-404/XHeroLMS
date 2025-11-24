using UnityEngine;
using UnityEngine.UI;

public class PlayerPanelUI : MonoBehaviour
{
    public GameObject container;
    public GameObject loginContainer;
    public GameObject unLogginContainer;
    public GameObject defaultContainer;
    public Button returnBtn;
    public PlayerInformationUI playerInformation;
    public GameObject iconGroup;
    public GameObject coinGroup;

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
    }


    private void OnDestroy()
    {
        LoginController.OnLoginComplete -= ShowLoginUI;
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
    
}
