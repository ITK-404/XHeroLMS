using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CourseReviewUI : MonoBehaviour
{
    [SerializeField] GameObject container;
    public Button returnBtn;
    [SerializeField] ChapterReviewCourseUI chapterUIPrefab;
    [SerializeField] LessonReviewUI lessonUIPrefab;
    [SerializeField] List<ChapterReviewCourseUI> chapterList = new();
    [SerializeField] PlayVideoOpenBook playVideoOpenBook;
    [SerializeField] private BookPageCreator bookPageCreator;

    private ChapterReviewCourseUI chapterReviewCourseUI;

    // === NEW: cache lại course private hiện tại để bạn lấy list video bất cứ lúc nào
    private LmsCoursePrivate _currentPrivate;

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

        _currentPrivate = lmsCoursePrivate; // NEW
        BuildData(lmsCoursePrivate);
    }

    private void BuildData(LmsCoursePrivate lmsCoursePrivate)
    {
        bookPageCreator.ClearExistPages();
        chapterList.Clear();

        if (lmsCoursePrivate.chapters == null || lmsCoursePrivate.chapters.Count == 0)
        {
            Debug.Log("Chapter đang null, không thể load khóa học");
            return;
        }

        foreach (var ch in lmsCoursePrivate.chapters)
        {
            var page = bookPageCreator.TryGetOrCreatePageHolder();
            var header = BuildChapter(ch, page);

            if (ch.lessons != null)
            {
                foreach (var lesson in ch.lessons)
                    BuildLesson(header, lesson);
            }
        }

        var finalExamId = CourseListView.TryGetFinalExamId(lmsCoursePrivate);

        if (!string.IsNullOrEmpty(finalExamId))
        {
            TryHandleFinalExam(finalExamId);
        }

        bookPageCreator.InitFirstPage();
    }

    private void TryHandleFinalExam(string finalExamId)
    {
        LmsChapter lmsChapter = new();
        lmsChapter._id = finalExamId;
        lmsChapter.chapterTitle = "Bài thi cuối khóa";
        var page = bookPageCreator.TryGetOrCreatePageHolder();
        var header = BuildChapter(lmsChapter, page);
        header.ShowFinalExam();
    }

    private ChapterReviewCourseUI BuildChapter(LmsChapter ch, Transform content)
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
        if (lesson == null) return;

        string lessonTitle = string.IsNullOrEmpty(lesson.title) ? "" : lesson.title.Trim();

        int.TryParse(lesson.duration, out int value);
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
            lessonUI.duration.text = duration;
    }

    [SerializeField] private bool isMobilePlatform = false;
    private string FormatFromSeconds(int totalSeconds)
    {
        if (isMobilePlatform)
        {
            totalSeconds = Math.Max(0, totalSeconds);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            return $"{minutes:D2} phút {seconds:D2} giây";
        }
        else
        {
            return $"{totalSeconds / 60:D2}:{totalSeconds % 60:D2}";
        }
    }

    public void Select(ChapterReviewCourseUI selectedChapter)
    {
        // If the clicked chapter is already selected, treat this as a request to deselect.
        if (selectedChapter == this.chapterReviewCourseUI)
        {
            selectedChapter = null;
        }

        if (selectedChapter == null)
        {
            // hide all chapters
            foreach (var chapter in chapterList)
            {
                chapter.UnHighlight();
                chapter.ToggleOff();
                chapter.ShowActiveUI(false);
            }

            this.chapterReviewCourseUI = null;
            return;
        }

        Debug.Log("Select Chapter: " + chapterList.Count);
        foreach (var chapter in chapterList)
        {
            if (chapter == selectedChapter)
            {
                chapter.Highlight();
                chapter.ToggleOn();
            }
            else
            {
                chapter.UnHighlight();
                chapter.ToggleOff();
            }

            chapter.ShowActiveUI(chapter == selectedChapter);
        }

        this.chapterReviewCourseUI = selectedChapter;
    }

    // =====================================================================
    // ======================= GET VIDEO LIST (NEW) =========================
    // =====================================================================

    [Serializable]
    public class VideoLessonInfo
    {
        public string courseId;
        public string courseTitle;

        public string chapterId;
        public string chapterTitle;

        public string lessonId;
        public string lessonTitle;

        public string type;       // "video" ...
        public string videoLink;  // lesson.videoLink
        public string videoLink2; // lesson.videoLink2
        public int durationSec;   // parse từ lesson.duration (nếu được)
    }

    /// <summary>
    /// Lấy danh sách video từ course private đang được show (cache _currentPrivate).
    /// </summary>
    public List<VideoLessonInfo> GetVideoLessonsFromCurrent()
    {
        return GetVideoLessons(_currentPrivate);
    }

    /// <summary>
    /// Lấy danh sách video từ 1 LmsCoursePrivate bất kỳ.
    /// - Lọc theo: lesson.type == "video" (ignore-case) HOẶC có videoLink/videoLink2.
    /// </summary>
    public List<VideoLessonInfo> GetVideoLessons(LmsCoursePrivate p)
    {
        var result = new List<VideoLessonInfo>();
        if (p == null || p.chapters == null) return result;

        for (int ci = 0; ci < p.chapters.Count; ci++)
        {
            var ch = p.chapters[ci];
            if (ch == null || ch.lessons == null) continue;

            string chapTitle = string.IsNullOrEmpty(ch.chapterTitle) ? "" : ch.chapterTitle.Trim();

            for (int li = 0; li < ch.lessons.Count; li++)
            {
                var ls = ch.lessons[li];
                if (ls == null) continue;

                string type = ls.type ?? "";
                bool isVideoType = string.Equals(type, "video", StringComparison.OrdinalIgnoreCase);

                bool hasLink =
                    !string.IsNullOrWhiteSpace(ls.videoLink) ||
                    !string.IsNullOrWhiteSpace(ls.videoLink2);

                if (!isVideoType && !hasLink) continue;

                int durSec = 0;
                if (!string.IsNullOrEmpty(ls.duration)) int.TryParse(ls.duration, out durSec);

                result.Add(new VideoLessonInfo
                {
                    courseId = p._id,
                    courseTitle = p.title,

                    chapterId = ch._id,
                    chapterTitle = chapTitle,

                    lessonId = ls._id,
                    lessonTitle = ls.title,

                    type = type,
                    videoLink = ls.videoLink,
                    videoLink2 = ls.videoLink2,
                    durationSec = durSec
                });
            }
        }

        return result;
    }

    /// <summary>
    /// Debug nhanh để bạn thấy list video trong Console.
    /// </summary>
    public void DebugLogVideoLessons()
    {
        var list = GetVideoLessonsFromCurrent();
        Debug.Log($"[CourseReviewUI] Video lessons count = {list.Count}");

        for (int i = 0; i < list.Count; i++)
        {
            var v = list[i];
            Debug.Log($"  [{i}] chap='{v.chapterTitle}' lesson='{v.lessonTitle}' " +
                      $"link='{(string.IsNullOrEmpty(v.videoLink) ? v.videoLink2 : v.videoLink)}' durSec={v.durationSec}");
        }
    }
}
