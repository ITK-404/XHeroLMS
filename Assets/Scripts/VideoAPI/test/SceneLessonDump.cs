using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public class LessonDumpItem
{
    public string chapterTitle;
    public string title;
    public string videoLink2;
    public string questionTitle;
}
// ===== Wrapper cho JsonUtility để serialize List<> =====
[System.Serializable]
public class LessonDumpList
{
    public List<LessonDumpItem> items = new List<LessonDumpItem>();
}

public class SceneLessonDump : MonoBehaviour
{
    [Header("Config")]
    // Tên file JSON (Resources) ánh xạ scene ↔ seo. Không kèm .json
    public string resourceJsonName = "courses";

    // Tự fetch private nếu cache thiếu/hết hạn
    public bool autoFetchPrivateIfMissing = true;

    // Ghi thêm file JSON ra persistentDataPath để dễ kiểm tra
    public bool alsoWriteToDisk = true;

    [Header("Override (debug)")]
    // Nếu set, sẽ dùng luôn SEO này và bỏ qua map sceneName ↔ seo trong courses.json
    public string overrideSeo = "";

    private string _seo;

    private void Awake()
    {
        _ = LmsStore.Instance; // đảm bảo singleton tồn tại
    }

    private IEnumerator Start()
    {
        Debug.Log($"[SceneLessonDump] persistentDataPath = {Application.persistentDataPath}");
        Debug.Log($"[SceneLessonDump] TokenStore.IsAuthenticated={TokenStore.IsAuthenticated}, AccessTokenLen={(string.IsNullOrEmpty(TokenStore.AccessToken) ? 0 : TokenStore.AccessToken.Length)}");

        if (!string.IsNullOrEmpty(overrideSeo))
        {
            _seo = overrideSeo.Trim();
            Debug.Log($"[SceneLessonDump] Using overrideSeo='{_seo}'");
            yield return DumpBySeo_NoLmsStorePatch(_seo);
            yield break;
        }

        // Đọc courses.json -> tìm seo theo scene hiện tại
        var txt = Resources.Load<TextAsset>(resourceJsonName);
        if (!txt)
        {
            Debug.LogError($"[SceneLessonDump] Missing Resources/{resourceJsonName}.json");
            yield break;
        }

        var wrapped = "{\"items\":" + txt.text + "}";
        var map = JsonUtility.FromJson<SceneSeoList>(wrapped);

        var sceneName = SceneManager.GetActiveScene().name;

        // Match linh hoạt (có/không <> và trim spaces, ignore case)
        string s1 = sceneName;
        string s2 = $"<{sceneName}>";
        string s3 = sceneName.Trim('<', '>');
        string s4 = s3.Trim();
        var candidates = new HashSet<string>(new[] { s1, s2, s3, s4 }, System.StringComparer.OrdinalIgnoreCase);

        SceneSeoItem item = null;
        foreach (var it in map.items)
        {
            if (it == null || string.IsNullOrEmpty(it.sceneName)) continue;
            var raw = it.sceneName;
            var norm = raw.Trim().Trim('<', '>');
            if (candidates.Contains(raw) || candidates.Contains(norm) || candidates.Contains($"<{norm}>"))
            {
                item = it; break;
            }
        }

        if (item == null)
        {
            var names = new StringBuilder();
            foreach (var it in map.items)
            {
                if (it == null) continue;
                names.AppendLine($"- '{it.sceneName}'");
            }
            Debug.LogWarning(
                $"[SceneLessonDump] No SEO mapping for scene '{sceneName}' in {resourceJsonName}.json\n" +
                $"Available sceneName values in JSON:\n{names}");
            yield break;
        }

        _seo = item.seo;
        Debug.Log($"[SceneLessonDump] Scene '{sceneName}' -> seo='{_seo}'");

        // Dump theo SEO (chỉ dùng SEO)
        yield return DumpBySeo_NoLmsStorePatch(_seo);
    }
    
