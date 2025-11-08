using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChapterUI : ChapterBaseUI
{
    public string chapterID;
    [Header("References (chapter)")]
    public Button bannerBtn;

    [Header("Setting")]
    [SerializeField] private bool isOpenSerialized; // preserved for inspector compatibility

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

    protected override void Awake()
    {
        base.Awake();
        if (bannerBtn != null)
        {
            bannerBtn.onClick.AddListener(SelectThisChapter);
        }
        ShowActiveUI(false);
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
        if (ChapterUIManager.Instance.IsSelectChapter(this))
        {
            return;
        }
        ChapterUIManager.Instance.Select(this);
    }

    public override void ToggleOn()
    {
        base.ToggleOn();
        if (!ChapterUIManager.Instance.IsSelectChapter(this))
        {
            ChapterUIManager.Instance.Select(this);
        }
        ShowActiveUI(true);
        lessonContainer.gameObject.SetActive(true);
        
    }

    public override void ToggleOff()
    {
        base.ToggleOff();
        ShowActiveUI(false);
        lessonContainer.gameObject.SetActive(false);
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
        Debug.Log("Highlight");
        ToggleOn();
    }

    [ContextMenu("UnHighlight")]
    public void UnHighlight()
    {
        Debug.Log("UnHighlight");
        ToggleOff();
    }

    public void SetUnlock(bool unlock)
    {
        if (scrollActiveImg != null) scrollActiveImg.sprite = unlock ? scrollActiveUnlock : scrollActiveLock;
        if (scrollDeActiveImg != null) scrollDeActiveImg.sprite = unlock ? scrollDeActiveUnlock : scrollDeActiveLock;
        if (titleName != null) titleName.color = unlock ? finishColor : unFinishColor;
    }
    
    [ContextMenu("SetUnlock UI")]
    public void SetUnLockUI() => SetUnlock(true);
    [ContextMenu("SetLock UI")]
    public void SetLockUI() => SetUnlock(false);
    
}
