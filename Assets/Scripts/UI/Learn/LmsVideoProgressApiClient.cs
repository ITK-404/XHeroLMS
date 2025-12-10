using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class LmsVideoProgressApiClient : MonoBehaviour
{
    [Header("Progress API")]
    private string baseUrl;
    [SerializeField] private string _courseId;

    public bool verboseLog = true;

    private void Awake()
    {
        baseUrl = LmsStore.Instance.baseUrl?.TrimEnd('/');
    }

    //======== TOKEN =========
    private string GetTokenBare()
    {
        string raw = TokenStore.AccessToken;
        if (string.IsNullOrEmpty(raw)) return null;

        raw = raw.Trim();
        if (raw.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            raw = raw.Substring(7).Trim();

        return raw;
    }

    //======== PUBLIC API =========
    public void SendProgress(LessonUI target)
    {
        if (target == null) return;
        StartCoroutine(SendProgressCoroutine(target));
    }

    public IEnumerator SendProgressCoroutine(LessonUI target)
    {
        int progress = Mathf.Max(0, (int)target.progressTime);
        UnityWebRequest req = BuildRequest(progress, target);
        if (req == null) yield break;

        yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
        bool error = req.result == UnityWebRequest.Result.ConnectionError ||
                     req.result == UnityWebRequest.Result.ProtocolError;
#else
        bool error = req.isNetworkError || req.isHttpError;
#endif
        if (verboseLog)
        {
            Debug.Log($"[LPM] Response ({req.responseCode}) => {req.downloadHandler.text}");
        }

        if (error || req.responseCode < 200 || req.responseCode >= 300)
        {
            Debug.LogWarning($"[LPM] PUT FAILED lesson={target.lessonID} progress={progress}s");
        }
        else
        {
            if (verboseLog)
                Debug.Log($"[LPM] PUT OK lesson={target.lessonID} progress={progress}s");
        }

        req.Dispose();
    }

    //======== BUILD REQUEST =========
    private UnityWebRequest BuildRequest(int progress, LessonUI target)
    {
        if (string.IsNullOrEmpty(baseUrl))
        {
            Debug.LogError("[LPM] baseUrl NULL");
            return null;
        }

        if (string.IsNullOrEmpty(_courseId))
        {
            Debug.LogError("[LPM] CourseId NULL");
            return null;
        }

        var tokenBare = GetTokenBare();
        if (string.IsNullOrEmpty(tokenBare))
        {
            Debug.LogWarning("[LPM] No Token => Skip");
            return null;
        }

        string url = $"{baseUrl}/lms/result-lesson/{_courseId}";

        var dto = new ProgressDto
        {
            lesson = target.lessonID,
            lessonType = string.IsNullOrEmpty(target.type) ? "video" : target.type,
            progressTime = progress
        };

        string json = JsonUtility.ToJson(dto);
        byte[] body = Encoding.UTF8.GetBytes(json);

        var req = new UnityWebRequest(url, "PUT");
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();

        req.SetRequestHeader("Authorization", "Bearer " + tokenBare);
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Accept", "application/json");

        // ADD x-data
        string xData = LmsSecurityHeader.BuildXDataHeader();
        req.SetRequestHeader("x-data", xData);

        if (verboseLog)
            Debug.Log($"[LPM] PUT {url}\nBody={json}");

        return req;
    }

    [Serializable]
    public class ProgressDto
    {
        public string lesson;
        public string lessonType;
        public int progressTime;
    }

    public void SetCourseID(string courseID)
    {
        _courseId = courseID;
    }
}