    private IEnumerator DumpBySeo_NoLmsStorePatch(string seo)
    {
        // Thử lấy courseId từ cache trước
        var courseId = LmsStore.Instance.GetCourseIdBySeo(seo);

        // Nếu chưa có, thử tự fetch Market/MyCourses rồi tra lại
        if (string.IsNullOrEmpty(courseId))
        {
            if (!TokenStore.IsAuthenticated)
            {
                Debug.LogError("[SceneLessonDump] Not authenticated -> không thể gọi server để resolve SEO.");
                yield break;
            }

            // Gọi fetch (nếu cache đã có và TTL còn hạn, LmsStore sẽ tự bỏ qua)
            yield return LmsStore.Instance.FetchMarketIfExpired(0, 500, "", "", "", "");
            yield return LmsStore.Instance.FetchMyCoursesIfExpired();

            // Tra lại sau fetch
            courseId = LmsStore.Instance.GetCourseIdBySeo(seo);
            Debug.Log($"[SceneLessonDump] After fetch, courseId by seo='{seo}' -> '{courseId}'");
        }

        if (string.IsNullOrEmpty(courseId))
        {
            // Debug.LogError($"[SceneLessonDump] Không resolve được courseId cho seo='{seo}'. Kiểm tra: " +
            //                $"1) SEO có tồn tại trên môi trường {LmsStore.Instance.baseUrl} không? " +
            //                $"2) Tài khoản đã có quyền/my-courses chưa? " +
            //                $"3) DEV/PROD mismatch?");
            yield break;
        }

        // Fetch private nếu cần
        if (autoFetchPrivateIfMissing && LmsStore.Instance.GetPrivate(courseId) == null)
        {
            yield return LmsStore.Instance.FetchPrivateIfExpired(courseId);
        }

        var p = LmsStore.Instance.GetPrivate(courseId);
        if (p == null)
        {
            // Debug.LogError($"[SceneLessonDump] courseId='{courseId}' tồn tại nhưng Private null. " +
            //                $"Có thể do quyền hoặc course không tồn tại trên môi trường {LmsStore.Instance.baseUrl}.");
            yield break;
        }
        
        var result = new LessonDumpList();

        if (p.chapters != null && p.chapters.Count > 0)
        {
            foreach (var ch in p.chapters)
            {
                if (ch?.lessons == null || ch.lessons.Count == 0) continue;

                // tiêu đề chương: ch.chapterTitle (khác với p.title là tiêu đề khóa)
                string chapTitle = string.IsNullOrEmpty(ch.chapterTitle) ? "(no chapter title)" : ch.chapterTitle;

                foreach (var lesson in ch.lessons)
                {
                    if (lesson == null) continue;

                    // Lấy title + video (ưu tiên videoLink2, fallback videoLink)
                    string lessonTitle  = string.IsNullOrEmpty(lesson.title) ? "(no lesson title)" : lesson.title;
                    string link2        = !string.IsNullOrEmpty(lesson.videoLink2) ? lesson.videoLink2 :
                                        (!string.IsNullOrEmpty(lesson.videoLink) ? lesson.videoLink : "");

                    // Suy luận câu hỏi đơn giản (nếu tên bài chứa "Câu hỏi")
                    string question = (lessonTitle.IndexOf("Câu hỏi", System.StringComparison.OrdinalIgnoreCase) >= 0) ? lessonTitle : "";

                    result.items.Add(new LessonDumpItem
                    {
                        chapterTitle  = chapTitle,
                        title         = lessonTitle,
                        videoLink2    = link2,
                        questionTitle = question
                    });
                }
            }
        }
        else
        {
            // Không có chapters -> vẫn xuất 1 item “trống” để dễ debug
            result.items.Add(new LessonDumpItem
            {
                chapterTitle  = string.IsNullOrEmpty(p.title) ? "(no course title)" : p.title,
                title         = "",
                videoLink2    = "",
                questionTitle = ""
            });
        }

        string json = JsonUtility.ToJson(result, true);
        Debug.Log($"[SceneLessonDump] JSON dump for seo='{seo}':\n{json}");

        if (alsoWriteToDisk)
        {
            var sceneName = SceneManager.GetActiveScene().name;
            string fileName = $"lesson_dump_{sceneName}.json";
            string dir = Application.persistentDataPath;
            string path = Path.Combine(dir, fileName);

            try
            {
                Directory.CreateDirectory(dir);
                File.WriteAllText(path, json, Encoding.UTF8);
                Debug.Log($"[SceneLessonDump] Wrote file -> {path}");

#if UNITY_EDITOR
                UnityEditor.EditorUtility.RevealInFinder(path);
#elif UNITY_ANDROID
                Debug.Log($"[SceneLessonDump][Android] File: {path}\n/storage/emulated/0/Android/data/<bundle-id>/files/");
#elif UNITY_IOS
                Debug.Log($"[SceneLessonDump][iOS] File trong sandbox: {path}");
#elif UNITY_STANDALONE_WIN
                Debug.Log($"[SceneLessonDump][Windows] Mở đường dẫn: {path}");
#elif UNITY_WEBGL
                Debug.LogWarning("[SceneLessonDump][WebGL] persistentDataPath nằm trong IndexedDB của trình duyệt.");
#endif
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SceneLessonDump] Write file failed: {e.GetType().Name} - {e.Message}\n{e.StackTrace}");
            }
        }
    }

    // ===== helper models cho courses.json =====
    [System.Serializable] public class SceneSeoItem { public string sceneName; public string _id; public string seo; public string image; public string title; }
    [System.Serializable] class SceneSeoList { public List<SceneSeoItem> items = new(); }
}
