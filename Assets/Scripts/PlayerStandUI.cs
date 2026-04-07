using UnityEngine;
using UnityEngine.UI;
using System.Collections;

#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
#endif

public class PlayerStandUI : MonoBehaviour
{
    public Button returnBtn;
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

        UILearnCanvas.onCourseListShow += CourseListShow;
        
        returnBtn.onClick.AddListener(ReturnMainScene);
    }

    private void OnDestroy()
    {
        if (playerChairManager != null)
        {
            standupButton.onClick.RemoveListener(playerChairManager.PlayerStandup);
            sitdownButton.onClick.RemoveListener(playerChairManager.PlayerSitdown);
        }
        UILearnCanvas.OnClickReturnBtn -= playerChairManager.PlayerStandup;
        UILearnCanvas.onCourseListShow -= CourseListShow;
        returnBtn.onClick.RemoveListener(ReturnMainScene);

    }

    private void ReturnMainScene()
    {
        // LoadingTransition.Load("New Scene");
        StartCoroutine(CoLoadSceneSmart("New Scene"));
    }
    
    private bool localIsShow = false;
    public void CourseListShow(bool isShow)
    {
        // hiển thị hoặc ẩn UI điều khiển video
        // nếu UI danh sách bài học được hiển thị thì ẩn UI điều khiển video
        if (!isShow)
        {
            if(playerChairManager.playerState == PlayerChairManager.PlayerState.Sitdown)
            {
                ShowWatchVideoUI();
            }
        }
        else
        {
            HideWatchVideoUI();
        }
        localIsShow = isShow;
    }

    private bool isShowOneTime = false;
    private void Update()
    {
        bool canInteract = playerChairManager.currentCheckPoint;
        sitdownButton.interactable = canInteract;
        if (TutorialHandler.Instance.CurrentStep == TutorialStepType.GoToChair)
        {
            if (isShowOneTime == false && playerChairManager.currentCheckPoint != null && playerChairManager.currentCheckPoint.GetComponent<TutorialChair>())
            {
                Debug.Log("Hiển thị hướng dẫn ngồi xuống");
                TutorialHandler.Instance.SetCurrentStep(TutorialStepType.Sitdown);
                isShowOneTime = true;
            }
        }
        
    }

    private void ShowWatchVideoUI()
    {
        navigationBarUI.gameObject.SetActive(true);

        sitdownButton.GetComponent<RectTransform>().anchoredPosition = activePosition.anchoredPosition;
        standupButton.GetComponent<RectTransform>().anchoredPosition = activePosition.anchoredPosition;
    }

    private void HideWatchVideoUI()
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

    public void HideLearningUI()
    {
        // ẩn giao diện danh sách bài học
        // ẩn giao diện điều khiển video
        UILearnCanvas.Hide();
        HideWatchVideoUI();
    }

    public void ShowLearningUI()
    {
        // Hiện giao diện danh sách bài học
        UILearnCanvas.Show();
        CourseListShow(localIsShow);
    }

    private IEnumerator CoLoadSceneSmart(string targetScene)
    {
        yield return LoadingTransition.LoadScene(targetScene);
    }
}