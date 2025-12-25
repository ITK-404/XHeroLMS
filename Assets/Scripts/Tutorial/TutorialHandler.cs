using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public enum TutorialStepType
{
    GoToChair = 0,
    Sitdown = 1,
    OpenLesson = 2,
    CloseLesson = 3,
    PauseVideo = 4,
    ResumeVideo = 5,
    ScaleVideo = 6,
    Standup = 7,
    Skip = 8,

}
[Serializable]
public class TutorialUIObject
{
    public TutorialStepType type;
    public Transform oldParent;
    public RectTransform currentItem;
    public Transform newParent;
    private Vector3 oldAnchorPosition;

    private bool isChangedParent = false;
    public void ShowTutorial()
    {
        currentItem.transform.SetParent(newParent,true);
        isChangedParent = true;
    }

    public void HideTutorial()
    {
        if (!isChangedParent)
        {
            return;
        }

        isChangedParent = false;
        currentItem.transform.SetParent(oldParent,true);
    }
}
public class TutorialHandler : MonoBehaviour
{
    private int index = 0;
    [Header("Tutorial Hand")]
    public GameObject worldTutorialStep;
    public GameObject sitdownStandupUI;
    public GameObject standStandupUI;
    public GameObject baiHocUI;
    public GameObject closeBaiHocUI;
    public GameObject pauseAndResumeUI;
    public GameObject skipVideoUI;
    public GameObject scaleVideoUI;
    public static TutorialHandler Instance;
    private const string keyPlayedBefore = "TutorialPlayedBefore";
    [SerializeField] private bool isPlayedBefore = false;

    public TutorialStepType CurrentStep => (TutorialStepType)index;
    [SerializeField]private TutorialStepType debugStep;

    [SerializeField] private List<TutorialUIObject> _tutorialUIObjects = new();
    [SerializeField] private Image backgroundImg;

    [Header("World Space Item")] 

    [SerializeField] private PointClickSystem _pointClickSystem;
    [SerializeField] private GameObject player;
    [SerializeField] private Transform newParent;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private RectTransform followWorldItem;
    private bool isPlayAllStep = false;

    private string key => $"{TokenStore.UserID} {keyPlayedBefore}";
    //private string key => $"{keyPlayedBefore}";
    public void Save()
    {
        // Debug.Log("Save key");
        // PlayerPrefs.SetInt(key, 1);
        isPlayedBefore = true;
        isPlayAllStep = true;
        
        backgroundImg.DOFade(0, 2f).OnComplete(() =>
        {
            backgroundImg.gameObject.SetActive(false);
        });
        
        HideAllTutorialHand();

        foreach (var item in _tutorialUIObjects)
        {
            item.HideTutorial();
        }
    }

    private void LoadSave()
    {
        if (!PlayerPrefs.HasKey(key))
        {
            isPlayedBefore = false;
            return;
        }
        isPlayedBefore = PlayerPrefs.GetInt(key) == 1;
    }

    [ContextMenu("Reset Key")]
    public void ResetKey()
    {
        PlayerPrefs.SetInt(key, 0);
    }

    private void Start()
    {
        Instance = this;
        
        // start setup
        backgroundImg.gameObject.SetActive(false);
        backgroundImg.DOFade(0, 0);
                
        worldTutorialStep.gameObject.SetActive(false);
        sitdownStandupUI.gameObject.SetActive(false);
        // this
        SetupForChangeParentUI();

        LoadSave();

        CreateHandList();
        // if player is played tutorial before
        if (isPlayedBefore)
        {
            return;
        }

        SetupStartingClick();
        // if player not play tutorial then fade background
        
        backgroundImg.gameObject.SetActive(true);
        backgroundImg.DOFade(0.95f, 1);
        // then showing first step of tutorial
        SetCurrentStep(TutorialStepType.GoToChair);
    }

    private void CreateHandList()
    {
        tutorialSteps.Add(followWorldItem.gameObject);
        tutorialSteps.Add(worldTutorialStep);
        tutorialSteps.Add(sitdownStandupUI);
        tutorialSteps.Add(baiHocUI);
        tutorialSteps.Add(pauseAndResumeUI);
        tutorialSteps.Add(skipVideoUI);
        tutorialSteps.Add(scaleVideoUI);
       
        // new add must set type for ui
        tutorialSteps.Add(standStandupUI);
        tutorialSteps.Add(closeBaiHocUI);

        HideAllTutorialHand();
    }

    private void HideAllTutorialHand()
    {
        foreach(var item in tutorialSteps)
        {
            item.SetActive(false);
        }
    }
    

