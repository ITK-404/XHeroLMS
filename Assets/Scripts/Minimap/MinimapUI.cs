using UnityEngine;
using UnityEngine.UI;

public class MinimapUI : MonoBehaviour
{
    [SerializeField] private GameObject container;
    public Button turnOnBtn;
    public Button turnOffBtn;
    public GameObject maskView;
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
        ShowBottomViewUI();
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

    public void ShowBottomViewUI()
    {
        turnOnBtn.gameObject.SetActive(true);
        turnOffBtn.gameObject.SetActive(false);
        maskView.gameObject.SetActive(true);
    }

    public void ShowTopViewUI()
    {
        turnOnBtn.gameObject.SetActive(false);
        turnOffBtn.gameObject.SetActive(true);
        maskView.gameObject.SetActive(false);
    }
    
}