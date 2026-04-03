using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;

public class CourseShareLink : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button shareButton;

    private string shareTemplate =
            "Tham gia khóa học \"{COURSE_NAME}\" ngay nào cả nhà!\n{COURSE_URL}";

    [Header("Optional")]
    [SerializeField] private bool autoUpdateButtonState = true;

    private void Awake()
    {
        if (shareButton != null)
            shareButton.onClick.AddListener(HandleShare);
    }

    private void OnEnable()
    {
        CourseDetailStaticStore.OnChanged += HandleCourseChanged;
        RefreshUIState();
    }

    private void OnDisable()
    {
        CourseDetailStaticStore.OnChanged -= HandleCourseChanged;
    }

    private void OnDestroy()
    {
        if (shareButton != null)
            shareButton.onClick.RemoveListener(HandleShare);
    }

    private void HandleCourseChanged()
    {
        RefreshUIState();
    }

    private void RefreshUIState()
    {
        if (!autoUpdateButtonState || shareButton == null)
            return;

        shareButton.interactable = CanShareCurrentCourse();
    }

    private bool CanShareCurrentCourse()
    {
        return !string.IsNullOrWhiteSpace(GetCurrentCourseId());
    }

    private void HandleShare()
    {
        string courseId = GetCurrentCourseId();
        if (string.IsNullOrWhiteSpace(courseId))
        {
            Debug.LogWarning("[CourseShareLink] CurrentCourseId is empty, cannot share.");
            return;
        }

        string shareText = BuildShareText(GetCurrentCourseName());
        if (string.IsNullOrWhiteSpace(shareText))
        {
            Debug.LogWarning("[CourseShareLink] Share text is empty.");
            return;
        }

        ShareTextOnly(shareText);
    }

    private string GetCurrentCourseId()
    {
        return CourseDetailStaticStore.CurrentCourseId;
    }

    private string GetCurrentCourseName()
    {
        if (CourseDetailStaticStore.CurrentDetail != null &&
            !string.IsNullOrWhiteSpace(CourseDetailStaticStore.CurrentDetail.title))
        {
            return CourseDetailStaticStore.CurrentDetail.title;
        }

        if (CourseDetailStaticStore.CurrentCourseFlow != null &&
            !string.IsNullOrWhiteSpace(CourseDetailStaticStore.CurrentCourseFlow.title))
        {
            return CourseDetailStaticStore.CurrentCourseFlow.title;
        }

        return "này";
    }

    private string BuildShareText(string courseName)
    {
        string courseUrl = BuildCourseUrl();
        string finalCourseName = string.IsNullOrWhiteSpace(courseName) ? "này" : courseName;

        return shareTemplate
            .Replace("{COURSE_NAME}", finalCourseName)
            .Replace("{COURSE_URL}", courseUrl);
    }

    private string BuildCourseUrl()
    {
        string seoUrl = CourseDetailStaticStore.CurrentDetail.seo.url;

        // nếu backend trả về dạng "/khoa-hoc/abc"
        if (seoUrl.StartsWith("/"))
            return SecurityConfig.UrlWeb + seoUrl;

        // nếu đã là full url thì dùng luôn
        if (seoUrl.StartsWith("http"))
            return seoUrl;

        // fallback nếu format lạ
        return SecurityConfig.UrlWeb + "/course-detail/" + seoUrl;
    }

    private void ShareTextOnly(string text)
    {
        try
        {
            new NativeShare()
                .SetSubject("Chia sẻ khóa học")
                .SetText(text)
                .Share();
        }
        catch (System.Exception e)
        {
            Debug.LogError("[CourseShareLink] Share failed: " + e);
        }
    }
}