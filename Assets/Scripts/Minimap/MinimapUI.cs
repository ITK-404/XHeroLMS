using UnityEngine;
using UnityEngine.UI;

public class MinimapUI : MonoBehaviour
{
    [SerializeField] private GameObject container;
    public Button turnOnBtn;
    public Button turnOffBtn;

    private void Start()
    {
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