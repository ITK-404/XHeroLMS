using System;
using System.Collections;
using System.Globalization;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR_WIN
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
#endif

public class XHeroWebViewTexturePlayer : MonoBehaviour
{
    public const string MethodName = "XHero WebView Texture Bridge";

    [SerializeField] private int textureWidth = 1920;
    [SerializeField] private int textureHeight = 1080;
    [SerializeField] private int frameRate = 30;
    [SerializeField] private float webVideoPollInterval = 0.25f;

    private Coroutine playRoutine;
    private Coroutine statePollRoutine;
    private RawImage targetRawImage;
    private Texture previousTexture;
    private Rect previousUvRect;
    private bool targetUvRectOverridden;
    private Texture2D texture;
    private Action firstFrameReady;
    private Action<string> loadFailed;
    private WaitForSeconds frameWait;
    private byte[] lastFrameBuffer;
    private float activeFrameRate;
    private float lastLoggedEstimatedFrameRate;
    private bool framePacingOverridden;
    private int previousTargetFrameRate;
    private int previousVSyncCount;
    private bool applicationPaused;
    private bool applicationFocused = true;
    private bool lifecycleSuspended;
    private bool resumePlaybackAfterLifecycle;
    private static XHeroWebViewTexturePlayer activeInstance;

#if UNITY_ANDROID && !UNITY_EDITOR
    private const string AndroidBridgeClassName = "com.xherozone.webviewvideo.XHeroNativeTexturePlayer";
    private const int AndroidRenderEventInitialize = 1;
    private const int AndroidRenderEventUpdate = 2;
    private const int AndroidRenderEventRelease = 3;
    private AndroidJavaClass androidBridge;
    private IntPtr androidRenderEventFunc;
#elif UNITY_EDITOR_WIN
    private EditorChromeCaptureBridge editorBridge;
#endif

    public bool IsActive { get; private set; }
    public bool IsReady { get; private set; }
    public bool IsPlaying { get; private set; }
    public double CurrentTime { get; private set; }
    public double Duration { get; private set; }
    public double EstimatedFrameRate { get; private set; }
    public int SourceVideoWidth { get; private set; }
    public int SourceVideoHeight { get; private set; }

    public static bool IsSupportedRuntime
    {
        get
        {
#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR) || UNITY_EDITOR_WIN
            return true;
#else
            return false;
#endif
        }
    }

