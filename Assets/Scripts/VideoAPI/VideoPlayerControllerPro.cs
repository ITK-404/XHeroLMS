using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI; // chỉ cần nếu bạn muốn nối vào UI sau này

[DisallowMultipleComponent]
public class VideoPlayerControllerPro : MonoBehaviour
{
    [Header("Required")]
    public VideoPlayer videoPlayer;          // Kéo VideoPlayer vào đây
    public AudioSource audioSource;          // Optional (giúp chỉnh volume mượt hơn)

    [Header("Seek & Speed")]
    public double seekStepSeconds = 5.0;     // tua ±5s
    [Range(0.25f, 3f)] public float playbackSpeed = 1.0f;
    public float speedStep = 0.1f;           // , . để tăng/giảm tốc

    [Header("Volume")]
    [Range(0f, 1f)] public float volume = 1.0f;
    public float volumeStep = 0.05f;         // ↑/↓ ±5%
    public bool startMuted = false;

    [Header("Quality (Like YouTube)")]
    public QualityOption[] qualities;        // cấu hình nhiều chất lượng
    public int defaultQualityIndex = 0;      // chất lượng khi start

    [Header("HUD (optional)")]
    public bool showHUD = true;
    public Vector2 hudOffset = new Vector2(10, 10);

    [Serializable]
    public class QualityOption
    {
        public string label = "1080p";
        public SourceType sourceType = SourceType.Url;
        public string url;                   // dùng khi SourceType.Url
        public VideoClip clip;               // dùng khi SourceType.Clip
    }

    public enum SourceType { Url, Clip }

    int _currentQualityIndex = -1;
    bool _preparedOnce = false;
    bool _isSwitchingQuality = false;
    bool _wasPlayingBeforeSwitch = false;
    double _savedTimeOnSwitch = 0.0;
    bool _muted;

    void Reset()
    {
        videoPlayer = GetComponent<VideoPlayer>();
        audioSource = GetComponent<AudioSource>();
    }

    void Awake()
    {
        if (!videoPlayer)
            videoPlayer = GetComponent<VideoPlayer>();

        if (audioSource)
        {
            audioSource.playOnAwake = false;
            audioSource.volume = startMuted ? 0f : volume;
        }

        _muted = startMuted;
        ApplyVolume();
        ApplyPlaybackSpeed();

        // Wire events
        if (videoPlayer)
        {
            videoPlayer.playOnAwake = false;
            videoPlayer.errorReceived += OnVideoError;
            videoPlayer.prepareCompleted += OnVideoPrepared;
        }
    }

    void Start()
    {
        // Chọn chất lượng mặc định nếu có cấu hình
        if (qualities != null && qualities.Length > 0)
        {
            int idx = Mathf.Clamp(defaultQualityIndex, 0, qualities.Length - 1);
            StartCoroutine(SwitchQualityAndRestore(idx, 0.0, autoPlay:true));
        }
        else
        {
            // Không set quality list thì chỉ play nguồn có sẵn trong VideoPlayer
            PrepareIfNeeded(autoPlay:false);
        }
    }

