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
    public Transform content;
    public ChapterUI headerPrefab;
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

    public float fallbackItemHeight = 120f;
    public float verticalSpacing = 6f;

    public SceneLessonUI sceneLessonUI;
    public string courseID;

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

    // Android Local Proxy Stable Cache
    private bool useProxyPreloadOnAndroid = true;

    // Buffer nhỏ để hiện frame đầu nhanh. Mượt khi phát sẽ do playback buffer guard quyết định.
    private int startupPlayableBufferKB = 2048;

    //Mức cache tối thiểu để bắt đầu sớm. Thấp quá sẽ có first frame nhưng dễ giật.
    private int minimumPlayableBufferKB = 1024;

    //Số luồng booster tải cache-ahead quanh vị trí player đang đọc.
    private int proxyBoosterThreads = 3;

    //Dung lượng mỗi chunk booster tải xuống disk.
    private int proxyChunkKB = 1024;

    //Thời gian tối đa warm head/tail trước khi bắt đầu Prepare video.
    private float startupPreloadTimeoutSeconds = 4f;

    //Sau thời gian này nếu đạt minimumPlayableBufferKB thì Prepare sớm.
    private float fastStartMinWaitSeconds = 0.75f;

    //Khi qua bài mới, cancel downloader bài cũ và xóa file cache bài cũ.
    private bool deleteOldProxyCacheOnLessonChange = true;

    //Ép cache-ahead chạy tuần tự. Chỉ bật khi upstream reset quá nhiều với nhiều Range song song.
    private bool forceSingleThreadProxyStartup = false;

    //Warm đoạn cuối MP4 trước Prepare vì Android thường seek tới cuối để đọc moov metadata.
    private bool preloadTailMetadataBeforePrepare = true;

    private int tailPreloadMB = 2;
    private int tailReadyKB = 512;

    //Legacy fallback. Để false nếu Android URL gốc không phát ổn định.
    private bool fallbackToOriginWhenProxyNotReady = false;

    //Android luôn phát qua local proxy nếu proxy start được. URL gốc đang không ổn định với MediaHTTPConnection.
    private bool forceProxyPlaybackOnAndroid = true;

    //Nếu VideoPlayer lỗi khi đang dùng proxy, restart network/proxy stream và thử lại local proxy 1 lần.
    private bool retryProxyOnProxyError = true;

    //Chỉ bật khi đã xác nhận Android phát trực tiếp URL gốc ổn định.
    private bool fallbackToOriginOnProxyError = false;

    //Bật log đo cache/preload.
    private bool debugProxyPreload = true;

    // Android Local Proxy Network Recovery
    private bool enableProxyNetworkWatch = true;

    private float networkWatchIntervalSeconds = 1f;
    private float networkChangeNotifyCooldownSeconds = 2f;

    // Android Local Proxy Health
    private bool enableProxyHealthWatch = true;

    private float proxyHealthIntervalSeconds = 1f;
    private float proxyHealthLogIntervalSeconds = 4f;
    private int lowBufferAheadWarningKB = 4096;

    // Pause playback before decoder starves; resume only after a real reservoir is available.
    private bool enableProxyPlaybackBufferGuard = true;
    private float pauseWhenAheadBelowSeconds = 3.0f;
    private float resumeWhenAheadAboveSeconds = 14.0f;
    private int guardBoostWindowMB = 24;

    // Android VideoPlayer can stall after decoder flush/recreate even while proxy buffer is healthy.
    private bool enableProxyPlaybackWatchdog = true;
    private float proxyPlaybackStallSeconds = 2.5f;
    private float proxyWatchdogMinAheadSeconds = 5.0f;
    private float proxyWatchdogResumeCooldownSeconds = 2.0f;

    private const string AndroidProxyClass = "com.unity.localproxy.LocalVideoProxy";
    private const int RuntimeStartupPlayableBufferKB = 2048;
    private const int RuntimeMinimumPlayableBufferKB = 1024;
    private const int RuntimeProxyChunkKB = 1024;
    private const int RuntimeTailReadyKB = 512;
    private const float RuntimeStartupPreloadTimeoutSeconds = 4f;
    private const float RuntimeFastStartMinWaitSeconds = 0.75f;
    private const float RuntimeProxyHealthBoostCooldownSeconds = 1.5f;

    private Coroutine _playVideoRoutine;
    private Coroutine _networkWatchRoutine;
    private Coroutine _proxyHealthRoutine;

    private int _playVideoToken;
    private string _activeVideoOriginUrl;
    private string _currentOriginUrl;
    private bool _videoStopRequested = true;

    private bool _usingProxyForCurrentVideo;
    private bool _fallbackToOriginTriedForCurrentVideo;
    private bool _proxyRetryTriedForCurrentVideo;
    private bool _proxyBufferGuardPaused;
    private long _lastProxyRangeBoostStart = -1L;

    private NetworkReachability _lastReachability;
    private float _lastProxyNetworkNotifyRealtime = -999f;
    private float _lastProxyHealthLogRealtime = -999f;
    private float _lastProxyHealthBoostRealtime = -999f;
    private float _lastProxyPlaybackProgressRealtime = -999f;
    private float _lastProxyWatchdogResumeRealtime = -999f;
    private double _lastProxyObservedVideoTime = -1.0;
    private int _proxyWatchdogResumeCount;

    public const string FinalExamType = "FINAL_EXAM";
    public Action<LessonUI> OnClickFinalExamEvt;

    private bool debugFinalExam = true;

    private static readonly string[] FinalExamIdKeys = { "examId", "_id", "id" };

    void Awake()
    {
        proxyBoot = FindAnyObjectByType<LocalProxyAutoBoot>();

        learnUI = FindAnyObjectByType<LearnUI>();
        videoPlayerControllerPro = FindAnyObjectByType<VideoPlayerControllerPro>();
        examResultReviewPanel = FindAnyObjectByType<ExamResultReviewPanel>();
        playerStandUI = FindAnyObjectByType<PlayerStandUI>();
    }

    private void OnEnable()
    {
        StartProxyWatchers();
    }

