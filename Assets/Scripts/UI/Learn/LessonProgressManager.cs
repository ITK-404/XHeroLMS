using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class LessonProgressManager : MonoBehaviour
{
    [Header("Scene/Refs")]
    private CourseListView courseListView;           // để lấy courseId và list LessonUI

    [Header("Progress API")]
    public string baseUrl = "https://apis-dev.xheroapp.com";
    public bool useTokenFromStore = true;           // lấy TokenStore.AccessToken nếu không override
    public string overrideAccessToken = "";         // KHÔNG cần kèm "Bearer "

    [Header("Policy")]
    public int sendIntervalSeconds = 15;            // nhịp PUT
    public int minDeltaToSend = 3;                  // chỉ PUT khi tăng >= 3 giây so với lần gửi trước
    public int hardCapSeconds = 0;                  // 0 = không giới hạn

    [Header("Auto resolve IDs (fallback)")]
    public bool autoCourseIdFromPrefs = true;
    public string courseIdPrefsKey = "COURSE_CURRENT_ID";

    [Header("Debug")]
    public bool verboseLog = true;

    // ===== runtime =====
    private readonly HashSet<LessonUI> _wired = new HashSet<LessonUI>();
    private LessonUI _current;                      // bài đang theo dõi
    private string _courseId;                       // courseId hiện tại
    private Coroutine _progressCo;
    private int _watchedSec;
    private int _lastSent;
    private bool _quitting;

    void Awake()
    {
        courseListView= FindFirstObjectByType<CourseListView>();
        // nêu không gán courseListView, thử tự tìm
        if (courseListView == null) courseListView = FindFirstObjectByType<CourseListView>();
    }

    void OnEnable()
    {
        // bắt đầu auto-wire các LessonUI mới spawn
        StartCoroutine(AutoWireLoop());
    }

    void OnDisable()
    {
        StopAllCoroutines();
        if (_current != null) TrySendNow(); // send final
        UnwireAll(); 
    }

    void OnApplicationQuit()
    {
        _quitting = true;
        TrySendNow(true);
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus && _current != null) TrySendNow();
    }

    IEnumerator AutoWireLoop()
    {
        var wait = new WaitForSeconds(0.5f);
        while (true)
        {
            WireAllLessonsIfNeeded();
            yield return wait;
        }
    }

    void WireAllLessonsIfNeeded()
    {
        if (courseListView == null || courseListView.content == null) return;

        // cập nhật courseId
        ResolveCourseId();

        // tìm tất cả LessonUI (kể cả inactive con dưới content)
        var all = courseListView.content.GetComponentsInChildren<LessonUI>(true);
        foreach (var lu in all)
        {
            if (lu == null || _wired.Contains(lu)) continue;

            // bắt click của LessonUI (không sửa LessonUI)
            var btn = lu.btn;
            if (btn != null)
            {
                btn.onClick.AddListener(() => OnLessonClicked(lu));
                _wired.Add(lu);
            }

            if (verboseLog)
                Debug.Log($"[LPM] Wired lesson '{lu.titleTMP?.text}' ({lu.lessonID})");
        }
    }

    void UnwireAll()
    {
        if (courseListView == null || courseListView.content == null) return;
        var all = courseListView.content.GetComponentsInChildren<LessonUI>(true);
        foreach (var lu in all)
        {
            if (lu == null || lu.btn == null) continue;
            // khó gỡ chính xác delegate lambda; bỏ qua. Không nghiêm trọng vì scene unload sẽ hủy.
        }
        _wired.Clear();
    }

    void ResolveCourseId()
    {
        string cid = null;
        if (courseListView != null && !string.IsNullOrEmpty(courseListView.courseID))
            cid = courseListView.courseID;

        if (string.IsNullOrEmpty(cid) && autoCourseIdFromPrefs)
            cid = PlayerPrefs.GetString(courseIdPrefsKey, "");

        if (!string.IsNullOrEmpty(cid)) _courseId = cid;

        if (verboseLog)
            Debug.Log($"[LPM] courseId='{_courseId ?? "<null>"}'");
    }

    void OnLessonClicked(LessonUI lesson)
    {
        // chuyển bài: gửi lần cuối cho bài cũ
        if (_current != null && _current != lesson)
            TrySendNow();

        _current = lesson;
        _watchedSec = 0;
        _lastSent = 0;

        if (_progressCo != null) StopCoroutine(_progressCo);
        _progressCo = StartCoroutine(ProgressLoop());

        if (verboseLog)
            Debug.Log($"[LPM] Start tracking lesson '{lesson.titleTMP?.text}' id={lesson.lessonID}");
    }

    IEnumerator ProgressLoop()
    {
        var oneSec = new WaitForSeconds(1f);
        int tick = 0;

        while (_current != null)
        {
            yield return oneSec;

            _watchedSec++;

            if (hardCapSeconds > 0 && _watchedSec > hardCapSeconds)
                _watchedSec = hardCapSeconds;

            tick++;
            if (tick >= sendIntervalSeconds)
            {
                tick = 0;
                TrySendNow();
            }
        }
    }

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

    void TrySendNow(bool blocking = false)
    {
        if (_current == null) return;

        // điều kiện đủ
        if (string.IsNullOrEmpty(baseUrl) || string.IsNullOrEmpty(_courseId) || string.IsNullOrEmpty(_current.lessonID))
        {
            if (verboseLog)
                Debug.LogWarning($"[LPM] Missing baseUrl/courseId/lessonId -> baseUrl='{baseUrl}', courseId='{_courseId}', lessonId='{_current.lessonID}'");
            return;
        }

        int cur = _watchedSec;
        if (cur <= 0 && !_quitting)
        {
            if (verboseLog) Debug.Log("[LPM] Skip PUT (progress == 0)");
            return;
        }
        if (cur - _lastSent < minDeltaToSend && !_quitting)
        {
            if (verboseLog) Debug.Log($"[LPM] Skip PUT (delta < {minDeltaToSend}). cur={cur}, last={_lastSent}");
            return;
        }

        if (blocking) SendOnceBlocking(cur, _current);
        else StartCoroutine(SendOnce(cur, _current));
    }

    IEnumerator SendOnce(int progress, LessonUI target)
    {
        using (var req = BuildRequest(progress, target))
        {
            if (req == null) yield break;
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool error = req.result == UnityWebRequest.Result.ConnectionError ||
                         req.result == UnityWebRequest.Result.ProtocolError;
#else
            bool error = req.isNetworkError || req.isHttpError;
#endif
            if (!error && req.responseCode >= 200 && req.responseCode < 300)
            {
                _lastSent = progress;
                if (verboseLog) Debug.Log($"[LPM] PUT OK lesson={target.lessonID} progress={progress}s");
            }
            else
            {
                Debug.LogWarning($"[LPM] PUT FAIL code={req.responseCode} err={req.error} body={req.downloadHandler?.text}");
            }
        }
    }

    void SendOnceBlocking(int progress, LessonUI target)
    {
        var req = BuildRequest(progress, target);
        if (req == null) return;

        var op = req.SendWebRequest();
        float start = Time.realtimeSinceStartup;
        while (!op.isDone && Time.realtimeSinceStartup - start < 1.5f) { }

#if UNITY_2020_2_OR_NEWER
        bool error = req.result == UnityWebRequest.Result.ConnectionError ||
                     req.result == UnityWebRequest.Result.ProtocolError;
#else
        bool error = req.isNetworkError || req.isHttpError;
#endif
        if (!error && req.responseCode >= 200 && req.responseCode < 300)
        {
            _lastSent = progress;
            if (verboseLog) Debug.Log($"[LPM] (blocking) PUT OK lesson={target.lessonID} progress={progress}s");
        }
        req.Dispose();
    }

    UnityWebRequest BuildRequest(int progress, LessonUI target)
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
}