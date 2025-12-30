using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using Unity.Cinemachine;


public class CourseListView : MonoBehaviour
{
    public ScrollRect scrollRect;
    public Transform content; // Content của ScrollView
    public ChapterUI headerPrefab; // Prefab dùng cho cả Header khóa học và Header chương (Tag "Chapter")
    public LessonUI itemPrefab;
    public VideoPlayer videoPlayer;

    [Header("Exam Camera & Panel")]
    [SerializeField] private Transform examCamera;      // gán Main Camera (hoặc camera bạn dùng)
    [SerializeField] private GameObject examPanelRoot;  // panel bài kiểm tra (ẩn sẵn)
    [SerializeField] private float examMoveDuration = 1.5f;
    private Coroutine examCamRoutine;
    private Vector3 defaultCameraPosition;
    private Quaternion defaultCameraRotation;
    private bool hasDefaultCameraTransform;

    [SerializeField] private CinemachineHardLookAt examLookAt;
    private Vector3 defaultLookAtOffset;
    private bool hasDefaultOffset;

    [Tooltip("Chiều cao mặc định cho item nếu prefab không có LayoutElement.")]
    public float fallbackItemHeight = 120f;

    public float verticalSpacing = 6f;

    public SceneLessonUI sceneLessonUI;
    public string courseID;

    // ====== FINAL EXAM ======
    [Header("Final Exam")]
    public string finalExamSectionTitle = "Bài thi cuối khóa";

    public string finalExamItemTitle = "Vào bài thi";

    private List<ChapterUI> chapterList = new();

    private LearnUI learnUI;
    private VideoPlayerControllerPro videoPlayerControllerPro;
    private ExamResultReviewPanel examResultReviewPanel;
    private PlayerStandUI playerStandUI;
    private readonly List<LessonUI> _videoLessons = new();
    private LessonUI _currentLesson;

    [SerializeField] private LocalProxyAutoBoot proxyBoot;
    void Awake()
    {
        proxyBoot = FindAnyObjectByType<LocalProxyAutoBoot>();

        learnUI = FindAnyObjectByType<LearnUI>();
        videoPlayerControllerPro = FindAnyObjectByType<VideoPlayerControllerPro>();
        examResultReviewPanel = FindAnyObjectByType<ExamResultReviewPanel>();
        playerStandUI = FindAnyObjectByType<PlayerStandUI>();
    }

