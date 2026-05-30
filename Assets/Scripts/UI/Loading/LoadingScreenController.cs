using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
#endif

public class LoadingScreenController : MonoBehaviour
{
    [Header("UI References")]
    public Image imageScene1;
    public Image progressRing;
    public TMP_Text textLoading;
    public ParticleSystem loadingParticle;
    public Slider sliderUI;

    [Header("Loading Text Animation")]
    public float dotSpeed = 0.35f;
    public string baseText = "Đang tải";

    [Header("Fast Loading Settings")]
    [Tooltip("Tổng thời gian loading UI từ 0 đến 100 nếu scene đã preload xong.")]
    public float maxLoadingSeconds = 2f;

    [Tooltip("Nếu scene chưa ready, thanh loading giữ tối đa ở mức này thay vì lên 100 giả.")]
    [Range(0.9f, 0.999f)]
    public float waitCapProgress = 0.99f;

    [Tooltip("Thời gian tối thiểu để người dùng thấy loading screen, tránh chớp màn quá nhanh.")]
    public float minVisibleSeconds = 0.25f;

    [Tooltip("Có unload unused assets sau khi activate scene không. Bật cái này sẽ sạch RAM hơn nhưng có thể chậm thêm.")]
    public bool unloadUnusedAssetsAfterLoad = false;

    [Header("Image Cycle")]
    public float imageSwitchInterval = 1f;

    private bool _isLoading;
    private float _dotTimer;
    private int _dotCount;
    private float _currentProgress;
    private float _displayStartTime;
    private string _targetSceneName;

    private readonly List<Image> _images = new();
    private int _currentImageIndex = -1;

#if ADDRESSABLES
    private static readonly Dictionary<string, AsyncOperationHandle<SceneInstance>> _preloadedScenes = new();
    private static AsyncOperationHandle<SceneInstance>? _lastActivatedAddressableSceneHandle;
#endif

    void Awake()
    {
        if (imageScene1)
            _images.Add(imageScene1);

        foreach (var img in _images)
        {
            if (img)
                img.gameObject.SetActive(false);
        }

        if (imageScene1)
            imageScene1.gameObject.SetActive(true);

        if (progressRing)
            progressRing.fillAmount = 0f;

        if (sliderUI)
            sliderUI.value = 0f;

        SetProgress(0f);
    }

    void Start()
    {
        if (!string.IsNullOrEmpty(LoadingTransition.TargetSceneName))
        {
            _targetSceneName = LoadingTransition.TargetSceneName;
            StartCoroutine(LoadByNameRoutine(_targetSceneName));
        }
        else
        {
            Debug.LogError("[LoadingScreenController] TargetSceneName is empty.");
        }
    }

    // ============================================================
    // PUBLIC PRELOAD API
    // Gọi hàm này từ scene hiện tại TRƯỚC KHI chuyển sang loading scene.
    // Ví dụ:
    // StartCoroutine(LoadingScreenController.PreloadAddressableSceneRoutine("Scene_KyMon"));
    // ============================================================

#if ADDRESSABLES
    public static IEnumerator PreloadAddressableSceneRoutine(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[LoadingScreenController] Preload failed: sceneName is empty.");
            yield break;
        }

        if (_preloadedScenes.TryGetValue(sceneName, out var existingHandle))
        {
            if (existingHandle.IsValid())
            {
                if (!existingHandle.IsDone)
                {
                    Debug.Log($"[LoadingScreenController] Waiting existing preload: {sceneName}");

                    while (!existingHandle.IsDone)
                        yield return null;
                }

                Debug.Log($"[LoadingScreenController] Already preloaded: {sceneName}");
                yield break;
            }

            _preloadedScenes.Remove(sceneName);
        }

        Debug.Log($"[LoadingScreenController] Start preload addressable scene: {sceneName}");

        var handle = Addressables.LoadSceneAsync(
            sceneName,
            LoadSceneMode.Single,
            activateOnLoad: false
        );

        _preloadedScenes[sceneName] = handle;

        while (!handle.IsDone)
            yield return null;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            Debug.Log($"[LoadingScreenController] Preload completed: {sceneName}");
        }
        else
        {
            Debug.LogError($"[LoadingScreenController] Preload failed: {sceneName}");
            _preloadedScenes.Remove(sceneName);
        }
    }

    public static bool IsAddressableScenePreloaded(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
            return false;

        if (!_preloadedScenes.TryGetValue(sceneName, out var handle))
            return false;

        return handle.IsValid()
               && handle.IsDone
               && handle.Status == AsyncOperationStatus.Succeeded;
    }
