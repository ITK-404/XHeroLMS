using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class LoadingScreenController : MonoBehaviour
{
    [Header("UI References")]
    public Image imageScene1;
    public Image progressRing;
    public TMP_Text textLoading;

    [Header("Loading Text Animation")]
    public float dotSpeed = 0.5f;
    string baseText = "Loading";
    float imageSwitchInterval = 1f;

    [Header("Visual Progress Settings")]
    [Tooltip("Cho phép % hiển thị đi trước tiến trình thật một chút để đỡ đứng hình.")]
    public float headroom = 0.06f; // 6%

    [Tooltip("Danh sách mốc phần trăm trước khi vào 90% (0..1).")]
    public float[] milestonePercents = new float[] { 0.30f, 0.50f, 0.60f, 0.70f, 0.80f, 0.90f };

    [Tooltip("Thời gian cho từng mốc ở trên (giây). Nếu để trống hoặc khác độ dài sẽ dùng giá trị mặc định.")]
    public float[] milestoneDurations = new float[] { 0.35f, 0.45f, 0.30f, 0.30f, 0.35f, 0.50f };

    [Tooltip("Thời gian mượt từ 90% -> 100%")]
    public float fakeFillDuration = 3f;

    // --- private ---
    private AsyncOperation _async;
    private bool _isLoading;
    private float _dotTimer;
    private int _dotCount;
    private float _currentProgress; // lưu giá trị hiện tại
    private List<Image> _images = new();
    private int _currentImageIndex = -1;

    void Awake()
    {
        if (imageScene1) _images.Add(imageScene1);
        foreach (var img in _images) if (img) img.gameObject.SetActive(false);

        if (progressRing) progressRing.fillAmount = 0f;

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
            StartCoroutine(LoadByNameRoutine(LoadingTransition.TargetSceneName));
    }

    IEnumerator LoadByNameRoutine(string sceneName)
    {
        _isLoading = true;
        SetProgress(0f);

        var op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;
        _async = op;

        StartCoroutine(CycleRandomImages());

        // chạy qua các mốc 30-50-60-70-80-90
        float visual = 0f;
        for (int i = 0; i < milestonePercents.Length; i++)
        {
            float target = Mathf.Clamp01(milestonePercents[i]);
            float dur = Mathf.Max(0.05f, milestoneDurations[i]);
            
            float t = 0f;
            float start = visual;
            while (t < dur)
            {
                t += Time.deltaTime;
                float planned = Mathf.Lerp(start, target, t / dur);

                // không cho hiển thị vượt quá tiến trình thật + headroom
                float realCap = (op != null ? op.progress : planned);
                float allowed = Mathf.Min(planned, realCap + headroom);

                visual = Mathf.Clamp01(allowed);
                SetProgress(visual);
                yield return null;
            }

            visual = target;
            SetProgress(visual);

            // nếu Unity đã đạt 0.9 sớm, break khỏi vòng mốc
            if (op.progress >= 0.9f) break;
        }

        // đảm bảo không vượt 90% khi load thật còn <0.9
        while (op.progress < 0.9f)
        {
            float allowed = Mathf.Min(0.90f, op.progress + headroom);
            visual = Mathf.Max(visual, allowed);
            SetProgress(visual);
            yield return null;
        }

        // 90% -> 100% mượt trong fakeFillDuration
        float elapsed = 0f;
        while (elapsed < fakeFillDuration)
        {
            elapsed += Time.deltaTime;
            float s = Mathf.Clamp01(elapsed / fakeFillDuration);
            float planned = Mathf.Lerp(0.90f, 1f, s);

            // phần này không cần cap theo op.progress nữa vì Unity đã sẵn sàng activate
            SetProgress(planned);
            yield return null;
        }

        SetProgress(1f);
        yield return new WaitForSeconds(0.2f);

        _isLoading = false;
        op.allowSceneActivation = true;
    }

    void SetProgress(float t)
    {
        _currentProgress = Mathf.Clamp01(t);
        int percent = Mathf.RoundToInt(_currentProgress * 100f);

        // hiển thị Loading + phần trăm + dấu chấm
        if (textLoading)
            textLoading.text = $"{baseText} {percent}%{new string('.', _dotCount)}";

        if (progressRing)
            progressRing.fillAmount = _currentProgress;
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

            yield return new WaitForSeconds(imageSwitchInterval);
        }

        foreach (var img in _images) img.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!_isLoading) return;

        _dotTimer += Time.deltaTime;
        if (_dotTimer >= dotSpeed)
        {
            _dotTimer = 0f;
            _dotCount = (_dotCount + 1) % 4;

            // cập nhật lại text mỗi khi chấm đổi
            SetProgress(_currentProgress);
        }
    }
}
