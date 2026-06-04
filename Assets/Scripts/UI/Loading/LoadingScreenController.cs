using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if ADDRESSABLES
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
    [Tooltip("Tổng thời gian loading UI từ 0 đến 100 nếu scene đã sẵn sàng.")]
    public float maxLoadingSeconds = 2f;

    [Tooltip("Nếu scene chưa ready, thanh loading giữ tối đa ở mức này thay vì lên 100 giả.")]
    [Range(0.9f, 0.999f)]
    public float waitCapProgress = 0.99f;

    [Tooltip("Thời gian tối thiểu để người dùng thấy loading screen, tránh chớp màn quá nhanh.")]
    public float minVisibleSeconds = 0.25f;

    [Tooltip("Có unload unused assets sau khi activate scene không. Bật cái này sẽ sạch RAM hơn nhưng có thể chậm thêm.")]
    public bool unloadUnusedAssetsAfterLoad = false;

    [Header("Addressables Prepare UI")]
    [Tooltip("Phần progress dành cho bước AddressablesPreload tải + giải nén scene.")]
    [Range(0.1f, 0.85f)]
    public float prepareProgressWeight = 0.75f;

    [Tooltip("Mốc progress bắt đầu khi load package scene sau bước prepare.")]
    [Range(0.5f, 0.95f)]
    public float sceneLoadStartProgress = 0.75f;

    [Tooltip("Mốc progress bắt đầu khi activate scene.")]
    [Range(0.7f, 0.99f)]
    public float sceneActivateStartProgress = 0.88f;

    [Header("Image Cycle")]
    public float imageSwitchInterval = 1f;

    private bool _isLoading;
    private float _dotTimer;
    private int _dotCount;
    private float _currentProgress;
    private float _displayStartTime;
    private string _targetSceneName;
    private string _loadingStatusOverride = "";

    private readonly List<Image> _images = new List<Image>();
    private int _currentImageIndex = -1;

#if ADDRESSABLES
    private static AsyncOperationHandle<SceneInstance>? _lastActivatedAddressableSceneHandle;
#endif

    private void Awake()
    {
        if (imageScene1 != null)
            _images.Add(imageScene1);

        foreach (var img in _images)
        {
            if (img != null)
                img.gameObject.SetActive(false);
        }

        if (imageScene1 != null)
            imageScene1.gameObject.SetActive(true);

        if (progressRing != null)
            progressRing.fillAmount = 0f;

        if (sliderUI != null)
            sliderUI.value = 0f;

        SetProgress(0f);
    }

    private void Start()
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

    private IEnumerator LoadByNameRoutine(string sceneName)
    {
        _isLoading = true;
        _displayStartTime = Time.unscaledTime;

        float startTime = Time.realtimeSinceStartup;
        float visualStartTime = Time.unscaledTime;

        _loadingStatusOverride = baseText;
        SetProgress(0f);

        if (loadingParticle != null && !loadingParticle.isPlaying)
            loadingParticle.Play();

        StartCoroutine(CycleRandomImages());

        bool useAddressables = LoadingTransition.UseAddressables;

        Debug.Log(
            $"[LoadingScreenController] Load start. " +
            $"Scene={sceneName}, UseAddressables={useAddressables}, Previous={LoadingTransition.PreviousSceneName}"
        );

        yield return SafeUnloadPreviousScene();

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

        _loadingStatusOverride = "Hoàn tất";
        SetProgress(1f);

        _isLoading = false;

        if (loadingParticle != null && loadingParticle.isPlaying)
            loadingParticle.Stop();

        Debug.Log($"[LoadingScreenController] Finished total={Time.realtimeSinceStartup - startTime:0.00}s");

        Destroy(gameObject);
    }

    private IEnumerator SafeUnloadPreviousScene()
    {
        string previousScene = LoadingTransition.PreviousSceneName;

        if (string.IsNullOrWhiteSpace(previousScene))
            yield break;

        Scene prev = SceneManager.GetSceneByName(previousScene);

        if (!prev.IsValid() || !prev.isLoaded)
        {
            Debug.Log($"[LoadingScreenController] Previous scene not loaded or invalid. Skip unload: {previousScene}");
            yield break;
        }

        if (previousScene == SceneManager.GetActiveScene().name && SceneManager.sceneCount <= 1)
        {
            Debug.LogWarning(
                $"[LoadingScreenController] Skip unload previous scene because it is the only loaded scene: {previousScene}"
            );
            yield break;
        }

        Debug.Log($"[LoadingScreenController] Unload previous scene: {previousScene}");

        AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(previousScene);

        if (unloadOp == null)
        {
            Debug.LogWarning($"[LoadingScreenController] UnloadSceneAsync returned null: {previousScene}");
            yield break;
        }

        while (!unloadOp.isDone)
            yield return null;
    }

