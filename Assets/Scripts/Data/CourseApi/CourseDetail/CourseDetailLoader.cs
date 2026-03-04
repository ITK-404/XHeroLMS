using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class CourseDetailLoader : MonoBehaviour
{
    [Header("API")]
    private string baseUrl = "";

    [SerializeField] private int timeoutSeconds = 20;

    [Header("Debug")]
    [SerializeField] private bool debugLog = true;

    // runtime
    private Coroutine _loadRoutine;
    private UnityWebRequest _activeRequest;

    // Dùng để chặn request cũ ghi đè request mới
    private int _loadVersion = 0;

    private void Awake()
    {
        // Ưu tiên lấy từ LmsStore nếu user chưa set trong inspector
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            if (LmsStore.Instance != null && !string.IsNullOrWhiteSpace(LmsStore.Instance.baseUrl))
                baseUrl = LmsStore.Instance.baseUrl;
        }

        baseUrl = (baseUrl ?? "").Trim().TrimEnd('/');

        if (debugLog)
            Debug.Log($"[CourseDetailLoader] Awake baseUrl='{baseUrl}'");
    }

    /// <summary>
    /// Load course detail và lưu vào CourseDetailStaticStore.
    /// Nếu đang load course khác, sẽ huỷ và reset trước.
    /// </summary>
    public void Load(string courseId) => Load(courseId, forceReload: false);

    /// <summary>
    /// forceReload=true: bỏ qua check HasData + CurrentCourseId (luôn gọi lại API)
    /// </summary>
    public void Load(string courseId, bool forceReload)
    {
        if (string.IsNullOrWhiteSpace(courseId))
        {
            CourseDetailStaticStore.SetError(courseId, "courseId is null/empty");
            return;
        }

        courseId = courseId.Trim();

        if (debugLog)
        {
            Debug.Log($"[CourseDetailLoader] Load({courseId}) forceReload={forceReload} | StoreId={CourseDetailStaticStore.CurrentCourseId} | HasData={CourseDetailStaticStore.HasData}");
        }

        // Nếu đang có data đúng courseId rồi thì khỏi gọi lại (trừ khi forceReload)
        if (!forceReload &&
            CourseDetailStaticStore.HasData &&
            CourseDetailStaticStore.CurrentCourseId == courseId)
        {
            if (debugLog)
                Debug.Log("[CourseDetailLoader] Skip reload because store already has this courseId.");
            return;
        }

        // Huỷ request cũ + reset store
        Dispose();

        // Tăng version để invalidate mọi request/Coroutine cũ
        _loadVersion++;

        CourseDetailStaticStore.Reset();

        _loadRoutine = StartCoroutine(LoadRoutine(courseId, _loadVersion));
    }

    /// <summary>
    /// Huỷ request đang chạy (khi vào khóa khác / đổi scene).
    /// </summary>
    public void Dispose()
    {
        if (_loadRoutine != null)
        {
            StopCoroutine(_loadRoutine);
            _loadRoutine = null;
        }

        if (_activeRequest != null)
        {
            try { _activeRequest.Abort(); } catch { /* ignore */ }
            _activeRequest.Dispose();
            _activeRequest = null;
        }
    }

    private IEnumerator LoadRoutine(string courseId, int version)
    {
        CourseDetailStaticStore.SetLoading(courseId);

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            CourseDetailStaticStore.SetError(courseId, "baseUrl is empty");
            yield break;
        }

        // Thêm cache-buster để tránh proxy/server cache trả data cũ
        var url = $"{baseUrl}/lms/courses/{courseId}?t={DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";

        if (debugLog)
            Debug.Log($"[CourseDetailLoader] v{version} GET: {url}");

        _activeRequest = UnityWebRequest.Get(url);
        _activeRequest.timeout = timeoutSeconds;

        // Chặn cache ở client/proxy phổ biến
        _activeRequest.SetRequestHeader("Cache-Control", "no-cache, no-store, must-revalidate");
        _activeRequest.SetRequestHeader("Pragma", "no-cache");
        _activeRequest.SetRequestHeader("Expires", "0");

        yield return _activeRequest.SendWebRequest();

        // Nếu trong lúc chờ, user bấm load course khác -> version đổi -> bỏ kết quả cũ
        if (version != _loadVersion)
        {
            if (debugLog)
                Debug.LogWarning($"[CourseDetailLoader] v{version} ignored (newer loadVersion={_loadVersion}).");
            SafeDisposeRequest();
            yield break;
        }

        if (_activeRequest == null)
        {
            if (debugLog)
                Debug.LogWarning($"[CourseDetailLoader] v{version} request was disposed during wait.");
            yield break;
        }

        if (_activeRequest.result != UnityWebRequest.Result.Success)
        {
            var err = $"HTTP Error: {_activeRequest.responseCode} | {_activeRequest.error}";
            if (debugLog) Debug.LogError($"[CourseDetailLoader] v{version} {err}");
            CourseDetailStaticStore.SetError(courseId, err);
            SafeDisposeRequest();
            _loadRoutine = null;
            yield break;
        }

        var json = _activeRequest.downloadHandler != null ? _activeRequest.downloadHandler.text : null;

        if (debugLog)
        {
            string head = json ?? "null";
            if (head.Length > 400) head = head.Substring(0, 400);
            Debug.Log($"[CourseDetailLoader] v{version} OK code={_activeRequest.responseCode} jsonHead={head}");
        }

        CourseDetailResponse resp = null;
        try
        {
            resp = JsonUtility.FromJson<CourseDetailResponse>(json);
        }
        catch (Exception e)
        {
            var err = "JSON parse failed: " + e.Message;
            if (debugLog) Debug.LogError($"[CourseDetailLoader] v{version} {err}");
            CourseDetailStaticStore.SetError(courseId, err);
            SafeDisposeRequest();
            _loadRoutine = null;
            yield break;
        }

        if (resp == null || resp.status == false || resp.course == null)
        {
            var err = "Invalid response or status=false or course=null";
            if (debugLog) Debug.LogError($"[CourseDetailLoader] v{version} {err}");
            CourseDetailStaticStore.SetError(courseId, err);
            SafeDisposeRequest();
            _loadRoutine = null;
            yield break;
        }

        // OK
        CourseDetailStaticStore.SetCourse(courseId, resp.course);

        if (debugLog)
        {
            int count = (resp.course.products == null) ? 0 : resp.course.products.Count;
            Debug.Log($"[CourseDetailLoader] v{version} SetCourse OK | id={courseId} | title={resp.course.title} | products={count}");
        }

        SafeDisposeRequest();
        _loadRoutine = null;
    }

    private void SafeDisposeRequest()
    {
        if (_activeRequest != null)
        {
            try { _activeRequest.Dispose(); } catch { /* ignore */ }
            _activeRequest = null;
        }
    }

    private void OnDestroy()
    {
        Dispose();
    }
}