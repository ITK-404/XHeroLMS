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
    [Header("Handle Position")]
    [SerializeField] private RectTransform activePosition;
    [SerializeField] private RectTransform deActivePosition;
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
        if (playerChairManager != null)
        {
            standupButton.onClick.RemoveListener(playerChairManager.PlayerStandup);
            sitdownButton.onClick.RemoveListener(playerChairManager.PlayerSitdown);
        }
        UILearnCanvas.OnClickReturnBtn -= playerChairManager.PlayerStandup;
    }
    private bool isShowOneTime = false;
    private void Update()
    {
        bool canInteract = playerChairManager.currentCheckPoint;
        sitdownButton.interactable = canInteract;
        if (TutorialHandler.Instance.IsPlayedBefore())
        {
            return;
        }
        if (isShowOneTime == false && playerChairManager.currentCheckPoint != null && playerChairManager.currentCheckPoint.GetComponent<TutorialChair>())
        {
            if(TutorialHandler.Instance.IsPlayedBefore())
            {
                return;
            }
            Debug.Log("Hiển thị hướng dẫn ngồi xuống");
            TutorialHandler.Instance.SetCurrentStep(TutorialStepType.Sitdown);
            isShowOneTime = true;
        }
    }

    public void ShowWatchVideoUI()
    {
        navigationBarUI.gameObject.SetActive(true);

        sitdownButton.GetComponent<RectTransform>().anchoredPosition = activePosition.anchoredPosition;
        standupButton.GetComponent<RectTransform>().anchoredPosition = activePosition.anchoredPosition;
    }

    public void HideWatchVideoUI()
    {
        navigationBarUI.gameObject.SetActive(false);

        sitdownButton.GetComponent<RectTransform>().anchoredPosition = deActivePosition.anchoredPosition;
        standupButton.GetComponent<RectTransform>().anchoredPosition = deActivePosition.anchoredPosition;
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