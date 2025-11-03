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

    public void AddToList(ChapterUI chapterUI)
    {
        chaptersList.Add(chapterUI);
    }

    public void Select(ChapterUI chapter)
    {
        currentChapter?.UnHighlight();
        currentChapter = chapter;
        currentChapter?.Highlight();
    }

    public bool IsSelectChapter(ChapterUI chapterUI)
    {
        return currentChapter == chapterUI;
    }
}