private void OnDisable()
{
    StopProxyWatchers();
    StopAndReleaseActiveVideo();
    HardStopVideoAudio();
}

    private void OnApplicationPause(bool pause)
    {
        if (!pause)
        {
            NotifyProxyNetworkChanged("app_resume");
        }
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            NotifyProxyNetworkChanged("app_focus");
        }
    }

    private void StartProxyWatchers()
    {
        _lastReachability = Application.internetReachability;

        if (enableProxyNetworkWatch && _networkWatchRoutine == null)
        {
            _networkWatchRoutine = StartCoroutine(NetworkWatchLoop());
        }

        if (enableProxyHealthWatch && _proxyHealthRoutine == null)
        {
            _proxyHealthRoutine = StartCoroutine(ProxyHealthLoop());
        }
    }

    private void StopProxyWatchers()
    {
        if (_networkWatchRoutine != null)
        {
            StopCoroutine(_networkWatchRoutine);
            _networkWatchRoutine = null;
        }

        if (_proxyHealthRoutine != null)
        {
            StopCoroutine(_proxyHealthRoutine);
            _proxyHealthRoutine = null;
        }
    }

    private void ResetProxyPlaybackWatchdogState()
    {
        _lastProxyPlaybackProgressRealtime = -999f;
        _lastProxyWatchdogResumeRealtime = -999f;
        _lastProxyObservedVideoTime = -1.0;
        _proxyWatchdogResumeCount = 0;
    }

    private IEnumerator NetworkWatchLoop()
    {
        while (true)
        {
            var current = Application.internetReachability;

            if (current != _lastReachability)
            {
                Debug.Log($"[LocalProxy][NetworkWatch] changed: {_lastReachability} -> {current}");
                _lastReachability = current;

                NotifyProxyNetworkChanged("reachability_changed");
            }

            yield return new WaitForSecondsRealtime(Mathf.Max(0.5f, networkWatchIntervalSeconds));
        }
    }

    private void NotifyProxyNetworkChanged(string reason)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!useProxyPreloadOnAndroid)
            return;

        float now = Time.realtimeSinceStartup;

        if (now - _lastProxyNetworkNotifyRealtime < networkChangeNotifyCooldownSeconds)
            return;

        _lastProxyNetworkNotifyRealtime = now;

        if (!proxyBoot)
            proxyBoot = FindAnyObjectByType<LocalProxyAutoBoot>();

        if (!proxyBoot || !proxyBoot.enableProxyOnAndroid)
            return;

        if (!proxyBoot.EnsureStarted())
            return;

        bool ok = ProxyOnNetworkChanged();

        Debug.Log(
            $"[LocalProxy][NetworkChanged] reason={reason} ok={ok} activeUrl={_activeVideoOriginUrl ?? "<null>"}"
        );
#endif
    }

    private IEnumerator ProxyHealthLoop()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(Mathf.Max(0.5f, proxyHealthIntervalSeconds));