    public void BuildListUI(LmsCoursePrivate p)
    {
        _videoLessons.Clear();

        Debug.Log("Bắt đầu hiển thị danh sách bài học");

        // Clear cũ
        ChapterUIManager.Instance.ClearList();
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        // Lấy courseID an toàn (p._id hoặc p.course._id)
        courseID = GetStringMember(p, "_id")
                   ?? GetStringMember(GetMemberValue(p, "course"), "_id");

        // ===== Với mỗi CHAPTER: tạo header chương + các item bài =====
        if (p.chapters != null)
        {
            foreach (var ch in p.chapters)
            {
                if (ch == null) continue;
                if (ch.chapterTitle == "Tài liệu khóa học")
                {
                    continue;
                }

                // Header CHAPTER (nếu có tên)
                string chapTitle = string.IsNullOrEmpty(ch.chapterTitle) ? "" : ch.chapterTitle.Trim();
                ChapterUI headerChapter = null;
                if (!string.IsNullOrEmpty(chapTitle))
                {
                    headerChapter = Instantiate(headerPrefab, content);
                    headerChapter.titleName.text = $"{chapTitle}";
                    headerChapter.chapterID = ch._id;
                }

                ChapterUIManager.Instance.AddToList(headerChapter);

                // Các bài học trong chapter
                if (ch.lessons == null) continue;
                foreach (var lesson in ch.lessons)
                {
                    if (lesson == null) continue;

                    string lessonTitle = string.IsNullOrEmpty(lesson.title) ? "" : lesson.title.Trim();
                    if (string.IsNullOrEmpty(lessonTitle)) continue; // ẩn bài không tên

                    string link2 = !string.IsNullOrEmpty(lesson.videoLink2)
                        ? lesson.videoLink2
                        : (!string.IsNullOrEmpty(lesson.videoLink) ? lesson.videoLink : "");

                    var lessonUI = Instantiate(itemPrefab, headerChapter.lessonContainer.transform);
                    lessonUI.titleTMP.text = $"{lessonTitle}";
                    lessonUI.linkVideo2 = link2;
                    lessonUI.lessonID = lesson._id;
                    lessonUI.type = lesson.type;

                    lessonUI.chapterUI = headerChapter;

                    lessonUI.percent = lesson.completionCondition.percent;
                    // lessonUI.OnClickPlayVideo = PlayVideo;
                    lessonUI.OnClickPlayVideo = (url) =>
                    {
                        // lưu link gốc cho Next (không phải proxy url)
                        if (videoPlayerControllerPro) videoPlayerControllerPro.SetCurrentUrl(url);
                        PlayVideo(url);
                    };

                    lessonUI.progressTime = lesson.progressTime;


                    // parse duration 
                    int.TryParse(lesson.duration, out var duration);
                    lessonUI.duration = duration;
                    // update progress time
                    // int.TryParse(lesson.progressTime, out int progressTime);
                    if (lessonUI.type != "FINAL_EXAM" && !string.IsNullOrEmpty(lessonUI.linkVideo2))
                        _videoLessons.Add(lessonUI);


                    Debug.Log(
                        $"Title {lesson.title} Condition {lesson.completionCondition.condition} Percent {lesson.completionCondition.percent}");
                    headerChapter.AddToList(lessonUI);
                }
            }
        }

        // ====== Append “Bài thi cuối khóa” nếu course.settings.finalExam có ID hợp lệ ======
        var finalExamId = TryGetFinalExamId(p);
        if (!string.IsNullOrEmpty(finalExamId))
        {
            var headerFinal = Instantiate(headerPrefab, content);
            headerFinal.titleName.text = finalExamSectionTitle;
            headerFinal.chapterID = null; // không cần id chương
            headerFinal.SetFinalExam();
            ChapterUIManager.Instance.AddToList(headerFinal);
            ChapterUIManager.Instance.finalExamChapter = headerFinal;
            var finalItem = Instantiate(itemPrefab, headerFinal.lessonContainer.transform);
            finalItem.titleTMP.text = finalExamItemTitle;
            finalItem.linkVideo2 = ""; // không dùng video
            finalItem.lessonID = finalExamId; // giữ examId để xử lý sau
            finalItem.type = "FINAL_EXAM"; // đánh dấu loại
            finalItem.chapterUI = headerFinal;
            headerFinal.AddToList(finalItem);

            // Click = chuyển sang scene thi (lưu prefs)
            // finalItem.OnClickPlayVideo = (_) => OnClickFinalExam(finalItem);
            finalItem.OnClickPlayVideo = (_) => OnClickFinalExamEvt?.Invoke(finalItem);

            ChapterUIManager.Instance.AddToList(headerFinal);
        }

        // Rebuild layout để tính lại vị trí/chiều cao
        ChapterUIManager.Instance.UpdateLessonProgress();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)content);

        if (scrollRect) scrollRect.verticalNormalizedPosition = 1f;
    }

    public Action<LessonUI> OnClickFinalExamEvt;

    private void PlayVideo(string url)
    {
        if (string.IsNullOrEmpty(url) || !videoPlayer) return;

        if (!proxyBoot) proxyBoot = FindAnyObjectByType<LocalProxyAutoBoot>();

        var finalUrl = proxyBoot ? proxyBoot.GetPlayableUrl(url) : url;

        Debug.Log("[PlayVideo] FINAL URL = " + finalUrl);

        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = finalUrl;

        videoPlayer.errorReceived -= OnVideoError;
        videoPlayer.errorReceived += OnVideoError;

        videoPlayer.Play();
    }

    private void OnVideoError(VideoPlayer vp, string msg)
    {
        Debug.LogError("[VideoPlayer] error: " + msg);
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

    static object GetMemberValue(object obj, string name)
    {
        if (obj == null) return null;
        var t = obj.GetType();

        var fi = t.GetField(name,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);
        if (fi != null) return fi.GetValue(obj);

        var pi = t.GetProperty(name,
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);
        if (pi != null) return pi.GetValue(obj, null);

        return null;
    }

    static string GetStringMember(object obj, params string[] names)
    {
        if (obj == null || names == null) return null;
        foreach (var n in names)
        {
            var v = GetMemberValue(obj, n) as string;
            if (!string.IsNullOrEmpty(v)) return v;
        }

        return null;
    }

    // ====== Tìm ID chỉ trong finalExam ======
    static readonly string[] FinalExamIdKeys = { "examId", "_id", "id" };

    // ID hợp lệ (Mongo 24 hex)
    static bool IsLikelyId(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        var t = s.Trim();
        return System.Text.RegularExpressions.Regex.IsMatch(t, "^[a-fA-F0-9]{24}$");
    }

    // Chỉ quét trong object finalExam, KHÔNG fallback sang settings/course
    static string FindIdInObjectOnly(object obj)
    {
        if (obj == null) return null;

        // string trực tiếp
        if (obj is string s && IsLikelyId(s)) return s.Trim();

        // dictionary
        if (obj is System.Collections.IDictionary dict)
        {
            foreach (var key in dict.Keys)
            {
                var ks = key?.ToString();
                if (string.IsNullOrEmpty(ks)) continue;

                foreach (var k in FinalExamIdKeys)
                {
                    if (string.Equals(ks, k, System.StringComparison.OrdinalIgnoreCase))
                    {
                        var val = dict[key];
                        var hit = FindIdInObjectOnly(val);
                        if (IsLikelyId(hit)) return hit;
                    }
                }
            }

            return null;
        }

        // object thường: thử đúng các key ID
        var t = obj.GetType();
        foreach (var k in FinalExamIdKeys)
        {
            var v = GetMemberValue(obj, k);
            var hit = FindIdInObjectOnly(v);
            if (IsLikelyId(hit)) return hit;
        }

        return null;
    }

    [SerializeField] bool debugFinalExam = true;

    // Chỉ trả ID khi THẬT SỰ có settings.finalExam
    public static string TryGetFinalExamId(object courseLike)
    {
        // Một số API trả { course: {...} }, số khác trả thẳng {...}
        var course = GetMemberValue(courseLike, "course") ?? courseLike;

        // Tìm settings ở các tên phổ biến
        var settings = GetMemberValue(course, "settings")
                       ?? GetMemberValue(course, "courseSettings");

        // finalExam có thể nằm trong settings hoặc (ít gặp) ngay trên course
        var finalExam = GetMemberValue(settings, "finalExam")
                        ?? GetMemberValue(course, "finalExam");

        // Không có finalExam => không có bài thi
        if (finalExam == null)
        {
            // if (debugFinalExam) Debug.Log("[CourseListView] finalExam: null -> no exam.");
            return null;
        }

        // Nếu là string/id hoặc object chứa id
        string id = FindIdInObjectOnly(finalExam);

        // if (debugFinalExam)
        {
            var tFinal = finalExam.GetType().FullName;
            Debug.Log($"[CourseListView] finalExam type={tFinal}, parsedId={(id ?? "<null>")}");
        }

        return IsLikelyId(id) ? id : null;
    }
    public LessonUI PlayNextFromUrl(string currentUrl)
    {
        if (_videoLessons == null || _videoLessons.Count == 0) return null;

        int startIndex = -1;

        if (!string.IsNullOrEmpty(currentUrl))
        {
            for (int i = 0; i < _videoLessons.Count; i++)
            {
                if (_videoLessons[i] != null &&
                    _videoLessons[i].linkVideo2 == currentUrl)
                {
                    startIndex = i;
                    break;
                }
            }
        }

        int nextIndex = startIndex + 1;
        if (nextIndex < 0) nextIndex = 0;
        if (nextIndex >= _videoLessons.Count) return null;

        return _videoLessons[nextIndex];
    }

    public void PlayLesson(LessonUI lesson)
    {
        if (lesson == null) return;

        if (_currentLesson != null && _currentLesson != lesson)
        {
            // Check bài HIỆN TẠI đã done chưa
            if (!_currentLesson.IsLessonDone())
            {
                LoadingUI.ShowErrorPopup(
                    message: "Vui lòng hoàn thành bài học trước khi qua bài mới.",
                    header: "Thông báo",
                    onReturn: null
                );
                return;
            }
        }

        // Clear selection cũ
        if (_currentLesson != null && _currentLesson != lesson)
        {
            _currentLesson.SetActive(false);

            if (_currentLesson.chapterUI != null && _currentLesson.chapterUI != lesson.chapterUI)
            {
                _currentLesson.chapterUI.ChangeState(ChapterUI.ChapterState.Normal);
                _currentLesson.chapterUI.ResetLessonState();
            }
        }

        _currentLesson = lesson;
        lesson.chapterUI.SelectLesson(lesson);
        lesson.chapterUI.SelectThisChapter();

        PlayVideo(lesson.linkVideo2);
    }
}