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

    [Header("Legacy (not used for API sync)")]
    public string seoUrl;

    [Header("TTS")]
    public bool useTTS = true;
    [Range(0.2f, 2f)] public float ttsRate = 0.95f;
    [Range(0.5f, 2f)] public float ttsPitch = 1.05f;

    [Header("Spawn Words")]
    public float startDelay = 0.05f;
    public float fadeTime = 0.08f;

    [Tooltip("Tốc độ cơ bản (từ/giây). Sẽ scale theo ttsRate.")]
    public float baseWordsPerSecond = 7.5f;

    [Header("Pause by punctuation (ms)")]
    public int pauseCommaMs = 180;      // ,
    public int pauseSemicolonMs = 220;  // ; :
    public int pausePeriodMs = 380;     // . ! ?
    public int pauseEllipsisMs = 520;   // ...
    public int pauseLineBreakMs = 420;  // \n

    private Coroutine playCoroutine;
    private bool isShowTextDone = true;
    private bool hasSpawned = false;

    public bool IsPlaying() => isShowTextDone == false;
    public bool HasSpawned() => hasSpawned;

    public void PlayTextAndSpeak(string text)
    {
        ResetRuntimeState(stopTTS: true);

        if (string.IsNullOrWhiteSpace(text))
            return;

        ShowUI(true);
        ClearContainer();
        ResetScrollPos();

        isShowTextDone = false;
        hasSpawned = false;

        if (useTTS && TTSManager.I != null)
        {
            TTSManager.I.SetRatePitch(ttsRate, ttsPitch);
            TTSManager.I.Speak(text);
        }

        playCoroutine = StartCoroutine(SpawnWordsEstimated(text));
    }

    public void StopText()
    {
        ResetRuntimeState(stopTTS: true);
        ShowUI(false);
    }

    public void ResetRuntimeState(bool stopTTS)
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

        if (stopTTS && TTSManager.I != null)
            TTSManager.I.Stop();
    }

    private IEnumerator SpawnWordsEstimated(string text)
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

        float speed = Mathf.Max(1f, baseWordsPerSecond * Mathf.Clamp(ttsRate, 0.6f, 1.6f));
        float baseDelay = 1f / speed;

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

            float extra = GetExtraPauseSeconds(w);
            yield return new WaitForSeconds(baseDelay + extra);
        }

        playCoroutine = null;
        isShowTextDone = true;
    }

    private float GetExtraPauseSeconds(string word)
    {
        if (string.IsNullOrEmpty(word)) return 0f;

        if (word.Contains("\n")) return pauseLineBreakMs / 1000f;
        if (word.Contains("...")) return pauseEllipsisMs / 1000f;

        char last = word[word.Length - 1];
        switch (last)
        {
            case ',': return pauseCommaMs / 1000f;
            case ';':
            case ':': return pauseSemicolonMs / 1000f;
            case '.':
            case '!':
            case '?': return pausePeriodMs / 1000f;
            default: return 0f;
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