    void Update()
    {
        if (!videoPlayer) return;

        // Keyboard control
        if (Input.GetKeyDown(KeyCode.Space))
            TogglePlayPause();

        if (Input.GetKeyDown(KeyCode.LeftArrow))
            SeekRelative(-seekStepSeconds);

        if (Input.GetKeyDown(KeyCode.RightArrow))
            SeekRelative(+seekStepSeconds);

        if (Input.GetKeyDown(KeyCode.UpArrow))
            ChangeVolume(+volumeStep);

        if (Input.GetKeyDown(KeyCode.DownArrow))
            ChangeVolume(-volumeStep);

        if (Input.GetKeyDown(KeyCode.Comma))
            ChangeSpeed(-speedStep);

        if (Input.GetKeyDown(KeyCode.Period))
            ChangeSpeed(+speedStep);

        if (Input.GetKeyDown(KeyCode.M))
            ToggleMute();

        if (Input.GetKeyDown(KeyCode.Q))
            CycleQuality(+1);

        // number keys 1..9 choose quality
        for (int i = 0; i < 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                int pick = i; // 0-based for Alpha1
                if (qualities != null && pick < qualities.Length)
                    SwitchQualityKeepTime(pick);
                break;
            }
        }
    }

    void OnDestroy()
    {
        if (videoPlayer)
        {
            videoPlayer.errorReceived -= OnVideoError;
            videoPlayer.prepareCompleted -= OnVideoPrepared;
        }
    }

    // ===== Controls =====
    public void TogglePlayPause()
    {
        if (!videoPlayer) return;
        if (!videoPlayer.isPrepared)
        {
            PrepareIfNeeded(autoPlay:true);
            return;
        }

        if (videoPlayer.isPlaying) videoPlayer.Pause();
        else videoPlayer.Play();
    }

    public void SeekRelative(double deltaSeconds)
    {
        if (!videoPlayer || !videoPlayer.isPrepared) return;
        double t = Mathf.Clamp((float)(videoPlayer.time + deltaSeconds), 0f, (float)videoPlayer.length);
        SetTimeSafely(t);
    }

    public void ChangeVolume(float delta)
    {
        volume = Mathf.Clamp01(volume + delta);
        ApplyVolume();
    }

    public void ToggleMute()
    {
        _muted = !_muted;
        ApplyVolume();
    }

    public void ChangeSpeed(float delta)
    {
        playbackSpeed = Mathf.Clamp(playbackSpeed + delta, 0.25f, 3f);
        ApplyPlaybackSpeed();
    }

    void ApplyVolume()
    {
        float vol = _muted ? 0f : volume;
        if (audioSource) audioSource.volume = vol;

        // VideoPlayer direct audio (nếu không dùng AudioSource)
        if (videoPlayer && videoPlayer.audioOutputMode == VideoAudioOutputMode.Direct)
        {
            // kênh 0
            try { videoPlayer.SetDirectAudioVolume(0, vol); } catch { /* ignore */ }
        }
    }

    void ApplyPlaybackSpeed()
    {
        if (!videoPlayer) return;
        videoPlayer.playbackSpeed = playbackSpeed;
        if (audioSource) audioSource.pitch = playbackSpeed; // lưu ý: pitch ≠ time-stretch
    }

    // ===== Quality Handling =====
    public void CycleQuality(int direction)
    {
        if (qualities == null || qualities.Length == 0) return;
        int next = (_currentQualityIndex + direction + qualities.Length) % qualities.Length;
        SwitchQualityKeepTime(next);
    }

    public void SwitchQualityKeepTime(int index)
    {
        if (!videoPlayer || qualities == null || index < 0 || index >= qualities.Length) return;

        double curTime = videoPlayer.isPrepared ? videoPlayer.time : 0.0;
        bool wasPlaying = videoPlayer.isPlaying;
        StartCoroutine(SwitchQualityAndRestore(index, curTime, wasPlaying));
    }

    IEnumerator SwitchQualityAndRestore(int index, double timeToRestore, bool autoPlay)
    {
        if (_isSwitchingQuality) yield break;
        _isSwitchingQuality = true;

        _savedTimeOnSwitch = timeToRestore;
        _wasPlayingBeforeSwitch = autoPlay;

        var q = qualities[index];

        videoPlayer.Stop();
        videoPlayer.source = (q.sourceType == SourceType.Url) ? VideoSource.Url : VideoSource.VideoClip;
        if (q.sourceType == SourceType.Url) videoPlayer.url = q.url;
        else videoPlayer.clip = q.clip;

        _currentQualityIndex = index;

        // Prepare then restore time
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared)
            yield return null;

        _preparedOnce = true;

        // Một số nguồn cần 1 frame mới set time chuẩn
        yield return null;

        SetTimeSafely(_savedTimeOnSwitch);

        if (_wasPlayingBeforeSwitch) videoPlayer.Play();

        _isSwitchingQuality = false;
    }

    // ===== Prepare & Time Helpers =====
    void PrepareIfNeeded(bool autoPlay)
    {
        if (!videoPlayer) return;

        if (!videoPlayer.isPrepared)
        {
            StartCoroutine(PrepareAndMaybePlay(autoPlay));
        }
        else
        {
            if (autoPlay) videoPlayer.Play();
        }
    }

    IEnumerator PrepareAndMaybePlay(bool autoPlay)
    {
        videoPlayer.Prepare();
        while (!videoPlayer.isPrepared) yield return null;
        _preparedOnce = true;
        if (autoPlay) videoPlayer.Play();
    }

    void SetTimeSafely(double t)
    {
        if (!videoPlayer) return;

        // Nếu có frameRate hợp lệ thì set frame để chính xác hơn
        if (videoPlayer.frameRate > 0.01f)
        {
            long frame = (long)Mathf.Clamp((float)(t * videoPlayer.frameRate), 0, (float)(videoPlayer.frameCount - 1));
            try
            {
                videoPlayer.frame = frame;
            }
            catch
            {
                videoPlayer.time = t; // fallback
            }
        }
        else
        {
            videoPlayer.time = t;
        }
    }

    // ===== Events =====
    void OnVideoPrepared(VideoPlayer vp)
    {
        // giữ tốc độ/âm lượng đồng bộ
        ApplyPlaybackSpeed();
        ApplyVolume();
    }

    void OnVideoError(VideoPlayer vp, string msg)
    {
        Debug.LogError("[VideoPlayer] Error: " + msg);
    }

    // ===== Simple HUD =====
    void OnGUI()
    {
        if (!showHUD || !videoPlayer) return;

        string qLabel = (_currentQualityIndex >= 0 && qualities != null && _currentQualityIndex < qualities.Length)
            ? qualities[_currentQualityIndex].label : "—";

        string playState = videoPlayer.isPlaying ? "PLAY" : (videoPlayer.isPrepared ? "PAUSE" : "PREPARE...");
        string t = videoPlayer.isPrepared ? FormatTime(videoPlayer.time) + " / " + FormatTime(videoPlayer.length) : "--:-- / --:--";

        var rect = new Rect(hudOffset.x, hudOffset.y, 420, 90);
        GUI.Box(rect, "");
        GUILayout.BeginArea(rect);
        GUILayout.Label($"State: {playState}");
        GUILayout.Label($"Time : {t}");
        GUILayout.Label($"Speed: {playbackSpeed:0.00}x   Vol: {(_muted ? 0f : volume):0.00}   Quality: {qLabel}");
        GUILayout.Label("Space=Play/Pause  ←/→=±5s  ↑/↓=Vol  ,/.=Speed  Q/1..9=Quality  M=Mute");
        GUILayout.EndArea();
    }

    string FormatTime(double seconds)
    {
        if (double.IsNaN(seconds) || double.IsInfinity(seconds)) return "--:--";
        int s = Mathf.Max(0, (int)Math.Round(seconds));
        int h = s / 3600; s %= 3600;
        int m = s / 60; s %= 60;
        return h > 0 ? $"{h:00}:{m:00}:{s:00}" : $"{m:00}:{s:00}";
    }
}
 