using NUnit.Framework;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChapterUI : MonoBehaviour
{
    [Header("References")]
    public TextMeshProUGUI titleName;
    public GameObject lessonContainer;
    public Button toggleOpenBtn;
    public Button toggleOffBtn;
    public Button bannerBtn;
    public GameObject activeGroup; 
    public GameObject deActiveGroup; 
    [Header("Setting")]
    [SerializeField] private bool isOpen = false;

    public List<LessonUI> lessonList = new();

    public Color finishColor;
    public Color unFinishColor;

    [Header("Sprite")]
    public Sprite scrollActiveUnlock;
    public Sprite scrollActiveLock;
    public Sprite scrollDeActiveUnlock;
    public Sprite scrollDeActiveLock;
    public Image scrollActiveImg;
    public Image scrollDeActiveImg;

    private bool isUnlock = false;
    private void Awake()
    {
        toggleOpenBtn.onClick.AddListener(ToggleOn);
        toggleOffBtn.onClick.AddListener(ToggleOff);
        bannerBtn.onClick.AddListener(SelectThisChapter);

        UnHighlight();
    }

    private void OnDestroy()
    {
        toggleOpenBtn.onClick.RemoveListener(ToggleOn);
        toggleOffBtn.onClick.RemoveListener(ToggleOff);
        bannerBtn.onClick.RemoveListener(SelectThisChapter);
    }

    private void SelectThisChapter()
    {
        Debug.Log("On Select This Chapter");
        if (ChapterUIManager.Instance.IsSelectChapter(this))
        {
            return;
        }
        ChapterUIManager.Instance.Select(this);
    }
    
    private void ToggleOn()
    {
        Debug.Log("Toggle on");
        isOpen = true;
        lessonContainer.gameObject.SetActive(isOpen);
        toggleOpenBtn.gameObject.SetActive(false);
        toggleOffBtn.gameObject.SetActive(true);
    }

    private void ToggleOff()
    {
        Debug.Log("Toggle off");
        isOpen = false;
        lessonContainer.gameObject.SetActive(isOpen);
        toggleOpenBtn.gameObject.SetActive(true);
        toggleOffBtn.gameObject.SetActive(false);
    }

    public void SelectLesson(LessonUI lessonUI)
    {
        foreach (var item in lessonList)
        {
            item.SetActive(item == lessonUI);
        }
    }

    public void AddToList(LessonUI lessonUI)
    {
        lessonList.Add(lessonUI);
    }
    [ContextMenu("Highlight")]
    public void Highlight()
    {
        ToggleOn();
        activeGroup.gameObject.SetActive(true);
        deActiveGroup.gameObject.SetActive(false);
    }

    [ContextMenu("UnHighlight")]
    public void UnHighlight()
    {
        ToggleOff();
        activeGroup.gameObject.SetActive(false);
        deActiveGroup.gameObject.SetActive(true);
    }

    public void SetUnlock(bool unlock)
    {
        scrollActiveImg.sprite = unlock ? scrollActiveUnlock : scrollActiveLock;
        scrollDeActiveImg.sprite = unlock ? scrollDeActiveUnlock : scrollDeActiveLock;
        titleName.color = unlock ? finishColor : unFinishColor;
        
        isUnlock = unlock;
    }
    
    [ContextMenu("SetUnlock UI")]
    public void SetUnLockUI() => SetUnlock(true);
    [ContextMenu("SetLock UI")]
    public void SetLockUI() => SetUnlock(false);
}

public class QuestionUI : MonoBehaviour
{

}
