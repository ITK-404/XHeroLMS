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

    // ====== FINAL EXAM (thêm mới) ======
    [Header("Final Exam")]
    public string finalExamSectionTitle = "Bài thi cuối khóa";

    public string finalExamItemTitle = "Vào bài thi";

    private List<ChapterUI> chapterList = new();

    private LearnUI learnUI;
    private VideoPlayerControllerPro videoPlayerControllerPro;
    private ExamResultReviewPanel examResultReviewPanel;
    private PlayerStandUI playerStandUI;
    void Awake()
    {
        learnUI = FindAnyObjectByType<LearnUI>();
        videoPlayerControllerPro = FindAnyObjectByType<VideoPlayerControllerPro>();
        examResultReviewPanel = FindAnyObjectByType<ExamResultReviewPanel>();
        playerStandUI = FindAnyObjectByType<PlayerStandUI>();
    }

    void Update()
    {
        if (ExamResultReviewPanel.FlagContinue)
        {
            // reset cờ NGAY ở đây để đảm bảo chỉ chạy một lần
            ExamResultReviewPanel.FlagContinue = false;
            ResetFromExam();
        }
    }
    public void BuildListUI(LmsCoursePrivate p)
    {
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
                    lessonUI.OnClickPlayVideo = PlayVideo;
                    lessonUI.progressTime = lesson.progressTime;


                    // parse duration 
                    int.TryParse(lesson.duration, out var duration);
                    lessonUI.duration = duration;
                    // update progress time
                    // int.TryParse(lesson.progressTime, out int progressTime);

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

            var finalItem = Instantiate(itemPrefab, headerFinal.lessonContainer.transform);
            finalItem.titleTMP.text = finalExamItemTitle;
            finalItem.linkVideo2 = ""; // không dùng video
            finalItem.lessonID = finalExamId; // giữ examId để xử lý sau
            finalItem.type = "FINAL_EXAM"; // đánh dấu loại
            finalItem.chapterUI = headerFinal;
            headerFinal.AddToList(finalItem);

            // Click = chuyển sang scene thi (lưu prefs)
            finalItem.OnClickPlayVideo = (_) => OnClickFinalExam(finalItem);

            ChapterUIManager.Instance.AddToList(headerFinal);
        }

        // Rebuild layout để tính lại vị trí/chiều cao
        ChapterUIManager.Instance.UpdateLessonProgress();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)content);

        if (scrollRect) scrollRect.verticalNormalizedPosition = 1f;
    }


    private void PlayVideo(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            Debug.LogWarning("[SceneLessonUI] Video URL rỗng.");
            return;
        }

        if (!videoPlayer)
        {
            Debug.LogWarning("[SceneLessonUI] videoPlayer null.");
            return;
        }

        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = url;
        videoPlayer.Play();
        Debug.Log("[SceneLessonUI] Playing: " + url);
    }

    private void EnsureListLayout(RectTransform rt)
    {
        var vlg = rt.GetComponent<VerticalLayoutGroup>();
        if (!vlg) vlg = rt.gameObject.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;
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
    private void OnClickFinalExam(LessonUI finalItem)
    {
        QuadCinemachineController.Instance.ChangeState(ViewState.Exam);

        PlayerPrefs.SetString("EXAM_CURRENT_ID", finalItem.lessonID);
        PlayerPrefs.SetString("EXAM_CURRENT_COURSE_ID", courseID);
        PlayerPrefs.Save();

        Debug.Log($"[CourseListView] Saved ExamID={finalItem.lessonID}, CourseID={courseID}");

        learnUI.Hide();
        videoPlayerControllerPro.ExitFullscreenUI();
        playerStandUI.HideWatchVideoUI();

        if (examCamRoutine != null)
            StopCoroutine(examCamRoutine);

        examCamRoutine = StartCoroutine(MoveCameraAndOpenExam());
    }

    private bool InitExamCamera()
    {
        if (examCamera == null || examPanelRoot == null)
        {
            Debug.LogWarning("[CourseListView] examCamera hoặc examPanelRoot chưa gán.");
            return false;
        }

        if (examLookAt == null)
            examLookAt = examCamera.GetComponent<CinemachineHardLookAt>();

        if (examLookAt == null)
        {
            Debug.LogWarning("[CourseListView] Không tìm thấy CinemachineHardLookAt trên examCamera.");
            return false;
        }

        if (!hasDefaultOffset)
        {
            defaultLookAtOffset = examLookAt.LookAtOffset;
            hasDefaultOffset = true;
        }

        if (!hasDefaultCameraTransform)
        {
            defaultCameraPosition = examCamera.position;
            defaultCameraRotation = examCamera.rotation;
            hasDefaultCameraTransform = true;
        }

        return true;
    }

    private IEnumerator MoveCameraAndOpenExam()
    {
        if (!InitExamCamera())
            yield break;

        examPanelRoot.SetActive(false);

        Vector3 startPos = examCamera.position;
        Vector3 endPos = new Vector3(startPos.x, 0.3f, startPos.z + 0.5f);

        float dur = Mathf.Max(0.01f, examMoveDuration);
        float t = 0f;

        // Tiến tới
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            float k = Mathf.SmoothStep(0f, 1f, t);
            examCamera.position = Vector3.Lerp(startPos, endPos, k);
            yield return null;
        }
        examCamera.position = endPos;

        // Cúi đầu
        Vector3 startOffset = examLookAt.LookAtOffset;
        Vector3 endOffset = startOffset;
        endOffset.y = -270f;

        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            float k = Mathf.SmoothStep(0f, 1f, t);
            examLookAt.LookAtOffset = Vector3.Lerp(startOffset, endOffset, k);
            yield return null;
        }
        examLookAt.LookAtOffset = endOffset;

        examPanelRoot.SetActive(true);
        Debug.Log("[CourseListView] Camera đã tiến tới và cúi đầu, mở panel exam.");
    }

    private void ResetFromExam()
    {
        QuadCinemachineController.Instance.ChangeState(ViewState.Sitdown);
        if (examCamRoutine != null)
        {
            StopCoroutine(examCamRoutine);
            examCamRoutine = null;
        }

        if (examPanelRoot != null)
            examPanelRoot.SetActive(false);

        examCamRoutine = StartCoroutine(ResetExamRoutine());
    }

    private IEnumerator MoveCameraBackFromExam()
    {
        if (!InitExamCamera())
            yield break;

        Vector3 startPos = examCamera.position;
        Quaternion startRot = examCamera.rotation;
        Vector3 startOffset = examLookAt.LookAtOffset;

        Vector3 endPos = hasDefaultCameraTransform ? defaultCameraPosition : startPos;
        Quaternion endRot = hasDefaultCameraTransform ? defaultCameraRotation : startRot;
        Vector3 endOffset = hasDefaultOffset ? defaultLookAtOffset : startOffset;

        float halfDur = Mathf.Max(0.01f, examMoveDuration) * 0.5f;
        float t;

        // Ngửa đầu
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / halfDur;
            float k = Mathf.SmoothStep(0f, 1f, t);
            examLookAt.LookAtOffset = Vector3.Lerp(startOffset, endOffset, k);
            yield return null;
        }
        examLookAt.LookAtOffset = endOffset;

        // Lùi về vị trí/rotation ban đầu
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / halfDur;
            float k = Mathf.SmoothStep(0f, 1f, t);
            examCamera.position = Vector3.Lerp(startPos, endPos, k);
            examCamera.rotation = Quaternion.Slerp(startRot, endRot, k);
            yield return null;
        }

        examCamera.position = endPos;
        examCamera.rotation = endRot;
    }

    private IEnumerator ResetExamRoutine()
    {
        yield return MoveCameraBackFromExam();

        learnUI.Show();
        videoPlayerControllerPro.EnterFullscreenUI();
        playerStandUI.ShowSitdownButton();

        Debug.Log("[CourseListView] ResetFromExam -> quay lại chế độ học (camera đã lerp về chỗ cũ).");
    }

}