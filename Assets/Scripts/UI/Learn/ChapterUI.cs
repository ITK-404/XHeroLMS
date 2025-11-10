using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChapterUI : ChapterBaseUI
{
    public enum ChapterState
    {
        Lock,
        Normal,
        Select
    }

    public string chapterID;

    [Header("References (chapter)")]
    public Button bannerBtn;

    [Header("Setting")]
    [SerializeField] private bool isOpenSerialized; // preserved for inspector compatibility

    public List<LessonUI> lessonList = new();

    [SerializeField] Color finishColor;
    [SerializeField] Color unFinishColor;

    [Header("Sprite")]
    [SerializeField] Sprite scrollActiveUnlock;

    [SerializeField] Sprite scrollActiveLock;
    [SerializeField] Sprite scrollDeActiveUnlock;
    [SerializeField] Sprite scrollDeActiveLock;
    [SerializeField] Image scrollActiveImg;
    [SerializeField] Image scrollDeActiveImg;

    private ChapterUIManager chapterUIManager;

    private ChapterState chapterState;
    public GameObject lockGroup;

    protected override void Awake()
    {
        base.Awake();
        if (bannerBtn != null)
        {
            bannerBtn.onClick.AddListener(SelectThisChapter);
        }
    }

    private void Start()
    {
        chapterUIManager = ChapterUIManager.Instance;
    }

    protected override void OnDestroy()
    {
        if (bannerBtn != null)
        {
            bannerBtn.onClick.RemoveListener(SelectThisChapter);
        }

        base.OnDestroy();
    }

    private void SelectThisChapter()
    {
        Debug.Log("On Select This Chapter");
        if (chapterState == ChapterState.Lock)
        {
            Debug.Log("This chapter is lock, cannot select");
            return;
        }
        // chapter state will handle inside here
        if (!chapterUIManager.IsSelectChapter(this))
        {
            chapterUIManager.Select(this);
        }
        else
        {
            // toggle between 2 UI statee
            if (chapterState == ChapterState.Normal)
            {
                ChangeState(ChapterState.Select);
            }
            else
            {
                ChangeState(ChapterState.Normal);
            }
        }

    }

    public override void ToggleOn()
    {
        if (chapterState == ChapterState.Lock)
        {
            return;
        }
        base.ToggleOn();
        SelectThisChapter();
        UpdateUI();
    }

    public override void ToggleOff()
    {
        if (chapterState == ChapterState.Lock)
        {
            return;
        }
        base.ToggleOff();
        ChangeState(ChapterState.Normal);
        UpdateUI();
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

    public void UpdateUI()
    {
        var currentState = chapterState;
        deActiveGroup.gameObject.SetActive(currentState == ChapterState.Normal);
        activeGroup.gameObject.SetActive(currentState == ChapterState.Select);
        lessonContainer.gameObject.SetActive(currentState == ChapterState.Select);
        lockGroup.gameObject.SetActive(currentState == ChapterState.Lock);
        
        titleName.color = currentState == ChapterState.Lock ? unFinishColor : finishColor;
        titleName.enableVertexGradient = currentState == ChapterState.Normal;
    }

    public bool IsCompleteAll()
    {
        foreach (var lesson in lessonList)
        {
            bool isComplete = lesson.progressTime >= lesson.duration;

            if (isComplete == false)
            {
                return false;
            }
        }

        return true;
    }

    public void ChangeState(ChapterState chapterState)
    {
        this.chapterState = chapterState;
        UpdateUI();
    }
}