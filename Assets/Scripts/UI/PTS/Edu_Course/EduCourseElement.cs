using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.UI;

public class EduCourseElement : MonoBehaviour
{
    [SerializeField] private Image courseImg;
    [SerializeField] private TextMeshProUGUI courseTitle;
    [SerializeField] private TextMeshProUGUI courseDate;
    [SerializeField] private TextMeshProUGUI coursSeatTmp;
    [SerializeField] private Button goToDetailBtn;
    [SerializeField] private CourseTagHandle courseTag;

    [SerializeField] private CourseDetailLoader courseDetailLoader;
    [SerializeField] private CourseReviewLoader courseReviewLoader;

    [Header("Fallback")]
    [SerializeField] private Sprite fallbackSprite;

    [SerializeField] private UnityEvent OnChangeViewClicked;

    private string _courseId;
    private Coroutine _loadImageRoutine;
    private Coroutine _waitDataRoutine;

    private static bool _isLoadingCourse;

    private void Awake()
    {
        if (goToDetailBtn != null)
            goToDetailBtn.onClick.AddListener(GoToDetail);
    }

    private void OnDestroy()
    {
        if (goToDetailBtn != null)
            goToDetailBtn.onClick.RemoveListener(GoToDetail);

        if (_loadImageRoutine != null)
            StopCoroutine(_loadImageRoutine);

        if (_waitDataRoutine != null)
            StopCoroutine(_waitDataRoutine);
    }

    public void Setup(CourseModels.CourseLite data)
    {
        if (data == null) return;

        _courseId = data._id;

        if (courseTitle != null)
            courseTitle.text = string.IsNullOrWhiteSpace(data.title) ? "Khóa học" : data.title;

        if (courseDate != null)
        {
            string startDateText = GetFirstStartDateText(data.courseStartDate);
            courseDate.text = string.IsNullOrWhiteSpace(startDateText)
                ? "Chưa có lịch khai giảng"
                : startDateText;
        }

        if (coursSeatTmp != null)
        {
            int learners = data.learners > 0 ? data.learners : data.totalStudent;
            coursSeatTmp.text = learners.ToString();
        }

        if (courseTag != null)
        {
            // setup tag nếu cần
        }

        if (_loadImageRoutine != null)
            StopCoroutine(_loadImageRoutine);

        if (courseImg != null)
        {
            courseImg.sprite = fallbackSprite;

            if (!string.IsNullOrWhiteSpace(data.image))
                _loadImageRoutine = StartCoroutine(LoadImage(data.image));
        }
    }

    private void GoToDetail()
    {
        if (_isLoadingCourse)
            return;

        if (string.IsNullOrEmpty(_courseId))
        {
            Debug.LogWarning("[EduCourseElement] courseId is null/empty");
            return;
        }

        if (_waitDataRoutine != null)
            StopCoroutine(_waitDataRoutine);

        _isLoadingCourse = true;

        Debug.Log($"[EduCourseElement] Start load detail/review for courseId = {_courseId}");

        courseDetailLoader.Load(_courseId);
        courseReviewLoader.LoadReviews(_courseId);

        _waitDataRoutine = StartCoroutine(WaitAllDataThenShow(_courseId));
    }

    private IEnumerator WaitAllDataThenShow(string courseId)
    {
        float timeout = 10f;
        float t = 0f;

        while (t < timeout)
        {
            bool detailDone = IsCourseDetailLoaded(courseId);
            bool reviewDone = IsCourseReviewLoaded(courseId);

            bool detailError = !string.IsNullOrEmpty(CourseDetailStaticStore.LastError);
            bool reviewError = !string.IsNullOrEmpty(CourseReviewStaticStore.LastError);

            if (detailError)
            {
                Debug.LogWarning("[EduCourseElement] Course detail load error: " + CourseDetailStaticStore.LastError);
                _isLoadingCourse = false;
                _waitDataRoutine = null;
                yield break;
            }

            if (reviewError)
            {
                Debug.LogWarning("[EduCourseElement] Course review load error: " + CourseReviewStaticStore.LastError);

                if (detailDone)
                {
                    _isLoadingCourse = false;
                    _waitDataRoutine = null;
                    OnChangeViewClicked?.Invoke();
                    yield break;
                }
            }

            if (detailDone && reviewDone)
            {
                Debug.Log("[EduCourseElement] Detail + Review loaded successfully");
                _isLoadingCourse = false;
                _waitDataRoutine = null;
                OnChangeViewClicked?.Invoke();
                yield break;
            }

            t += Time.unscaledDeltaTime;
            yield return null;
        }

        Debug.LogWarning("[EduCourseElement] WaitAllDataThenShow timeout");

        if (IsCourseDetailLoaded(courseId))
        {
            _isLoadingCourse = false;
            _waitDataRoutine = null;
            OnChangeViewClicked?.Invoke();
            yield break;
        }

        _isLoadingCourse = false;
        _waitDataRoutine = null;
    }

    private bool IsCourseDetailLoaded(string courseId)
    {
        return CourseDetailStaticStore.HasData
               && !CourseDetailStaticStore.IsLoading
               && CourseDetailStaticStore.CurrentCourseId == courseId
               && CourseDetailStaticStore.CurrentCourse != null;
    }

    private bool IsCourseReviewLoaded(string courseId)
    {
        return CourseReviewStaticStore.CurrentCourseId == courseId
               && !CourseReviewStaticStore.IsLoading
               && string.IsNullOrEmpty(CourseReviewStaticStore.LastError);
    }

    private string GetFirstStartDateText(List<CourseModels.CourseStartDateItem> dates)
    {
        if (dates == null || dates.Count == 0)
            return "";

        var first = dates[0];
        if (first == null || first.start == null)
            return "";

        int day = first.start.day;
        int month = first.start.month;
        int year = first.start.year;

        if (day <= 0 || month <= 0 || year <= 0)
            return "";

        return $"{day:00}/{month:00}/{year}";
    }

    private IEnumerator LoadImage(string url)
    {
        using (var req = UnityWebRequestTexture.GetTexture(url))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                _loadImageRoutine = null;
                yield break;
            }

            var texture = DownloadHandlerTexture.GetContent(req);
            if (texture == null)
            {
                _loadImageRoutine = null;
                yield break;
            }

            var sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );

            if (courseImg != null)
                courseImg.sprite = sprite;
        }

        _loadImageRoutine = null;
    }
}