#if UNITY_ANDROID && !UNITY_EDITOR
            if (!useProxyPreloadOnAndroid)
                continue;

            if (string.IsNullOrWhiteSpace(_activeVideoOriginUrl))
                continue;

            if (!videoPlayer)
                continue;

            if (!_usingProxyForCurrentVideo)
                continue;

            if (!proxyBoot)
                proxyBoot = FindAnyObjectByType<LocalProxyAutoBoot>();

            if (!proxyBoot || !proxyBoot.enableProxyOnAndroid)
                continue;

            long cachedUntil = proxyBoot.GetCachedUntil(_activeVideoOriginUrl);
            long totalBytes = proxyBoot.GetTotalBytes(_activeVideoOriginUrl);
            long cachedBytes = ProxyGetCachedBytes(_activeVideoOriginUrl);

            long estimatedBytePos = -1;
            long aheadBytes = -1;
            double bytesPerSecond = -1.0;
            double aheadSeconds = -1.0;

            if (cachedUntil > 0 && totalBytes > 0 && videoPlayer.length > 1.0)
            {
                bytesPerSecond = totalBytes / Math.Max(1.0, videoPlayer.length);

                double ratio = videoPlayer.time / videoPlayer.length;
                ratio = Math.Max(0.0, Math.Min(1.0, ratio));

                estimatedBytePos = (long)(totalBytes * ratio);
                long cachedFromEstimatedPos = ProxyGetCachedUntilFrom(_activeVideoOriginUrl, estimatedBytePos);

                if (cachedFromEstimatedPos >= estimatedBytePos)
                    cachedUntil = cachedFromEstimatedPos;

                aheadBytes = cachedUntil - estimatedBytePos;

                if (bytesPerSecond > 1.0)
                    aheadSeconds = aheadBytes / bytesPerSecond;
            }

            float now = Time.realtimeSinceStartup;
            double currentVideoTime = videoPlayer.time;
            float stalledForSeconds = -1f;

            if (videoPlayer.isPrepared && currentVideoTime > 0.05)
            {
                if (_lastProxyObservedVideoTime < 0.0 ||
                    currentVideoTime > _lastProxyObservedVideoTime + 0.15)
                {
                    _lastProxyObservedVideoTime = currentVideoTime;
                    _lastProxyPlaybackProgressRealtime = now;
                    _proxyWatchdogResumeCount = 0;
                }
                else if (_lastProxyPlaybackProgressRealtime > 0f)
                {
                    stalledForSeconds = now - _lastProxyPlaybackProgressRealtime;
                }
            }

            bool shouldLog = now - _lastProxyHealthLogRealtime >= proxyHealthLogIntervalSeconds;

            if (shouldLog)
            {
                _lastProxyHealthLogRealtime = now;

                Debug.Log(
                    $"[LocalProxy][Health] usingProxy={_usingProxyForCurrentVideo} " +
                    $"time={videoPlayer.time:F1}/{videoPlayer.length:F1}s " +
                    $"estimatedPos={FormatBytes(estimatedBytePos)} " +
                    $"cachedUntil={FormatBytes(cachedUntil)} " +
                    $"ahead={FormatBytes(aheadBytes)} " +
                    $"aheadSeconds={(aheadSeconds >= 0.0 ? aheadSeconds.ToString("F1") : "<unknown>")} " +
                    $"playing={videoPlayer.isPlaying} prepared={videoPlayer.isPrepared} " +
                    $"guardPaused={_proxyBufferGuardPaused} " +
                    $"stalledFor={(stalledForSeconds >= 0f ? stalledForSeconds.ToString("F1") : "<none>")} " +
                    $"rangeCached={FormatBytes(cachedBytes)} " +
                    $"total={FormatBytes(totalBytes)}"
                );
            }

            long warningBytes = Mathf.Max(128, lowBufferAheadWarningKB) * 1024L;

            if (aheadBytes >= 0 && aheadBytes < warningBytes)
            {
                Debug.LogWarning(
                    $"[LocalProxy][Health] Low buffer ahead={FormatBytes(aheadBytes)} " +
                    $"({(aheadSeconds >= 0.0 ? aheadSeconds.ToString("F1") : "<unknown>")}s). Network may be weak."
                );
            }

            if (enableProxyPlaybackBufferGuard &&
                aheadSeconds >= 0.0 &&
                estimatedBytePos >= 0 &&
                videoPlayer.isPrepared)
            {
                if (!_proxyBufferGuardPaused &&
                    videoPlayer.isPlaying &&
                    aheadSeconds < Math.Max(0.5f, pauseWhenAheadBelowSeconds))
                {
                    _proxyBufferGuardPaused = true;
                    videoPlayer.Pause();

                    Debug.LogWarning(
                        $"[LocalProxy][BufferGuard] Pause playback. ahead={aheadSeconds:F1}s " +
                        $"needResume={resumeWhenAheadAboveSeconds:F1}s"
                    );
                }
                else if (_proxyBufferGuardPaused &&
                         aheadSeconds >= Math.Max(pauseWhenAheadBelowSeconds + 1f, resumeWhenAheadAboveSeconds))
                {
                    _proxyBufferGuardPaused = false;
                    ResetProxyPlaybackWatchdogState();
                    videoPlayer.Play();

                    Debug.Log(
                        $"[LocalProxy][BufferGuard] Resume playback. ahead={aheadSeconds:F1}s"
                    );
                }
            }

            if (enableProxyPlaybackWatchdog &&
                !_proxyBufferGuardPaused &&
                videoPlayer.isPrepared &&
                currentVideoTime > 0.05 &&
                videoPlayer.length > 1.0 &&
                currentVideoTime < videoPlayer.length - 1.0 &&
                aheadSeconds >= Math.Max(1.0f, proxyWatchdogMinAheadSeconds) &&
                stalledForSeconds >= Math.Max(1.0f, proxyPlaybackStallSeconds) &&
                now - _lastProxyWatchdogResumeRealtime >= Math.Max(0.5f, proxyWatchdogResumeCooldownSeconds))
            {
                _lastProxyWatchdogResumeRealtime = now;
                _proxyWatchdogResumeCount++;

                if (_proxyWatchdogResumeCount >= 2 && videoPlayer.canSetTime)
                {
                    double nudgeTime = Math.Min(videoPlayer.length - 0.1, currentVideoTime + 0.05);
                    videoPlayer.time = nudgeTime;
                }

                videoPlayer.Play();
                videoPlayerControllerPro?.OnPlayStateChanged?.Invoke(true);

                Debug.LogWarning(
                    $"[LocalProxy][PlaybackWatchdog] Resume stalled player. " +
                    $"count={_proxyWatchdogResumeCount} time={currentVideoTime:F2}s " +
                    $"playing={videoPlayer.isPlaying} ahead={aheadSeconds:F1}s " +
                    $"nudge={(_proxyWatchdogResumeCount >= 2 && videoPlayer.canSetTime)}"
                );
            }

            if (estimatedBytePos >= 0 &&
                aheadBytes >= 0 &&
                aheadBytes < warningBytes &&
                now - _lastProxyHealthBoostRealtime >= RuntimeProxyHealthBoostCooldownSeconds)
            {
                _lastProxyHealthBoostRealtime = now;

                long boostStart = Math.Max(0L, estimatedBytePos);
                long boostLength = Math.Max(1, guardBoostWindowMB) * 1024L * 1024L;

                bool windowOk = proxyBoot.Preload(_activeVideoOriginUrl, boostStart);

                if (_proxyBufferGuardPaused &&
                    (_lastProxyRangeBoostStart < 0 ||
                     Math.Abs(boostStart - _lastProxyRangeBoostStart) >= RuntimeProxyChunkKB * 1024L))
                {
                    _lastProxyRangeBoostStart = boostStart;
                    ProxyPreloadRange(_activeVideoOriginUrl, boostStart, boostLength);
                }

                if (debugProxyPreload)
                {
                    Debug.Log(
                        $"[LocalProxy][HealthBoost] start={FormatBytes(boostStart)} " +
                        $"window={FormatBytes(boostLength)} windowOk={windowOk} paused={_proxyBufferGuardPaused}"
                    );
                }
            }
