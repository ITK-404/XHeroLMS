using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class LmsVideoProgressApiClient : MonoBehaviour
{
    [Header("Scene/Refs")]
    private CourseListView courseListView;           // để lấy courseId và list LessonUI

    [Header("Progress API")]
    // private string baseUrl = LmsStore.Instance.baseUrl; // Tự động đồng bộ baseUrl với LmsStore (DEV/PROD đổi 1 chỗ duy nhất)
    private string baseUrl;
    public bool useTokenFromStore = true;           // lấy TokenStore.AccessToken nếu không override
    public string overrideAccessToken = "";         // KHÔNG cần kèm "Bearer "


    [Header("Auto resolve IDs (fallback)")]
    public bool autoCourseIdFromPrefs = true;
    public string courseIdPrefsKey = "COURSE_CURRENT_ID";

    [Header("Debug")]
    public bool verboseLog = true;

    // ===== runtime =====

    private void Awake()
    {
        baseUrl = LmsStore.Instance.baseUrl;
    }

    [SerializeField] private string _courseId;
    // ================= Networking =================
    string GetTokenBare()
    {
        if (!string.IsNullOrEmpty(overrideAccessToken))
            return NormalizeBearer(overrideAccessToken);

        if (useTokenFromStore)
        {
            try
            {
                var t = Type.GetType("TokenStore");
                var prop = t?.GetProperty("AccessToken");
                var raw = prop?.GetValue(null, null) as string;
                return NormalizeBearer(raw);
            }
            catch { }
        }
        return null;
    }

    string NormalizeBearer(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return null;
        var t = raw.Trim();
        if (t.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            t = t.Substring(7).Trim();
        return t;
    }

    public void SendOnceBlocking(LessonUI target, bool safeBlock = true)
    {
        if (target == null)
        {
            return;
        }
        int progress = (int)target.progressTime;
        var req = BuildRequest(progress, target);
        if (req == null) return;

        var op = req.SendWebRequest();
        float start = Time.realtimeSinceStartup;
        if (safeBlock)
        {
            while (!op.isDone && Time.realtimeSinceStartup - start < 1.5f) { }
        }

#if UNITY_2020_2_OR_NEWER
        bool error = req.result == UnityWebRequest.Result.ConnectionError ||
                     req.result == UnityWebRequest.Result.ProtocolError;
#else
        bool error = req.isNetworkError || req.isHttpError;
#endif
        if (!error && req.responseCode >= 200 && req.responseCode < 300)
        {
            if (verboseLog) Debug.Log($"[LPM] (blocking) PUT OK lesson={target.lessonID} progress={progress}s");
        }
        req.Dispose();
    }

    private UnityWebRequest BuildRequest(int progress, LessonUI target)
    {
        string tokenBare = GetTokenBare();
        if (string.IsNullOrEmpty(tokenBare))
        {
            Debug.LogWarning("[LPM] No token => skip PUT");
            return null;
        }

        string url = $"{baseUrl}/lms/result-lesson/{_courseId}";
        var dto = new ProgressDto
        {
            lesson = target.lessonID,
            lessonType = string.IsNullOrEmpty(target.type) ? "video" : target.type,
            progressTime = Mathf.Max(0, progress)
        };
        string json = JsonUtility.ToJson(dto);
        byte[] body = Encoding.UTF8.GetBytes(json);

        var req = new UnityWebRequest(url, "PUT");
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("Authorization", "Bearer " + tokenBare);
        req.SetRequestHeader("Content-Type", "application/json");
        req.SetRequestHeader("Accept", "application/json");

        if (verboseLog) Debug.Log($"[LPM] PUT {url} body={json}");
        return req;
    }

    [Serializable]
    class ProgressDto
    {
        public string lesson;
        public string lessonType;
        public int progressTime;
    }

    public void SetCourseID(string courseID)
    {
        if (string.IsNullOrEmpty(courseID))
        {
            Debug.Log("Course ID bị rỗng, vui lòng kiểm tra lại");
            return;
        }
        _courseId = courseID;
    }
}

