using UnityEngine;
using UnityEngine.UI;

public class PlayerPanelUI : MonoBehaviour
{
    public GameObject container;
    public Button returnBtn;
    public PlayerInformationUI playerInformation;
    public GameObject iconGroup;
    public GameObject coinGroup;

    private void Awake()
    {
        container.gameObject.SetActive(false);
        // LoginController.OnLoginComplete += Show;
        if (TokenStore.IsAuthenticated)
        {
            Show();
        }
        else
        {
            LoginController.OnLoginComplete += Show;
        }
    }

    private void OnDestroy()
    {
        LoginController.OnLoginComplete -= Show;
    }

    public void Show()
    {
        container.gameObject.SetActive(true);
    }

    public void Hide()
    {
        container.gameObject.SetActive(false);
    }
    
}
