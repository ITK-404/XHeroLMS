using System.Collections.Generic;
using UnityEngine;

public class ChapterUIManager : MonoBehaviour
{
    public static ChapterUIManager Instance;
    private List<ChapterUI> chaptersList = new();
    private ChapterUI currentChapter;

    private void Awake()
    {
        Instance = this;
    }

    public void ClearList()
    {
        chaptersList.Clear();
    }
    
    public void AddToList(ChapterUI chapterUI)
    {
        chaptersList.Add(chapterUI);
    }
    
    public void UpdateLessonProgress()
    {
        chaptersList[0].ChangeState(ChapterUI.ChapterState.Lock);
        for (int i = 1; i < chaptersList.Count; i++)
        {
            var chapter = chaptersList[i];
            var isUnlockAll = chaptersList[i - 1].IsCompleteAll();
            var state = isUnlockAll ? ChapterUI.ChapterState.Normal : ChapterUI.ChapterState.Lock;
            chapter.ChangeState(state);
        }
    }

    public void Select(ChapterUI chapter)
    {
        // update previous chapter
        var previousChapter = currentChapter;
        if (previousChapter != null)
        {
            previousChapter.ChangeState(ChapterUI.ChapterState.Normal);
        }
        // update current chapter
        currentChapter = chapter;
        if (currentChapter != null)
        {
            currentChapter.ChangeState(ChapterUI.ChapterState.Select);
        }
    }

    public bool IsSelectChapter(ChapterUI chapterUI)
    {
        return currentChapter == chapterUI;
    }
}