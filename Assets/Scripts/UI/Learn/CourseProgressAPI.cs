using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class CourseProgressAPI : MonoBehaviour
{
    public string baseUrl;
    public string courseID;
    public SceneLessonUI lessonUI;

    private Dictionary<string, int> lessonProgressDictionary = new();
    private CustomPrivateData privateRoot;
    private void Awake()
    {
        lessonUI.OnLoadCourseDone += OnLoadCourseDone;
    }

    private void OnDestroy()
    {
        lessonUI.OnLoadCourseDone -= OnLoadCourseDone;
    }

    private void OnLoadCourseDone(LmsCoursePrivate obj)
    {
        // update course ID
        courseID = obj._id;
    }

    [ContextMenu("Try Get Course")]
    public void TryGetCourse()
    {
        StartCoroutine(GetProgressCourseCoroutine());
    }
    public IEnumerator GetProgressCourseCoroutine()
    {
        var accessToken = TokenStore.AccessToken;
        string url = $"{baseUrl}/users/lms/courses/get-progress-learn/{courseID}";

        using var req = UnityWebRequest.Get(url);
        if (!string.IsNullOrEmpty(accessToken))
            req.SetRequestHeader("Authorization", "Bearer " + accessToken);

        req.SetRequestHeader("Accept", "application/json");
        yield return req.SendWebRequest();
#if UNITY_2020_2_OR_NEWER
        bool error = req.result == UnityWebRequest.Result.ConnectionError ||
                     req.result == UnityWebRequest.Result.ProtocolError;
#else
        bool error = req.isNetworkError || req.isHttpError;
#endif
        string body = req.downloadHandler?.text;
        Debug.Log($"Course Fetech data: \n {body}");
        privateRoot = JsonUtility.FromJson<CustomPrivateData>(body);
        lessonProgressDictionary.Clear();
        foreach (var item in privateRoot.data.course.chapters)
        {
            foreach (var lesson in item.lessons)
            {
                lessonProgressDictionary.Add(lesson._id,lesson.progressTime);
            }
        }
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
        
    }
}