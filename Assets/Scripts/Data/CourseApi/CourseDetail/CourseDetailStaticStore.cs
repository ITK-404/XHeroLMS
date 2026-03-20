using System;
using System.Collections.Generic;

public static class CourseDetailStaticStore
{
    public static string CurrentCourseId { get; private set; }

    // Giữ raw detail mới
    public static CourseModels.CourseDetail CurrentDetail { get; private set; }

    // Giữ normalized private-flow để các UI cũ / flow chapter-lesson dùng tiếp
    public static LmsCoursePrivate CurrentCourseFlow { get; private set; }

    public static bool IsLoading { get; private set; }
    public static string LastError { get; private set; }

    public static bool HasData =>
        !string.IsNullOrEmpty(CurrentCourseId) &&
        (CurrentDetail != null || CurrentCourseFlow != null);

    public static event Action OnChanged;

    public static void Reset()
    {
        CurrentCourseId = null;
        CurrentDetail = null;
        CurrentCourseFlow = null;
        IsLoading = false;
        LastError = null;
        OnChanged?.Invoke();
    }

    public static void SetLoading(string courseId)
    {
        CurrentCourseId = courseId;
        CurrentDetail = null;
        CurrentCourseFlow = null;
        IsLoading = true;
        LastError = null;
        OnChanged?.Invoke();
    }

    public static void SetCourse(string courseId, CourseModels.CourseDetail detail)
    {
        CurrentCourseId = courseId;
        CurrentDetail = detail;
        CurrentCourseFlow = ConvertToCourseFlow(detail);
        IsLoading = false;
        LastError = null;
        OnChanged?.Invoke();
    }

    public static void SetError(string courseId, string error)
    {
        CurrentCourseId = courseId;
        CurrentDetail = null;
        CurrentCourseFlow = null;
        IsLoading = false;
        LastError = error;
        OnChanged?.Invoke();
    }

    public static bool IsCurrent(string courseId)
    {
        return !string.IsNullOrEmpty(courseId) &&
               string.Equals(CurrentCourseId, courseId, StringComparison.OrdinalIgnoreCase);
    }

    public static string GetDescription()
    {
        if (CurrentDetail != null)
            return CurrentDetail.description;

        return CurrentCourseFlow != null ? CurrentCourseFlow.description : null;
    }

    public static string GetFirstBanner()
    {
        if (CurrentDetail != null &&
            CurrentDetail.banner != null &&
            CurrentDetail.banner.Length > 0)
            return CurrentDetail.banner[0];

        if (CurrentCourseFlow != null &&
            CurrentCourseFlow.banner != null &&
            CurrentCourseFlow.banner.Count > 0)
            return CurrentCourseFlow.banner[0];

        return null;
    }

    public static List<LmsChapter> GetChaptersFlow()
    {
        return CurrentCourseFlow != null ? CurrentCourseFlow.chapters : null;
    }

    public static LmsCoursePrivate GetCourseFlow()
    {
        return CurrentCourseFlow;
    }

    public static void ClearIfCurrent(string courseId)
    {
        if (IsCurrent(courseId))
            Reset();
    }

    // =========================
    // CONVERTER: Detail mới -> Flow cũ
    // =========================
    private static LmsCoursePrivate ConvertToCourseFlow(CourseModels.CourseDetail detail)
    {
        if (detail == null)
            return null;

        var result = new LmsCoursePrivate
        {
            _id = detail._id,
            title = detail.title,
            description = detail.description,
            introduction = detail.introduction,
            image = detail.image,
            videoLink = null, // nếu API detail mới không trả thì để null
            finalExam = "",

            banner = detail.banner != null ? new List<string>(detail.banner) : new List<string>(),

            seo = detail.seo != null
                ? new SeoInfo
                {
                    url = detail.seo.url,
                    title = null,
                    description = null,
                    keywords = detail.seo.keywords != null
                        ? new List<string>(detail.seo.keywords)
                        : new List<string>()
                }
                : null,

            instructor = detail.instructor != null
                ? new LmsInstructor
                {
                    _id = detail.instructor._id,
                    fullName = detail.instructor.fullName,
                    description = detail.instructor.description,
                    learners = detail.instructor.learners,
                    courses = detail.instructor.courses
                }
                : null,

            totalDuration = detail.totalDuration,
            stars = detail.stars,
            evaluate = detail.evaluate,

            upsell = ConvertUpsell(detail.upsell),
            products = ConvertProducts(detail.products),
            chapters = ConvertChapters(detail.chapters),

            coursePrice = detail.coursePrice != null
                ? new LmsCoursePrice
                {
                    isFree = detail.coursePrice.isFree,
                    originalPrice = detail.coursePrice.originalPrice,
                    currentPrice = detail.coursePrice.currentPrice,
                    isQuotation = detail.coursePrice.isQuotation,
                    isContract = detail.coursePrice.isContract
                }
                : null
        };

        return result;
    }

    private static List<LmsChapter> ConvertChapters(List<CourseModels.CourseChapter> chapters)
    {
        var result = new List<LmsChapter>();
        if (chapters == null || chapters.Count == 0)
            return result;

        for (int i = 0; i < chapters.Count; i++)
        {
            var ch = chapters[i];
            if (ch == null) continue;

            var newChapter = new LmsChapter
            {
                _id = ch._id,
                type = null,
                chapterTitle = !string.IsNullOrEmpty(ch.title) ? ch.title : "",
                lessons = ConvertLessons(ch.lessons)
            };

            result.Add(newChapter);
        }

        return result;
    }

    private static List<LmsPrivateLesson> ConvertLessons(List<CourseModels.CourseLesson> lessons)
    {
        var result = new List<LmsPrivateLesson>();
        if (lessons == null || lessons.Count == 0)
            return result;

        for (int i = 0; i < lessons.Count; i++)
        {
            var lesson = lessons[i];
            if (lesson == null) continue;

            result.Add(new LmsPrivateLesson
            {
                _id = lesson._id,
                title = lesson.title,
                type = null,
                videoLink = null,
                videoLink2 = null,
                duration = null,
                progressTime = -1,
                completionCondition = null
            });
        }

        return result;
    }

    private static List<LmsProduct> ConvertProducts(List<CourseModels.CourseProduct> products)
    {
        var result = new List<LmsProduct>();
        if (products == null || products.Count == 0)
            return result;

        for (int i = 0; i < products.Count; i++)
        {
            var p = products[i];
            if (p == null) continue;

            result.Add(new LmsProduct
            {
                productName = p.productName,
                image = p.image,
                externalUrl = p.externalUrl
            });
        }

        return result;
    }

    private static List<LmsRelatedCourse> ConvertUpsell(List<CourseModels.CourseRelated> upsell)
    {
        var result = new List<LmsRelatedCourse>();
        if (upsell == null || upsell.Count == 0)
            return result;

        for (int i = 0; i < upsell.Count; i++)
        {
            var u = upsell[i];
            if (u == null) continue;

            result.Add(new LmsRelatedCourse
            {
                _id = u._id,
                title = u.title,
                image = u.image,
                learners = u.learners,
                stars = (int)u.stars
            });
        }

        return result;
    }
}