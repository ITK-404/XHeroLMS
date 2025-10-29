using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLessonUI : MonoBehaviour
{
    [Header("Data")]
    public string overrideSeo = "";

    public string resourceJsonName = "courses";

    [Header("UI")]
    // Prefab item (Tag "Title" và/hoặc "QA")
    [Header("Options")] 
    public bool autoFetchPrivateIfMissing = true;

    public bool autoStart = true;

    // --- internal ---
    private string _seo;
    private string _courseTitle = "(no course title)";

    public Action<LmsCoursePrivate> OnLoadCourseDone;

    private void Awake()
    {
        _ = LmsStore.Instance; // đảm bảo singleton tồn tại
    }

    private IEnumerator Start()
    {
        if (autoStart == false)
        {
            yield break;
        }
        Debug.Log("Bắt đầu load data");
        yield return LoadCourseDataCoroutine();
    }

    private bool isLoading = false;

    public bool IsLoading
    {
        get => isLoading;
    }

    public IEnumerator LoadCourseDataCoroutine()
    {
        //if (!content || !itemPrefab || !headerPrefab)
        //{
        //    Debug.LogError("[SceneLessonUI] Thiếu tham chiếu content/itemPrefab/headerPrefab.");
        //    yield break;
        //}
        isLoading = true;
        // Lấy SEO
        if (!string.IsNullOrEmpty(overrideSeo)) _seo = overrideSeo.Trim();
        else
        {
            var txt = Resources.Load<TextAsset>(resourceJsonName);
            if (!txt)
            {
                Debug.LogError($"[SceneLessonUI] Missing Resources/{resourceJsonName}.json");
                isLoading = false;
                yield break;
            }

            var wrapped = "{\"items\":" + txt.text + "}";
            var map = JsonUtility.FromJson<SceneSeoList>(wrapped);

            var sceneName = SceneManager.GetActiveScene().name;
            string s1 = sceneName, s2 = $"<{sceneName}>", s3 = sceneName.Trim('<', '>'), s4 = s3.Trim();
            var candidates = new HashSet<string>(new[] { s1, s2, s3, s4 }, System.StringComparer.OrdinalIgnoreCase);

            SceneSeoItem item = null;
            foreach (var it in map.items)
            {
                if (it == null || string.IsNullOrEmpty(it.sceneName)) continue;
                var raw = it.sceneName;
                var norm = raw.Trim().Trim('<', '>');
                if (candidates.Contains(raw) || candidates.Contains(norm) || candidates.Contains($"<{norm}>"))
                {
                    item = it;
                    break;
                }
            }

            if (item == null)
            {
                Debug.LogError($"[SceneLessonUI] No SEO mapping for scene '{sceneName}'");
                isLoading = false;
                yield break;
            }

            _seo = item.seo;
        }

        // Resolve courseId theo seo.url
        var courseId = LmsStore.Instance.GetCourseIdBySeo(_seo);
        if (string.IsNullOrEmpty(courseId))
        {
            if (!TokenStore.IsAuthenticated)
            {
                Debug.LogError("[SceneLessonUI] Not authenticated -> không thể fetch.");
                isLoading = false;
                yield break;
            }

            yield return LmsStore.Instance.FetchMarketIfExpired(0, 500, "", "", "", "");
            yield return LmsStore.Instance.FetchMyCoursesIfExpired();
            courseId = LmsStore.Instance.GetCourseIdBySeo(_seo);
            if (string.IsNullOrEmpty(courseId))
            {
                isLoading = false;
                Debug.LogError($"[SceneLessonUI] Không resolve được courseId cho seo='{_seo}'");
                yield break;
            }
        }

        // Private
        if (autoFetchPrivateIfMissing && LmsStore.Instance.GetPrivate(courseId) == null)
            yield return LmsStore.Instance.FetchPrivateIfExpired(courseId);

        var p = LmsStore.Instance.GetPrivate(courseId);
        if (p == null)
        {
            isLoading = false;
            Debug.LogError($"[SceneLessonUI] Private null cho courseId='{courseId} {overrideSeo}'");
            yield break;
        }

        _courseTitle = string.IsNullOrEmpty(p.title) ? "(no course title)" : p.title;

        OnLoadCourseDone?.Invoke(p);
        isLoading = false;
    }


    // ===== Layout helpers =====


    [System.Serializable]
    public class SceneSeoItem
    {
        public string sceneName;
        public string _id;
        public string seo;
        public string image;
        public string title;
    }

    [System.Serializable]
    class SceneSeoList
    {
        public List<SceneSeoItem> items = new();
    }
}