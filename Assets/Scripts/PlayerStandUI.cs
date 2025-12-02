using UnityEngine;
using UnityEngine.UI;

public class PlayerStandUI : MonoBehaviour
{
    public GameObject returnBtn;
    public Button standupButton;
    public Button sitdownButton;
    public RectTransform navigationBarUI;
    public LearnUI UILearnCanvas;
    public PlayerChairManager playerChairManager;
    private void Awake()
    {
        playerChairManager = FindAnyObjectByType<PlayerChairManager>();
        if (playerChairManager != null)
        {
            standupButton.onClick.AddListener(playerChairManager.PlayerStandup);
            sitdownButton.onClick.AddListener(playerChairManager.PlayerSitdown);
        }
        UILearnCanvas.OnClickReturnBtn += playerChairManager.PlayerStandup;
        
        HideWatchVideoUI();
        UILearnCanvas.Hide();

        ShowSitdownButton();
    }

    private void OnDestroy()
    {
        if (playerChairManager!= null)
        {
            standupButton.onClick.RemoveListener(playerChairManager.PlayerStandup);
            sitdownButton.onClick.RemoveListener(playerChairManager.PlayerSitdown);
        }
        UILearnCanvas.OnClickReturnBtn -= playerChairManager.PlayerStandup;
    }

    private void Update()
    {
        bool canInteract = playerChairManager.currentCheckPoint;
        sitdownButton.interactable = canInteract;
    }

    public void ShowWatchVideoUI()
    {
        navigationBarUI.gameObject.SetActive(true);
        returnBtn.gameObject.SetActive(false);
    }
    
    public void HideWatchVideoUI()
    {
        navigationBarUI.gameObject.SetActive(false);
        returnBtn.gameObject.SetActive(true);
    }

    
    public void ShowSitdownButton()
    {
        sitdownButton.gameObject.SetActive(true);
        standupButton.gameObject.SetActive(false);
    }

    public void ShowStandUpButton()
    {
        sitdownButton.gameObject.SetActive(false);
        standupButton.gameObject.SetActive(true);
    }

    public void HideButtons()
    {
        sitdownButton.gameObject.SetActive(false);
        standupButton.gameObject.SetActive(false);
    }
}