    public static bool IsSupportedIframeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out Uri uri))
            return false;

        return string.Equals(uri.Host, "iframe.mediadelivery.net", StringComparison.OrdinalIgnoreCase) &&
               uri.AbsolutePath.IndexOf("/embed/", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    public void Play(
        string iframeUrl,
        RawImage rawImage,
        Action onFirstFrameReady,
        Action<string> onLoadFailed)
    {
        if (activeInstance != null && activeInstance != this)
            activeInstance.Stop();

        Stop();

        activeInstance = this;
        targetRawImage = rawImage;
        firstFrameReady = onFirstFrameReady;
        loadFailed = onLoadFailed;
        playRoutine = StartCoroutine(PlayRoutine(iframeUrl));
    }

    public static void StopActiveInstance()
    {
        if (activeInstance != null)
            activeInstance.Stop();
    }

    public void Stop()
    {
        bool hadWork = IsActive || playRoutine != null || statePollRoutine != null || texture != null;

        if (playRoutine != null)
        {
            StopCoroutine(playRoutine);
            playRoutine = null;
        }

        if (statePollRoutine != null)
        {
            StopCoroutine(statePollRoutine);
            statePollRoutine = null;
        }

        if (targetRawImage != null)
        {
            if (targetRawImage.texture == texture)
                targetRawImage.texture = previousTexture;

            RestoreTargetUvRect(targetRawImage);
        }

        PlatformStop();
        RestoreRuntimeFramePacing();

        if (texture != null)
        {
            Destroy(texture);
            texture = null;
        }

        targetRawImage = null;
        previousTexture = null;
        firstFrameReady = null;
        loadFailed = null;
        lastFrameBuffer = null;
        IsActive = false;
        IsReady = false;
        IsPlaying = false;
        CurrentTime = 0;
        Duration = 0;
        EstimatedFrameRate = 0;
        SourceVideoWidth = 0;
        SourceVideoHeight = 0;
        activeFrameRate = 0;
        lastLoggedEstimatedFrameRate = 0;
        lifecycleSuspended = false;
        resumePlaybackAfterLifecycle = false;

        if (activeInstance == this)
            activeInstance = null;

        if (hadWork)
            Debug.Log($"[{MethodName}] Stop native texture player.");
    }

    public void SetTargetRawImage(RawImage rawImage)
    {
        if (rawImage == null || targetRawImage == rawImage)
            return;

        if (targetRawImage != null && targetRawImage.texture == texture)
        {
            targetRawImage.texture = previousTexture;
            RestoreTargetUvRect(targetRawImage);
        }

        targetRawImage = rawImage;
        previousTexture = rawImage.texture;
        previousUvRect = rawImage.uvRect;
        targetUvRectOverridden = false;

        if (texture != null && IsReady)
            ApplyBridgeTextureToTarget();
    }

    public void PlayWebVideo()
    {
        if (!IsActive)
            return;

        PlatformPlay();
        IsPlaying = true;
        Debug.Log($"[{MethodName}] Play requested.");
    }

    public void PauseWebVideo()
    {
        if (!IsActive)
            return;

        PlatformPause();
        IsPlaying = false;
        Debug.Log($"[{MethodName}] Pause requested.");
    }

    public void TogglePlayPause()
    {
        if (!IsActive)
            return;

        if (IsPlaying) PauseWebVideo();
        else PlayWebVideo();
    }

    public void Seek(double time)
    {
        if (!IsActive)
            return;

        PlatformSeek(time);
        CurrentTime = Math.Max(0, time);
    }

    public void SetVolume(float volume)
    {
        if (!IsActive)
            return;

        PlatformSetVolume(Mathf.Clamp01(volume));
    }

    private void OnApplicationPause(bool pause)
    {
        applicationPaused = pause;
        UpdateLifecyclePlaybackState(pause ? "pause" : "resume");
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        applicationFocused = hasFocus;
        UpdateLifecyclePlaybackState(hasFocus ? "focus" : "lost-focus");
    }

    private void OnApplicationQuit()
    {
        Stop();
    }

    private void OnDisable()
    {
        Stop();
    }

    private void OnDestroy()
    {
        Stop();
    }

    private void UpdateLifecyclePlaybackState(string reason)
    {
        bool shouldSuspend = ShouldSuspendForLifecycle();
        if (shouldSuspend)
        {
            if (!IsActive || lifecycleSuspended)
                return;

            resumePlaybackAfterLifecycle = IsPlaying;
            lifecycleSuspended = true;
            IsPlaying = false;
            PlatformLifecyclePause();
            Debug.Log($"[{MethodName}] Lifecycle pause native video. reason={reason} resume={resumePlaybackAfterLifecycle}");
            return;
        }

        if (!lifecycleSuspended)
            return;

        bool shouldResume = resumePlaybackAfterLifecycle;
        lifecycleSuspended = false;
        resumePlaybackAfterLifecycle = false;

        if (!CanUseLifecycleTarget())
        {
            Debug.Log($"[{MethodName}] Lifecycle resume skipped because target RawImage is hidden. reason={reason}");
            Stop();
            return;
        }

        PlatformLifecycleResume(shouldResume);
        IsPlaying = shouldResume;
        Debug.Log($"[{MethodName}] Lifecycle resume native video. reason={reason} resume={shouldResume}");
    }

    private bool ShouldSuspendForLifecycle()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return applicationPaused;
#else
        return applicationPaused || !applicationFocused;
#endif
    }

    private bool CanUseLifecycleTarget()
    {
        return targetRawImage != null &&
               targetRawImage.isActiveAndEnabled &&
               targetRawImage.gameObject.activeInHierarchy;
    }

    private IEnumerator PlayRoutine(string iframeUrl)
    {
        if (!IsSupportedRuntime)
        {
            Fail("Native WebView texture bridge is not supported on this runtime.");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(iframeUrl) || targetRawImage == null)
        {
            Fail("Missing iframe URL or target RawImage.");
            yield break;
        }

        textureWidth = Mathf.Clamp(textureWidth, 320, 1920);
        textureHeight = Mathf.Clamp(textureHeight, 180, 1080);
        frameRate = Mathf.Clamp(frameRate, 5, 60);
        SetCaptureFrameRate(frameRate);

        IsActive = true;
        IsReady = false;
        IsPlaying = false;
        CurrentTime = 0;
        Duration = 0;
        EstimatedFrameRate = 0;
        SourceVideoWidth = 0;
        SourceVideoHeight = 0;
        previousTexture = targetRawImage.texture;
        previousUvRect = targetRawImage.uvRect;
        targetUvRectOverridden = false;

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!PrepareAndroidNativeTexture(textureWidth, textureHeight))
        {
            Fail("Native texture renderer failed to initialize.");
            yield break;
        }

        yield return new WaitForEndOfFrame();
        yield return null;

        IntPtr nativeTexturePtr = GetAndroidNativeTexturePtr();
        if (nativeTexturePtr == IntPtr.Zero)
        {
            Fail("Native texture pointer is null.");
            yield break;
        }

        texture = Texture2D.CreateExternalTexture(textureWidth, textureHeight, TextureFormat.RGBA32, false, false, nativeTexturePtr);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        ApplyRuntimeFramePacing();

        if (!PlatformStart(iframeUrl, textureWidth, textureHeight, frameRate))
        {
            Fail("Native texture player failed to start.");
            yield break;
        }

        Debug.Log($"[{MethodName}] Resolve iframe and play by native texture player: {iframeUrl}");

        statePollRoutine = StartCoroutine(PollVideoState());

        WaitForEndOfFrame endOfFrame = new WaitForEndOfFrame();

        while (IsActive)
        {
            yield return endOfFrame;

            UpdateAndroidNativeTexture();
            ParseState(PlatformGetState());

            if (!IsReady && HasAndroidNativeFrame() && HasPlayableVideoFrame())
            {
                IsReady = true;
                ApplyBridgeTextureToTarget();
                firstFrameReady?.Invoke();
                firstFrameReady = null;
                PlayWebVideo();
                SetVolume(1f);
                Debug.Log($"[{MethodName}] First native player texture frame READY | time={CurrentTime:F2}/{Duration:F2} playing={IsPlaying}");
            }

            string error = PlatformGetLastError();
            if (!string.IsNullOrWhiteSpace(error))
            {
                Fail(error);
                yield break;
            }

            yield return null;
        }
