using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AutomaticTextPreview : MonoBehaviour
{
    [Header("UI")]
    public ScrollRect scrollRect;
    public TextMeshProUGUI textPrefab;
    public RectTransform container;

    [Header("Course SEO (runtime)")]
    // BuyReviewCourseManager sẽ set seoCourse mỗi lần chọn khoá học.
    public string seoCourse;

    [Header("Course Audio (Resources)")]
    // AudioSource để play audio mô tả khoá học. Nếu null sẽ tự AddComponent.
    public AudioSource audioSource;

    // Thư mục trong Resources. Mặc định: Resources/Audio_Course/<seo>.*
    public string resourcesAudioFolder = "Audio_Course";

    [Header("Spawn Words")]
    public float startDelay = 0.05f;
    public float fadeTime = 0.08f;

    [Header("Auto Fit Text To Audio")]
    // Nếu bật: tự tính delay để tổng thời gian spawn text ~ bằng audio.length (có ngắt câu theo dấu).
    public bool autoFitTextToAudio = true;

    // Trừ bớt phần im lặng đầu audio (nếu audio có lead-in).
    public float audioLeadInSeconds = 0.25f;

    // Clamp giây/chữ để tránh quá nhanh/quá chậm.
    public float minSecondsPerWord = 0.06f;
    public float maxSecondsPerWord = 0.60f;

    [Header("Timing Units (for auto-fit)")]
    // Base unit cho mỗi chữ (tỷ lệ), không phải giây.
    public float baseUnitPerWord = 1f;

    // Extra unit cho dấu phẩy ',' (tỷ lệ).
    public float extraUnitComma = 0.5f;

    // Extra unit cho dấu mạnh . ! ? ; : (tỷ lệ).
    public float extraUnitStrongPunc = 2.0f;

    // Extra unit cho dấu '...' (tỷ lệ).
    public float extraUnitEllipsis = 2.5f;

    // Extra unit cho xuống dòng \\n (tỷ lệ).
    public float extraUnitLineBreak = 2.0f;

    [Header("Fixed Timing Fallback (when no audio or auto-fit OFF)")]
    // Mỗi chữ (giây)")]
    public float fixedPerWordSeconds = 0.5f;

    // Dấu ',' thêm (giây)")]
    public float fixedCommaExtraSeconds = 0.25f;

    // Các dấu còn lại (. ! ? ; : ... \\n) thêm (giây)
    public float fixedStrongExtraSeconds = 1.0f;

    private Coroutine playCoroutine;
    private bool isShowTextDone = true;
    private bool hasSpawned = false;

    private AudioClip _currentClip;

    public bool IsPlaying() => isShowTextDone == false;
    public bool HasSpawned() => hasSpawned;

    /// <summary>
    /// Spawn text + play course audio by seoCourse (if exists).
    /// Nếu không có audio -> vẫn spawn text, chỉ silent.
    /// </summary>
    public void PlayTextAndSpeak(string text)
    {
        ResetRuntimeState(stopAudio: true);

        if (string.IsNullOrWhiteSpace(text))
            return;

        ShowUI(true);
        ClearContainer();
        ResetScrollPos();

        isShowTextDone = false;
        hasSpawned = false;

        EnsureAudioSource();

        // Load + play audio by SEO
        _currentClip = LoadCourseClip(seoCourse);
        if (_currentClip != null)
        {
            audioSource.clip = _currentClip;
            audioSource.loop = false;
            audioSource.Play();
        }
        else
        {
            // Không có audio -> silent
            if (audioSource != null)
            {
                audioSource.Stop();
                audioSource.clip = null;
            }
        }

        playCoroutine = StartCoroutine(SpawnWordsAutoFit(text, _currentClip));
    }

    public void StopText()
    {
        ResetRuntimeState(stopAudio: true);
        ShowUI(false);
    }

    public void ResetRuntimeState(bool stopAudio)
    {
        if (playCoroutine != null)
        {
            StopCoroutine(playCoroutine);
            playCoroutine = null;
        }

        KillTweensSafe();
        ClearContainer();
        ResetScrollPos();

        isShowTextDone = true;
        hasSpawned = false;

        if (stopAudio && audioSource != null)
        {
            audioSource.Stop();
            audioSource.clip = null;
        }

        _currentClip = null;
    }

    private IEnumerator SpawnWordsAutoFit(string text, AudioClip clip)
    {
        yield return new WaitForSeconds(startDelay);

        if (scrollRect == null || scrollRect.viewport == null || container == null || textPrefab == null)
        {
            isShowTextDone = true;
            yield break;
        }

        var words = text.Split(new[] { ' ', '\t', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            isShowTextDone = true;
            yield break;
        }

        hasSpawned = true;

        // ====== TÍNH TIMING ======
        float baseDelaySeconds = fixedPerWordSeconds; // fallback
        float unitSeconds = 0f;
        bool useAuto = autoFitTextToAudio && clip != null && clip.length > 0.25f;

        if (useAuto)
        {
            // tính total units
            float totalUnits = 0f;
            for (int i = 0; i < words.Length; i++)
                totalUnits += (baseUnitPerWord + GetExtraPauseUnits(words[i]));

            totalUnits = Mathf.Max(1f, totalUnits);

            float effectiveLen = Mathf.Max(0.1f, clip.length - Mathf.Max(0f, audioLeadInSeconds));
            unitSeconds = effectiveLen / totalUnits;

            baseDelaySeconds = baseUnitPerWord * unitSeconds;
            baseDelaySeconds = Mathf.Clamp(baseDelaySeconds, minSecondsPerWord, maxSecondsPerWord);
        }

        // ====== SPAWN LOOP ======
        for (int i = 0; i < words.Length; i++)
        {
            string w = words[i];

            var inst = Instantiate(textPrefab, container);
            inst.text = w;
            inst.alpha = 0f;
            inst.DOFade(1f, fadeTime);

            LayoutRebuilder.ForceRebuildLayoutImmediate(container);

            float contentWidth = container.rect.width;
            float viewportWidth = scrollRect.viewport.rect.width;
            if (contentWidth > viewportWidth)
                scrollRect.DOHorizontalNormalizedPos(1f, 0.1f);

            float extraSeconds;

            if (useAuto && unitSeconds > 0f)
            {
                extraSeconds = GetExtraPauseUnits(w) * unitSeconds;
            }
            else
            {
                extraSeconds = GetExtraPauseSeconds_Fixed(w);
            }

            yield return new WaitForSeconds(baseDelaySeconds + extraSeconds);
        }

        playCoroutine = null;
        isShowTextDone = true;
    }

    private void EnsureAudioSource()
    {
        if (audioSource != null) return;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    private AudioClip LoadCourseClip(string seo)
    {
        if (string.IsNullOrWhiteSpace(seo)) return null;

        seo = seo.Trim();
        seo = StripExtension(seo);

        string path = $"{resourcesAudioFolder}/{seo}";
        var clip = Resources.Load<AudioClip>(path);

        // Không có clip -> ok, silent mode
        if (clip == null)
            Debug.Log($"[AutomaticTextPreview] No audio for seo='{seo}' at Resources/{path}.*");

        return clip;
    }

    private string StripExtension(string s)
    {
        int dot = s.LastIndexOf('.');
        if (dot > 0 && dot > s.LastIndexOf('/') && dot > s.LastIndexOf('\\'))
            return s.Substring(0, dot);
        return s;
    }

    // ======= AUTO-FIT: UNITS =======
    private float GetExtraPauseUnits(string word)
    {
        if (string.IsNullOrEmpty(word)) return 0f;

        if (word.Contains("\n")) return extraUnitLineBreak;
        if (word.Contains("...")) return extraUnitEllipsis;

        char last = word[word.Length - 1];
        switch (last)
        {
            case ',': return extraUnitComma;

            case '.':
            case '!':
            case '?':
            case ';':
            case ':':
                return extraUnitStrongPunc;

            default:
                return 0f;
        }
    }

    // ======= FALLBACK: FIXED SECONDS =======
    private float GetExtraPauseSeconds_Fixed(string word)
    {
        if (string.IsNullOrEmpty(word)) return 0f;

        if (word.Contains("\n")) return fixedStrongExtraSeconds;
        if (word.Contains("...")) return fixedStrongExtraSeconds;

        char last = word[word.Length - 1];
        switch (last)
        {
            case ',':
                return fixedCommaExtraSeconds;

            case '.':
            case '!':
            case '?':
            case ';':
            case ':':
                return fixedStrongExtraSeconds;

            default:
                return 0f;
        }
    }

    private void ShowUI(bool active)
    {
        if (scrollRect != null && scrollRect.gameObject != null)
            scrollRect.gameObject.SetActive(active);
    }

    private void ResetScrollPos()
    {
        if (scrollRect == null) return;
        scrollRect.horizontalNormalizedPosition = 0f;
        scrollRect.verticalNormalizedPosition = 1f;
    }

    private void ClearContainer()
    {
        if (container == null) return;

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            var child = container.GetChild(i);
            if (child != null)
                Destroy(child.gameObject);
        }
    }

    private void KillTweensSafe()
    {
        try
        {
            if (scrollRect != null) DOTween.Kill(scrollRect);
            if (container != null) DOTween.Kill(container);

            if (container != null)
            {
                for (int i = container.childCount - 1; i >= 0; i--)
                {
                    var child = container.GetChild(i);
                    if (child != null) DOTween.Kill(child);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[AutomaticTextPreview] DOTween.Kill exception: {ex}");
        }
    }
}
