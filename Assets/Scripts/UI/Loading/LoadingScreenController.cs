using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
#endif

public class LoadingScreenController : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panelLoadingRoot;
    public Image imageScene1;
    public Image progressRing;
    public TMP_Text textLoading;
    public ParticleSystem loadingParticle;
    public Slider sliderUI;

    [Header("Loading Text Animation")]
    public float dotSpeed = 0.35f;
    public string baseText = "Đang tải";

    [Header("Fast Loading Settings")]
    [Tooltip("Chỉ dùng để log cảnh báo nếu scene load quá lâu. Không dùng để đẩy progress ảo.")]
    public float maxLoadingSeconds = 2f;

// Network / Load Fail Protection")]
public bool enableRollbackProtection = true;

// Tên scene loading trung gian. Không rollback về scene này.")]
public string loadingSceneName = "LoadingScene";

public string networkLostHeader = "Mạng không ổn định";
public string networkLostMessage = "Đường truyền yếu.\nĐã hủy tải và quay lại màn trước.";

public bool showRollbackPopup = true;
public float rollbackPopupDelaySeconds = 0.15f;

private bool _loadSucceeded;
private bool _loadFailedOrCancelled;
private GameObject _inputBlockerRoot;
private Image _inputBlockerImage;
#if ADDRESSABLES
private Coroutine _prepareAddressablesCoroutine;
#endif

    [Tooltip("Thời gian tối thiểu để người dùng thấy loading screen, tránh chớp màn quá nhanh.")]
    public float minVisibleSeconds = 0.25f;

    [Tooltip("Có unload unused assets sau khi activate scene không. Bật cái này sẽ sạch RAM hơn nhưng có thể chậm thêm.")]
    public bool unloadUnusedAssetsAfterLoad = false;

    [Header("New Scene Late Content")]
    public bool waitForNewSceneLateContentBeforeReveal = false;
    public float newSceneLateContentWaitTimeoutSeconds = 300f;
    public float newSceneLateContentFindTimeoutSeconds = 5f;
    public bool waitForCachedNewSceneLateContentBeforeReveal = true;
    public float cachedNewSceneLateContentWaitTimeoutSeconds = 12f;
    public float uncachedNewSceneLateContentRevealGraceSeconds = 4f;
    public float newSceneLateContentCacheProbeWaitSeconds = 1.5f;

    [Header("Image Cycle")]
    public float imageSwitchInterval = 1f;

    [Header("Load Next Video")]
    public bool enableLoadNextVideo = true;
    public string loadNextSourceSceneName = "New Scene";
    public string loadNextVideoFileName = "Load_next.mp4";
    public float loadNextVideoDuration = 6f;
    public float loadNextVideoPrepareTimeout = 3f;
    public bool muteLoadNextVideo = true;
    public GameObject loadNextVideoRoot;
    public RawImage loadNextVideoRawImage;
    public VideoPlayer loadNextVideoPlayer;
    public RenderTexture loadNextVideoRenderTexture;
    public long minValidLoadNextVideoBytes = 1024 * 50;
    public bool forceRefreshLoadNextVideoCache = false;

    private readonly List<Image> _images = new List<Image>();

    private bool _isLoading;
    private bool _defaultLoadingVisible;
    private bool _loadNextVideoRequired;
    private bool _loadNextVideoFinished = true;
    private CanvasGroup _panelLoadingCanvasGroup;

    private float _dotTimer;
    private int _dotCount;
    private float _currentProgress;
    private float _displayStartTime;
    private string _targetSceneName;
    private string _loadingStatusOverride = "";

    private int _currentImageIndex = -1;
    private Coroutine _imageCycleRoutine;
    private RenderTexture _runtimeLoadNextRenderTexture;

#if ADDRESSABLES
    private static AsyncOperationHandle<SceneInstance>? _lastActivatedAddressableSceneHandle;
#endif

