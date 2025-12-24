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
    private string key => $"{TokenStore.UserID} {keyPlayedBefore}";
    //private string key => $"{keyPlayedBefore}";
    public void Save()
    {
        Debug.Log("Save key");
        PlayerPrefs.SetInt(key, 1);
        isPlayedBefore = true;
        foreach (var item in tutorialSteps)
        {
            item.gameObject.SetActive(false);
        }
    }

    private void Load()
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

    private void Awake()
    {
        backgroundImg.gameObject.SetActive(false);
        backgroundImg.DOFade(0, 0);
        
        InitFocusItems();
        
        Instance = this;
        worldTutorialStep.gameObject.SetActive(false);
        sitdownStandupUI.gameObject.SetActive(false);
        Load();

        tutorialSteps.Add(worldTutorialStep);
        tutorialSteps.Add(sitdownStandupUI);
        tutorialSteps.Add(baiHocUI);
        tutorialSteps.Add(pauseAndResumeUI);
        tutorialSteps.Add(skipVideoUI);
        tutorialSteps.Add(scaleVideoUI);
       
        // new add must set type for ui
        tutorialSteps.Add(standStandupUI);
        tutorialSteps.Add(closeBaiHocUI);
        foreach(var item in tutorialSteps)
        {
            item.SetActive(false);
        }

        if (isPlayedBefore)
        {
            return;
        }
        backgroundImg.gameObject.SetActive(true);
        backgroundImg.DOFade(0.95f, 1);
        
        SetCurrentStep(TutorialStepType.GoToChair);
    }

    [SerializeField] private GameObject player;
    [SerializeField] private Transform newParent;
    
    private void InitFocusItems()
    {
        foreach (var item in _tutorialUIObjects)
        {
            item.oldParent = item.currentItem.transform.parent; 
            item.newParent = newParent;
        }
    }
    

    [SerializeField] private Camera mainCamera;
    private void HandleFollowWorldPosition()
    {
        var screenPosition = mainCamera.WorldToScreenPoint(worldTutorialStep.transform.position);
        Debug.Log($"Screen Position: {screenPosition}");
        followWorldItem.position = screenPosition;

    }

    [SerializeField] private RectTransform followWorldItem;
    
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
                OpenUI(worldTutorialStep);
                break;
            case TutorialStepType.Sitdown:
                OpenUI(sitdownStandupUI);
                sitdownStandupUI.GetComponentInChildren<TextMeshProUGUI>().text = "CLICK ĐỂ NGỒI";
                break;
            case TutorialStepType.OpenLesson:
                OpenUI(baiHocUI);
                baiHocUI.GetComponentInChildren<TextMeshProUGUI>().text = "CLICK ĐỂ XEM BÀI HỌC";
                break;
            case TutorialStepType.CloseLesson:
                OpenUI(closeBaiHocUI);
                baiHocUI.GetComponentInChildren<TextMeshProUGUI>().text = "CLICK ĐỂ ẨN BÀI HỌC";
                break;
            case TutorialStepType.PauseVideo:
                OpenUI(pauseAndResumeUI);
                pauseAndResumeUI.GetComponentInChildren<TextMeshProUGUI>().text = "DỪNG BÀI HỌC";
                break;
            case TutorialStepType.ResumeVideo:
                OpenUI(pauseAndResumeUI);
                pauseAndResumeUI.GetComponentInChildren<TextMeshProUGUI>().text = "TIẾP TỤC BÀI HỌC";
                break;
            case TutorialStepType.ScaleVideo:
                OpenUI(scaleVideoUI);
                break;
            case TutorialStepType.Standup:
                OpenUI(standStandupUI);
                standStandupUI.GetComponentInChildren<TextMeshProUGUI>().text = "CLICK ĐỂ ĐỨNG";
                break;
            case TutorialStepType.Skip:
                OpenUI(skipVideoUI);
                break;
        }

        FocusCorrectItem(tutorialStep);
        this.index = index;
    }

   
    
    private void FocusCorrectItem(TutorialStepType type)
    {
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
    
    private void OpenUI(GameObject UIObject)
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
        return true;
        //return isPlayedBefore;
    }

    private void Update()
    {
        debugStep = (TutorialStepType)index;
        HandleFollowWorldPosition();
    }
}