#endif

    IEnumerator LoadByNameRoutine(string sceneName)
    {
        _isLoading = true;
        _displayStartTime = Time.unscaledTime;

        float startTime = Time.realtimeSinceStartup;
        float visualStartTime = Time.unscaledTime;

        SetProgress(0f);

        if (loadingParticle && !loadingParticle.isPlaying)
            loadingParticle.Play();

        StartCoroutine(CycleRandomImages());

        bool useAddressables = LoadingTransition.UseAddressables;
        
        Debug.Log($"[LoadingScreenController] Load start. Scene={sceneName}, UseAddressables={useAddressables}");

        var previousScene = LoadingTransition.PreviousSceneName;

        yield return SceneManager.UnloadSceneAsync(previousScene);
        
        // yield return new WaitForSecondsRealtime(5f);
        
        if (useAddressables)
        {
#if ADDRESSABLES
            yield return LoadAddressableSceneFast(sceneName, startTime, visualStartTime);
#else
            Debug.LogError("[LoadingScreenController] ADDRESSABLES define is OFF but UseAddressables=true.");
            yield break;
#endif
        }
        else
        {
            yield return LoadBuildSceneFast(sceneName, startTime, visualStartTime);
        }

        if (unloadUnusedAssetsAfterLoad)
        {
            Debug.Log("[LoadingScreenController] UnloadUnusedAssets started.");

            var unloadOp = Resources.UnloadUnusedAssets();
            while (!unloadOp.isDone)
                yield return null;

            Debug.Log("[LoadingScreenController] UnloadUnusedAssets completed.");
        }

        float visibleTime = Time.unscaledTime - _displayStartTime;
        if (visibleTime < minVisibleSeconds)
            yield return new WaitForSecondsRealtime(minVisibleSeconds - visibleTime);

        SetProgress(1f);

        _isLoading = false;

        if (loadingParticle && loadingParticle.isPlaying)
            loadingParticle.Stop();

        Debug.Log($"[LoadingScreenController] Finished total={Time.realtimeSinceStartup - startTime:0.00}s");

        Destroy(gameObject);
    }

#if ADDRESSABLES
    IEnumerator LoadAddressableSceneFast(string sceneName, float startTime, float visualStartTime)
    {
        AsyncOperationHandle<SceneInstance> handle;
        bool usedPreloadedHandle = false;

        if (_preloadedScenes.TryGetValue(sceneName, out var preloadedHandle) && preloadedHandle.IsValid())
        {
            handle = preloadedHandle;
            usedPreloadedHandle = true;

            Debug.Log($"[LoadingScreenController] Use preloaded addressable scene: {sceneName}");
        }
        else
        {
            Debug.LogWarning($"[LoadingScreenController] Scene was NOT preloaded. Loading now: {sceneName}");

            handle = Addressables.LoadSceneAsync(
                sceneName,
                LoadSceneMode.Single,
                activateOnLoad: false
            );

            _preloadedScenes[sceneName] = handle;
        }

        // Phase 1: chờ preload/load scene package xong.
        while (!handle.IsDone)
        {
            float elapsed = Time.unscaledTime - visualStartTime;
            float time01 = Mathf.Clamp01(elapsed / maxLoadingSeconds);

            float realProgress = Mathf.Clamp01(handle.PercentComplete);
            float visual = Mathf.Min(waitCapProgress, Mathf.Max(time01 * 0.85f, realProgress * 0.85f));

            SetProgress(visual);

            if (elapsed > maxLoadingSeconds && !usedPreloadedHandle)
            {
                Debug.LogWarning(
                    $"[LoadingScreenController] Loading exceeded {maxLoadingSeconds:0.00}s because scene was not preloaded yet. " +
                    $"Scene={sceneName}, Percent={handle.PercentComplete:0.00}"
                );
            }

            yield return null;
        }

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"[LoadingScreenController] Addressables LoadSceneAsync failed: {sceneName}");
            yield break;
        }

        Debug.Log($"[LoadingScreenController] Scene package ready at {Time.realtimeSinceStartup - startTime:0.00}s");

        // Nếu scene đã preload xong từ trước, đến đây gần như tức thì.
        SetProgress(Mathf.Max(_currentProgress, 0.88f));

        // Phase 2: activate scene.
        Debug.Log($"[LoadingScreenController] Activate scene started: {sceneName}");

        var activateOp = handle.Result.ActivateAsync();

        while (!activateOp.isDone)
        {
            float elapsed = Time.unscaledTime - visualStartTime;
            float time01 = Mathf.Clamp01(elapsed / maxLoadingSeconds);

            float activateProgress = Mathf.Clamp01(activateOp.progress);
            float visual = Mathf.Lerp(0.88f, waitCapProgress, Mathf.Max(time01, activateProgress));

            SetProgress(visual);

            if (elapsed > maxLoadingSeconds)
            {
                Debug.LogWarning(
                    $"[LoadingScreenController] Scene activation exceeded {maxLoadingSeconds:0.00}s. " +
                    $"This is usually caused by heavy Awake/OnEnable/Start/shader/lighting in scene: {sceneName}"
                );
            }

            yield return null;
        }

        Debug.Log($"[LoadingScreenController] Scene activated at {Time.realtimeSinceStartup - startTime:0.00}s");

        _lastActivatedAddressableSceneHandle = handle;
        _preloadedScenes.Remove(sceneName);

        // Phase 3: fill 100 trong phần thời gian còn lại, không cố giữ loading lâu.
        yield return FillToCompleteWithinLimit(visualStartTime);
    }
