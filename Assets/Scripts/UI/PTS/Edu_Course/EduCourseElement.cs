using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class EduCourseElement : MonoBehaviour
{
    public static event Action<string> CourseOpenRequested;

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

    [Header("Debug")]
    [SerializeField] private bool debugImageLog;

    [SerializeField] private UnityEvent OnChangeViewClicked;

    private const int CourseImageSize = 512;

    private string _courseId;
    private string _imageUrl;
    private string _runtimeImageUrl;

    private int _imageRequestId;
    private Coroutine _waitDataRoutine;

    private Sprite _runtimeSprite;
    private GameObject _imageLoadingHandle;
    private int _imageLoadVersion;
    private bool _imageLoadRequested;

    private static bool _isLoadingCourse;

    private void Awake()
    {
        if (goToDetailBtn != null)
            goToDetailBtn.onClick.AddListener(GoToDetail);
    }

    private void OnEnable()
    {
        if (_imageLoadRequested && !string.IsNullOrWhiteSpace(_imageUrl) && _runtimeSprite == null)
            ApplyOrLoadImage();
    }

    private void OnDisable()
    {
        StopImageLoad();
        HideImageLoading();

        if (_waitDataRoutine != null)
        {
            StopCoroutine(_waitDataRoutine);
            _waitDataRoutine = null;
            _isLoadingCourse = false;
        }
    }

    private void OnDestroy()
    {
        if (goToDetailBtn != null)
            goToDetailBtn.onClick.RemoveListener(GoToDetail);

        StopImageLoad();
        HideImageLoading();

        if (_waitDataRoutine != null)
        {
            StopCoroutine(_waitDataRoutine);
            _waitDataRoutine = null;
            _isLoadingCourse = false;
        }

        ReleaseRuntimeImage();
    }

    public void Setup(CourseListItemData data, bool loadImageImmediately = true)
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

        string nextImageUrl = NormalizeImageUrl(data.image);

        StopImageLoad();
        HideImageLoading();

        if (!string.Equals(_imageUrl, nextImageUrl, StringComparison.Ordinal))
            ReleaseRuntimeImage();

        _imageUrl = nextImageUrl;
        _imageLoadRequested = loadImageImmediately;

        if (courseImg != null)
        {
            courseImg.enabled = true;

            if (_runtimeSprite != null &&
                string.Equals(_runtimeImageUrl, _imageUrl, StringComparison.Ordinal))
            {
                courseImg.sprite = _runtimeSprite;
                HideImageLoading();
            }
            else
            {
                courseImg.sprite = fallbackSprite;

                if (TryApplyCachedImage())
                    return;

                if (!string.IsNullOrWhiteSpace(_imageUrl))
                    ShowImageLoading();

                if (loadImageImmediately)
                    ApplyOrLoadImage();
            }
        }
    }

    public void LoadImageNow()
    {
        _imageLoadRequested = true;

        if (TryApplyCachedImage())
            return;

        if (!string.IsNullOrWhiteSpace(_imageUrl))
            ShowImageLoading();

        ApplyOrLoadImage();
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

        CourseOpenRequested?.Invoke(_courseId);

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

    private void ShowImageLoading()
    {
        if (courseImg == null)
            return;

        _imageLoadingHandle = LoadingUI.ShowInside(courseImg.rectTransform);
    }

    private void HideImageLoading()
    {
        LoadingUI.HideInside(_imageLoadingHandle);
        _imageLoadingHandle = null;
    }

    private void ApplyOrLoadImage()
    {
        if (courseImg == null || string.IsNullOrWhiteSpace(_imageUrl))
        {
            HideImageLoading();
            return;
        }

        if (TryApplyCachedImage())
            return;

        if (_imageRequestId != 0)
            return;

        ShowImageLoading();
        int version = ++_imageLoadVersion;
        string requestUrl = _imageUrl;
        _imageRequestId = CourseImageRuntimeCache.Request(requestUrl, CourseImageSize, (sprite, error) =>
        {
            if (version != _imageLoadVersion || !string.Equals(requestUrl, _imageUrl, StringComparison.Ordinal))
                return;

            _imageRequestId = 0;

            if (sprite == null)
            {
                if (debugImageLog)
                    Debug.LogWarning($"[EduCourseElement] Load image failed: {_courseId} | {requestUrl} | {error}");

                HideImageLoading();
                return;
            }

            ApplyImageSprite(requestUrl, sprite);

            if (debugImageLog)
                Debug.Log($"[EduCourseElement] Image applied: {_courseId} | {requestUrl}");
        });
    }

    private bool TryApplyCachedImage()
    {
        if (string.IsNullOrWhiteSpace(_imageUrl))
            return false;

        if (CourseImageRuntimeCache.TryGet(_imageUrl, CourseImageSize, out var cachedSprite))
        {
            ApplyImageSprite(_imageUrl, cachedSprite);
            return true;
        }

        return false;
    }

    private void ApplyImageSprite(string url, Sprite sprite)
    {
        _runtimeSprite = sprite;
        _runtimeImageUrl = url;
        HideImageLoading();

        if (courseImg == null)
            return;

        courseImg.sprite = sprite;
        courseImg.enabled = true;
    }

    private void StopImageLoad()
    {
        _imageLoadVersion++;

        if (_imageRequestId == 0)
            return;

        CourseImageRuntimeCache.Cancel(_imageRequestId);
        _imageRequestId = 0;
    }

    private void ReleaseRuntimeImage()
    {
        if (courseImg != null && courseImg.sprite == _runtimeSprite)
            courseImg.sprite = fallbackSprite;

        HideImageLoading();
        _runtimeSprite = null;
        _runtimeImageUrl = null;
    }

    private static string NormalizeImageUrl(string raw)
    {
        return CourseImageRuntimeCache.NormalizeUrl(raw);
    }
}