#else
        texture = new Texture2D(textureWidth, textureHeight, GetTextureFormat(), false, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        ApplyBridgeTextureToTarget();
        ApplyRuntimeFramePacing();

        if (!PlatformStart(iframeUrl, textureWidth, textureHeight, frameRate))
        {
            Fail("Native WebView texture bridge failed to start.");
            yield break;
        }

        Debug.Log($"[{MethodName}] Load iframe into native WebView texture bridge: {iframeUrl}");

        statePollRoutine = StartCoroutine(PollVideoState());

        while (IsActive)
        {
            byte[] frame = PlatformConsumeFrame();
            if (frame != null && frame.Length > 0)
            {
                ApplyFrame(frame);
                ParseState(PlatformGetState());

                if (!IsReady && HasPlayableVideoFrame())
                {
                    IsReady = true;
                    IsPlaying = true;
                    firstFrameReady?.Invoke();
                    firstFrameReady = null;
                    PlayWebVideo();
                    SetVolume(1f);
                    Debug.Log($"[{MethodName}] First native WebView texture frame READY | time={CurrentTime:F2}/{Duration:F2} playing={IsPlaying}");
                }
            }

            string error = PlatformGetLastError();
            if (!string.IsNullOrWhiteSpace(error))
            {
                Fail(error);
                yield break;
            }

            yield return frameWait;
        }
#endif
    }

    private IEnumerator PollVideoState()
    {
        var wait = new WaitForSeconds(webVideoPollInterval);

        while (IsActive)
        {
            PlatformRequestState();
            ParseState(PlatformGetState());
            yield return wait;
        }
    }

    private void ApplyFrame(byte[] frame)
    {
        if (texture == null)
            return;

#if (UNITY_IOS && !UNITY_EDITOR) || UNITY_EDITOR_WIN
        texture.LoadImage(frame, false);
#else
        int expectedLength = textureWidth * textureHeight * 4;
        if (frame.Length != expectedLength)
            return;

        texture.LoadRawTextureData(frame);
        texture.Apply(false);
#endif
    }

    private void ParseState(string state)
    {
        if (string.IsNullOrWhiteSpace(state))
            return;

        string[] parts = state.Trim('"').Split('|');
        if (parts.Length < 3)
            return;

        if (double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double time))
            CurrentTime = time;

        if (double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double duration))
            Duration = duration;

        IsPlaying = parts[2] == "1";

        if (parts.Length >= 4 &&
            double.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out double fps) &&
            fps >= 10.0 && fps <= 60.0)
        {
            EstimatedFrameRate = NormalizeVideoFrameRate((float)fps);
            SetCaptureFrameRate((float)EstimatedFrameRate);
        }

        if (parts.Length >= 6)
        {
            int.TryParse(parts[4], NumberStyles.Any, CultureInfo.InvariantCulture, out int videoWidth);
            int.TryParse(parts[5], NumberStyles.Any, CultureInfo.InvariantCulture, out int videoHeight);

            if (videoWidth > 0 && videoHeight > 0 &&
                (SourceVideoWidth != videoWidth || SourceVideoHeight != videoHeight))
            {
                SourceVideoWidth = videoWidth;
                SourceVideoHeight = videoHeight;
                Debug.Log($"[{MethodName}] Source video info {SourceVideoWidth}x{SourceVideoHeight} @~{EstimatedFrameRate:F1}fps | texture={textureWidth}x{textureHeight}");
            }
        }
    }

    private bool HasPlayableVideoFrame()
    {
        return Duration > 0.0001 && (CurrentTime > 0.05 || IsPlaying);
    }

    private void Fail(string reason)
    {
        Debug.LogWarning($"[{MethodName}] {reason}");
        Action<string> failed = loadFailed;
        Stop();
        failed?.Invoke(reason);
    }

    private static TextureFormat GetTextureFormat()
    {
        return TextureFormat.RGBA32;
    }

    private void ApplyBridgeTextureToTarget()
    {
        if (targetRawImage == null || texture == null)
            return;

        targetRawImage.texture = texture;
    }

    private void RestoreTargetUvRect(RawImage rawImage)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (targetUvRectOverridden && rawImage != null)
            rawImage.uvRect = previousUvRect;

        targetUvRectOverridden = false;
