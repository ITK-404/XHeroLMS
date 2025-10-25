using UnityEngine;
using UnityEngine.UI;

public class PlayerStandUI : MonoBehaviour
{
    public Button standupButton;
    public Button sitdownButton;
    public RectTransform navigationBarUI;
    public LearnUI UILearnCanvas;
    public PlayerChairManager playerChairManager;
    private void Awake()
    {
        playerChairManager = GetComponent<PlayerChairManager>();
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
    }
    
    public void HideWatchVideoUI()
    {
        navigationBarUI.gameObject.SetActive(false);
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

}