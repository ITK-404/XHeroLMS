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
    public float dotSpeed = 0.5f;
    string baseText = "Đang tải";
    float imageSwitchInterval = 1f;

    [Header("Display Timing")]
    // Thời gian hiển thị loading tối thiểu (giây).
    public float minDisplaySeconds = 3f;

    [Header("Visual Progress Settings")]
    // Cho phép % hiển thị đi trước tiến trình thật một chút để đỡ đứng hình.
    public float headroom = 0.06f; // 6%

    // Danh sách mốc phần trăm trước khi vào 90% (0..1).
    public float[] milestonePercents = new float[] { 0.30f, 0.50f, 0.60f, 0.70f, 0.80f, 0.90f };

    // Thời gian cho từng mốc ở trên (giây). Nếu để trống hoặc khác độ dài sẽ dùng giá trị mặc định.
    public float[] milestoneDurations = new float[] { 0.35f, 0.45f, 0.30f, 0.30f, 0.35f, 0.50f };

    public float fakeFillDuration = 3f;

    // --- private ---
    private AsyncOperation _async;
    private bool _isLoading;
    private float _dotTimer;
    private int _dotCount;
    private float _currentProgress; // lưu giá trị hiện tại
    private readonly List<Image> _images = new();
    private int _currentImageIndex = -1;

    // post-activate syncing
    private bool _sceneLoadedFired = false;
    private string _targetSceneName;
    private float _displayStartTime;

    void Awake()
    {
        if (imageScene1) _images.Add(imageScene1);
        foreach (var img in _images)
            if (img) img.gameObject.SetActive(false);
        // Bật đỡ lên để tránh không có background
        imageScene1.gameObject.SetActive(true);

        if (progressRing) progressRing.fillAmount = 0f;
        if (sliderUI) sliderUI.value = 0f;

        // bảo vệ độ dài mảng mốc/thời gian
        if (milestoneDurations == null || milestoneDurations.Length != milestonePercents.Length)
        {
            milestoneDurations = new float[milestonePercents.Length];
            for (int i = 0; i < milestoneDurations.Length; i++)
                milestoneDurations[i] = 0.35f;
        }
    }

    void Start()
    {
        if (!string.IsNullOrEmpty(LoadingTransition.TargetSceneName))
        {
            _targetSceneName = LoadingTransition.TargetSceneName;
            StartCoroutine(LoadByNameRoutine(_targetSceneName));
        }
    }

    IEnumerator LoadByNameRoutine(string sceneName)
    {
        _isLoading = true;
        _sceneLoadedFired = false;
        _displayStartTime = Time.unscaledTime;

        SetProgress(0f);

        // Unload scene cũ (nếu unload fail vẫn cứ tiếp tục)
        var opUnLoad = SceneManager.UnloadSceneAsync(LoadingTransition.PreviousSceneName);
        if (opUnLoad != null)
        {
            while (!opUnLoad.isDone) yield return null;
        }

        // bắt đầu đổi ảnh
        StartCoroutine(CycleRandomImages());

        // ======= LOAD THẬT (build vs addressables) =======
        bool useAddr = LoadingTransition.UseAddressables;

        AsyncOperation op = null;

#if ADDRESSABLES
        AsyncOperationHandle<SceneInstance> addrHandle = default;
#endif

        float visual = 0f;

        if (!useAddr)
        {
            // BUILD scene
            op = SceneManager.LoadSceneAsync(sceneName);
            op.allowSceneActivation = false;
            _async = op;
        }
        else
        {
#if ADDRESSABLES
            // ADDRESSABLE scene
            // addrHandle = Addressables.LoadSceneAsync(sceneName, LoadSceneMode.Single, activateOnLoad: false);
            addrHandle = Addressables.LoadSceneAsync(sceneName, LoadSceneMode.Single, activateOnLoad: true);
#else
        Debug.LogError("[LoadingScreenController] ADDRESSABLES define OFF but UseAddressables=true");
        _isLoading = false;
        yield break;
#endif
        }

        // === Phase 0 -> ~90% ===
        for (int i = 0; i < milestonePercents.Length; i++)
        {
            float target = Mathf.Clamp01(milestonePercents[i]);
            float dur = Mathf.Max(0.05f, milestoneDurations[i]);

            float t = 0f;
            float start = visual;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float planned = Mathf.Lerp(start, target, t / dur);

                float realCap;
                if (!useAddr)
                    realCap = (op != null ? op.progress : planned);
                else
                {
#if ADDRESSABLES
                    realCap = addrHandle.PercentComplete; // 0..1
#else
                realCap = planned;
#endif
                }

                float allowed = Mathf.Min(planned, realCap + headroom);
                visual = Mathf.Clamp01(allowed);
                SetProgress(visual);

                yield return null;
            }

            visual = target;
            SetProgress(visual);

            // break sớm nếu load đã gần xong
            if (!useAddr)
            {
                if (op != null && op.progress >= 0.9f) break;
            }
            else
            {
#if ADDRESSABLES
                if (addrHandle.PercentComplete >= 0.9f) break;
#endif
            }
        }

        // đẩy lên gần 90% cho đến khi “ready-to-activate”
        if (!useAddr)
        {
            while (op != null && op.progress < 0.9f)
            {
                float allowed = Mathf.Min(0.90f, op.progress + headroom);
                visual = Mathf.Max(visual, allowed);
                SetProgress(visual);
                yield return null;
            }
        }
        else
        {
#if ADDRESSABLES
while (!addrHandle.IsDone)
{
    float allowed = Mathf.Min(0.90f, addrHandle.PercentComplete + headroom);
    visual = Mathf.Max(visual, allowed);
    SetProgress(visual);
    yield return null;
}
#endif
        }

        visual = Mathf.Max(visual, 0.90f);
        SetProgress(visual);

        // Bắt sự kiện sceneLoaded chỉ cần cho build scene; addressables ta tự check handle
        SceneManager.sceneLoaded += OnSceneLoaded;
        DontDestroyOnLoad(gameObject);

        float fakeStartTime = Time.unscaledTime;
        float targetVisualBeforeDone = 0.98f;

        // ACTIVATE
        if (!useAddr)
        {
            op.allowSceneActivation = true;
        }
        else
        {
#if ADDRESSABLES
            // addrHandle.ActivateAsync();
            // addrHandle.Result.Activate();
#endif
        }

        while (true)
        {
            float fakeElapsed = Time.unscaledTime - fakeStartTime;
            float t01 = Mathf.Clamp01(fakeElapsed / fakeFillDuration);
            float planned = Mathf.Lerp(visual, targetVisualBeforeDone, t01);
            SetProgress(planned);

            bool done;
            if (!useAddr)
            {
                done = _sceneLoadedFired; // build scene bắn event
            }
            else
            {
#if ADDRESSABLES
                done = addrHandle.IsDone && addrHandle.Status == AsyncOperationStatus.Succeeded;
#else
            done = false;
#endif
            }

            if (done && fakeElapsed >= fakeFillDuration)
                break;

            yield return null;
        }

        // 98% -> 100%
        float endLerpTime = 0.3f;
        float endElapsed = 0f;
        float startValue = _currentProgress;

        while (endElapsed < endLerpTime)
        {
            endElapsed += Time.unscaledDeltaTime;
            float s = Mathf.Clamp01(endElapsed / endLerpTime);
            float planned = Mathf.Lerp(startValue, 1f, s);
            SetProgress(planned);
            yield return null;
        }

        SetProgress(1f);

        // min display
        float totalDisplayTime = Time.unscaledTime - _displayStartTime;
        float remain = Mathf.Max(0f, minDisplaySeconds - totalDisplayTime);
        if (remain > 0f)
            yield return new WaitForSecondsRealtime(remain);

        _isLoading = false;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        Destroy(gameObject);
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (string.IsNullOrEmpty(_targetSceneName) || scene.name == _targetSceneName)
        {
            _sceneLoadedFired = true;
        }
    }

    void SetProgress(float t)
    {
        _currentProgress = Mathf.Clamp01(t);
        int percent = Mathf.RoundToInt(_currentProgress * 100f);

        // hiển thị Loading + phần trăm + dấu chấm
        if (textLoading)
            textLoading.text = $"{baseText} {percent}%{new string('.', _dotCount)}";

        // BỎ lerp - gán trực tiếp để đồng bộ với text
        if (progressRing)
        {
            progressRing.fillAmount = _currentProgress;
            if (sliderUI) sliderUI.value = _currentProgress;
        }
        else if (sliderUI)
        {
            sliderUI.value = _currentProgress;
        }
    }

    IEnumerator CycleRandomImages()
    {
        if (_images.Count == 0) yield break;

        while (_isLoading)
        {
            if (_currentImageIndex >= 0 && _currentImageIndex < _images.Count)
                _images[_currentImageIndex].gameObject.SetActive(false);

            int nextIndex;
            do { nextIndex = Random.Range(0, _images.Count); }
            while (nextIndex == _currentImageIndex && _images.Count > 1);

            _currentImageIndex = nextIndex;
            _images[_currentImageIndex].gameObject.SetActive(true);

            yield return new WaitForSecondsRealtime(imageSwitchInterval);
        }

        foreach (var img in _images)
            if (img) img.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!_isLoading) return;

        _dotTimer += Time.unscaledDeltaTime;
        if (_dotTimer >= dotSpeed)
        {
            _dotTimer = 0f;
            _dotCount = (_dotCount + 1) % 4;

            // cập nhật lại text mỗi khi chấm đổi
            SetProgress(_currentProgress);
        }
    }
}