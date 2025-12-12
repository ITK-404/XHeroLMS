using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
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
public class TutorialHandler : MonoBehaviour
{
    private int index = 0;
    public GameObject worldTutorialStep;
    public GameObject sitdownStandupUI;
    public GameObject baiHocUI;
    public GameObject pauseAndResumeUI;
    public GameObject skipVideoUI;
    public GameObject scaleVideoUI;
    public static TutorialHandler Instance;
    private const string keyPlayedBefore = "TutorialPlayedBefore";
    private bool isPlayedBefore = false;

    public TutorialStepType CurrentStep => (TutorialStepType)index;
    [SerializeField]private TutorialStepType debugStep;
    private string key => $"{TokenStore.UserID} {keyPlayedBefore}";
    //private string key => $"{keyPlayedBefore}";
    public void Save()
    {
        Debug.Log("Save key");
        PlayerPrefs.SetInt(key, 1);
        isPlayedBefore = true;
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
        foreach(var item in tutorialSteps)
        {
            item.SetActive(false);
        }
        SetCurrentStep(TutorialStepType.GoToChair);
    }

    public bool IsStep(int index)
    {
        return this.index == index;
    }
    private List<GameObject> tutorialSteps = new();

    public void SetCurrentStep(TutorialStepType index)
    {
        ShowStep((int)index);
    }

    private void ShowStep(int index)
    {
        Debug.Log($"Tutorial hiện tại đang đóng");
        return;
        if (isPlayedBefore)
        {
            return;
        }
        var tutorialStep = (TutorialStepType)index;
        switch (tutorialStep)
        {
            case TutorialStepType.GoToChair:
                OpenUI(worldTutorialStep);
                break;
            case TutorialStepType.Sitdown:
                OpenUI(sitdownStandupUI);
                sitdownStandupUI.GetComponentInChildren<TextMeshProUGUI>().text = "CLICK ĐỂ NGỒI XUỐNG";
                break;
            case TutorialStepType.OpenLesson:
                OpenUI(baiHocUI);
                baiHocUI.GetComponentInChildren<TextMeshProUGUI>().text = "CLICK ĐỂ MỞ BÀI HỌC";
                break;
            case TutorialStepType.CloseLesson:
                OpenUI(baiHocUI);
                baiHocUI.GetComponentInChildren<TextMeshProUGUI>().text = "CLICK ĐỂ ĐÓNG BÀI HỌC";
                break;
            case TutorialStepType.PauseVideo:
                OpenUI(pauseAndResumeUI);
                pauseAndResumeUI.GetComponentInChildren<TextMeshProUGUI>().text = "CLICK ĐỂ DỪNG BÀI HỌC";
                break;
            case TutorialStepType.ResumeVideo:
                OpenUI(pauseAndResumeUI);
                pauseAndResumeUI.GetComponentInChildren<TextMeshProUGUI>().text = "CLICK ĐỂ TIẾP TỤC BÀI HỌC";
                break;
            case TutorialStepType.ScaleVideo:
                OpenUI(scaleVideoUI);
                break;
            case TutorialStepType.Standup:
                OpenUI(sitdownStandupUI);
                sitdownStandupUI.GetComponentInChildren<TextMeshProUGUI>().text = "CLICK ĐỂ ĐỨNG";
                break;
            case TutorialStepType.Skip:
                OpenUI(skipVideoUI);
                break;
        }
        this.index = index;
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
    }
}