#endif
        }
    }

    public void BuildListUI(LmsCoursePrivate p)
    {
        _videoLessons.Clear();

        Debug.Log("Bắt đầu hiển thị danh sách bài học");

        ChapterUIManager.Instance.ClearList();
        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);

        courseID = GetStringMember(p, "_id")
                   ?? GetStringMember(GetMemberValue(p, "course"), "_id");

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
            finalItem.OnClickPlayVideo = (_) => PlayLesson(finalItem);
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

        if (isDocumentType)
        {
            string docUrl = GetFirstDocAttachUri(lesson);
            if (!string.IsNullOrWhiteSpace(docUrl))
                return docUrl.Trim();
        }

        string video2 = GetStringMember(lesson, "videoLink2");
        if (!string.IsNullOrWhiteSpace(video2))
            return video2.Trim();

        string video1 = GetStringMember(lesson, "videoLink");
        if (!string.IsNullOrWhiteSpace(video1))
            return video1.Trim();

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

        string oldUrl = _activeVideoOriginUrl;

        StopVideoPipeline();

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!proxyBoot)
            proxyBoot = FindAnyObjectByType<LocalProxyAutoBoot>();

        if (proxyBoot && proxyBoot.enableProxyOnAndroid && proxyBoot.EnsureStarted())
        {
            if (!string.IsNullOrWhiteSpace(oldUrl) && oldUrl != url)
            {
                ProxyRelease(oldUrl, deleteOldProxyCacheOnLessonChange);
            }

            ProxySetActiveUrl(url, deleteOldProxyCacheOnLessonChange);
        }
#endif

        _activeVideoOriginUrl = url;
        _currentOriginUrl = url;
        _fallbackToOriginTriedForCurrentVideo = false;
        _proxyRetryTriedForCurrentVideo = false;

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
        bool useProxyForThisPlay = false;

        _videoStartRealtime = Time.realtimeSinceStartup;
        _videoStartUrl = originUrl;
        _loggedFirstFrame = false;
        _currentOriginUrl = originUrl;
        _fallbackToOriginTriedForCurrentVideo = false;
        _proxyRetryTriedForCurrentVideo = false;
        _lastProxyHealthBoostRealtime = -999f;
        _proxyBufferGuardPaused = false;
        _lastProxyRangeBoostStart = -1L;
        ResetProxyPlaybackWatchdogState();
        _usingProxyForCurrentVideo = false;

        Debug.Log($"[testvideo][1] Got origin URL. t={_videoStartRealtime:F3}s | url={originUrl}");

        PrepareVideoPlayerForNewUrl();