#endif

    IEnumerator LoadBuildSceneFast(string sceneName, float startTime, float visualStartTime)
    {
        Debug.Log($"[LoadingScreenController] Build scene load started: {sceneName}");

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);

        if (op == null)
        {
            Debug.LogError($"[LoadingScreenController] LoadSceneAsync returned null: {sceneName}");
            yield break;
        }

        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            float elapsed = Time.unscaledTime - visualStartTime;
            float time01 = Mathf.Clamp01(elapsed / maxLoadingSeconds);

            float realProgress = Mathf.Clamp01(op.progress / 0.9f);
            float visual = Mathf.Min(waitCapProgress, Mathf.Max(time01 * 0.85f, realProgress * 0.85f));

            SetProgress(visual);

            if (elapsed > maxLoadingSeconds)
            {
                Debug.LogWarning(
                    $"[LoadingScreenController] Build scene loading exceeded {maxLoadingSeconds:0.00}s. " +
                    $"Scene={sceneName}, Progress={op.progress:0.00}"
                );
            }

            yield return null;
        }

        Debug.Log($"[LoadingScreenController] Build scene ready at {Time.realtimeSinceStartup - startTime:0.00}s");

        SetProgress(Mathf.Max(_currentProgress, 0.9f));

        op.allowSceneActivation = true;

        while (!op.isDone)
        {
            float elapsed = Time.unscaledTime - visualStartTime;
            float time01 = Mathf.Clamp01(elapsed / maxLoadingSeconds);

            float visual = Mathf.Lerp(0.9f, waitCapProgress, time01);
            SetProgress(visual);

            if (elapsed > maxLoadingSeconds)
            {
                Debug.LogWarning(
                    $"[LoadingScreenController] Build scene activation exceeded {maxLoadingSeconds:0.00}s. " +
                    $"Check Awake/Start/shader/lighting in scene: {sceneName}"
                );
            }

            yield return null;
        }

        Debug.Log($"[LoadingScreenController] Build scene activated at {Time.realtimeSinceStartup - startTime:0.00}s");

        yield return FillToCompleteWithinLimit(visualStartTime);
    }

    IEnumerator FillToCompleteWithinLimit(float visualStartTime)
    {
        float elapsed = Time.unscaledTime - visualStartTime;
        float remaining = Mathf.Max(0.05f, maxLoadingSeconds - elapsed);

        float start = _currentProgress;
        float duration = Mathf.Min(0.18f, remaining);

        if (elapsed >= maxLoadingSeconds)
            duration = 0.05f;

        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float s = Mathf.Clamp01(t / duration);
            SetProgress(Mathf.Lerp(start, 1f, s));
            yield return null;
        }

        SetProgress(1f);
    }

    void SetProgress(float t)
    {
        _currentProgress = Mathf.Clamp01(t);

        int percent = Mathf.RoundToInt(_currentProgress * 100f);

        if (textLoading)
            textLoading.text = $"{baseText} {percent}%{new string('.', _dotCount)}";

        if (progressRing)
            progressRing.fillAmount = _currentProgress;

        if (sliderUI)
            sliderUI.value = _currentProgress;
    }

    IEnumerator CycleRandomImages()
    {
        if (_images.Count == 0)
            yield break;

        while (_isLoading)
        {
            if (_currentImageIndex >= 0 && _currentImageIndex < _images.Count)
            {
                if (_images[_currentImageIndex])
                    _images[_currentImageIndex].gameObject.SetActive(false);
            }

            int nextIndex;

            do
            {
                nextIndex = Random.Range(0, _images.Count);
            }
            while (nextIndex == _currentImageIndex && _images.Count > 1);

            _currentImageIndex = nextIndex;

            if (_images[_currentImageIndex])
                _images[_currentImageIndex].gameObject.SetActive(true);

            yield return new WaitForSecondsRealtime(imageSwitchInterval);
        }

        foreach (var img in _images)
        {
            if (img)
                img.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!_isLoading)
            return;

        _dotTimer += Time.unscaledDeltaTime;

        if (_dotTimer >= dotSpeed)
        {
            _dotTimer = 0f;
            _dotCount = (_dotCount + 1) % 4;

            SetProgress(_currentProgress);
        }
    }
}