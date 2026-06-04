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
    [SerializeField] private Transform examCamera;
    [SerializeField] private GameObject examPanelRoot;
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

    private float _videoStartRealtime;
    private string _videoStartUrl;
    private bool _loggedFirstFrame;


    [SerializeField] private LocalProxyAutoBoot proxyBoot;
    [Header("Android Local Proxy Buffer")]
    [SerializeField] private bool useProxyPreloadOnAndroid = true;

    [Tooltip("Số MB cần cache trước rồi mới Prepare video.")]
    [SerializeField] private int preloadBeforePrepareMB = 20;

    [Tooltip("Thời gian chờ cache tối đa trước khi vẫn cho video chạy.")]
    [SerializeField] private float preloadTimeoutSeconds = 10f;

    [Tooltip("Bật log đo cache/preload.")]
    [SerializeField] private bool debugProxyPreload = true;

    private Coroutine _playVideoRoutine;
    private int _playVideoToken;

    public const string FinalExamType = "FINAL_EXAM";
    public Action<LessonUI> OnClickFinalExamEvt;

    [SerializeField] private bool debugFinalExam = true;

    private static readonly string[] FinalExamIdKeys = { "examId", "_id", "id" };

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
                    continue;

                string chapTitle = string.IsNullOrEmpty(ch.chapterTitle) ? "" : ch.chapterTitle.Trim();
                ChapterUI headerChapter = null;

                if (!string.IsNullOrEmpty(chapTitle))
                {
                    headerChapter = Instantiate(headerPrefab, content);
                    headerChapter.titleName.text = $"{chapTitle}";
                    headerChapter.chapterID = ch._id;
                    ChapterUIManager.Instance.AddToList(headerChapter);
                }

                if (headerChapter == null)
                    continue;

                if (ch.lessons == null) continue;

                foreach (var lesson in ch.lessons)
                {
                    if (lesson == null) continue;

                    string lessonTitle = string.IsNullOrEmpty(lesson.title) ? "" : lesson.title.Trim();
                    if (string.IsNullOrEmpty(lessonTitle)) continue;

                    // Quan trọng:
                    // - Video: ưu tiên videoLink2/videoLink.
                    // - Tài liệu/PDF/Text: ưu tiên docAttach[0].uri.
                    string link2 = ResolveLessonPlayableUrl(lesson);

                    Debug.Log($"[Lesson Link Map] title={lesson.title} | type={lesson.type} | finalLink={link2}");

                    var lessonUI = Instantiate(itemPrefab, headerChapter.lessonContainer.transform);
                    lessonUI.titleTMP.text = $"{lessonTitle}";
                    lessonUI.linkVideo2 = link2;
                    lessonUI.lessonID = lesson._id;
                    lessonUI.type = lesson.type;
                    lessonUI.chapterUI = headerChapter;

                    if (lesson.completionCondition != null)
                        lessonUI.percent = lesson.completionCondition.percent;

                    lessonUI.OnClickPlayVideo = (_) =>
                    {
                        PlayLesson(lessonUI);
                    };

                    lessonUI.progressTime = lesson.progressTime;

                    int.TryParse(lesson.duration, out var duration);
                    lessonUI.duration = duration;

                    // Chỉ đưa video thật vào danh sách Next.
                    // Tài liệu/PDF không được đưa vào pipeline video/proxy.
                    if (!string.IsNullOrEmpty(lessonUI.linkVideo2) && IsVideoLesson(lessonUI))
                    {
                        _videoLessons.Add(lessonUI);
                    }

                    lessonUI.SetActive(false);

                    if (lesson.completionCondition != null)
                    {
                        Debug.Log(
                            $"Title {lesson.title} Condition {lesson.completionCondition.condition} Percent {lesson.completionCondition.percent}"
                        );
                    }
                    else
                    {
                        Debug.Log($"Title {lesson.title} Condition <null>");
                    }

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
            headerFinal.chapterID = null;
            headerFinal.SetFinalExam();

            ChapterUIManager.Instance.AddToList(headerFinal);
            ChapterUIManager.Instance.finalExamChapter = headerFinal;

            var finalItem = Instantiate(itemPrefab, headerFinal.lessonContainer.transform);
            finalItem.titleTMP.text = finalExamItemTitle;
            finalItem.linkVideo2 = "";
            finalItem.lessonID = finalExamId;
            finalItem.type = FinalExamType;
            finalItem.chapterUI = headerFinal;
            finalItem.OnClickPlayVideo = (_) => OnClickFinalExamEvt?.Invoke(finalItem);
            finalItem.SetActive(false);

            headerFinal.AddToList(finalItem);
        }

        ChapterUIManager.Instance.UpdateLessonProgress();
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)content);

        if (scrollRect)
            scrollRect.verticalNormalizedPosition = 1f;
    }

    private static string ResolveLessonPlayableUrl(object lesson)
    {
        if (lesson == null) return "";

        string type = GetStringMember(lesson, "type")?.Trim().ToUpperInvariant();

        bool isDocumentType =
            type == "TEXT" ||
            type == "DOCUMENT" ||
            type == "DOC" ||
            type == "PDF" ||
            type == "FILE" ||
            type == "HTML";

        // Với tài liệu/text/pdf: ưu tiên docAttach[].uri trước.
        if (isDocumentType)
        {
            string docUrl = GetFirstDocAttachUri(lesson);
            if (!string.IsNullOrWhiteSpace(docUrl))
                return docUrl.Trim();
        }

        // Với video: ưu tiên videoLink2 rồi videoLink.
        string video2 = GetStringMember(lesson, "videoLink2");
        if (!string.IsNullOrWhiteSpace(video2))
            return video2.Trim();

        string video1 = GetStringMember(lesson, "videoLink");
        if (!string.IsNullOrWhiteSpace(video1))
            return video1.Trim();

        // Nếu API trả tài liệu bằng field trực tiếp.
        string direct =
            GetStringMember(lesson, "url") ??
            GetStringMember(lesson, "link") ??
            GetStringMember(lesson, "content") ??
            GetStringMember(lesson, "text") ??
            GetStringMember(lesson, "html") ??
            GetStringMember(lesson, "documentUrl") ??
            GetStringMember(lesson, "documentLink") ??
            GetStringMember(lesson, "fileUrl") ??
            GetStringMember(lesson, "fileLink");

        if (!string.IsNullOrWhiteSpace(direct))
            return direct.Trim();

        // Fallback cuối: vẫn thử đọc docAttach để tránh API type thiếu/chưa chuẩn.
        string fallbackDoc = GetFirstDocAttachUri(lesson);
        return string.IsNullOrWhiteSpace(fallbackDoc) ? "" : fallbackDoc.Trim();
    }

    private static string GetFirstDocAttachUri(object lesson)
    {
        object docAttach =
            GetMemberValue(lesson, "docAttach") ??
            GetMemberValue(lesson, "docAttachments") ??
            GetMemberValue(lesson, "documents") ??
            GetMemberValue(lesson, "files");

        if (docAttach == null) return null;

        // Trường hợp docAttach là string trực tiếp.
        if (docAttach is string s)
            return string.IsNullOrWhiteSpace(s) ? null : s;

        if (!(docAttach is IEnumerable enumerable))
            return null;

        foreach (var item in enumerable)
        {
            if (item == null) continue;

            string uri =
                GetStringMember(item, "uri") ??
                GetStringMember(item, "url") ??
                GetStringMember(item, "link") ??
                GetStringMember(item, "fileUrl") ??
                GetStringMember(item, "fileLink");

            if (!string.IsNullOrWhiteSpace(uri))
                return uri;
        }

        return null;
    }

    private void PlayVideo(LessonUI lesson)
    {
        if (lesson == null) return;

        string url = lesson.linkVideo2;

        if (string.IsNullOrWhiteSpace(url))
        {
            Debug.LogWarning("[CourseListView] Empty video url.");
            return;
        }

        if (!IsVideoLesson(lesson))
        {
            Debug.LogWarning($"[CourseListView] Block non-video lesson from PlayVideo. type={lesson.type}, url={url}");
            return;
        }

        // Dừng VideoPlayer/coroutine cũ trước khi mở URL mới để socket cũ đóng hẳn.
        StopVideoPipeline();

        _playVideoToken++;
        _playVideoRoutine = StartCoroutine(PlayVideoWithProxyPreload(url, _playVideoToken));
    }

    private IEnumerator PlayVideoWithProxyPreload(string originUrl, int token)
    {
        if (string.IsNullOrEmpty(originUrl) || !videoPlayer)
            yield break;

        if (!proxyBoot)
            proxyBoot = FindAnyObjectByType<LocalProxyAutoBoot>();

        string finalUrl = originUrl;

        _videoStartRealtime = Time.realtimeSinceStartup;
        _videoStartUrl = originUrl;
        _loggedFirstFrame = false;

        Debug.Log($"[testvideo][1] Got origin URL. t={_videoStartRealtime:F3}s | url={originUrl}");

        PrepareVideoPlayerForNewUrl();

#if UNITY_ANDROID && !UNITY_EDITOR
        if (useProxyPreloadOnAndroid && proxyBoot && proxyBoot.enableProxyOnAndroid)
        {
            bool started = proxyBoot.EnsureStarted();

            if (started)
            {
                long minBufferBytes = Mathf.Max(1, preloadBeforePrepareMB) * 1024L * 1024L;

                proxyBoot.Preload(originUrl, 0);

                float waitStart = Time.realtimeSinceStartup;
                long cachedUntil = -1;
                long totalBytes = -1;

                while (Time.realtimeSinceStartup - waitStart < preloadTimeoutSeconds)
                {
                    if (token != _playVideoToken)
                        yield break;

                    cachedUntil = proxyBoot.GetCachedUntil(originUrl);
                    totalBytes = proxyBoot.GetTotalBytes(originUrl);

                    if (debugProxyPreload)
                    {
                        Debug.Log(
                            $"[LocalProxy][Preload] cached={FormatBytes(cachedUntil)} / total={FormatBytes(totalBytes)} / need={FormatBytes(minBufferBytes)}"
                        );
                    }

                    if (cachedUntil >= minBufferBytes)
                        break;

                    yield return null;
                }

                finalUrl = proxyBoot.GetPlayableUrl(originUrl);

                if (debugProxyPreload)
                {
                    float waited = Time.realtimeSinceStartup - waitStart;

                    Debug.Log(
                        $"[LocalProxy][Preload] Done wait={waited:F2}s | cached={FormatBytes(cachedUntil)} | total={FormatBytes(totalBytes)} | finalUrl={finalUrl}"
                    );
                }
            }
            else
            {
                Debug.LogWarning("[LocalProxy] Proxy start failed. Fallback to origin URL.");
            }
        }
#endif

        if (token != _playVideoToken)
            yield break;

        _videoStartUrl = finalUrl;

        Debug.Log($"[testvideo][1.5] Prepare video. t={Time.realtimeSinceStartup:F3}s | url={finalUrl}");

        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = finalUrl;
        videoPlayer.Prepare();

        if (learnUI && learnUI.toggleLessonScrollView != null)
        {
            learnUI.toggleLessonScrollView.ChangeState(ToggleBaseUI.State.DeActive);
        }
    }

    private void PrepareVideoPlayerForNewUrl()
    {
        if (!videoPlayer) return;

        videoPlayer.errorReceived -= OnVideoError;
        videoPlayer.prepareCompleted -= OnVideoPrepared;
        videoPlayer.frameReady -= OnVideoFrameReady;

        videoPlayer.errorReceived += OnVideoError;
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.frameReady += OnVideoFrameReady;
        videoPlayer.sendFrameReadyEvents = true;

        videoPlayer.playOnAwake = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = true;

        try
        {
            videoPlayer.Stop();
        }
        catch { }

        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = string.Empty;
        videoPlayer.clip = null;
    }

    private void StopVideoPipeline()
    {
        if (_playVideoRoutine != null)
        {
            StopCoroutine(_playVideoRoutine);
            _playVideoRoutine = null;
        }

        // Tăng token để mọi coroutine video cũ tự hủy.
        _playVideoToken++;

        if (videoPlayer)
        {
            videoPlayer.errorReceived -= OnVideoError;
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.frameReady -= OnVideoFrameReady;
            videoPlayer.sendFrameReadyEvents = false;

            try
            {
                videoPlayer.Stop();
            }
            catch { }

            videoPlayer.url = string.Empty;
            videoPlayer.clip = null;
        }

        _videoStartUrl = null;
        _loggedFirstFrame = false;
    }

    private static string FormatBytes(long bytes)
    {
        if (bytes < 0)
            return "<unknown>";

        const long KB = 1024;
        const long MB = KB * 1024;
        const long GB = MB * 1024;

        if (bytes >= GB)
            return $"{bytes / (float)GB:F2}GB";

        if (bytes >= MB)
            return $"{bytes / (float)MB:F2}MB";

        if (bytes >= KB)
            return $"{bytes / (float)KB:F2}KB";

        return $"{bytes}B";
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

    private static object GetMemberValue(object obj, string name)
    {
        if (obj == null) return null;
        var t = obj.GetType();

        var fi = t.GetField(
            name,
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance
        );

        if (fi != null) return fi.GetValue(obj);

        var pi = t.GetProperty(
            name,
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance
        );

        if (pi != null) return pi.GetValue(obj, null);

        return null;
    }

    private static string GetStringMember(object obj, params string[] names)
    {
        if (obj == null || names == null) return null;

        foreach (var n in names)
        {
            var v = GetMemberValue(obj, n) as string;
            if (!string.IsNullOrEmpty(v)) return v;
        }

        return null;
    }

    // ID hợp lệ (Mongo 24 hex)
    private static bool IsLikelyId(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;

        var t = s.Trim();
        return System.Text.RegularExpressions.Regex.IsMatch(t, "^[a-fA-F0-9]{24}$");
    }

    // Chỉ quét trong object finalExam, KHÔNG fallback sang settings/course
    private static string FindIdInObjectOnly(object obj)
    {
        if (obj == null) return null;

        // string trực tiếp
        if (obj is string s && IsLikelyId(s))
            return s.Trim();

        // dictionary
        if (obj is System.Collections.IDictionary dict)
        {
            foreach (var key in dict.Keys)
            {
                var ks = key?.ToString();
                if (string.IsNullOrEmpty(ks)) continue;

                foreach (var k in FinalExamIdKeys)
                {
                    if (string.Equals(ks, k, StringComparison.OrdinalIgnoreCase))
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
        foreach (var k in FinalExamIdKeys)
        {
            var v = GetMemberValue(obj, k);
            var hit = FindIdInObjectOnly(v);
            if (IsLikelyId(hit)) return hit;
        }

        return null;
    }

    // Chỉ trả ID khi THẬT SỰ có settings.finalExam
    public static string TryGetFinalExamId(object courseLike)
    {
        var course = GetMemberValue(courseLike, "course") ?? courseLike;

        var settings = GetMemberValue(course, "settings")
                       ?? GetMemberValue(course, "courseSettings");

        var finalExam = GetMemberValue(settings, "finalExam")
                        ?? GetMemberValue(course, "finalExam");

        if (finalExam == null)
            return null;

        string id = FindIdInObjectOnly(finalExam);

        var tFinal = finalExam.GetType().FullName;
        Debug.Log($"[CourseListView] finalExam type={tFinal}, parsedId={(id ?? "<null>")}");

        return IsLikelyId(id) ? id : null;
    }

    public LessonUI PlayNextFromUrl(string currentUrl)
    {
        if (_videoLessons == null || _videoLessons.Count == 0) return null;

        // Nếu currentUrl là local proxy URL thì đổi ngược về origin URL
        // để so với lesson.linkVideo2.
        currentUrl = NormalizeProxyUrlToOrigin(currentUrl);

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

    private string NormalizeProxyUrlToOrigin(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return url;

        int uIndex = url.IndexOf("?u=", StringComparison.OrdinalIgnoreCase);
        if (uIndex < 0)
            uIndex = url.IndexOf("&u=", StringComparison.OrdinalIgnoreCase);

        if (uIndex < 0)
            return url;

        string encoded = url.Substring(uIndex + 3);
        int amp = encoded.IndexOf('&');
        if (amp >= 0)
            encoded = encoded.Substring(0, amp);

        try
        {
            return Uri.UnescapeDataString(encoded);
        }
        catch
        {
            return url;
        }
    }

    public void PlayLesson(LessonUI lesson)
    {
        if (lesson == null) return;

        bool targetIsDocument = IsDocumentLesson(lesson);
        bool targetIsVideo = IsVideoLesson(lesson);
        bool targetIsFinalExam = IsFinalExamLesson(lesson);

        // Không chặn tài liệu bằng rule hoàn thành video.
        // Chỉ chặn khi đang xem video chưa xong mà muốn qua video/bài thi khác.
        if (_currentLesson != null && _currentLesson != lesson)
        {
            bool currentIsBlockingVideo = IsVideoLesson(_currentLesson);
            bool targetRequiresPreviousDone = targetIsVideo || targetIsFinalExam;

            if (currentIsBlockingVideo && targetRequiresPreviousDone && !_currentLesson.IsLessonDone())
            {
                LoadingUI.ShowErrorPopup(
                    message: "Vui lòng hoàn thành bài học trước khi qua bài mới.",
                    header: "Thông báo",
                    onReturn: null
                );
                return;
            }
        }

        LessonProgressTracker.Instance.UpdateLesson(null);

        // Clear selection cũ
        if (_currentLesson != null && _currentLesson != lesson)
        {
            Debug.Log("[CourseListView] Clear selection lesson cũ");
            _currentLesson.SetActive(false);

            if (_currentLesson.chapterUI != null && _currentLesson.chapterUI != lesson.chapterUI)
            {
                Debug.Log("[CourseListView] Clear chapter lesson cũ");
                _currentLesson.chapterUI.ChangeState(ChapterUI.ChapterState.Normal);
                _currentLesson.chapterUI.ResetLessonState();
            }
        }

        Debug.Log($"[CourseListView] Cập nhật lesson mới | title={lesson.titleTMP?.text} | type={lesson.type} | url={lesson.linkVideo2}");

        _currentLesson = lesson;

        if (_currentLesson.chapterUI != null)
        {
            _currentLesson.chapterUI.SelectThisChapter();
            _currentLesson.chapterUI.SelectLesson(_currentLesson);
        }

        // ===== FINAL EXAM =====
        if (targetIsFinalExam)
        {
            StopVideoPipeline();
            OnClickFinalExamEvt?.Invoke(_currentLesson);
            return;
        }

        // ===== DOCUMENT/PDF/TEXT =====
        if (targetIsDocument)
        {
            Debug.Log("[CourseListView] Open document, skip video/proxy: " + _currentLesson.linkVideo2);

            if (string.IsNullOrWhiteSpace(_currentLesson.linkVideo2))
            {
                Debug.LogWarning(
                    $"[CourseListView] Document lesson has empty URL. title={_currentLesson.titleTMP?.text}, type={_currentLesson.type}"
                );
                return;
            }

            StopVideoPipeline();

            if (videoPlayerControllerPro != null)
            {
                videoPlayerControllerPro.SetCurrentUrl(null);
                videoPlayerControllerPro.ShowDocumentInCurrentMode(_currentLesson.linkVideo2);
            }
            else
            {
                Debug.LogWarning("[CourseListView] Missing VideoPlayerControllerPro, cannot show document.");
            }

            return;
        }

        // ===== VIDEO =====
        if (targetIsVideo)
        {
            Debug.Log("[CourseListView] Play video: " + _currentLesson.linkVideo2);

            if (videoPlayerControllerPro != null)
            {
                videoPlayerControllerPro.SetCurrentUrl(_currentLesson.linkVideo2);
                videoPlayerControllerPro.ShowVideoInCurrentMode();
            }

            PlayVideo(_currentLesson);
            return;
        }

        Debug.LogWarning(
            $"[CourseListView] Unsupported lesson content. type={_currentLesson.type}, url={_currentLesson.linkVideo2}"
        );
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        vp.Play();
    }

    private void OnVideoFrameReady(VideoPlayer vp, long frameIdx)
    {
        if (_loggedFirstFrame)
            return;

        _loggedFirstFrame = true;

        float now = Time.realtimeSinceStartup;
        float delta = now - _videoStartRealtime;

        Debug.Log($"[testvideo][2] First frame READY after {delta:F3}s | frame={frameIdx} | url={_videoStartUrl}");

        vp.frameReady -= OnVideoFrameReady;
        vp.sendFrameReadyEvents = false;
    }

    private bool IsFinalExamLesson(LessonUI lesson)
    {
        return lesson != null &&
               string.Equals(lesson.type, FinalExamType, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsVideoLesson(LessonUI lesson)
    {
        if (lesson == null) return false;

        string type = lesson.type?.Trim().ToUpperInvariant();

        if (type == "VIDEO" ||
            type == "LESSON_VIDEO" ||
            type == "MP4" ||
            type == "HLS")
        {
            return true;
        }

        if (type == "TEXT" ||
            type == "DOCUMENT" ||
            type == "DOC" ||
            type == "PDF" ||
            type == "FILE" ||
            type == "HTML")
        {
            return false;
        }

        return IsVideoUrl(lesson.linkVideo2);
    }

    private bool IsDocumentLesson(LessonUI lesson)
    {
        if (lesson == null) return false;

        string type = lesson.type?.Trim().ToUpperInvariant();

        if (type == "TEXT" ||
            type == "DOCUMENT" ||
            type == "DOC" ||
            type == "PDF" ||
            type == "FILE" ||
            type == "HTML")
        {
            return true;
        }

        if (type == "VIDEO" ||
            type == "LESSON_VIDEO" ||
            type == "MP4" ||
            type == "HLS")
        {
            return false;
        }

        return IsDocumentUrl(lesson.linkVideo2);
    }

    private bool IsVideoUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;

        string lower = url.ToLowerInvariant();

        return lower.Contains(".mp4")
               || lower.Contains(".m3u8")
               || lower.Contains(".mov")
               || lower.Contains(".webm")
               || lower.Contains(".m4v");
    }

    private bool IsDocumentUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;

        string lower = url.ToLowerInvariant();

        return lower.Contains(".pdf")
               || lower.Contains(".doc")
               || lower.Contains(".docx")
               || lower.Contains(".ppt")
               || lower.Contains(".pptx")
               || lower.Contains(".xls")
               || lower.Contains(".xlsx");
    }
}