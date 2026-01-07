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
    public string seoCourse;

    [Header("Course Audio (Resources)")]
    public AudioSource audioSource;
    public string resourcesAudioFolder = "Audio_Course";

    [Header("Spawn Words")]
    public float startDelay = 0.05f;
    public float fadeTime = 0.08f;

    [Header("Auto Fit Text To Audio")]
    public bool autoFitTextToAudio = true;
    public float audioLeadInSeconds = 0.25f;

    public float minSecondsPerWord = 0.04f;
    public float maxSecondsPerWord = 0.35f;

    [Header("Character-weighted (recommended for AI/TTS)")]
    public float charUnitScale = 0.22f;

    public float minUnitsPerWord = 0.6f;
    public float maxUnitsPerWord = 3.0f;

    [Header("Extra Units (auto-fit)")]
    public float extraUnitComma = 0.5f;
    public float extraUnitStrongPunc = 1.4f;   // giảm bớt cho hợp TTS
    public float extraUnitEllipsis = 2.0f;
    public float extraUnitLineBreak = 1.2f;    // nếu có line-break pause

    [Header("Fixed Timing Fallback (no audio / auto-fit OFF)")]
    public float fixedPerWordSeconds = 0.22f;
    public float fixedCommaExtraSeconds = 0.15f;
    public float fixedStrongExtraSeconds = 0.55f;

    private Coroutine playCoroutine;
    private bool isShowTextDone = true;
    private bool hasSpawned = false;
    private AudioClip _currentClip;

    public bool IsPlaying() => isShowTextDone == false;
    public bool HasSpawned() => hasSpawned;

    // Cho PlayVideoOpenBook chờ
    public bool IsTextDone() => isShowTextDone;

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

        _currentClip = LoadCourseClip(seoCourse);
        if (_currentClip != null)
        {
            audioSource.clip = _currentClip;
            audioSource.loop = false;
            audioSource.Play();
        }
        else
        {
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
        yield return new WaitForSecondsRealtime(startDelay);

        if (scrollRect == null || scrollRect.viewport == null || container == null || textPrefab == null)
        {
            isShowTextDone = true;
            yield break;
        }

        var words = text.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
        {
            isShowTextDone = true;
            yield break;
        }

        hasSpawned = true;

        bool useAuto = autoFitTextToAudio && clip != null && clip.length > 0.25f;

        float unitSeconds = 0f;

        if (useAuto)
        {
            float totalUnits = 0f;
            for (int i = 0; i < words.Length; i++)
                totalUnits += GetWordUnits(words[i]);

            totalUnits = Mathf.Max(1f, totalUnits);

            float effectiveLen = Mathf.Max(0.1f, clip.length - Mathf.Max(0f, audioLeadInSeconds));
            unitSeconds = effectiveLen / totalUnits;
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

            float waitSeconds;

            if (useAuto && unitSeconds > 0f)
            {
                // WordUnits * unitSeconds => delay theo độ dài từ + dấu câu
                float wordUnits = GetWordUnits(w);

                // đổi units -> seconds, rồi clamp theo giây/word để không quá chậm/quá nhanh
                waitSeconds = wordUnits * unitSeconds;
                waitSeconds = Mathf.Clamp(waitSeconds, minSecondsPerWord, maxSecondsPerWord);
            }
            else
            {
                // fallback cố định
                waitSeconds = fixedPerWordSeconds + GetExtraPauseSeconds_Fixed(w);
            }

            yield return new WaitForSecondsRealtime(waitSeconds);
        }

        playCoroutine = null;
        isShowTextDone = true;
    }

    private float GetWordUnits(string word)
    {
        if (string.IsNullOrEmpty(word)) return 1f;

        // Extra pause: chỉ tính 1 lần và cap
        float extra = 0f;

        if (word.Contains("...")) extra += extraUnitEllipsis;

        // line break
        if (word.Contains("\n")) extra += extraUnitLineBreak;

        // dấu câu cuối: nếu có nhiều dấu liên tiếp, vẫn coi như 1 lần
        char last = word[word.Length - 1];
        if (last == ',') extra += extraUnitComma;
        else if (last == '.' || last == '!' || last == '?' || last == ';' || last == ':')
            extra += extraUnitStrongPunc;

        // tránh token bị pause quá nhiều
        extra = Mathf.Min(extra, extraUnitStrongPunc); // hoặc 1.6f tuỳ bạn

        int pureLen = GetApproxPureLength(word);
        float lenUnits = Mathf.Clamp(pureLen * charUnitScale, minUnitsPerWord, maxUnitsPerWord);

        return lenUnits + extra;
    }

    // Đếm “độ dài tương đối” (bỏ bớt ký tự dấu câu cuối) để units ổn định hơn
    private int GetApproxPureLength(string w)
    {
        if (string.IsNullOrEmpty(w)) return 1;

        w = w.Replace("\n", "").Replace("\r", "");

        // trim hàng loạt dấu câu ở cuối (kể cả '-')
        while (w.Length > 0)
        {
            char c = w[w.Length - 1];
            if (c == '.' || c == ',' || c == '!' || c == '?' || c == ';' || c == ':' || c == '-' || c == '—' || c == '–' ||
                c == ')' || c == ']' || c == '}' || c == '"' || c == '\'')
            {
                w = w.Substring(0, w.Length - 1);
            }
            else break;
        }

        // nếu token chỉ còn toàn dấu (ví dụ "----") thì pureLen sẽ thành 0 => ép về 1
        return Mathf.Max(1, w.Length);
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