#endif
    }

    private void SetCaptureFrameRate(float fps)
    {
        float clamped = NormalizeVideoFrameRate(fps);
        if (frameWait != null && Mathf.Abs(activeFrameRate - clamped) < 0.5f)
            return;

        activeFrameRate = clamped;
        frameWait = new WaitForSeconds(1f / activeFrameRate);

        if (EstimatedFrameRate > 0.0 && Mathf.Abs(lastLoggedEstimatedFrameRate - activeFrameRate) >= 1f)
        {
            lastLoggedEstimatedFrameRate = activeFrameRate;
            Debug.Log($"[{MethodName}] Source video fps detected ~{activeFrameRate:F1}; texture capture synced.");
        }
    }

    private static float NormalizeVideoFrameRate(float fps)
    {
        if (fps >= 27f && fps <= 33f) return 30f;
        if (fps >= 58f && fps <= 62f) return 60f;
        if (fps >= 23f && fps <= 25.5f) return 24f;
        if (fps >= 47f && fps <= 49.5f) return 48f;
        if (fps >= 49.5f && fps <= 51.5f) return 50f;
        return Mathf.Clamp(Mathf.Round(fps), 5f, 60f);
    }

    private void ApplyRuntimeFramePacing()
    {
        if (!framePacingOverridden)
        {
            previousTargetFrameRate = Application.targetFrameRate;
            previousVSyncCount = QualitySettings.vSyncCount;
            framePacingOverridden = true;
        }

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = Mathf.Max(60, Application.targetFrameRate);
        Debug.Log($"[{MethodName}] Runtime frame pacing boosted for WebView texture. targetFrameRate={Application.targetFrameRate}");
    }

    private void RestoreRuntimeFramePacing()
    {
        if (!framePacingOverridden)
            return;

        QualitySettings.vSyncCount = previousVSyncCount;
        Application.targetFrameRate = previousTargetFrameRate;
        framePacingOverridden = false;
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private bool PrepareAndroidNativeTexture(int width, int height)
    {
        try
        {
            XHeroNative_SetSize(width, height);
            androidRenderEventFunc = XHeroNative_GetRenderEventFunc();
            if (androidRenderEventFunc == IntPtr.Zero)
                return false;

            GL.IssuePluginEvent(androidRenderEventFunc, AndroidRenderEventInitialize);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[{MethodName}] Android native texture init failed: {e.Message}");
            return false;
        }
    }

    private IntPtr GetAndroidNativeTexturePtr()
    {
        try { return XHeroNative_GetTexturePtr(); }
        catch { return IntPtr.Zero; }
    }

    private void UpdateAndroidNativeTexture()
    {
        if (androidRenderEventFunc != IntPtr.Zero)
            GL.IssuePluginEvent(androidRenderEventFunc, AndroidRenderEventUpdate);
    }

    private bool HasAndroidNativeFrame()
    {
        try { return XHeroNative_HasFrame() != 0; }
        catch { return false; }
    }

    private void ReleaseAndroidNativeTexture()
    {
        if (androidRenderEventFunc != IntPtr.Zero)
        {
            try { GL.IssuePluginEvent(androidRenderEventFunc, AndroidRenderEventRelease); } catch { }
            androidRenderEventFunc = IntPtr.Zero;
        }
    }
#endif

    private bool PlatformStart(string url, int width, int height, int fps)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            androidBridge = new AndroidJavaClass(AndroidBridgeClassName);
            return androidBridge.CallStatic<bool>("start", url, width, height, fps);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[{MethodName}] Android bridge start failed: {e.Message}");
            return false;
        }
#elif UNITY_IOS && !UNITY_EDITOR
        return XHeroWV_Start(url, width, height, fps);
#elif UNITY_EDITOR_WIN
        editorBridge = new EditorChromeCaptureBridge();
        return editorBridge.Start(url, width, height, fps);
#else
        return false;
#endif
    }

    private void PlatformStop()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try { androidBridge?.CallStatic("stop"); } catch { }
        androidBridge?.Dispose();
        androidBridge = null;
        ReleaseAndroidNativeTexture();
#elif UNITY_IOS && !UNITY_EDITOR
        XHeroWV_Stop();
#elif UNITY_EDITOR_WIN
        editorBridge?.Stop();
        editorBridge = null;
#endif
    }

    private void PlatformPlay()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try { androidBridge?.CallStatic("play"); } catch { }