#if UNITY_ANDROID && !UNITY_EDITOR
        if (useProxyPreloadOnAndroid && proxyBoot && proxyBoot.enableProxyOnAndroid)
        {
            bool started = proxyBoot.EnsureStarted();

            if (started)
            {
                int effectiveStartupKB = Mathf.Max(Mathf.Max(128, startupPlayableBufferKB), RuntimeStartupPlayableBufferKB);
                int effectiveMinKB = Mathf.Max(Mathf.Max(128, minimumPlayableBufferKB), RuntimeMinimumPlayableBufferKB);
                int effectiveChunkKB = Mathf.Max(Mathf.Max(256, proxyChunkKB), RuntimeProxyChunkKB);
                int effectiveTailReadyKB = Mathf.Max(Mathf.Max(128, tailReadyKB), RuntimeTailReadyKB);
                float effectiveTimeout = Mathf.Max(Mathf.Max(0.25f, startupPreloadTimeoutSeconds), RuntimeStartupPreloadTimeoutSeconds);
                float effectiveFastStart = Mathf.Max(Mathf.Max(0.25f, fastStartMinWaitSeconds), RuntimeFastStartMinWaitSeconds);

                long startupBytes = effectiveStartupKB * 1024L;
                long minBytes = effectiveMinKB * 1024L;
                long chunkBytes = effectiveChunkKB * 1024L;
                int boosters = forceSingleThreadProxyStartup ? 0 : Mathf.Clamp(proxyBoosterThreads, 1, 3);
                long tailBytes = Mathf.Max(1, tailPreloadMB) * 1024L * 1024L;
                long tailReadyBytes = effectiveTailReadyKB * 1024L;

                ProxyConfigure(startupBytes, boosters, chunkBytes);
                ProxySetActiveUrl(originUrl, deleteOldProxyCacheOnLessonChange);
                proxyBoot.Preload(originUrl, 0);

                float waitStart = Time.realtimeSinceStartup;
                long cachedUntil = -1;
                long totalBytes = -1;
                long tailStart = -1;
                long tailCachedUntil = -1;
                bool tailPreloadRequested = false;

                bool reachedStartupBuffer = false;
                bool reachedMinBuffer = false;
                bool reachedTailBuffer = false;

                while (Time.realtimeSinceStartup - waitStart < effectiveTimeout)
                {
                    if (token != _playVideoToken)
                        yield break;

                    cachedUntil = ProxyGetCachedUntilFrom(originUrl, 0);
                    if (cachedUntil < 0)
                        cachedUntil = proxyBoot.GetCachedUntil(originUrl);

                    totalBytes = proxyBoot.GetTotalBytes(originUrl);

                    reachedStartupBuffer = cachedUntil >= startupBytes;
                    reachedMinBuffer = cachedUntil >= minBytes;

                    if (preloadTailMetadataBeforePrepare && totalBytes > 0 && reachedMinBuffer)
                    {
                        tailStart = Math.Max(0L, totalBytes - tailBytes);

                        if (!tailPreloadRequested)
                        {
                            tailPreloadRequested = ProxyPreloadRange(originUrl, tailStart, tailBytes);

                            if (debugProxyPreload)
                            {
                                Debug.Log(
                                    $"[LocalProxy][WarmTail] request tail start={FormatBytes(tailStart)} length={FormatBytes(tailBytes)} ok={tailPreloadRequested}"
                                );
                            }
                        }

                        tailCachedUntil = ProxyGetCachedUntilFrom(originUrl, tailStart);
                        long tailNeedUntil = Math.Min(totalBytes, tailStart + tailReadyBytes);
                        reachedTailBuffer = tailCachedUntil >= tailNeedUntil;
                    }
                    else
                    {
                        reachedTailBuffer = !preloadTailMetadataBeforePrepare;
                    }

                    if (debugProxyPreload)
                    {
                        Debug.Log(
                            $"[LocalProxy][FastFramePreload] head={FormatBytes(cachedUntil)} / total={FormatBytes(totalBytes)} / startupNeed={FormatBytes(startupBytes)} / min={FormatBytes(minBytes)} / tail={FormatBytes(tailCachedUntil)} / tailStart={FormatBytes(tailStart)} / boosters={boosters}"
                        );
                    }

                    float elapsed = Time.realtimeSinceStartup - waitStart;

                    if (reachedStartupBuffer && reachedTailBuffer)
                        break;

                    if (elapsed >= effectiveFastStart && reachedMinBuffer && reachedTailBuffer)
                        break;

                    yield return null;
                }

                cachedUntil = ProxyGetCachedUntilFrom(originUrl, 0);
                if (cachedUntil < 0)
                    cachedUntil = proxyBoot.GetCachedUntil(originUrl);

                totalBytes = proxyBoot.GetTotalBytes(originUrl);
                if (tailStart >= 0)
                    tailCachedUntil = ProxyGetCachedUntilFrom(originUrl, tailStart);

                reachedStartupBuffer = cachedUntil >= startupBytes;
                reachedMinBuffer = cachedUntil >= minBytes;
                reachedTailBuffer =
                    !preloadTailMetadataBeforePrepare ||
                    (tailStart >= 0 && totalBytes > 0 && tailCachedUntil >= Math.Min(totalBytes, tailStart + tailReadyBytes));

                // bool shouldUseProxy = reachedMinBuffer || (forceProxyPlaybackOnAndroid && cachedUntil > 0);
                bool readyForSmoothProxy =
                    reachedStartupBuffer &&
                    (!preloadTailMetadataBeforePrepare || reachedTailBuffer);

                // Không fallback origin vì origin direct đang 400.
                // Nhưng cũng không Prepare quá sớm nếu muốn xem mượt.
                float maxWaitForSmoothSeconds = Mathf.Max(6f, effectiveTimeout + 1.5f);

                while (!readyForSmoothProxy && Time.realtimeSinceStartup - waitStart < maxWaitForSmoothSeconds)
                {
                    if (token != _playVideoToken)
                        yield break;

                    cachedUntil = ProxyGetCachedUntilFrom(originUrl, 0);
                    if (cachedUntil < 0)
                        cachedUntil = proxyBoot.GetCachedUntil(originUrl);

                    totalBytes = proxyBoot.GetTotalBytes(originUrl);

                    reachedStartupBuffer = cachedUntil >= startupBytes;
                    reachedMinBuffer = cachedUntil >= minBytes;

                    if (preloadTailMetadataBeforePrepare && totalBytes > 0)
                    {
                        if (tailStart < 0)
                            tailStart = Math.Max(0L, totalBytes - tailBytes);

                        if (!tailPreloadRequested && reachedMinBuffer)
                        {
                            tailPreloadRequested = ProxyPreloadRange(originUrl, tailStart, tailBytes);

                            if (debugProxyPreload)
                            {
                                Debug.Log(
                                    $"[LocalProxy][WarmTail] request tail start={FormatBytes(tailStart)} length={FormatBytes(tailBytes)} ok={tailPreloadRequested}"
                                );
                            }
                        }

                        tailCachedUntil = ProxyGetCachedUntilFrom(originUrl, tailStart);
                        long tailNeedUntil = Math.Min(totalBytes, tailStart + tailReadyBytes);
                        reachedTailBuffer = tailCachedUntil >= tailNeedUntil;
                    }
                    else
                    {
                        reachedTailBuffer = !preloadTailMetadataBeforePrepare;
                    }

                    readyForSmoothProxy =
                        reachedStartupBuffer &&
                        (!preloadTailMetadataBeforePrepare || reachedTailBuffer);

                    if (debugProxyPreload)
                    {
                        Debug.Log(
                            $"[LocalProxy][WaitFirstFrame] head={FormatBytes(cachedUntil)} / " +
                            $"startupNeed={FormatBytes(startupBytes)} / total={FormatBytes(totalBytes)} / " +
                            $"tail={FormatBytes(tailCachedUntil)} / tailReady={reachedTailBuffer} / " +
                            $"smoothReady={readyForSmoothProxy}"
                        );
                    }

                    yield return null;
                }

                finalUrl = proxyBoot.GetPlayableUrl(originUrl);
                useProxyForThisPlay = true;

                if (debugProxyPreload)
                {
                    float waited = Time.realtimeSinceStartup - waitStart;

                    Debug.Log(
                        $"[LocalProxy][FastFramePreload] Use proxy. wait={waited:F2}s | " +
                        $"cached={FormatBytes(cachedUntil)} | total={FormatBytes(totalBytes)} | " +
                        $"startupReady={reachedStartupBuffer} | minReady={reachedMinBuffer} | " +
                        $"tailReady={reachedTailBuffer} | smoothReady={readyForSmoothProxy} | " +
                        $"finalUrl={finalUrl}"
                    );
                }

                if (!readyForSmoothProxy)
                {
                    Debug.LogWarning(
                        $"[LocalProxy][FastFramePreload] Max wait reached. Continue through proxy read-through. " +
                        $"head={FormatBytes(cachedUntil)}, startupNeed={FormatBytes(startupBytes)}, " +
                        $"tailReady={reachedTailBuffer}"
                    );
                }
            }
            else
            {
                Debug.LogWarning("[LocalProxy] Proxy start failed. Stop prepare because origin direct may return 400.");
                yield break;
            }
        }
