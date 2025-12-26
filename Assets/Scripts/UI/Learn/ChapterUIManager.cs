using System;
using System.Collections.Generic;
using UnityEngine;

public class ChapterUIManager : MonoBehaviour
{
    public static ChapterUIManager Instance;
    private List<ChapterUI> chaptersList = new();
    public ChapterUI currentChapter;
    public ChapterUI finalExamChapter;
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
        if (chaptersList.Contains(chapterUI) == false)
        {
            chaptersList.Add(chapterUI);
        }
    }
    
    [ContextMenu("UpdateLessonProgress")]
    public void UpdateLessonProgress()
    {
        if (chaptersList.Count == 0) return;

        // snapshot completion status so changing UI states doesn't affect checks
        var completed = new bool[chaptersList.Count];
        for (int i = 0; i < chaptersList.Count; i++)
        {
            completed[i] = chaptersList[i].IsCompleteAll();
        }

        // decide state for the first chapter (usually unlocked)
        chaptersList[0].ChangeState(ChapterUI.ChapterState.Normal);

        for (int i = 1; i < chaptersList.Count; i++)
        {
            var isUnlockAll = completed[i - 1];
            var state = isUnlockAll ? ChapterUI.ChapterState.Normal : ChapterUI.ChapterState.Lock;
            chaptersList[i].ChangeState(state);

            Debug.Log($"Chapter :{chaptersList[i].titleName.text} is Unlock all {isUnlockAll}");

            //string title = chaptersList[i].titleName.text.Trim();
            //if (title == "Bài thi cuối khóa")
            //{
            //    Debug.Log($"Dang bật tự động mở khóa bài thì, nhớ tắt khi build",gameObject);
            //    chaptersList[i].ChangeState(ChapterUI.ChapterState.Normal);
            //    Debug.Log("Final Exam ALWAYS UNLOCKED for testing.");
            //    continue;
            //}
        }
        // reapply selection state after updating progress so selection doesn't affect completion checks
        if (currentChapter != null && chaptersList.Contains(currentChapter))
        {
            currentChapter.ChangeState(ChapterUI.ChapterState.Select);
        }
    }

    public void Select(ChapterUI chapter)
    {
        // update previous chapter
        var previousChapter = currentChapter;
        if (previousChapter != null)
        {
            previousChapter.ChangeState(ChapterUI.ChapterState.Normal);
            previousChapter.ResetLessonState();
        }

        // update current chapter
        currentChapter = chapter;

        // update progress first (will reapply selection)
        UpdateLessonProgress();
    }

    public bool IsSelectChapter(ChapterUI chapterUI)
    {
        return currentChapter == chapterUI;
    }
}