#else
        PlatformEvaluate("window.xheroUserPaused=false; var v=document.querySelector('video'); if(v){v.play().catch(function(){}); '1';} else {'0';}");
#endif
    }

    private void PlatformPause()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try { androidBridge?.CallStatic("pause"); } catch { }
#else
        PlatformEvaluate("window.xheroUserPaused=true; var v=document.querySelector('video'); if(v){v.pause(); '1';} else {'0';}");
#endif
    }

    private void PlatformLifecyclePause()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try { androidBridge?.CallStatic("lifecyclePause"); } catch { }
#else
        PlatformPause();
        PlatformSetVolume(0f);
#endif
    }

    private void PlatformLifecycleResume(bool shouldResume)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try { androidBridge?.CallStatic("lifecycleResume", shouldResume); } catch { }
#else
        if (shouldResume)
        {
            PlatformSetVolume(1f);
            PlatformPlay();
        }
#endif
    }

    private void PlatformSeek(double time)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try { androidBridge?.CallStatic("seek", time); } catch { }
#else
        string value = time.ToString(CultureInfo.InvariantCulture);
        PlatformEvaluate($"var v=document.querySelector('video'); if(v){{v.currentTime={value}; '1';}} else {{'0';}}");
#endif
    }

    private void PlatformSetVolume(float volume)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try { androidBridge?.CallStatic("setVolume", volume); } catch { }
#else
        string value = Mathf.Clamp01(volume).ToString(CultureInfo.InvariantCulture);
        PlatformEvaluate($"var v=document.querySelector('video'); if(v){{v.volume={value}; v.muted={value}<=0; '1';}} else {{'0';}}");
#endif
    }

    private byte[] PlatformConsumeFrame()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            sbyte[] signedFrame = androidBridge?.CallStatic<sbyte[]>("consumeFrame");
            if (signedFrame == null || signedFrame.Length == 0)
                return null;

            if (lastFrameBuffer == null || lastFrameBuffer.Length != signedFrame.Length)
                lastFrameBuffer = new byte[signedFrame.Length];

            Buffer.BlockCopy(signedFrame, 0, lastFrameBuffer, 0, signedFrame.Length);
            return lastFrameBuffer;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[{MethodName}] Android consume frame failed: {e.Message}");
            return null;
        }
#elif UNITY_IOS && !UNITY_EDITOR
        int length = XHeroWV_GetFrameLength();
        if (length <= 0)
            return null;

        IntPtr ptr = XHeroWV_CopyFrame();
        if (ptr == IntPtr.Zero)
            return null;

        if (lastFrameBuffer == null || lastFrameBuffer.Length != length)
            lastFrameBuffer = new byte[length];

        Marshal.Copy(ptr, lastFrameBuffer, 0, length);
        XHeroWV_ReleaseFrame(ptr);
        return lastFrameBuffer;
#elif UNITY_EDITOR_WIN
        return editorBridge?.ConsumeFrame();
#else
        return null;
#endif
    }

    private void PlatformEvaluate(string script)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try { androidBridge?.CallStatic("evaluate", script); } catch { }
#elif UNITY_IOS && !UNITY_EDITOR
        XHeroWV_Evaluate(script);
#elif UNITY_EDITOR_WIN
        editorBridge?.Evaluate(script);
#endif
    }

    private void PlatformRequestState()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try { androidBridge?.CallStatic("requestState"); } catch { }
#elif UNITY_IOS && !UNITY_EDITOR
        XHeroWV_RequestState();
#elif UNITY_EDITOR_WIN
        editorBridge?.RequestState();
#endif
    }

    private string PlatformGetState()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try { return androidBridge?.CallStatic<string>("getState"); }
        catch { return null; }
#elif UNITY_IOS && !UNITY_EDITOR
        return PtrToString(XHeroWV_GetState());
#elif UNITY_EDITOR_WIN
        return editorBridge?.State;
#else
        return null;
#endif
    }

    private string PlatformGetLastError()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try { return androidBridge?.CallStatic<string>("getLastError"); }
        catch { return null; }
#elif UNITY_IOS && !UNITY_EDITOR
        return PtrToString(XHeroWV_GetLastError());
#elif UNITY_EDITOR_WIN
        return editorBridge?.LastError;
#else
        return null;