    private void SetupStartingClick()
    {
        var container = PlayerChairManager.Instance.transform;
        var tutorialChair = container.GetComponentInChildren<TutorialChair>();
        var chairCheckPoint = tutorialChair.GetComponent<ChairCheckPoint>();
        // assign event
        var btn = followWorldItem.GetComponent<Button>();
        btn.onClick.AddListener(() =>
        {
            // make sure this check point have tutorial chair
            _pointClickSystem.MoveToChair(chairCheckPoint);
        });

        // look to target item
        
        var direction = tutorialChair.transform.position - _pointClickSystem.transform.position;
        direction.y = 0;
        direction.Normalize();
        var targetRotation = Quaternion.LookRotation(direction);
        _pointClickSystem.transform.DORotateQuaternion(targetRotation, 3f);
    }
    


    private void SetupForChangeParentUI()
    {
        foreach (var item in _tutorialUIObjects)
        {
            item.oldParent = item.currentItem.transform.parent; 
            item.newParent = newParent;
        }
    }
    


    private void HandleFollowWorldPosition()
    {
        var screenPosition = mainCamera.WorldToScreenPoint(worldTutorialStep.transform.position);
        Debug.Log($"Screen Position: {screenPosition}");
        followWorldItem.position = screenPosition;

    }

    
    public bool IsStep(int index)
    {
        return this.index == index;
    }
    private List<GameObject> tutorialSteps = new();

    public void SetCurrentStep(TutorialStepType index)
    {
        Debug.Log("Set current step to: "+index);
        ShowStep((int)index);
    }

    private void ShowStep(int index)
    {
        if (isPlayedBefore) return;
        var tutorialStep = (TutorialStepType)index;
        switch (tutorialStep)
        {
            case TutorialStepType.GoToChair:
                ShowSpecifyGameObject(followWorldItem.gameObject);
                break;
            case TutorialStepType.Sitdown:
                ShowSpecifyGameObject(sitdownStandupUI);
                sitdownStandupUI.GetComponentInChildren<TextMeshProUGUI>().text = "CLICK ĐỂ NGỒI";
                break;
            case TutorialStepType.OpenLesson:
                ShowSpecifyGameObject(baiHocUI);
                baiHocUI.GetComponentInChildren<TextMeshProUGUI>().text = "CLICK ĐỂ XEM BÀI HỌC";
                break;
            case TutorialStepType.CloseLesson:
                ShowSpecifyGameObject(closeBaiHocUI);
                baiHocUI.GetComponentInChildren<TextMeshProUGUI>().text = "CLICK ĐỂ ẨN BÀI HỌC";
                break;
            case TutorialStepType.PauseVideo:
                ShowSpecifyGameObject(pauseAndResumeUI);
                pauseAndResumeUI.GetComponentInChildren<TextMeshProUGUI>().text = "DỪNG BÀI HỌC";
                break;
            case TutorialStepType.ResumeVideo:
                ShowSpecifyGameObject(pauseAndResumeUI);
                pauseAndResumeUI.GetComponentInChildren<TextMeshProUGUI>().text = "TIẾP TỤC BÀI HỌC";
                break;
            case TutorialStepType.ScaleVideo:
                ShowSpecifyGameObject(scaleVideoUI);
                break;
            case TutorialStepType.Standup:
                ShowSpecifyGameObject(standStandupUI);
                standStandupUI.GetComponentInChildren<TextMeshProUGUI>().text = "CLICK ĐỂ ĐỨNG";
                break;
            case TutorialStepType.Skip:
                ShowSpecifyGameObject(skipVideoUI);
                break;
        }

        FocusCorrectItem(tutorialStep);
        this.index = index;
    }

    
    private void FocusCorrectItem(TutorialStepType type)
    {
        if (isPlayAllStep) return;
        
        foreach (var item in _tutorialUIObjects)
        {
            if (item.type == type)
            {
                item.ShowTutorial();
            }
            else
            {
                item.HideTutorial();
            }
        }
    }
    
    private void ShowSpecifyGameObject(GameObject UIObject)
    {
        foreach (var item in tutorialSteps)
        {
            if (item == UIObject)
            {
                item.SetActive(true);
            }
            else
            {
                item.SetActive(false);
            }
        }
    }

    public bool IsPlayedBefore()
    {
        // return true;
        return isPlayedBefore;
    }

    private void Update()
    {
        debugStep = (TutorialStepType)index;
        HandleFollowWorldPosition();
    }
}