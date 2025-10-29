using System;
using System.Collections.Generic;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;

public class CourseReviewUI : MonoBehaviour
{
    public GameObject container;

    public Button returnBtn;
    public ChapterReviewCourseUI chapterUIPrefab;
    public LessonUI lessonUIPrefab;
    public List<ChapterReviewCourseUI> chapterList = new();
    public void ReviewBook(BookHandler bookHandler)
    {
        // xử lý review
        Debug.Log($"Book Name {bookHandler.book_name}");
        Debug.Log($"Book SKU {bookHandler.book_sku}");
        Debug.Log($"Book Seo {bookHandler.book_seo}");
    }

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
        Show();
    }

    private void BuildData(LmsCoursePrivate lmsCoursePrivate)
    {
        if (lmsCoursePrivate.chapters != null)
        {
            foreach (var ch in lmsCoursePrivate.chapters)
            {
                if (ch == null) continue;

                // Header CHAPTER (nếu có tên)
                string chapTitle = string.IsNullOrEmpty(ch.chapterTitle) ? "" : ch.chapterTitle.Trim();
                ChapterReviewCourseUI headerChapter = null;
                if (!string.IsNullOrEmpty(chapTitle))
                {
                    headerChapter = Instantiate(chapterUIPrefab, container.transform);
                    //headerChapter.transform.SetParent(content, false);
                    //EnsureItemLayout((RectTransform)headerChapter.transform);
                    //SetLabel(headerChapter, "Chapter", chapTitle);
                    headerChapter.titleName.text = $"{chapTitle}";
                    // headerChapter.SetUnlock();
                }
                chapterList.Add(headerChapter);

                // Các bài học trong chapter
                if (ch.lessons == null) continue;
                foreach (var lesson in ch.lessons)
                {
                    if (lesson == null) continue;

                    string lessonTitle = string.IsNullOrEmpty(lesson.title) ? "" : lesson.title.Trim();
                    if (string.IsNullOrEmpty(lessonTitle)) continue; // ẩn bài không tên

                    string link2 = !string.IsNullOrEmpty(lesson.videoLink2) ? lesson.videoLink2 :
                        (!string.IsNullOrEmpty(lesson.videoLink) ? lesson.videoLink : "");

                    var lessonUI = Instantiate(lessonUIPrefab, headerChapter.lessonContainer.transform);
                    //lessonUI.transform.SetParent(headerChapter.lessonContainer.transform, false);
                    //EnsureItemLayout((RectTransform)lessonUI.transform);

                    // Luôn hiển thị vào Title (kể cả “Câu hỏi …”), QA ẩn
                    //SetLabel(lessonUI, "Title", lessonTitle);
                    //SetLabel(lessonUI, "QA", ""); // rỗng -> bị ẩn
                    lessonUI.titleTMP.text = $"{lessonTitle}";
                    // Click phát video

                    lessonUI.linkVideo2 = link2;
                    
                    Debug.Log($"Title {lesson.title} Condition {lesson.completionCondition.condition} Percent {lesson.completionCondition.percent}");
                }
            }
        }
    }

    public void Select(ChapterReviewCourseUI chapterReviewCourseUI)
    {
        foreach (var chapter in chapterList)
        {
            if (chapter == chapterReviewCourseUI)
            {
                chapter.Highlight();
            }
            else
            {
                chapter.UnHighlight();
            }   
        }
    }
}