#endif
    }

    private static string PtrToString(IntPtr ptr)
    {
        return ptr == IntPtr.Zero ? null : Marshal.PtrToStringAnsi(ptr);
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    [DllImport("xheronativetexture")] private static extern void XHeroNative_SetSize(int width, int height);
    [DllImport("xheronativetexture")] private static extern IntPtr XHeroNative_GetRenderEventFunc();
    [DllImport("xheronativetexture")] private static extern IntPtr XHeroNative_GetTexturePtr();
    [DllImport("xheronativetexture")] private static extern int XHeroNative_HasFrame();
#endif

#if UNITY_IOS && !UNITY_EDITOR
    [DllImport("__Internal")] private static extern bool XHeroWV_Start(string url, int width, int height, int fps);
    [DllImport("__Internal")] private static extern void XHeroWV_Stop();
    [DllImport("__Internal")] private static extern IntPtr XHeroWV_CopyFrame();
    [DllImport("__Internal")] private static extern int XHeroWV_GetFrameLength();
    [DllImport("__Internal")] private static extern void XHeroWV_ReleaseFrame(IntPtr frame);
    [DllImport("__Internal")] private static extern void XHeroWV_Evaluate(string script);
    [DllImport("__Internal")] private static extern void XHeroWV_RequestState();
    [DllImport("__Internal")] private static extern IntPtr XHeroWV_GetState();
    [DllImport("__Internal")] private static extern IntPtr XHeroWV_GetLastError();
#endif

#if UNITY_EDITOR_WIN
    private sealed class EditorChromeCaptureBridge
    {
        private readonly ConcurrentQueue<byte[]> frames = new ConcurrentQueue<byte[]>();
        private readonly ConcurrentQueue<string> pendingScripts = new ConcurrentQueue<string>();

        private System.Diagnostics.Process chromeProcess;
        private ClientWebSocket webSocket;
        private CancellationTokenSource cts;
        private Task worker;
        private string tempUserDataDir;
        private int commandId;

        public string State { get; private set; } = "0|0|0";
        public string LastError { get; private set; } = "";

        public bool Start(string url, int width, int height, int fps)
        {
            string chromePath = FindChromeExecutable();
            if (string.IsNullOrWhiteSpace(chromePath))
            {
                LastError = "Windows Editor WebView bridge requires Chrome/Edge/Chromium executable.";
                return false;
            }

            try
            {
                int port = GetFreeTcpPort();
                tempUserDataDir = Path.Combine(Path.GetTempPath(), "xhero-webview-editor-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempUserDataDir);

                var psi = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = chromePath,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    Arguments =
                        $"--remote-debugging-port={port} " +
                        $"--user-data-dir=\"{tempUserDataDir}\" " +
                        "--no-first-run --disable-extensions --disable-background-networking " +
                        "--autoplay-policy=no-user-gesture-required --hide-scrollbars " +
                        $"--window-size={width},{height} --window-position=-32000,-32000 " +
                        $"\"{url}\""
                };

                chromeProcess = System.Diagnostics.Process.Start(psi);
                cts = new CancellationTokenSource();
                worker = Task.Run(() => RunCaptureLoop(port, url, width, height, fps, cts.Token));
                return true;
            }
            catch (Exception e)
            {
                LastError = "Windows Editor WebView bridge start failed: " + e.Message;
                Stop();
                return false;
            }
        }

        public void Stop()
        {
            try { cts?.Cancel(); } catch { }

            try
            {
                webSocket?.Abort();
                webSocket?.Dispose();
            }
            catch { }

            try
            {
                if (chromeProcess != null && !chromeProcess.HasExited)
                    chromeProcess.Kill();
            }
            catch { }

            try { chromeProcess?.Dispose(); } catch { }

            if (!string.IsNullOrWhiteSpace(tempUserDataDir))
            {
                try { Directory.Delete(tempUserDataDir, true); } catch { }
            }

            chromeProcess = null;
            webSocket = null;
            cts = null;
            worker = null;
            tempUserDataDir = null;
            State = "0|0|0";
        }

        public byte[] ConsumeFrame()
        {
            while (frames.Count > 1)
                frames.TryDequeue(out _);

            return frames.TryDequeue(out byte[] frame) ? frame : null;
        }

        public void Evaluate(string script)
        {
            if (!string.IsNullOrWhiteSpace(script))
                pendingScripts.Enqueue(script);
        }

        public void RequestState()
        {
        }

        private async Task RunCaptureLoop(int port, string url, int width, int height, int fps, CancellationToken token)
        {
            try
            {
                string wsUrl = await WaitForPageWebSocketUrl(port, token);
                if (string.IsNullOrWhiteSpace(wsUrl))
                {
                    LastError = "Windows Editor WebView bridge cannot find Chrome DevTools page target.";
                    return;
                }

                webSocket = new ClientWebSocket();
                await webSocket.ConnectAsync(new Uri(wsUrl), token);

                await SendCommand("Page.enable", null, token);
                await SendCommand("Runtime.enable", null, token);
                await SendCommand(
                    "Emulation.setDeviceMetricsOverride",
                    $"{{\"width\":{width},\"height\":{height},\"deviceScaleFactor\":1,\"mobile\":false}}",
                    token);
                await SendCommand("Page.navigate", $"{{\"url\":\"{JsonEscape(url)}\"}}", token);

                await Task.Delay(1500, token);
                await InjectVideoPatch(width, height, token);

                int delayMs = Math.Max(16, Mathf.RoundToInt(1000f / Mathf.Max(5, Mathf.Min(60, fps))));
                long lastPatch = 0;

                while (!token.IsCancellationRequested)
                {
                    long now = CurrentMilliseconds();
                    if (now - lastPatch > 1000)
                    {
                        await InjectVideoPatch(width, height, token);
                        lastPatch = now;
                    }

                    while (pendingScripts.TryDequeue(out string script))
                        await EvaluateScript(script, token);

                    string state = await EvaluateScript(
                        "var v=document.querySelector('video'); v ? [(v.currentTime||0),(isFinite(v.duration)?v.duration:0),(!v.paused?1:0),(window.xheroEstimatedFps||0),(v.videoWidth||0),(v.videoHeight||0)].join('|') : '0|0|0|0|0|0';",
                        token);
                    if (!string.IsNullOrWhiteSpace(state))
                    {
                        State = state;
                        string[] parts = state.Split('|');
                        if (parts.Length >= 4 &&
                            float.TryParse(parts[3], NumberStyles.Any, CultureInfo.InvariantCulture, out float estimatedFps) &&
                            estimatedFps >= 10f && estimatedFps <= 60f)
                        {
                            delayMs = Math.Max(16, Mathf.RoundToInt(1000f / NormalizeVideoFrameRate(estimatedFps)));
                        }
                    }

                    string response = await SendCommand(
                        "Page.captureScreenshot",
                        "{\"format\":\"jpeg\",\"quality\":92,\"fromSurface\":true}",
                        token);

                    string base64 = ExtractJsonString(response, "data");
                    if (!string.IsNullOrEmpty(base64))
                    {
                        try
                        {
                            frames.Enqueue(Convert.FromBase64String(base64));
                        }
                        catch { }
                    }

                    await Task.Delay(delayMs, token);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception e)
            {
                LastError = "Windows Editor WebView bridge failed: " + e.Message;
            }
        }

        private async Task InjectVideoPatch(int width, int height, CancellationToken token)
        {
            string script =
                "(function(){" +
                "if(window.xheroVideoPatchInstalled!==true){window.xheroVideoPatchInstalled=true;window.xheroVideoCanvasLoop=false;window.xheroUserPaused=false;window.xheroAutoplayStarted=false;window.xheroEstimatedFps=0;window.xheroFrameCount=0;window.xheroFrameTime=0;}" +
                "var style=document.getElementById('xhero-video-css');" +
                "if(!style){style=document.createElement('style');style.id='xhero-video-css';document.head.appendChild(style);}" +
                "style.textContent='html,body{margin:0!important;padding:0!important;overflow:hidden!important;background:#000!important;}'+ " +
                "'video{position:absolute!important;inset:0!important;width:100%!important;height:100%!important;object-fit:contain!important;background:#000!important;opacity:.01!important;pointer-events:none!important;}'+ " +
                "'#xhero-video-canvas{position:fixed!important;left:0!important;top:0!important;width:100vw!important;height:100vh!important;background:#000!important;z-index:2147483647!important;pointer-events:none!important;}'+ " +
                "'.plyr__controls,.plyr__control,.plyr__menu,.plyr__progress,.plyr__volume,.plyr__poster,.plyr__control--overlaid{display:none!important;}'+ " +
                "'video::-webkit-media-controls{display:none!important;}';" +
                "var c=document.getElementById('xhero-video-canvas');" +
                "if(!c){c=document.createElement('canvas');c.id='xhero-video-canvas';(document.body||document.documentElement).appendChild(c);}" +
                $"function rz(){{var w={width};var h={height};if(c.width!==w||c.height!==h){{c.width=w;c.height=h;}}}}" +
                "rz();" +
                "var v=document.querySelector('video');" +
                "if(v){v.controls=false;v.playsInline=true;v.autoplay=true;v.muted=false;if(window.xheroUserPaused!==true&&window.xheroAutoplayStarted!==true){window.xheroAutoplayStarted=true;v.play().catch(function(){});}}" +
                "if(!window.xheroVideoCanvasLoop){window.xheroVideoCanvasLoop=true;(function loop(){try{rz();var cv=document.getElementById('xhero-video-canvas');var vv=document.querySelector('video');if(cv&&vv&&vv.readyState>=2){var now=(window.performance&&performance.now)?performance.now():Date.now();var q=vv.getVideoPlaybackQuality?vv.getVideoPlaybackQuality():null;if(q&&q.totalVideoFrames>0){if(!window.xheroFrameTime){window.xheroFrameTime=now;window.xheroFrameCount=q.totalVideoFrames;}else if(now-window.xheroFrameTime>=1000){var f=(q.totalVideoFrames-window.xheroFrameCount)*1000/(now-window.xheroFrameTime);if(f>=10&&f<=60){window.xheroEstimatedFps=f;}window.xheroFrameTime=now;window.xheroFrameCount=q.totalVideoFrames;}}var ctx=cv.getContext('2d',{alpha:false});ctx.imageSmoothingEnabled=true;ctx.imageSmoothingQuality='high';ctx.fillStyle='#000';ctx.fillRect(0,0,cv.width,cv.height);var vw=vv.videoWidth||cv.width;var vh=vv.videoHeight||cv.height;var s=Math.min(cv.width/vw,cv.height/vh);var dw=vw*s;var dh=vh*s;var dx=(cv.width-dw)/2;var dy=(cv.height-dh)/2;ctx.drawImage(vv,dx,dy,dw,dh);}}catch(e){}requestAnimationFrame(loop);})();}" +
                "})()";

            await EvaluateScript(script, token);
        }

        private async Task<string> EvaluateScript(string script, CancellationToken token)
        {
            string response = await SendCommand(
                "Runtime.evaluate",
                $"{{\"expression\":\"{JsonEscape(script)}\",\"returnByValue\":true,\"awaitPromise\":false}}",
                token);

            return ExtractJsonString(response, "value");
        }

        private async Task<string> SendCommand(string method, string parameters, CancellationToken token)
        {
            int id = Interlocked.Increment(ref commandId);
            string json = parameters == null
                ? $"{{\"id\":{id},\"method\":\"{method}\"}}"
                : $"{{\"id\":{id},\"method\":\"{method}\",\"params\":{parameters}}}";

            byte[] bytes = Encoding.UTF8.GetBytes(json);
            await webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token);

            while (!token.IsCancellationRequested)
            {
                string response = await ReceiveTextMessage(token);
                if (Regex.IsMatch(response, "\"id\"\\s*:\\s*" + id + "(\\D|$)"))
                    return response;
            }

            return null;
        }

        private async Task<string> ReceiveTextMessage(CancellationToken token)
        {
            byte[] buffer = new byte[1024 * 1024];
            using (var stream = new MemoryStream())
            {
                WebSocketReceiveResult result;
                do
                {
                    result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                    if (result.MessageType == WebSocketMessageType.Close)
                        throw new IOException("Chrome DevTools websocket closed.");

                    stream.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);

                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        private static async Task<string> WaitForPageWebSocketUrl(int port, CancellationToken token)
        {
            string endpoint = "http://127.0.0.1:" + port + "/json";
            for (int i = 0; i < 80 && !token.IsCancellationRequested; i++)
            {
                try
                {
                    string json = await ReadHttpText(endpoint);
                    foreach (Match item in Regex.Matches(json, "\\{[^\\{\\}]*\"type\"\\s*:\\s*\"page\"[^\\{\\}]*\\}"))
                    {
                        string ws = ExtractJsonString(item.Value, "webSocketDebuggerUrl");
                        if (!string.IsNullOrWhiteSpace(ws))
                            return ws;
                    }

                    string fallback = ExtractJsonString(json, "webSocketDebuggerUrl");
                    if (!string.IsNullOrWhiteSpace(fallback))
                        return fallback;
                }
                catch { }

                await Task.Delay(100, token);
            }

            return null;
        }

        private static async Task<string> ReadHttpText(string url)
        {
            var request = WebRequest.CreateHttp(url);
            request.Timeout = 1000;
            using (var response = await request.GetResponseAsync())
            using (var stream = response.GetResponseStream())
            using (var reader = new StreamReader(stream))
            {
                return await reader.ReadToEndAsync();
            }
        }

        private static int GetFreeTcpPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            int port = ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
            return port;
        }

        private static long CurrentMilliseconds()
        {
            return DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond;
        }

        private static string FindChromeExecutable()
        {
            string envPath = Environment.GetEnvironmentVariable("XHERO_CHROME_PATH");
            if (File.Exists(envPath))
                return envPath;

            string[] directCandidates =
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Google", "Chrome", "Application", "chrome.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft", "Edge", "Application", "msedge.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "Edge", "Application", "msedge.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Microsoft", "EdgeWebView", "Application", "msedgewebview2.exe")
            };

            foreach (string candidate in directCandidates)
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            string codeiumDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                ".codeium",
                "ws-browser");
            if (Directory.Exists(codeiumDir))
            {
                try
                {
                    string[] matches = Directory.GetFiles(codeiumDir, "chrome.exe", SearchOption.AllDirectories);
                    if (matches.Length > 0)
                        return matches[0];
                }
                catch { }
            }

            return null;
        }

        private static string JsonEscape(string value)
        {
            if (value == null)
                return "";

            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");
        }

        private static string ExtractJsonString(string json, string key)
        {
            if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key))
                return null;

            Match match = Regex.Match(json, "\"" + Regex.Escape(key) + "\"\\s*:\\s*\"((?:\\\\.|[^\"])*)\"");
            if (!match.Success)
                return null;

            return Regex.Unescape(match.Groups[1].Value);
        }
    }
#endif
}