#endif

        if (token != _playVideoToken)
            yield break;

        _usingProxyForCurrentVideo = useProxyForThisPlay;
        _proxyBufferGuardPaused = false;
        _lastProxyRangeBoostStart = -1L;
        ResetProxyPlaybackWatchdogState();
        _videoStartUrl = finalUrl;

        Debug.Log($"[testvideo][1.5] Prepare video. proxy={useProxyForThisPlay} t={Time.realtimeSinceStartup:F3}s | url={finalUrl}");

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

        _videoStopRequested = false;
        SetVideoAudioEnabled(true);

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

    _playVideoToken++;
    _videoStopRequested = true;

    HardStopVideoAudio();

    _videoStartUrl = null;
    _loggedFirstFrame = false;
    _usingProxyForCurrentVideo = false;
    _proxyRetryTriedForCurrentVideo = false;
    _lastProxyHealthBoostRealtime = -999f;
    _proxyBufferGuardPaused = false;
    _lastProxyRangeBoostStart = -1L;
    ResetProxyPlaybackWatchdogState();
}
private void SetVideoAudioEnabled(bool enabled)
{
    if (!videoPlayer) return;

    try
    {
        int trackCount = 1;

        try
        {
            trackCount = videoPlayer.controlledAudioTrackCount > 0
                ? videoPlayer.controlledAudioTrackCount
                : videoPlayer.audioTrackCount;
        }
        catch
        {
            trackCount = 1;
        }

        trackCount = Mathf.Clamp(trackCount, 1, 8);

        for (ushort i = 0; i < trackCount; i++)
        {
            try
            {
                videoPlayer.EnableAudioTrack(i, enabled);
            }
            catch { }

            try
            {
                videoPlayer.SetDirectAudioMute(i, !enabled);
                videoPlayer.SetDirectAudioVolume(i, enabled ? 1f : 0f);
            }
            catch { }

            try
            {
                AudioSource source = videoPlayer.GetTargetAudioSource(i);
                if (source)
                {
                    source.mute = !enabled;
                    source.volume = enabled ? 1f : 0f;

                    if (!enabled)
                    {
                        source.Stop();
                    }
                }
            }
            catch { }
        }
    }
    catch { }
}

private void HardStopVideoAudio()
{
    if (!videoPlayer) return;

    _videoStopRequested = true;

    videoPlayer.errorReceived -= OnVideoError;
    videoPlayer.prepareCompleted -= OnVideoPrepared;
    videoPlayer.frameReady -= OnVideoFrameReady;
    videoPlayer.sendFrameReadyEvents = false;

    SetVideoAudioEnabled(false);

    try
    {
        videoPlayer.Pause();
    }
    catch { }

    try
    {
        videoPlayer.Stop();
    }
    catch { }

    try
    {
        videoPlayer.url = string.Empty;
        videoPlayer.clip = null;
    }
    catch { }

    videoPlayerControllerPro?.OnPlayStateChanged?.Invoke(false);
}
    private void StopAndReleaseActiveVideo()
    {
        string oldUrl = _activeVideoOriginUrl;

        StopVideoPipeline();

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!string.IsNullOrWhiteSpace(oldUrl))
        {
            ProxyRelease(oldUrl, deleteOldProxyCacheOnLessonChange);
        }
