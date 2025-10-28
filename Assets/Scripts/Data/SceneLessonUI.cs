using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using TMPro;

public class SceneLessonUI : MonoBehaviour
{
    [Header("Data")]
    public string overrideSeo = "";
    public string resourceJsonName = "courses";

    [Header("UI")]
    public ScrollRect scrollRect;
    public Transform content;           // Content của ScrollView
    public ChapterUI headerPrefab;     // Prefab dùng cho cả Header khóa học và Header chương (Tag "Chapter")
    public LessonUI itemPrefab;       // Prefab item (Tag "Title" và/hoặc "QA")
    public VideoPlayer videoPlayer;

    [Header("Options")]
    public bool autoFetchPrivateIfMissing = true;
    [Tooltip("Chiều cao mặc định cho item nếu prefab không có LayoutElement.")]
    public float fallbackItemHeight = 120f;
    public float verticalSpacing = 6f;

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
        //if (!content || !itemPrefab || !headerPrefab)
        //{
        //    Debug.LogError("[SceneLessonUI] Thiếu tham chiếu content/itemPrefab/headerPrefab.");
        //    yield break;
        //}

        EnsureListLayout((RectTransform)content);

        // Lấy SEO
        if (!string.IsNullOrEmpty(overrideSeo)) _seo = overrideSeo.Trim();
        else
        {
            var txt = Resources.Load<TextAsset>(resourceJsonName);
            if (!txt) { Debug.LogError($"[SceneLessonUI] Missing Resources/{resourceJsonName}.json"); yield break; }
            var wrapped = "{\"items\":" + txt.text + "}";
            var map = JsonUtility.FromJson<SceneSeoList>(wrapped);

            var sceneName = SceneManager.GetActiveScene().name;
            string s1 = sceneName, s2 = $"<{sceneName}>", s3 = sceneName.Trim('<', '>'), s4 = s3.Trim();
            var candidates = new HashSet<string>(new[] { s1, s2, s3, s4 }, System.StringComparer.OrdinalIgnoreCase);

            SceneSeoItem item = null;
            foreach (var it in map.items)
            {
                if (it == null || string.IsNullOrEmpty(it.sceneName)) continue;
                var raw = it.sceneName; var norm = raw.Trim().Trim('<', '>');
                if (candidates.Contains(raw) || candidates.Contains(norm) || candidates.Contains($"<{norm}>")) { item = it; break; }
            }
            if (item == null) { Debug.LogError($"[SceneLessonUI] No SEO mapping for scene '{sceneName}'"); yield break; }
            _seo = item.seo;
        }

        // Resolve courseId theo seo.url
        var courseId = LmsStore.Instance.GetCourseIdBySeo(_seo);
        if (string.IsNullOrEmpty(courseId))
        {
            if (!TokenStore.IsAuthenticated) { Debug.LogError("[SceneLessonUI] Not authenticated -> không thể fetch."); yield break; }
            yield return LmsStore.Instance.FetchMarketIfExpired(0, 500, "", "", "", "");
            yield return LmsStore.Instance.FetchMyCoursesIfExpired();
            courseId = LmsStore.Instance.GetCourseIdBySeo(_seo);
            if (string.IsNullOrEmpty(courseId)) { Debug.LogError($"[SceneLessonUI] Không resolve được courseId cho seo='{_seo}'"); yield break; }
        }

        // Private
        if (autoFetchPrivateIfMissing && LmsStore.Instance.GetPrivate(courseId) == null)
            yield return LmsStore.Instance.FetchPrivateIfExpired(courseId);

