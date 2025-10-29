using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class CourseListView : MonoBehaviour
{
    public ScrollRect scrollRect;
    public Transform content;           // Content của ScrollView
    public ChapterUI headerPrefab;     // Prefab dùng cho cả Header khóa học và Header chương (Tag "Chapter")
    public LessonUI itemPrefab;  
    public VideoPlayer videoPlayer;

    [Tooltip("Chiều cao mặc định cho item nếu prefab không có LayoutElement.")]
    public float fallbackItemHeight = 120f;
    public float verticalSpacing = 6f;

    public SceneLessonUI sceneLessonUI;

    private void Awake()
    {
        sceneLessonUI.OnLoadCourseDone += BuildListUI;
    }

    private void OnDestroy()
    {
        sceneLessonUI.OnLoadCourseDone -= BuildListUI;
    }

    private void BuildListUI(LmsCoursePrivate p)
    {
        Debug.Log("Bắt đầu hiển thị danh sách bài học");
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
     
    private void PlayVideo(string url)
    {
        if (string.IsNullOrEmpty(url)) { Debug.LogWarning("[SceneLessonUI] Video URL rỗng."); return; }
        if (!videoPlayer) { Debug.LogWarning("[SceneLessonUI] videoPlayer null."); return; }
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = url;
        videoPlayer.Play();
        Debug.Log("[SceneLessonUI] Playing: " + url);
    }
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

}