#endif

        _activeVideoOriginUrl = null;
        _currentOriginUrl = null;
        _fallbackToOriginTriedForCurrentVideo = false;
        _proxyRetryTriedForCurrentVideo = false;
    }

    private void OnDestroy()
    {
        StopProxyWatchers();
        StopAndReleaseActiveVideo();
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private static bool ProxyConfigure(long startupBytes, int boosterThreads, long chunkBytes)
    {
        try
        {
            using (var jc = new AndroidJavaClass(AndroidProxyClass))
            {
                return jc.CallStatic<bool>("configure", startupBytes, boosterThreads, chunkBytes);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[LocalProxy] configure failed: " + e.Message);
            return false;
        }
    }

    private static bool ProxySetActiveUrl(string originUrl, bool deleteOldCaches)
    {
        if (string.IsNullOrWhiteSpace(originUrl))
            return false;

        try
        {
            using (var jc = new AndroidJavaClass(AndroidProxyClass))
            {
                return jc.CallStatic<bool>("setActiveUrl", originUrl, deleteOldCaches);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[LocalProxy] setActiveUrl failed: " + e.Message);
            return false;
        }
    }

    private static bool ProxyRelease(string originUrl, bool deleteFile)
    {
        if (string.IsNullOrWhiteSpace(originUrl))
            return false;

        try
        {
            using (var jc = new AndroidJavaClass(AndroidProxyClass))
            {
                return jc.CallStatic<bool>("release", originUrl, deleteFile);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[LocalProxy] release failed: " + e.Message);
            return false;
        }
    }

    private static bool ProxyOnNetworkChanged()
    {
        try
        {
            using (var jc = new AndroidJavaClass(AndroidProxyClass))
            {
                return jc.CallStatic<bool>("onNetworkChanged");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[LocalProxy] onNetworkChanged failed: " + e.Message);
            return false;
        }
    }

    private static bool ProxyPreloadRange(string originUrl, long start, long length)
    {
        if (string.IsNullOrWhiteSpace(originUrl) || length <= 0)
            return false;

        try
        {
            using (var jc = new AndroidJavaClass(AndroidProxyClass))
            {
                return jc.CallStatic<bool>("preloadRange", originUrl, start, length);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning("[LocalProxy] preloadRange failed: " + e.Message);
            return false;
        }
    }

    private static long ProxyGetCachedBytes(string originUrl)
    {
        if (string.IsNullOrWhiteSpace(originUrl))
            return -1L;

        try
        {
            using (var jc = new AndroidJavaClass(AndroidProxyClass))
            {
                return jc.CallStatic<long>("getCachedBytes", originUrl);
            }
        }
        catch
        {
            return -1L;
        }
    }

    private static long ProxyGetCachedUntilFrom(string originUrl, long start)
    {
        if (string.IsNullOrWhiteSpace(originUrl))
            return -1L;

        try
        {
            using (var jc = new AndroidJavaClass(AndroidProxyClass))
            {
                return jc.CallStatic<long>("getCachedUntilFrom", originUrl, start);
            }
        }
        catch
        {
            return -1L;
        }
    }
#endif

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

        if (!_usingProxyForCurrentVideo)
            return;

        if (string.IsNullOrWhiteSpace(_currentOriginUrl))
            return;

        if (retryProxyOnProxyError && !_proxyRetryTriedForCurrentVideo)
        {
            _proxyRetryTriedForCurrentVideo = true;
            int token = ++_playVideoToken;
            StartCoroutine(RetryProxyAfterVideoError(_currentOriginUrl, token));
            return;
        }

        if (!fallbackToOriginOnProxyError)
            return;

        if (_fallbackToOriginTriedForCurrentVideo)
            return;

        _fallbackToOriginTriedForCurrentVideo = true;

        StartCoroutine(FallbackToOriginAfterProxyError(_currentOriginUrl, ++_playVideoToken));
    }

    private IEnumerator RetryProxyAfterVideoError(string originUrl, int token)
    {
        yield return new WaitForSecondsRealtime(0.25f);

        if (token != _playVideoToken)
            yield break;

        Debug.LogWarning("[LocalProxy] Proxy playback failed. Restart proxy stream and retry local URL: " + originUrl);

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!proxyBoot)
            proxyBoot = FindAnyObjectByType<LocalProxyAutoBoot>();

        if (!proxyBoot || !proxyBoot.enableProxyOnAndroid || !proxyBoot.EnsureStarted())
        {
            Debug.LogWarning("[LocalProxy] Retry skipped because proxy is not available.");
            yield break;
        }

        ProxyOnNetworkChanged();

        long startupBytes = Mathf.Max(Mathf.Max(128, startupPlayableBufferKB), RuntimeStartupPlayableBufferKB) * 1024L;
        long chunkBytes = Mathf.Max(Mathf.Max(256, proxyChunkKB), RuntimeProxyChunkKB) * 1024L;
        int boosters = forceSingleThreadProxyStartup ? 0 : Mathf.Clamp(proxyBoosterThreads, 1, 3);

        ProxyConfigure(startupBytes, boosters, chunkBytes);
        ProxySetActiveUrl(originUrl, false);
        proxyBoot.Preload(originUrl, 0);

        string proxyUrl = proxyBoot.GetPlayableUrl(originUrl);
#else
        string proxyUrl = originUrl;
#endif

        if (token != _playVideoToken)
            yield break;

        PrepareVideoPlayerForNewUrl();

        _usingProxyForCurrentVideo = true;
        _videoStartRealtime = Time.realtimeSinceStartup;
        _videoStartUrl = proxyUrl;
        _loggedFirstFrame = false;
        _lastProxyHealthBoostRealtime = -999f;
        _proxyBufferGuardPaused = false;
        _lastProxyRangeBoostStart = -1L;
        ResetProxyPlaybackWatchdogState();

        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = proxyUrl;
        videoPlayer.Prepare();
    }

    private IEnumerator FallbackToOriginAfterProxyError(string originUrl, int token)
    {
        yield return null;

        if (token != _playVideoToken)
            yield break;

        Debug.LogWarning("[LocalProxy] Proxy playback failed. Fallback to origin URL: " + originUrl);

#if UNITY_ANDROID && !UNITY_EDITOR
        ProxyRelease(originUrl, true);
#endif

        _usingProxyForCurrentVideo = false;
        _proxyBufferGuardPaused = false;
        _lastProxyRangeBoostStart = -1L;
        ResetProxyPlaybackWatchdogState();

        PrepareVideoPlayerForNewUrl();

        _videoStartRealtime = Time.realtimeSinceStartup;
        _videoStartUrl = originUrl;
        _loggedFirstFrame = false;

        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = originUrl;
        videoPlayer.Prepare();
    }

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

    private static bool IsLikelyId(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;

        var t = s.Trim();
        return System.Text.RegularExpressions.Regex.IsMatch(t, "^[a-fA-F0-9]{24}$");
    }

    private static string FindIdInObjectOnly(object obj)
    {
        if (obj == null) return null;

        if (obj is string s && IsLikelyId(s))
            return s.Trim();

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

        foreach (var k in FinalExamIdKeys)
        {
            var v = GetMemberValue(obj, k);
            var hit = FindIdInObjectOnly(v);
            if (IsLikelyId(hit)) return hit;
        }

        return null;
    }

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

        if (_currentLesson != lesson && !IsLessonUnlocked(lesson))
        {
            LoadingUI.ShowErrorPopup(
                message: "Vui lòng hoàn thành bài học trước khi qua bài mới.",
                header: "Thông báo",
                onReturn: null
            );
            return;
        }

        LessonProgressTracker.Instance.UpdateLesson(null);

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

        if (targetIsFinalExam)
        {
            StopAndReleaseActiveVideo();
            OnClickFinalExamEvt?.Invoke(_currentLesson);
            return;
        }

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

            StopAndReleaseActiveVideo();

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

    private bool IsLessonUnlocked(LessonUI lesson)
    {
        if (lesson == null)
            return false;

        if (lesson.IsLessonDone())
            return true;

        if (lesson.chapterUI != null &&
            lesson.chapterUI.chapterState == ChapterUI.ChapterState.Lock)
        {
            return false;
        }

        if (!IsVideoLesson(lesson))
            return true;

        int targetIndex = _videoLessons.IndexOf(lesson);
        if (targetIndex <= 0)
            return true;

        for (int i = 0; i < targetIndex; i++)
        {
            var previousLesson = _videoLessons[i];
            if (previousLesson != null && !previousLesson.IsLessonDone())
                return false;
        }

        return true;
    }

private void OnVideoPrepared(VideoPlayer vp)
{
    if (_videoStopRequested ||
        !isActiveAndEnabled ||
        string.IsNullOrWhiteSpace(_currentOriginUrl) ||
        string.IsNullOrWhiteSpace(vp.url))
    {
        Debug.LogWarning("[CourseListView] Video prepared after stop/stand-up. Block auto play.");

        HardStopVideoAudio();
        return;
    }

    SetVideoAudioEnabled(true);
    vp.Play();
    videoPlayerControllerPro?.OnPlayStateChanged?.Invoke(true);
}

    private void OnVideoFrameReady(VideoPlayer vp, long frameIdx)
    {
        if (_loggedFirstFrame)
            return;

        _loggedFirstFrame = true;

        float now = Time.realtimeSinceStartup;
        float delta = now - _videoStartRealtime;

        Debug.Log($"[testvideo][2] First frame READY after {delta:F3}s | frame={frameIdx} | url={_videoStartUrl}");

#if UNITY_ANDROID && !UNITY_EDITOR
        if (enableProxyPlaybackBufferGuard &&
            _usingProxyForCurrentVideo &&
            !_proxyBufferGuardPaused &&
            !string.IsNullOrWhiteSpace(_activeVideoOriginUrl))
        {
            long totalBytes = proxyBoot ? proxyBoot.GetTotalBytes(_activeVideoOriginUrl) : -1L;

            if (totalBytes > 0 && vp.length > 1.0)
            {
                double ratio = vp.time / vp.length;
                ratio = Math.Max(0.0, Math.Min(1.0, ratio));

                long estimatedBytePos = (long)(totalBytes * ratio);
                long cachedUntil = ProxyGetCachedUntilFrom(_activeVideoOriginUrl, estimatedBytePos);
                double bytesPerSecond = totalBytes / Math.Max(1.0, vp.length);
                double aheadSeconds = (cachedUntil - estimatedBytePos) / Math.Max(1.0, bytesPerSecond);

                if (aheadSeconds >= 0.0 && aheadSeconds < Math.Max(0.5f, pauseWhenAheadBelowSeconds))
                {
                    _proxyBufferGuardPaused = true;
                    vp.Pause();

                    Debug.LogWarning(
                        $"[LocalProxy][BufferGuard] Pause after first frame. ahead={aheadSeconds:F1}s " +
                        $"needResume={resumeWhenAheadAboveSeconds:F1}s"
                    );
                }
            }
        }
#endif

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
