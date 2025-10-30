using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CourseReviewUI : MonoBehaviour
{
    [SerializeField]  GameObject container;
    public Button returnBtn;
    [SerializeField]  ChapterReviewCourseUI chapterUIPrefab;
    [SerializeField]  LessonReviewUI lessonUIPrefab;
    [SerializeField]  List<ChapterReviewCourseUI> chapterList = new();
    [SerializeField] PlayVideoOpenBook playVideoOpenBook;
    [SerializeField] private BookPageCreator bookPageCreator;
    public void Show()
    {
        container.gameObject.SetActive(true);
    }

    public void Hide()
    {
        container.gameObject.SetActive(false);
    }

    public void RefreshCourseUI(LmsCoursePrivate lmsCoursePrivate)
    {
        Debug.Log("Refresh preview course UI");
        if (lmsCoursePrivate == null)
        {
            Debug.LogError("course private is null");
            return;
        }
        BuildData(lmsCoursePrivate);
    }


    private void BuildData(LmsCoursePrivate lmsCoursePrivate)
    {
        bookPageCreator.ClearExistPages();

        if (lmsCoursePrivate.chapters == null || lmsCoursePrivate.chapters.Count == 0)
        {
            Debug.Log("Chapter đang null, không thể load khóa học");
            return;
        }

        int index = 0;
        foreach (var ch in lmsCoursePrivate.chapters)
        {
            var page = bookPageCreator.TryGetOrCreatePageHolder();
            var header = BuildChapter(ch,page);

            foreach (var lesson in ch.lessons)
            {
                BuildLesson(header, lesson);
            }
        }

        bookPageCreator.InitFirstPage();
    }

    private ChapterReviewCourseUI BuildChapter(LmsChapter ch,Transform content)
    {
        if (chapterUIPrefab == null || container == null) return null;

        string chapTitle = string.IsNullOrEmpty(ch.chapterTitle) ? "" : ch.chapterTitle.Trim();
        var headerChapter = Instantiate(chapterUIPrefab, content);
        if (headerChapter == null) return null;

        if (headerChapter.titleName != null)
            headerChapter.titleName.text = chapTitle;
        headerChapter.courseReviewUI = this;

        chapterList.Add(headerChapter);
        return headerChapter;
    }

    private void BuildLesson(ChapterReviewCourseUI headerChapter, LmsPrivateLesson lesson)
    {
        if (lessonUIPrefab == null || headerChapter == null) return;

        string lessonTitle = string.IsNullOrEmpty(lesson.title) ? "" : lesson.title.Trim();

        int.TryParse(lesson.duration,out int value);
        string duration = FormatFromSeconds(value);
        if (string.IsNullOrEmpty(lessonTitle)) return; // skip unnamed lessons


        if (headerChapter.lessonContainer == null)
        {
            Debug.LogWarning("Header chapter lessonContainer is null, skipping lesson instantiation");
            return;
        }

        var lessonUI = Instantiate(lessonUIPrefab, headerChapter.lessonContainer.transform);
        if (lessonUI == null) return;

        if (lessonUI.titleTMP != null)
            lessonUI.titleTMP.text = lessonTitle;
        if (lessonUI.duration != null)
        {
            lessonUI.duration.text = duration;
        }
    }
    private string FormatFromSeconds(int totalSeconds)
    {
        totalSeconds = Math.Max(0, totalSeconds);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        return $"{minutes:D2} phút {seconds:D2} giây";
    }
    private ChapterReviewCourseUI chapterReviewCourseUI;

    public void Select(ChapterReviewCourseUI chapterReviewCourseUI)
    {
        if (chapterReviewCourseUI == null)
        {
            foreach (var chapter in chapterList)
            {
                chapter.UnHighlight();
                chapter.ShowActiveUI(false);
            }

            return;
        }
        
        
        Debug.Log("Select Chapter: " + chapterList.Count);
        foreach (var chapter in chapterList)
        {
            if (chapter == chapterReviewCourseUI)
            {
                chapter.Highlight();
                chapter.ToggleOn();
            }
            else
            {
                chapter.UnHighlight();
                chapter.ToggleOff();
            }   
            chapter.ShowActiveUI(chapter == chapterReviewCourseUI);
        }

        this.chapterReviewCourseUI = chapterReviewCourseUI;
    }
}