        var p = LmsStore.Instance.GetPrivate(courseId);
        if (p == null) { Debug.LogError($"[SceneLessonUI] Private null cho courseId='{courseId}'"); yield break; }
        _courseTitle = string.IsNullOrEmpty(p.title) ? "(no course title)" : p.title;
        OnLoadCourseDone?.Invoke(p);
        BuildListUI(p);
    }

    private void BuildListUI(LmsCoursePrivate p)
    {
        // Clear cũ
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        // ===== Header khóa học (1 lần) =====
        //var headerCourse = Instantiate(headerPrefab);
        //headerCourse.transform.SetParent(content, false);
        //EnsureItemLayout((RectTransform)headerCourse.transform);
        //SetLabel(headerCourse, "Chapter", _courseTitle);

        // ===== Với mỗi CHAPTER: tạo header chương + các item bài =====
        if (p.chapters != null)
        {
            foreach (var ch in p.chapters)
            {
                if (ch == null) continue;

                // Header CHAPTER (nếu có tên)
                string chapTitle = string.IsNullOrEmpty(ch.chapterTitle) ? "" : ch.chapterTitle.Trim();
                ChapterUI headerChapter = null;
                if (!string.IsNullOrEmpty(chapTitle))
                {
                    headerChapter = Instantiate(headerPrefab, content);
                    //headerChapter.transform.SetParent(content, false);
                    //EnsureItemLayout((RectTransform)headerChapter.transform);
                    //SetLabel(headerChapter, "Chapter", chapTitle);
                    headerChapter.titleName.text = $"{chapTitle}";
                    // headerChapter.SetUnlock();
                }
                ChapterUIManager.Instance.AddToList(headerChapter);

                // Các bài học trong chapter
                if (ch.lessons == null) continue;
                foreach (var lesson in ch.lessons)
                {
                    if (lesson == null) continue;

                    string lessonTitle = string.IsNullOrEmpty(lesson.title) ? "" : lesson.title.Trim();
                    if (string.IsNullOrEmpty(lessonTitle)) continue; // ẩn bài không tên

                    string link2 = !string.IsNullOrEmpty(lesson.videoLink2) ? lesson.videoLink2 :
                                   (!string.IsNullOrEmpty(lesson.videoLink) ? lesson.videoLink : "");

                    var lessonUI = Instantiate(itemPrefab, headerChapter.lessonContainer.transform);
                    //lessonUI.transform.SetParent(headerChapter.lessonContainer.transform, false);
                    //EnsureItemLayout((RectTransform)lessonUI.transform);

                    // Luôn hiển thị vào Title (kể cả “Câu hỏi …”), QA ẩn
                    //SetLabel(lessonUI, "Title", lessonTitle);
                    //SetLabel(lessonUI, "QA", ""); // rỗng -> bị ẩn
                    lessonUI.titleTMP.text = $"{lessonTitle}";
                    // Click phát video

                    lessonUI.linkVideo2 = link2;
                    lessonUI.OnClickPlayVideo = PlayVideo;
                    lessonUI.chapterUI = headerChapter;
                    
                    headerChapter.AddToList(lessonUI);
                    
                    Debug.Log($"Title {lesson.title} Condition {lesson.completionCondition.condition} Percent {lesson.completionCondition.percent}");
                }
            }
        }

        // Rebuild layout để tính lại vị trí/chiều cao
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)content);

        if (scrollRect) scrollRect.verticalNormalizedPosition = 1f;
    }

    // ===== Layout helpers =====
    private void EnsureListLayout(RectTransform rt)
    {
        var vlg = rt.GetComponent<VerticalLayoutGroup>();
        if (!vlg) vlg = rt.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth  = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth      = true;
        vlg.childControlHeight     = true;
        vlg.spacing = verticalSpacing;
        vlg.padding = new RectOffset(0, 0, 0, 0);

        var fitter = rt.GetComponent<ContentSizeFitter>();
        if (!fitter) fitter = rt.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
    }

    private void EnsureItemLayout(RectTransform rt)
    {
        var le = rt.GetComponent<LayoutElement>();
        if (!le) le = rt.gameObject.AddComponent<LayoutElement>();

        // Nếu prefab chưa set preferredHeight, dùng fallback
        if (le.preferredHeight <= 0f)
        {
            float h = rt.sizeDelta.y;
            le.preferredHeight = (h > 0f ? h : fallbackItemHeight);
        }
        le.minHeight = le.preferredHeight;
        le.flexibleHeight = 0f;
    }

    // ===== Set text theo Tag & tự ẩn nếu rỗng =====
    private GameObject FindObjWithTag(GameObject root, string tag)
    {
        var tfs = root.GetComponentsInChildren<Transform>(true);
        foreach (var tf in tfs)
            if (tf && tf.gameObject.CompareTag(tag))
                return tf.gameObject;
        return null;
    }

    private void SetLabel(GameObject root, string tag, string value)
    {
        var obj = FindObjWithTag(root, tag);
        if (!obj) return;

        string trimmed = string.IsNullOrWhiteSpace(value) ? "" : value.Trim();

        var ui = obj.GetComponent<Text>();
        if (ui) ui.text = trimmed;

        var tmp = obj.GetComponent<TMP_Text>();
        if (tmp) tmp.text = trimmed;

        obj.SetActive(!string.IsNullOrEmpty(trimmed));
    }

    private void PlayVideo(string url)
    {
        if (string.IsNullOrEmpty(url)) { Debug.LogWarning("[SceneLessonUI] Video URL rỗng."); return; }
        if (!videoPlayer) { Debug.LogWarning("[SceneLessonUI] videoPlayer null."); return; }
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = url;
        videoPlayer.Play();
        Debug.Log("[SceneLessonUI] Playing: " + url);
    }
    
    [System.Serializable] public class SceneSeoItem { public string sceneName; public string _id; public string seo; public string image; public string title; }
    [System.Serializable] class SceneSeoList { public List<SceneSeoItem> items = new(); }
}

