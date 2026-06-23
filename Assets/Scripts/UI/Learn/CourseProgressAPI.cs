using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class CourseProgressAPI : MonoBehaviour
{
    [Header("Debug Only")]
    string baseUrl;   // chỉ để xem log, không set trong Inspector nữa

    [Header("Course Info")]
    public string courseID;
    public SceneLessonUI lessonUI;

    private Dictionary<string, int> lessonProgressDictionary = new();
    private CustomPrivateData privateRoot;
    public bool HasProgressData { get; private set; }

    private void Awake()
    {
        // Tự động đồng bộ baseUrl với LmsStore (DEV/PROD đổi 1 chỗ duy nhất)
        if (LmsStore.Instance != null)
        {
            baseUrl = LmsStore.Instance.baseUrl;
        }
        else
        {
            Debug.LogError("[CourseProgressAPI] LmsStore.Instance is NULL! baseUrl cannot update.");
        }
    }

    [ContextMenu("Try Get Course")]
    public void TryGetCourse()
    {
        StartCoroutine(GetProgressCourseCoroutine());
    }

    public IEnumerator GetProgressCourseCoroutine()
    {
        HasProgressData = false;
        lessonProgressDictionary.Clear();

        if (LmsStore.Instance != null)
            baseUrl = LmsStore.Instance.baseUrl;

        var accessToken = TokenStore.AccessToken;

        Debug.Log($"[CourseProgressAPI] baseUrl = {baseUrl}");
        Debug.Log($"[CourseProgressAPI] token = {(string.IsNullOrEmpty(accessToken) ? "EMPTY" : accessToken.Substring(0, 20) + "...")}");

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            Debug.LogError("[CourseProgressAPI] baseUrl rỗng, không thể lấy progress.");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(courseID))
        {
            Debug.LogError("[CourseProgressAPI] courseID rỗng, không thể lấy progress.");
            yield break;
        }

        string url = $"{baseUrl.TrimEnd('/')}/users/lms/courses/get-progress-learn/{courseID}";
        Debug.Log($"[CourseProgressAPI] URL = {url}");

        using var req = UnityWebRequest.Get(url);

        if (!string.IsNullOrEmpty(accessToken))
            req.SetRequestHeader("Authorization", "Bearer " + accessToken);

        req.SetRequestHeader("Accept", "application/json");

        // ===== THÊM HEADER x-data (AES-256-GCM) =====
        string xData = LmsSecurityHeader.BuildXDataHeader();
        req.SetRequestHeader("x-data", xData);
        Debug.Log($"[CourseProgressAPI] x-data: {xData}");

        yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
        bool error = req.result == UnityWebRequest.Result.ConnectionError ||
                     req.result == UnityWebRequest.Result.ProtocolError;
#else
        bool error = req.isNetworkError || req.isHttpError;
#endif

        string body = req.downloadHandler?.text;
        Debug.Log($"[CourseProgressAPI] Response ({req.responseCode}):\n{body}");

        if (error)
        {
            Debug.LogError($"[CourseProgressAPI] ERROR {req.responseCode}: {req.error}\nBody: {body}");
            yield break;
        }

        if (string.IsNullOrEmpty(body))
        {
            Debug.LogError("[CourseProgressAPI] Body rỗng.");
            yield break;
        }

        CustomPrivateData root = null;
        try
        {
            root = JsonUtility.FromJson<CustomPrivateData>(body);
        }
        catch (Exception e)
        {
            Debug.LogError("[CourseProgressAPI] FromJson FAILED: " + e);
            yield break;
        }

        if (root?.data?.course?.chapters == null)
        {
            Debug.LogError("[CourseProgressAPI] JSON không có data.course.chapters như model mong đợi.");
            yield break;
        }

        // Parse lessons
        lessonProgressDictionary.Clear();
        foreach (var chapter in root.data.course.chapters)
        {
            if (chapter?.lessons == null) continue;

            foreach (var lesson in chapter.lessons)
            {
                if (lesson == null || string.IsNullOrEmpty(lesson._id)) continue;
                lessonProgressDictionary[lesson._id] = lesson.progressTime;
            }
        }

        Debug.Log($"[CourseProgressAPI] Loaded {lessonProgressDictionary.Count} lesson progress entries.");
        HasProgressData = true;
    }

    public int GetLessonProgress(string lessonID)
    {
        if (lessonProgressDictionary.ContainsKey(lessonID))
        {
            return lessonProgressDictionary[lessonID];
        }
        Debug.LogWarning("Progress của lesson này không có trong dữ liệu, vui lòng kiểm tra lại !!!!");
        return 1;
    }

    public void UpdateProgressTime(string lessonID, int progressTime)
    {
    }

    [Serializable]
    public class CustomPrivateData
    {
        public bool status;
        public WarpperBigData data;
    }

    [Serializable]
    public class WarpperBigData
    {
        public string _id;
        public ResultExam resultExam;
        public LmsCoursePrivate course;
    }

    [Serializable]
    public class ResultExam
    {
        public string status;
    }

    private void FormatString(string rawData)
    {
        // hiện không dùng
    }
}