#if ADDRESSABLES
    private IEnumerator LoadAddressableSceneFast(string sceneName, float startTime, float visualStartTime)
    {
        // ============================================================
        // Phase 0: AddressablesPreload tải + giải nén đúng scene
        // ============================================================

        _loadingStatusOverride = "Đang chuẩn bị tài nguyên scene";
        SetProgress(0.01f);

        Debug.Log($"[LoadingScreenController] Prepare addressables target started: {sceneName}");

        bool prepareDone = false;

        StartCoroutine(RunPrepareTargetAddressablesRoutine(() =>
        {
            prepareDone = true;
        }));

        while (!prepareDone)
        {
            ApplyAddressablesPrepareProgress();

            if (LoadingTransition.HasPrepareFailed)
                break;

            yield return null;
        }

        ApplyAddressablesPrepareProgress();

        if (LoadingTransition.HasPrepareFailed)
        {
            Debug.LogError("[LoadingScreenController] Prepare target addressables failed: " + LoadingTransition.LastPrepareError);

            _loadingStatusOverride = "";
            SetProgress(_currentProgress);

            LoadingUI.ShowErrorPopup(
                null
            );

            yield break;
        }

        Debug.Log($"[LoadingScreenController] Prepare addressables target DONE at {Time.realtimeSinceStartup - startTime:0.00}s");

        SetProgress(Mathf.Max(_currentProgress, sceneLoadStartProgress));

        // ============================================================
        // Phase 1: Load scene package
        // ============================================================

        _loadingStatusOverride = "Đang load scene";

        Debug.Log($"[LoadingScreenController] Addressables LoadSceneAsync started: {sceneName}");

        AsyncOperationHandle<SceneInstance> handle = LoadingTransition.LoadAddressableAsync(false);

        while (handle.IsValid() && !handle.IsDone)
        {
            float realProgress = Mathf.Clamp01(handle.PercentComplete);
            float visual = Mathf.Lerp(sceneLoadStartProgress, sceneActivateStartProgress, realProgress);

            SetProgress(Mathf.Min(waitCapProgress, visual));

            yield return null;
        }

        if (!handle.IsValid())
        {
            Debug.LogError($"[LoadingScreenController] Addressables LoadSceneAsync handle invalid: {sceneName}");
            yield break;
        }

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            string err = handle.OperationException != null
                ? handle.OperationException.ToString()
                : handle.Status.ToString();

            Debug.LogError($"[LoadingScreenController] Addressables LoadSceneAsync failed: {sceneName}\n{err}");
            yield break;
        }

        Debug.Log($"[LoadingScreenController] Scene package ready at {Time.realtimeSinceStartup - startTime:0.00}s");

        SetProgress(Mathf.Max(_currentProgress, sceneActivateStartProgress));

        // ============================================================
        // Phase 2: Activate scene
        // ============================================================

        _loadingStatusOverride = "Đang mở scene";

        Debug.Log($"[LoadingScreenController] Activate scene started: {sceneName}");

        AsyncOperation activateOp = handle.Result.ActivateAsync();

        while (!activateOp.isDone)
        {
            float activateProgress = Mathf.Clamp01(activateOp.progress);
            float visual = Mathf.Lerp(sceneActivateStartProgress, waitCapProgress, activateProgress);

            SetProgress(Mathf.Min(waitCapProgress, visual));

            yield return null;
        }

        Debug.Log($"[LoadingScreenController] Scene activated at {Time.realtimeSinceStartup - startTime:0.00}s");

        _lastActivatedAddressableSceneHandle = handle;

        yield return FillToCompleteWithinLimit(visualStartTime);
    }

    private IEnumerator RunPrepareTargetAddressablesRoutine(Action onDone)
    {
        yield return LoadingTransition.PrepareTargetAddressablesRoutine();
        onDone?.Invoke();
    }

    private void ApplyAddressablesPrepareProgress()
    {
        if (AddressablesPreload.Instance == null)
        {
            _loadingStatusOverride = "Đang chuẩn bị tài nguyên";
            SetProgress(Mathf.Max(0.01f, _currentProgress));
            return;
        }

        float prepare01 = Mathf.Clamp01(AddressablesPreload.Instance.DownloadPercent01);
        float visual = Mathf.Lerp(0.01f, prepareProgressWeight, prepare01);

        // Quan trọng:
        // Lấy text thật từ AddressablesPreload:
        // "Đang tải scene: 25% | 2.5 MB/s | 25 MB/100 MB"
        _loadingStatusOverride = AddressablesPreload.Instance.LoadingText;

        SetProgress(Mathf.Min(prepareProgressWeight, visual));
    }
#endif

    private IEnumerator LoadBuildSceneFast(string sceneName, float startTime, float visualStartTime)
    {
        _loadingStatusOverride = "Đang load scene";

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

        _loadingStatusOverride = "Đang mở scene";

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

    private IEnumerator FillToCompleteWithinLimit(float visualStartTime)
    {
        _loadingStatusOverride = "Hoàn tất";

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

    private void SetProgress(float t)
    {
        _currentProgress = Mathf.Clamp01(t);

        int percent = Mathf.RoundToInt(_currentProgress * 100f);
        string dots = new string('.', _dotCount);

        string displayText = string.IsNullOrWhiteSpace(_loadingStatusOverride)
            ? baseText
            : _loadingStatusOverride;

        bool textAlreadyHasProgress =
            displayText.Contains("%") ||
            displayText.Contains("/") ||
            displayText.Contains("|");

        if (textLoading != null)
        {
            if (textAlreadyHasProgress)
                textLoading.text = $"{displayText}{dots}";
            else
                textLoading.text = $"{displayText} {percent}%{dots}";
        }

        if (progressRing != null)
            progressRing.fillAmount = _currentProgress;

        if (sliderUI != null)
            sliderUI.value = _currentProgress;
    }

    private IEnumerator CycleRandomImages()
    {
        if (_images.Count == 0)
            yield break;

        while (_isLoading)
        {
            if (_currentImageIndex >= 0 && _currentImageIndex < _images.Count)
            {
                if (_images[_currentImageIndex] != null)
                    _images[_currentImageIndex].gameObject.SetActive(false);
            }

            int nextIndex;

            do
            {
                nextIndex = UnityEngine.Random.Range(0, _images.Count);
            }
            while (nextIndex == _currentImageIndex && _images.Count > 1);

            _currentImageIndex = nextIndex;

            if (_images[_currentImageIndex] != null)
                _images[_currentImageIndex].gameObject.SetActive(true);

            yield return new WaitForSecondsRealtime(imageSwitchInterval);
        }

        foreach (var img in _images)
        {
            if (img != null)
                img.gameObject.SetActive(false);
        }
    }

    private void Update()
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