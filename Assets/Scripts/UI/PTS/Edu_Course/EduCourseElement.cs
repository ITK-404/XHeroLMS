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

    private Texture2D _runtimeTexture;
    private Sprite _runtimeSprite;

    private static bool _isLoadingCourse;

    private void Awake()
    {
        if (goToDetailBtn != null)
            goToDetailBtn.onClick.AddListener(GoToDetail);
    }

    private void OnDisable()
    {
        if (_loadImageRoutine != null)
        {
            StopCoroutine(_loadImageRoutine);
            _loadImageRoutine = null;
        }

        if (_waitDataRoutine != null)
        {
            StopCoroutine(_waitDataRoutine);
            _waitDataRoutine = null;
        }

        // Quan trọng khi dùng pooling:
        // item bị SetActive(false) phải nhả texture runtime
        ReleaseRuntimeImage();
    }

    private void OnDestroy()
    {
        if (goToDetailBtn != null)
            goToDetailBtn.onClick.RemoveListener(GoToDetail);

        if (_loadImageRoutine != null)
            StopCoroutine(_loadImageRoutine);

        if (_waitDataRoutine != null)
            StopCoroutine(_waitDataRoutine);

        ReleaseRuntimeImage();
    }

    public void Setup(CourseListItemData data)
    {
        if (data == null) return;

        _courseId = data.id;

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
            coursSeatTmp.text = $"<b>{FormatNumber(learners)}</b> lượt đặt chỗ";
        }

        if (courseTag != null)
            courseTag.ShowLearningMode(data.learningMode);

        if (_loadImageRoutine != null)
        {
            StopCoroutine(_loadImageRoutine);
            _loadImageRoutine = null;
        }

        // Mỗi lần setup item mới, phải nhả ảnh runtime cũ trước
        ReleaseRuntimeImage();

        if (courseImg != null)
        {
            courseImg.sprite = fallbackSprite;

            if (!string.IsNullOrWhiteSpace(data.image))
                _loadImageRoutine = StartCoroutine(LoadImage(data.image));
        }
    }

    public static string FormatNumber(int value)
    {
        if (value < 1000)
            return value.ToString();

        float num = value / 1000f;
        return num.ToString("0.#") + "k";
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

        if (courseDetailLoader == null || courseReviewLoader == null)
        {
            Debug.LogWarning("[EduCourseElement] Missing courseDetailLoader or courseReviewLoader");
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
               && CourseDetailStaticStore.CurrentDetail != null;
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
        using (var req = UnityWebRequestTexture.GetTexture(url, false))
        {
            yield return req.SendWebRequest();

            if (req.result != UnityWebRequest.Result.Success)
            {
                _loadImageRoutine = null;
                yield break;
            }

            var downloadedTexture = DownloadHandlerTexture.GetContent(req);
            // Resize về 512
            var resizedTexture = downloadedTexture.Resize(512);
            // Hủy texture gốc sau khi resize xong
            Destroy(downloadedTexture);

            if (resizedTexture == null)
            {
                _loadImageRoutine = null;
                yield break;
            }

            _runtimeTexture = resizedTexture;
            _runtimeSprite = Sprite.Create(
                _runtimeTexture,
                new Rect(0, 0, _runtimeTexture.width, _runtimeTexture.height),
                new Vector2(0.5f, 0.5f),
                100f
            );

            if (courseImg != null)
                courseImg.sprite = _runtimeSprite;
        }

        _loadImageRoutine = null;
    }

    private void ReleaseRuntimeImage()
    {
        if (courseImg != null && courseImg.sprite == _runtimeSprite)
            courseImg.sprite = fallbackSprite;

        if (_runtimeSprite != null)
        {
            Destroy(_runtimeSprite);
            _runtimeSprite = null;
        }

        if (_runtimeTexture != null)
        {
            Destroy(_runtimeTexture);
            _runtimeTexture = null;
        }
    }
}