private void Awake()
{
    if (imageScene1 != null)
        _images.Add(imageScene1);

    ResolvePanelLoadingRoot();
    ResolveExistingLoadNextVideoObjects();

    EnsureInputBlocker();
    SetInputBlockerVisible(false);

    HideLoadNextVideoSurface();
    SetDefaultLoadingVisible(false);

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
    _loadSucceeded = false;
    _loadFailedOrCancelled = false;

    _currentProgress = 0f;
    _loadingStatusOverride = baseText;
    SetProgress(0f);

    _loadNextVideoRequired = ShouldPlayLoadNextVideo(sceneName);
    _loadNextVideoFinished = !_loadNextVideoRequired;

    if (_loadNextVideoRequired)
    {
        HideDefaultLoadingPanel();
        yield return PlayLoadNextVideoRoutine();

        if (_loadFailedOrCancelled)
            yield break;

        _currentProgress = 0f;
        _loadingStatusOverride = baseText;
        SetProgress(0f);
        ShowDefaultLoadingVisuals();
    }
    else
    {
        ShowDefaultLoadingVisuals();
    }

    float startTime = Time.realtimeSinceStartup;
    float visualStartTime = Time.unscaledTime;
    bool useAddressables = LoadingTransition.UseAddressables;

    Debug.Log(
        $"[LoadingScreenController] Load start. " +
        $"Scene={sceneName}, UseAddressables={useAddressables}, Previous={LoadingTransition.PreviousSceneName}"
    );

    // FIX:
    // Không unload scene trước ở đây nữa.
    // Chỉ unload sau khi scene mới chắc chắn load/activate thành công.
    if (useAddressables)
    {
#if ADDRESSABLES
        yield return LoadAddressableSceneFast(sceneName, startTime, visualStartTime);
#else
        Debug.LogError("[LoadingScreenController] ADDRESSABLES define is OFF but UseAddressables=true.");
        yield return RollbackToPreviousSceneRoutine("ADDRESSABLES define is OFF.");
        yield break;
#endif
    }
    else
    {
        yield return LoadBuildSceneFast(sceneName, startTime, visualStartTime);
    }

    if (_loadFailedOrCancelled)
        yield break;

    if (!_loadSucceeded)
    {
        yield return RollbackToPreviousSceneRoutine("Load did not complete.");
        yield break;
    }

    // Chỉ tới đây mới được unload scene trước.
    yield return SafeUnloadPreviousScene();

    if (unloadUnusedAssetsAfterLoad)
    {
        Debug.Log("[LoadingScreenController] UnloadUnusedAssets started.");

        var unloadOp = Resources.UnloadUnusedAssets();
        while (!unloadOp.isDone)
            yield return null;

        Debug.Log("[LoadingScreenController] UnloadUnusedAssets completed.");
    }

    float visibleTime = Time.unscaledTime - _displayStartTime;
    if (_defaultLoadingVisible && visibleTime < minVisibleSeconds)
        yield return new WaitForSecondsRealtime(minVisibleSeconds - visibleTime);

    _loadingStatusOverride = "Hoàn tất";
    SetProgress(1f);

    _isLoading = false;
    StopDefaultLoadingVisuals();

    Debug.Log($"[LoadingScreenController] Finished total={Time.realtimeSinceStartup - startTime:0.00}s");

    yield return CloseLoadingScreenRoutine();
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

    private IEnumerator CloseLoadingScreenRoutine()
    {
        Scene ownScene = gameObject.scene;

        if (ownScene.IsValid() &&
            ownScene.isLoaded &&
            string.Equals(ownScene.name, loadingSceneName, StringComparison.Ordinal) &&
            SceneManager.sceneCount > 1)
        {
            Debug.Log($"[LoadingScreenController] Unload loading scene: {ownScene.name}");

            AsyncOperation unloadOp = SceneManager.UnloadSceneAsync(ownScene);

            if (unloadOp != null)
            {
                while (!unloadOp.isDone)
                    yield return null;

                yield break;
            }
        }

        Destroy(gameObject);
    }

#if ADDRESSABLES
    private IEnumerator LoadAddressableSceneFast(string sceneName, float startTime, float visualStartTime)
    {
        if (ShouldRunAddressablesPreloadPhase())
        {
            yield return PrepareAddressablesTarget(sceneName, startTime);
            if (LoadingTransition.HasPrepareFailed)
                yield break;
        }
        else
        {
            Debug.Log($"[LoadingScreenController] Editor mode: skip AddressablesPreload phase for {sceneName}.");
        }

        _loadingStatusOverride = "Đang kết nối đến thế giới";
        SetProgress(0f);

        Debug.Log($"[LoadingScreenController] Addressables LoadSceneAsync started: {sceneName}");

        bool loadTargetAdditively = ShouldLoadTargetAdditivelyBeforeReveal(sceneName);
        AsyncOperationHandle<SceneInstance> handle = LoadAddressableScenePackage(sceneName, loadTargetAdditively);

        while (handle.IsValid() && !handle.IsDone)
        {
            SetProgress(Mathf.Lerp(0.45f, 0.65f, Mathf.Clamp01(handle.PercentComplete)));
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

        SetProgress(0.65f);

        yield return WaitForLoadNextVideoFinished("addressables activation");

        _loadingStatusOverride = "Đang mở lối vào";
        SetProgress(_currentProgress);

        Debug.Log($"[LoadingScreenController] Activate scene started: {sceneName}");

        AsyncOperation activateOp = handle.Result.ActivateAsync();

        while (!activateOp.isDone)
        {
            SetProgress(Mathf.Lerp(0.65f, 0.75f, Mathf.Clamp01(activateOp.progress)));
            yield return null;
        }

        Debug.Log($"[LoadingScreenController] Scene activated at {Time.realtimeSinceStartup - startTime:0.00}s");

        if (loadTargetAdditively && handle.Result.Scene.IsValid())
        {
            SceneManager.SetActiveScene(handle.Result.Scene);
            Debug.Log($"[LoadingScreenController] Target scene set active after additive load: {handle.Result.Scene.name}");
        }

        SetProgress(0.75f);

        yield return WaitForNewSceneLateContentIfNeeded(sceneName, startTime);

        _lastActivatedAddressableSceneHandle = handle;

        _loadingStatusOverride = "Hoàn tất";
        SetProgress(1f);
        _loadSucceeded = true;
    }

    private bool ShouldLoadTargetAdditivelyBeforeReveal(string sceneName)
    {
        return SceneNameAliases.IsNewSceneFamily(sceneName);
    }

    private IEnumerator WaitForNewSceneLateContentIfNeeded(string sceneName, float startTime)
    {
        if (!SceneNameAliases.IsNewSceneFamily(sceneName))
            yield break;

        if (!waitForNewSceneLateContentBeforeReveal && !waitForCachedNewSceneLateContentBeforeReveal)
            yield break;

        _loadingStatusOverride = "Đang dựng thế giới";
        SetProgress(0.75f);

        AddressableAdditiveSceneLoader loader = null;
        float findTimer = 0f;

        while (loader == null && findTimer < Mathf.Max(0f, newSceneLateContentFindTimeoutSeconds))
        {
            loader = FindNewSceneLateLoader();

            if (loader != null)
                break;

            findTimer += Time.unscaledDeltaTime;
            yield return null;
        }

        if (loader == null)
        {
            Debug.LogWarning("[LoadingScreenController] New Scene late loader not found. Reveal scene without late wait.");
            yield break;
        }

        loader.BeginLoad();

        float cacheProbeTimer = 0f;

        while (!loader.CacheStateKnown &&
               cacheProbeTimer < Mathf.Max(0f, newSceneLateContentCacheProbeWaitSeconds) &&
               !loader.IsComplete)
        {
            cacheProbeTimer += Time.unscaledDeltaTime;
            yield return null;
        }

        bool shouldWaitForCachedContent =
            waitForCachedNewSceneLateContentBeforeReveal &&
            loader.CacheStateKnown &&
            loader.AllDependenciesCached;

        float timeoutSeconds;

        if (waitForNewSceneLateContentBeforeReveal)
            timeoutSeconds = newSceneLateContentWaitTimeoutSeconds;
        else if (shouldWaitForCachedContent)
            timeoutSeconds = cachedNewSceneLateContentWaitTimeoutSeconds;
        else
            timeoutSeconds = uncachedNewSceneLateContentRevealGraceSeconds;

        Debug.Log("[LoadingScreenController] New Scene late wait. cacheKnown="
                  + loader.CacheStateKnown
                  + ", allCached="
                  + loader.AllDependenciesCached
                  + ", timeout="
                  + timeoutSeconds.ToString("0.0"));

        float waitTimer = 0f;

        while (!loader.IsComplete)
        {
            waitTimer += Time.unscaledDeltaTime;

            float lateProgress = Mathf.Clamp01(loader.Progress01);
            SetProgress(Mathf.Lerp(0.75f, 0.98f, lateProgress));

            if (timeoutSeconds > 0f && waitTimer >= timeoutSeconds)
            {
                Debug.LogWarning("[LoadingScreenController] Continue before New Scene late content is fully loaded. loaded="
                                 + loader.LoadedSceneCount
                                 + "/"
                                 + loader.TotalSceneCount
                                 + ", failed="
                                 + loader.FailedSceneCount
                                 + ", allCached="
                                 + loader.AllDependenciesCached);
                yield break;
            }

            yield return null;
        }

        SetProgress(0.98f);

        Debug.Log("[LoadingScreenController] New Scene late content ready at "
                  + (Time.realtimeSinceStartup - startTime).ToString("0.00")
                  + "s. loaded="
                  + loader.LoadedSceneCount
                  + "/"
                  + loader.TotalSceneCount
                  + ", failed="
                  + loader.FailedSceneCount);
    }

private AddressableAdditiveSceneLoader FindNewSceneLateLoader()
{
    AddressableAdditiveSceneLoader[] loaders =
        FindObjectsByType<AddressableAdditiveSceneLoader>(FindObjectsInactive.Include, FindObjectsSortMode.None);

    for (int i = 0; i < loaders.Length; i++)
    {
        AddressableAdditiveSceneLoader loader = loaders[i];

        if (loader == null)
            continue;

        if (SceneNameAliases.IsNewSceneFamily(loader.gameObject.scene.name))
            return loader;
    }

    return loaders.Length > 0 ? loaders[0] : null;
}

private IEnumerator PrepareAddressablesTarget(string sceneName, float startTime)
{
    _loadingStatusOverride = "Đang chuẩn bị tài nguyên";
    SetProgress(0.01f);

    Debug.Log($"[LoadingScreenController] Prepare addressables target started: {sceneName}");

    bool prepareDone = false;

    _prepareAddressablesCoroutine = StartCoroutine(RunPrepareTargetAddressablesRoutine(() =>
    {
        prepareDone = true;
    }));

    while (!prepareDone)
    {
        if (IsNetworkDisconnectedNow())
        {
            Debug.LogWarning("[LoadingScreenController] Network lost while preparing addressables target.");

            if (_prepareAddressablesCoroutine != null)
            {
                StopCoroutine(_prepareAddressablesCoroutine);
                _prepareAddressablesCoroutine = null;
            }

            yield return RollbackToPreviousSceneRoutine("Network disconnected while preparing addressables.");
            yield break;
        }

        ApplyAddressablesPrepareProgress();

        if (LoadingTransition.HasPrepareFailed)
            break;

        yield return null;
    }

    _prepareAddressablesCoroutine = null;

    ApplyAddressablesPrepareProgress();

    if (LoadingTransition.HasPrepareFailed)
    {
        Debug.LogError("[LoadingScreenController] Prepare target addressables failed: " + LoadingTransition.LastPrepareError);

        yield return RollbackToPreviousSceneRoutine(LoadingTransition.LastPrepareError);
        yield break;
    }

    Debug.Log($"[LoadingScreenController] Prepare addressables target DONE at {Time.realtimeSinceStartup - startTime:0.00}s");

    SetProgress(0.45f);
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
            SetProgress(0f);
            return;
        }

        float prepare01 = Mathf.Clamp01(AddressablesPreload.Instance.DownloadPercent01);
        _loadingStatusOverride = AddressablesPreload.Instance.LoadingText;

        SetProgress(Mathf.Lerp(0.01f, 0.45f, prepare01));
    }

    private AsyncOperationHandle<SceneInstance> LoadAddressableScenePackage(string sceneName, bool loadAdditively)
    {
        LoadSceneMode mode = loadAdditively ? LoadSceneMode.Additive : LoadSceneMode.Single;
        Debug.Log($"[LoadingScreenController] Addressables scene mode={mode}, activateOnLoad=false, scene={sceneName}");
        return Addressables.LoadSceneAsync(sceneName, mode, false);
    }

    private bool ShouldRunAddressablesPreloadPhase()
    {
#if UNITY_EDITOR
        return false;
#else
        return true;
#endif
    }
#endif

    private IEnumerator LoadBuildSceneFast(string sceneName, float startTime, float visualStartTime)
    {
        _loadingStatusOverride = "Đang kết nối đến thế giới";

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
            float realProgress = Mathf.Clamp01(op.progress / 0.9f);
            SetProgress(realProgress);

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

        SetProgress(1f);

        yield return WaitForLoadNextVideoFinished("build scene activation");

        _loadingStatusOverride = "Đang mở lối vao";
        SetProgress(_currentProgress);

        op.allowSceneActivation = true;

        while (!op.isDone)
        {
            float elapsed = Time.unscaledTime - visualStartTime;

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

        _loadingStatusOverride = "Hoàn tất";
        SetProgress(1f);
        _loadSucceeded = true;
    }

    private void SetProgress(float t)
    {
        float nextProgress = Mathf.Clamp01(t);

        if (_isLoading && nextProgress < _currentProgress)
            nextProgress = _currentProgress;

        _currentProgress = nextProgress;

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

    private void ShowDefaultLoadingVisuals()
    {
        SetDefaultLoadingVisible(true);
        _displayStartTime = Time.unscaledTime;

        if (loadingParticle != null && !loadingParticle.isPlaying)
            loadingParticle.Play();

        if (_imageCycleRoutine == null)
            _imageCycleRoutine = StartCoroutine(CycleRandomImages());
    }

private void StopDefaultLoadingVisuals()
{
    if (_imageCycleRoutine != null)
    {
        StopCoroutine(_imageCycleRoutine);
        _imageCycleRoutine = null;
    }

    if (loadingParticle != null && loadingParticle.isPlaying)
        loadingParticle.Stop();

    SetDefaultLoadingVisible(false);

    // Load xong thì trả lại input bình thường.
    SetInputBlockerVisible(false);
}

private void SetDefaultLoadingVisible(bool visible)
{
    _defaultLoadingVisible = visible;
    SetPanelLoadingAlpha(visible ? 1f : 0f);

    foreach (Image img in _images)
    {
        if (img != null)
            img.gameObject.SetActive(false);
    }

    if (visible && imageScene1 != null)
        imageScene1.gameObject.SetActive(true);

    if (progressRing != null)
        progressRing.gameObject.SetActive(visible);

    if (textLoading != null)
        textLoading.gameObject.SetActive(visible);

    if (sliderUI != null)
        sliderUI.gameObject.SetActive(visible);

    if (loadingParticle != null)
        loadingParticle.gameObject.SetActive(visible);

    // Chặn người dùng bấm xuyên xuống scene cũ trong lúc đang load.
    SetInputBlockerVisible(_isLoading);
}

    private void HideDefaultLoadingPanel()
    {
        StopDefaultLoadingVisuals();
        SetPanelLoadingAlpha(0f);
    }

    private void ResolvePanelLoadingRoot()
    {
        if (panelLoadingRoot == null)
            panelLoadingRoot = FindScenePanelLoadingRoot();

        if (panelLoadingRoot == null)
            panelLoadingRoot = gameObject;

        _panelLoadingCanvasGroup = panelLoadingRoot.GetComponent<CanvasGroup>();

        if (_panelLoadingCanvasGroup == null)
            _panelLoadingCanvasGroup = panelLoadingRoot.AddComponent<CanvasGroup>();
    }

    private GameObject FindScenePanelLoadingRoot()
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Transform candidate in transforms)
        {
            if (candidate == null || candidate.gameObject.scene != gameObject.scene)
                continue;

            if (string.Equals(candidate.gameObject.name, "Panel_Loading", StringComparison.OrdinalIgnoreCase))
                return candidate.gameObject;
        }

        return null;
    }

    private void SetPanelLoadingAlpha(float alpha)
    {
        if (_panelLoadingCanvasGroup == null)
            ResolvePanelLoadingRoot();

        if (_panelLoadingCanvasGroup == null)
            return;

        bool visible = alpha > 0.001f;

        _panelLoadingCanvasGroup.alpha = visible ? 1f : 0f;
        _panelLoadingCanvasGroup.interactable = visible;
        _panelLoadingCanvasGroup.blocksRaycasts = visible;
    }
private void EnsureInputBlocker()
{
    if (_inputBlockerRoot != null && _inputBlockerImage != null)
        return;

    Transform parent = GetLoadNextVideoParent();

    _inputBlockerRoot = new GameObject("Loading_Input_Blocker", typeof(RectTransform), typeof(Image));
    _inputBlockerRoot.transform.SetParent(parent, false);

    RectTransform rect = _inputBlockerRoot.GetComponent<RectTransform>();
    rect.anchorMin = Vector2.zero;
    rect.anchorMax = Vector2.one;
    rect.offsetMin = Vector2.zero;
    rect.offsetMax = Vector2.zero;
    rect.pivot = new Vector2(0.5f, 0.5f);
    rect.localScale = Vector3.one;

    _inputBlockerImage = _inputBlockerRoot.GetComponent<Image>();

    // Trong suốt nhưng vẫn bắt raycast để không cho click xuyên xuống video/UI bên dưới.
    _inputBlockerImage.color = new Color(0f, 0f, 0f, 0f);
    _inputBlockerImage.raycastTarget = true;

    _inputBlockerRoot.SetActive(false);
}

private void PlaceInputBlockerOnTop()
{
    EnsureInputBlocker();

    if (_inputBlockerRoot == null)
        return;

    Transform parent = GetLoadNextVideoParent();

    if (_inputBlockerRoot.transform.parent != parent)
        _inputBlockerRoot.transform.SetParent(parent, false);

    RectTransform rect = _inputBlockerRoot.GetComponent<RectTransform>();
    rect.anchorMin = Vector2.zero;
    rect.anchorMax = Vector2.one;
    rect.offsetMin = Vector2.zero;
    rect.offsetMax = Vector2.zero;
    rect.pivot = new Vector2(0.5f, 0.5f);
    rect.localScale = Vector3.one;

    // Cho blocker nằm trên cùng để bắt toàn bộ touch/click.
    // Nó trong suốt nên không che hình ảnh/video.
    _inputBlockerRoot.transform.SetAsLastSibling();
}

private void SetInputBlockerVisible(bool visible)
{
    if (visible)
    {
        PlaceInputBlockerOnTop();
    }
    else
    {
        if (_inputBlockerRoot == null)
            return;
    }

    _inputBlockerRoot.SetActive(visible);

    if (_inputBlockerImage != null)
        _inputBlockerImage.raycastTarget = visible;
}
    private IEnumerator CycleRandomImages()
    {
        if (_images.Count == 0)
            yield break;

        while (_isLoading)
        {
            if (!_defaultLoadingVisible)
            {
                yield return null;
                continue;
            }

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

        foreach (Image img in _images)
        {
            if (img != null)
                img.gameObject.SetActive(false);
        }
    }

    private bool ShouldPlayLoadNextVideo(string targetSceneName)
    {
        if (!enableLoadNextVideo)
            return false;

        if (string.IsNullOrWhiteSpace(loadNextVideoFileName))
            return false;

        string previous = LoadingTransition.PreviousSceneName;

        if (string.IsNullOrWhiteSpace(previous))
            return false;

        return SceneNameEquals(previous, loadNextSourceSceneName) &&
               !SceneNameEquals(targetSceneName, loadNextSourceSceneName);
    }

    private static bool SceneNameEquals(string a, string b)
    {
        return SceneNameAliases.AreSameScene(a, b) ||
               NormalizeSceneName(a) == NormalizeSceneName(b);
    }

    private static string NormalizeSceneName(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return "";

        return sceneName
            .Replace(" ", "")
            .Replace("_", "")
            .Replace("-", "")
            .Trim()
            .ToLowerInvariant();
    }

    private IEnumerator WaitForLoadNextVideoFinished(string reason)
    {
        if (!_loadNextVideoRequired || _loadNextVideoFinished)
            yield break;

        Debug.Log($"[LoadingScreenController] Waiting Load_next video before {reason}.");

        while (!_loadNextVideoFinished)
            yield return null;
    }

    private IEnumerator PlayLoadNextVideoRoutine()
    {
        _loadNextVideoFinished = false;
        HideDefaultLoadingPanel();

        string videoUrl = "";

        yield return ResolveLoadNextVideoUrlRoutine(url =>
        {
            videoUrl = url;
        });

        bool canPlayVideo = !string.IsNullOrWhiteSpace(videoUrl) && EnsureLoadNextVideoObjects();

        if (canPlayVideo)
        {
            PrepareLoadNextVideoPlayer(videoUrl);

            float prepareStarted = Time.unscaledTime;

            try
            {
                loadNextVideoPlayer.Prepare();
            }
            catch (Exception e)
            {
                Debug.LogWarning("[LoadingScreenController] Load_next video Prepare failed: " + e);
                canPlayVideo = false;
            }

            while (canPlayVideo &&
                   !loadNextVideoPlayer.isPrepared &&
                   Time.unscaledTime - prepareStarted < Mathf.Max(0.1f, loadNextVideoPrepareTimeout))
            {
                yield return null;
            }

            if (canPlayVideo && !loadNextVideoPlayer.isPrepared)
            {
                Debug.LogWarning("[LoadingScreenController] Load_next video prepare timeout. Show default loading UI.");
                canPlayVideo = false;
            }
        }

        if (!canPlayVideo)
        {
            HideLoadNextVideoSurface();
            _loadNextVideoFinished = true;
            yield break;
        }

        ShowLoadNextVideoSurface();

        try
        {
            loadNextVideoPlayer.Play();
        }
        catch (Exception e)
        {
            Debug.LogWarning("[LoadingScreenController] Load_next video Play failed: " + e);
            HideLoadNextVideoSurface();
            _loadNextVideoFinished = true;
            yield break;
        }

        yield return WaitForLoadNextVideoStarted();

        float duration = Mathf.Max(0.1f, loadNextVideoDuration);
        float visibleStarted = Time.unscaledTime;

        while (Time.unscaledTime - visibleStarted < duration)
            yield return null;

        if (loadNextVideoPlayer != null)
        {
            try
            {
                loadNextVideoPlayer.Stop();
            }
            catch { }
        }

        HideLoadNextVideoSurface();

        _loadNextVideoFinished = true;
    }

    private IEnumerator WaitForLoadNextVideoStarted()
    {
        if (loadNextVideoPlayer == null)
            yield break;

        float waitStarted = Time.unscaledTime;
        float maxWait = Mathf.Max(0.25f, loadNextVideoPrepareTimeout);

        while (Time.unscaledTime - waitStarted < maxWait)
        {
            if (loadNextVideoPlayer.isPlaying)
                yield break;

            yield return null;
        }
    }

    private IEnumerator ResolveLoadNextVideoUrlRoutine(Action<string> onResolved)
    {
        string streamingPath = Path.Combine(Application.streamingAssetsPath, loadNextVideoFileName);
        bool streamingPathIsUrl = streamingPath.Contains("://") || streamingPath.Contains("jar:");

        if (!streamingPathIsUrl && IsValidLocalVideoFile(streamingPath))
        {
            onResolved?.Invoke(new Uri(streamingPath).AbsoluteUri);
            yield break;
        }

        string persistentPath = Path.Combine(Application.persistentDataPath, loadNextVideoFileName);
        bool needCopy = forceRefreshLoadNextVideoCache || !IsValidLocalVideoFile(persistentPath);

        if (needCopy)
        {
            string sourceUrl = streamingPath;

            if (!sourceUrl.Contains("://") && !sourceUrl.Contains("jar:"))
                sourceUrl = new Uri(sourceUrl).AbsoluteUri;

            using (UnityWebRequest request = UnityWebRequest.Get(sourceUrl))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning(
                        $"[LoadingScreenController] Cannot read Load_next video. Url={sourceUrl}, Error={request.error}"
                    );

                    onResolved?.Invoke("");
                    yield break;
                }

                byte[] data = request.downloadHandler.data;

                if (data == null || data.Length < minValidLoadNextVideoBytes)
                {
                    Debug.LogWarning(
                        $"[LoadingScreenController] Load_next video data invalid. Bytes={(data == null ? 0 : data.Length)}"
                    );

                    onResolved?.Invoke("");
                    yield break;
                }

                try
                {
                    string dir = Path.GetDirectoryName(persistentPath);

                    if (!string.IsNullOrWhiteSpace(dir))
                        Directory.CreateDirectory(dir);

                    File.WriteAllBytes(persistentPath, data);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("[LoadingScreenController] Cannot cache Load_next video: " + e);
                    onResolved?.Invoke("");
                    yield break;
                }
            }
        }

        if (!IsValidLocalVideoFile(persistentPath))
        {
            Debug.LogWarning("[LoadingScreenController] Load_next video cache missing or invalid: " + persistentPath);
            onResolved?.Invoke("");
            yield break;
        }

        onResolved?.Invoke(new Uri(persistentPath).AbsoluteUri);
    }

    private bool IsValidLocalVideoFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        try
        {
            if (!File.Exists(path))
                return false;

            FileInfo file = new FileInfo(path);
            return file.Length >= minValidLoadNextVideoBytes;
        }
        catch
        {
            return false;
        }
    }

    private void ResolveExistingLoadNextVideoObjects()
    {
        if (loadNextVideoPlayer == null && loadNextVideoRoot != null)
            loadNextVideoPlayer = loadNextVideoRoot.GetComponent<VideoPlayer>();

        if (loadNextVideoRawImage == null)
            loadNextVideoRawImage = FindSceneLoadNextRawImage();

        if (loadNextVideoPlayer == null && loadNextVideoRawImage != null)
            loadNextVideoPlayer = loadNextVideoRawImage.GetComponent<VideoPlayer>();

        if (loadNextVideoPlayer == null)
            loadNextVideoPlayer = FindSceneLoadNextVideoPlayer();

        if (loadNextVideoRoot == null && loadNextVideoPlayer != null)
            loadNextVideoRoot = loadNextVideoPlayer.gameObject;

        if (loadNextVideoPlayer != null)
        {
            loadNextVideoPlayer.playOnAwake = false;

            try
            {
                if (loadNextVideoPlayer.isPlaying)
                    loadNextVideoPlayer.Stop();
            }
            catch { }
        }
    }

    private RawImage FindSceneLoadNextRawImage()
    {
        RawImage[] rawImages = FindObjectsByType<RawImage>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (RawImage rawImage in rawImages)
        {
            if (rawImage == null || rawImage.gameObject.scene != gameObject.scene)
                continue;

            bool likelyLoadNextSurface =
                rawImage.texture == null &&
                rawImage.transform.parent != null &&
                rawImage.transform.parent.GetComponent<Canvas>() != null &&
                string.Equals(rawImage.gameObject.name, "RawImage", StringComparison.OrdinalIgnoreCase);

            if (likelyLoadNextSurface)
                return rawImage;
        }

        foreach (RawImage rawImage in rawImages)
        {
            if (rawImage == null || rawImage.gameObject.scene != gameObject.scene)
                continue;

            if (rawImage.gameObject.name.IndexOf("Load_next", StringComparison.OrdinalIgnoreCase) >= 0)
                return rawImage;
        }

        return null;
    }

    private VideoPlayer FindSceneLoadNextVideoPlayer()
    {
        VideoPlayer[] videoPlayers = FindObjectsByType<VideoPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (VideoPlayer videoPlayer in videoPlayers)
        {
            if (videoPlayer == null || videoPlayer.gameObject.scene != gameObject.scene)
                continue;

            if (string.Equals(videoPlayer.gameObject.name, "VideoPlayer", StringComparison.OrdinalIgnoreCase) ||
                videoPlayer.gameObject.name.IndexOf("Load_next", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return videoPlayer;
            }
        }

        return null;
    }

    private bool EnsureLoadNextVideoObjects()
    {
        if (loadNextVideoRawImage == null)
            loadNextVideoRawImage = CreateLoadNextVideoRawImage();

        if (loadNextVideoRawImage == null)
        {
            Debug.LogWarning("[LoadingScreenController] Cannot create Load_next RawImage.");
            return false;
        }

        PlaceLoadNextVideoSurface();

        if (loadNextVideoPlayer == null)
            loadNextVideoPlayer = loadNextVideoRawImage.GetComponent<VideoPlayer>();

        if (loadNextVideoPlayer == null && loadNextVideoRoot != null)
            loadNextVideoPlayer = loadNextVideoRoot.GetComponent<VideoPlayer>();

        if (loadNextVideoPlayer == null)
            loadNextVideoPlayer = loadNextVideoRawImage.gameObject.AddComponent<VideoPlayer>();

        if (loadNextVideoRoot == null && loadNextVideoPlayer != null)
            loadNextVideoRoot = loadNextVideoPlayer.gameObject;

        if (loadNextVideoRenderTexture == null && _runtimeLoadNextRenderTexture == null)
        {
            _runtimeLoadNextRenderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32)
            {
                name = "Load_next_RuntimeRT"
            };
            _runtimeLoadNextRenderTexture.Create();
        }

        RenderTexture targetTexture = loadNextVideoRenderTexture != null
            ? loadNextVideoRenderTexture
            : _runtimeLoadNextRenderTexture;

        loadNextVideoRawImage.texture = targetTexture;
        loadNextVideoPlayer.targetTexture = targetTexture;

        return true;
    }

    private RawImage CreateLoadNextVideoRawImage()
    {
        Transform parent = GetLoadNextVideoParent();

        GameObject go = new GameObject("Load_next_Video", typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(parent, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);

        RawImage rawImage = go.GetComponent<RawImage>();
        rawImage.raycastTarget = false;
        rawImage.color = Color.white;
        rawImage.gameObject.SetActive(false);

        return rawImage;
    }

    private void PlaceLoadNextVideoSurface()
    {
        if (loadNextVideoRawImage == null)
            return;

        RectTransform rect = loadNextVideoRawImage.rectTransform;
        Transform targetParent = GetLoadNextVideoParent();

        loadNextVideoRawImage.raycastTarget = false;

        if (loadNextVideoRawImage.transform.parent != targetParent)
            loadNextVideoRawImage.transform.SetParent(targetParent, false);

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;

        loadNextVideoRawImage.transform.SetAsLastSibling();
    }

    private Transform GetLoadNextVideoParent()
    {
        Canvas canvas = GetComponentInParent<Canvas>();

        if (canvas != null)
            return canvas.transform;

        if (panelLoadingRoot != null && panelLoadingRoot.transform.parent != null)
            return panelLoadingRoot.transform.parent;

        if (transform.parent != null)
            return transform.parent;

        return transform;
    }

    private void PrepareLoadNextVideoPlayer(string videoUrl)
    {
        loadNextVideoPlayer.Stop();
        loadNextVideoPlayer.playOnAwake = false;
        loadNextVideoPlayer.isLooping = false;
        loadNextVideoPlayer.waitForFirstFrame = true;
        loadNextVideoPlayer.skipOnDrop = false;
        loadNextVideoPlayer.renderMode = VideoRenderMode.RenderTexture;
        loadNextVideoPlayer.source = VideoSource.Url;
        loadNextVideoPlayer.url = videoUrl;

        if (muteLoadNextVideo)
        {
            loadNextVideoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        }
        else
        {
            loadNextVideoPlayer.audioOutputMode = VideoAudioOutputMode.Direct;
            loadNextVideoPlayer.SetDirectAudioMute(0, false);
        }
    }

private void ShowLoadNextVideoSurface()
{
    if (loadNextVideoRawImage == null)
        return;

    PlaceLoadNextVideoSurface();

    loadNextVideoRawImage.enabled = true;
    loadNextVideoRawImage.gameObject.SetActive(true);

    // Chặn touch khi đang phát video chuyển cảnh.
    // Đặt sau video để blocker nằm trên cùng, nhưng nó trong suốt nên không che hình.
    SetInputBlockerVisible(true);
}

private void HideLoadNextVideoSurface()
{
    if (loadNextVideoRawImage != null)
    {
        loadNextVideoRawImage.enabled = false;
        loadNextVideoRawImage.gameObject.SetActive(false);
    }

    // Nếu vẫn đang loading thì tiếp tục chặn click,
    // tránh có 1 frame bị click xuyên xuống scene/video bên dưới.
    SetInputBlockerVisible(_isLoading);
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

private void OnDestroy()
{
    if (loadNextVideoPlayer != null)
    {
        try
        {
            loadNextVideoPlayer.Stop();
            loadNextVideoPlayer.targetTexture = null;
        }
        catch { }
    }

    if (_runtimeLoadNextRenderTexture != null)
    {
        _runtimeLoadNextRenderTexture.Release();
        Destroy(_runtimeLoadNextRenderTexture);
        _runtimeLoadNextRenderTexture = null;
    }

    if (_inputBlockerRoot != null)
    {
        Destroy(_inputBlockerRoot);
        _inputBlockerRoot = null;
        _inputBlockerImage = null;
    }
}
private bool IsNetworkDisconnectedNow()
{
    if (!enableRollbackProtection)
        return false;

    return Application.internetReachability == NetworkReachability.NotReachable;
}

private IEnumerator RollbackToPreviousSceneRoutine(string reason)
{
    if (_loadFailedOrCancelled)
        yield break;

    _loadFailedOrCancelled = true;
    _loadSucceeded = false;
    _isLoading = false;

    Debug.LogWarning("[LoadingScreenController] Rollback to previous scene. Reason: " + reason);

    try
    {
        if (loadNextVideoPlayer != null)
            loadNextVideoPlayer.Stop();
    }
    catch { }

    HideLoadNextVideoSurface();
    StopDefaultLoadingVisuals();

    string fallbackScene = GetFallbackPreviousSceneName();

    if (showRollbackPopup)
    {
        LoadingUI.ShowErrorPopup(
            networkLostMessage,
            networkLostHeader
        );

        if (rollbackPopupDelaySeconds > 0f)
            yield return new WaitForSecondsRealtime(rollbackPopupDelaySeconds);
        else
            yield return null;
    }

    if (string.IsNullOrWhiteSpace(fallbackScene))
    {
        Debug.LogError("[LoadingScreenController] Cannot rollback. Previous scene is empty or invalid.");
        yield break;
    }

    Scene loadedFallback = SceneManager.GetSceneByName(fallbackScene);

    if (loadedFallback.IsValid() && loadedFallback.isLoaded)
    {
        Debug.Log("[LoadingScreenController] Previous scene is still loaded. Set active and unload loading scene: " + fallbackScene);

        SceneManager.SetActiveScene(loadedFallback);

        Scene selfScene = gameObject.scene;

        if (selfScene.IsValid() &&
            selfScene.isLoaded &&
            !SceneNameEquals(selfScene.name, loadedFallback.name))
        {
            AsyncOperation unloadLoading = SceneManager.UnloadSceneAsync(selfScene);

            if (unloadLoading != null)
            {
                while (!unloadLoading.isDone)
                    yield return null;
            }
        }

        yield break;
    }

    Debug.LogWarning("[LoadingScreenController] Previous scene is not loaded anymore. Reload previous scene by name: " + fallbackScene);

    AsyncOperation reloadPrevious = SceneManager.LoadSceneAsync(fallbackScene, LoadSceneMode.Single);

    if (reloadPrevious == null)
    {
        Debug.LogError("[LoadingScreenController] Cannot reload previous scene: " + fallbackScene);
        yield break;
    }

    while (!reloadPrevious.isDone)
        yield return null;
}

private string GetFallbackPreviousSceneName()
{
    string previous = LoadingTransition.PreviousSceneName;

    if (IsValidFallbackSceneName(previous))
        return previous;

    for (int i = 0; i < SceneManager.sceneCount; i++)
    {
        Scene scene = SceneManager.GetSceneAt(i);

        if (!scene.IsValid() || !scene.isLoaded)
            continue;

        if (IsValidFallbackSceneName(scene.name))
            return scene.name;
    }

    return "";
}

private bool IsValidFallbackSceneName(string sceneName)
{
    if (string.IsNullOrWhiteSpace(sceneName))
        return false;

    if (SceneNameEquals(sceneName, _targetSceneName))
        return false;

    if (IsLoadingSceneName(sceneName))
        return false;

    return true;
}

private bool IsLoadingSceneName(string sceneName)
{
    if (string.IsNullOrWhiteSpace(sceneName))
        return true;

    if (!string.IsNullOrWhiteSpace(loadingSceneName) &&
        SceneNameEquals(sceneName, loadingSceneName))
    {
        return true;
    }

    return false;
}

#if ADDRESSABLES
private void TryReleaseAddressableHandle(AsyncOperationHandle<SceneInstance> handle)
{
    try
    {
        if (handle.IsValid())
            Addressables.Release(handle);
    }
    catch (Exception e)
    {
        Debug.LogWarning("[LoadingScreenController] Addressables.Release failed: " + e.Message);
    }
}
#endif
}
