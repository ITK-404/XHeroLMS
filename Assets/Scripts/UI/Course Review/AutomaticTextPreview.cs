using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AutomaticTextPreview : MonoBehaviour
{
    public ScrollRect scrollRect;
    public TextMeshProUGUI textPrefab;
    public RectTransform container;

    [SerializeField] private SRTCourseData[] sourcesData;
    [SerializeField] private AudioSource audioSource;
    private Coroutine playCoroutine;
    private Coroutine currentWordCoroutine;

    private SRTCourseData currentSourceData;
    public string seoUrl;

    // Track whether any word has been spawned
    private bool hasSpawned = false;

    // Timer fields
    private Coroutine timerCoroutine;
    private bool timerCompleted = false;
    private float timerElapsed = 0f;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    [ContextMenu("Create Text")]
    public void CreateText()
    {
        currentSourceData = null;
        hasSpawned = false; // reset when creating
        StopTimer();

        if (string.IsNullOrEmpty(seoUrl))
        {
            Debug.Log("Không có seo url");
            return;
        }

        foreach (var item in sourcesData)
        {
            if (item.seoUrl == seoUrl)
            {
                Debug.Log("Đã tìm thấy url");
                currentSourceData = item;
                break;
            }
        }

        if (currentSourceData == null) return;

        scrollRect.gameObject.SetActive(true);

        // Stop any running playback
        if (playCoroutine != null)
        {
            StopCoroutine(playCoroutine);
            playCoroutine = null;
        }

        if (currentWordCoroutine != null)
        {
            StopCoroutine(currentWordCoroutine);
            currentWordCoroutine = null;
        }

        // Clear previous items
        if (container != null)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
                Destroy(container.GetChild(i).gameObject);
        }

        isShowTextDone = false;
        playCoroutine = StartCoroutine(PlaySubtitlesCoroutine());
    }

    public void StopText()
    {
        // Stop known coroutines only
        if (playCoroutine != null)
        {
            StopCoroutine(playCoroutine);
            playCoroutine = null;
        }

        if (currentWordCoroutine != null)
        {
            StopCoroutine(currentWordCoroutine);
            currentWordCoroutine = null;
        }

        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
        
        // Kill DOTween tweens that may target the UI elements
        // This prevents tweens from accessing destroyed/disabled objects
        try
        {
            if (scrollRect != null)
                DOTween.Kill(scrollRect);

            if (container != null)
                DOTween.Kill(container);

            // Also kill tweens on children before destroying them
            if (container != null)
            {
                for (int i = container.childCount - 1; i >= 0; i--)
                {
                    var child = container.GetChild(i);
                    if (child != null)
                        DOTween.Kill(child);
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"DOTween.Kill encountered an exception: {ex}");
        }

        // Destroy child items safely
        if (container != null)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                var childGo = container.GetChild(i)?.gameObject;
                if (childGo != null)
                    Destroy(childGo);
            }
        }

        // Deactivate UI and stop audio
        if (scrollRect != null && scrollRect.gameObject != null)
            scrollRect.gameObject.SetActive(false);

        if (audioSource != null)
            audioSource.Stop();

        // Reset flags and timer state
        isShowTextDone = true;
        hasSpawned = false;
        timerCompleted = false;
        timerElapsed = 0f;
    }

    private IEnumerator PlaySubtitlesCoroutine()
    {
        yield return new WaitForSeconds(6);

        if (textPrefab == null || container == null)
            yield break;

        SRTCourseData sourceData = currentSourceData;

        var entries = new List<SrtEntry>();
        entries = sourceData.srtEntries;
        
        if (entries.Count == 0)
        {
            Debug.Log($"No entries found: {entries.Count}");
            yield break;
        }
        audioSource.clip = sourceData.voiceClip;
        audioSource.Play();

        // Sort by start time
        entries.Sort((a, b) => a.Start.CompareTo(b.Start));

        int currentIndex = 0;

        while (audioSource.isPlaying && currentIndex < entries.Count)
        {
            float audioTime = audioSource.time;

            // Check if we need to display the current entry
            if (audioTime >= entries[currentIndex].Start)
            {
                // Stop previous word spawn if any
                if (currentWordCoroutine != null)
                {
                    StopCoroutine(currentWordCoroutine);
                }

                // Start spawning words for this entry
                currentWordCoroutine = StartCoroutine(SpawnWordsForEntry(entries[currentIndex]));
                currentIndex++;
            }

            yield return null;
        }

        // Wait for the last word coroutine to finish
        if (currentWordCoroutine != null)
        {
            yield return currentWordCoroutine;
        }

        yield return new WaitForSeconds(1);

        // Clear all text
        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);

        playCoroutine = null;
        isShowTextDone = true;
        hasSpawned = false;
        // Note: timerCompleted is not modified here; timer lifecycle handled by StartTimer/StopTimer
    }

    private IEnumerator SpawnWordsForEntry(SrtEntry entry)
    {
        var description = entry.Text;
        var duration = entry.End - entry.Start;
        var words = description.Split(new[] { ' ', '\t', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

        if (words.Length == 0)
            yield break;

        float totalSeconds = Mathf.Max(0, duration);
        float timePerWord = totalSeconds / words.Length;

        // Mark that spawning has begun for this entry
        hasSpawned = true;

        for (int i = 0; i < words.Length; i++)
        {
            var textInstance = Instantiate(textPrefab, container);
            textInstance.text = words[i];
            textInstance.alpha = 0;
            textInstance.DOFade(1, 0.1f);
            // Force layout rebuild to get correct width
            LayoutRebuilder.ForceRebuildLayoutImmediate(container);

            float contentWidth = container.rect.width;
            float viewportWidth = scrollRect.viewport.rect.width;

            // Auto scroll to the right if content exceeds viewport
            if (contentWidth > viewportWidth)
            {
                scrollRect.DOHorizontalNormalizedPos(1, 0.1f);
            }

            yield return new WaitForSeconds(timePerWord);
        }

        currentWordCoroutine = null;
    }

    private bool isShowTextDone = false;

    public bool IsPlaying()
    {
        if (currentSourceData == null)
        {
            return false;
        }
        if(audioSource.clip == null)
        {
            return false;
        }
        return isShowTextDone == false;
    }

    // Public accessor to check if any word has been spawned
    public bool HasSpawned()
    {
        return hasSpawned;
    }

    public float GetPlayTime() { return currentSourceData ? currentSourceData.time : 0; }

    // Timer API
    public void StartTimer()
    {
        StopTimer();
        timerCompleted = false;
        timerElapsed = 0f;
        timerCoroutine = StartCoroutine(TimerCoroutine());
    }

    public void StopTimer()
    {
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
            timerCoroutine = null;
        }
        timerElapsed = 0f;
    }

    private IEnumerator TimerCoroutine()
    {
        float required = GetPlayTime();
        if (required <= 0f)
        {
            timerCompleted = true;
            yield break;
        }

        while (timerElapsed < required)
        {
            timerElapsed += Time.deltaTime;
            yield return null;
        }

        timerCompleted = true;
        timerCoroutine = null;
    }

    public bool IsTimerCompleted()
    {
        return timerCompleted;
    }
}