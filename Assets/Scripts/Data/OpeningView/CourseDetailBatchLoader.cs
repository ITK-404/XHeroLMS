using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class CourseDetailBatchLoader : MonoBehaviour
{
    [Header("API")]
    private string baseUrl = "";
    [SerializeField] private int timeoutSeconds = 20;

    [Header("Options")]
    [SerializeField] private bool autoLoadOnStart = false;
    [SerializeField] private bool clearStoreBeforeLoad = true;
    [SerializeField] private float delayBetweenRequests = 0.05f;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    private Coroutine _loadRoutine;
    private UnityWebRequest _activeRequest;
    private int _loadVersion = 0;

    public bool IsLoading { get; private set; }

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            if (LmsStore.Instance != null && !string.IsNullOrWhiteSpace(LmsStore.Instance.baseUrl))
                baseUrl = LmsStore.Instance.baseUrl;
        }

        baseUrl = (baseUrl ?? "").Trim().TrimEnd('/');

        if (debugLog)
            Debug.Log($"[CourseDetailBatchLoader] Awake baseUrl='{baseUrl}'");
    }

    private void Start()
    {
        if (autoLoadOnStart)
            LoadAllCourseDetails();
    }

    public void LoadAllCourseDetails()
    {
        var courses = CourseStaticStore.GetAll();
        LoadAllCourseDetails(courses);
    }

    public void LoadAllCourseDetails(IReadOnlyList<CourseModels.CourseLite> courses)
    {
        if (courses == null || courses.Count == 0)
        {
            if (debugLog) Debug.LogWarning("[CourseDetailBatchLoader] No courses to load.");
            if (clearStoreBeforeLoad) CourseDetailSummaryStore.Clear();
            return;
        }

        Dispose();
        _loadVersion++;
        _loadRoutine = StartCoroutine(LoadAllRoutine(courses, _loadVersion));
    }

    public void Dispose()
    {
        IsLoading = false;

        if (_loadRoutine != null)
        {
            StopCoroutine(_loadRoutine);
            _loadRoutine = null;
        }

        if (_activeRequest != null)
        {
            try { _activeRequest.Abort(); } catch { }
            try { _activeRequest.Dispose(); } catch { }
            _activeRequest = null;
        }
    }

    private IEnumerator LoadAllRoutine(IReadOnlyList<CourseModels.CourseLite> courses, int version)
    {
        IsLoading = true;

        if (clearStoreBeforeLoad)
            CourseDetailSummaryStore.Clear();

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            Debug.LogError("[CourseDetailBatchLoader] baseUrl is empty.");
            IsLoading = false;
            yield break;
        }

        var result = new List<CourseDetailSummary>();

        for (int i = 0; i < courses.Count; i++)
        {
            if (version != _loadVersion)
            {
                if (debugLog) Debug.LogWarning("[CourseDetailBatchLoader] Load cancelled by newer request.");
                break;
            }

            var lite = courses[i];
            if (lite == null || string.IsNullOrWhiteSpace(lite._id))
                continue;

            yield return StartCoroutine(FetchSingleCourseDetail(lite, result, version));

            if (delayBetweenRequests > 0f)
                yield return new WaitForSeconds(delayBetweenRequests);
        }

        if (version == _loadVersion)
            CourseDetailSummaryStore.SetAll(result);

        IsLoading = false;
        _loadRoutine = null;
    }

    private IEnumerator FetchSingleCourseDetail(CourseModels.CourseLite lite, List<CourseDetailSummary> result, int version)
    {
        string url = $"{baseUrl}/lms/courses/{lite._id}?t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        if (debugLog)
            Debug.Log($"[CourseDetailBatchLoader] GET: {url}");

        _activeRequest = UnityWebRequest.Get(url);
        _activeRequest.timeout = timeoutSeconds;
        _activeRequest.SetRequestHeader("Cache-Control", "no-cache, no-store, must-revalidate");
        _activeRequest.SetRequestHeader("Pragma", "no-cache");
        _activeRequest.SetRequestHeader("Expires", "0");

        yield return _activeRequest.SendWebRequest();

        if (version != _loadVersion)
        {
            SafeDisposeRequest();
            yield break;
        }

        if (_activeRequest == null)
            yield break;

        if (_activeRequest.result != UnityWebRequest.Result.Success)
        {
            if (debugLog)
                Debug.LogError($"[CourseDetailBatchLoader] Failed courseId={lite._id} | code={_activeRequest.responseCode} | error={_activeRequest.error}");

            SafeDisposeRequest();
            yield break;
        }

        string json = _activeRequest.downloadHandler != null ? _activeRequest.downloadHandler.text : null;
        CourseDetailApiResponse resp = null;

        try
        {
            resp = JsonUtility.FromJson<CourseDetailApiResponse>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[CourseDetailBatchLoader] JSON parse failed for courseId={lite._id} | {e.Message}");
            SafeDisposeRequest();
            yield break;
        }

        if (resp == null || !resp.status || resp.course == null)
        {
            if (debugLog)
                Debug.LogError($"[CourseDetailBatchLoader] Invalid response for courseId={lite._id}");
            SafeDisposeRequest();
            yield break;
        }

        var c = resp.course;

        var summary = new CourseDetailSummary
        {
            courseId = c._id,
            title = !string.IsNullOrWhiteSpace(c.title) ? c.title : lite.title,
            learners = c.learners,
            image = !string.IsNullOrWhiteSpace(c.image) ? c.image : lite.image,
            instructorName = c.instructor != null ? c.instructor.fullName : "",
            startDateText = GetFirstStartDateText(c.courseStartDate),
            totalDuration = c.totalDuration,
            lessonCount = CountLessons(c.chapters)
        };

        result.Add(summary);

        if (debugLog)
        {
            Debug.Log($"[CourseDetailBatchLoader] OK | {summary.title} | learners={summary.learners} | start={summary.startDateText} | lessons={summary.lessonCount}");
        }

        SafeDisposeRequest();
    }

    private string GetFirstStartDateText(List<CourseStartDateItem> dates)
    {
        if (dates == null || dates.Count == 0) return "";

        var first = dates[0];
        if (first == null || first.start == null) return "";

        int day = first.start.day;
        int month = first.start.month;
        int year = first.start.year;

        if (day <= 0 || month <= 0 || year <= 0) return "";
        return $"{day:00}/{month:00}/{year}";
    }

    private int CountLessons(List<CourseChapter> chapters)
    {
        if (chapters == null || chapters.Count == 0) return 0;

        int count = 0;
        for (int i = 0; i < chapters.Count; i++)
        {
            var ch = chapters[i];
            if (ch?.lessons != null)
                count += ch.lessons.Count;
        }
        return count;
    }

    private void SafeDisposeRequest()
    {
        if (_activeRequest != null)
        {
            try { _activeRequest.Dispose(); } catch { }
            _activeRequest = null;
        }
    }

    private void OnDestroy()
    {
        Dispose();
    }
}