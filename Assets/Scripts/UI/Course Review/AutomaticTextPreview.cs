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

    private SRTCourseData currentSourceData;
    public string seoUrl;
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    [ContextMenu("Create Text")]
    public void CreateText()
    {
        if (string.IsNullOrEmpty(seoUrl))
        {
            Debug.Log("Không có seo url");
            return;
        }

        foreach(var item in sourcesData)
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
        // stop any running playback
        if (playCoroutine != null)
        {
            StopCoroutine(playCoroutine);
            playCoroutine = null;
        }

        // Clear previous items
        if (container != null)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
                Destroy(container.GetChild(i).gameObject);
        }

        playCoroutine = StartCoroutine(PlaySubtitlesCoroutine());
    }

    public void StopText()
    {
        if(playCoroutine != null)
        {
            StopCoroutine(playCoroutine);
        }
        scrollRect.gameObject.SetActive(false);
        audioSource.Stop();
        isShowTextDone = true;
    }

    private IEnumerator PlaySubtitlesCoroutine()
    {
        if (textPrefab == null || container == null)
            yield break;

        // Collect entries from provided sources

        SRTCourseData sourceData = currentSourceData;

        var entries = new List<SrtEntry>();
        entries = sourceData.srtEntries;
        audioSource.clip = sourceData.voiceClip;
        audioSource.Play();

        if (sourcesData != null && sourcesData.Length > 0)
        {
            foreach (var src in sourcesData)
            {
                if (src == null) continue;
                if (src.srtEntries != null && src.srtEntries.Count > 0)
                    entries.AddRange(src.srtEntries);
                else if (src.srtAsset != null)
                    entries.AddRange(SrtReader.Parse(src.srtAsset));
            }
        }

        if (entries.Count == 0)
            yield break;

        // Sort by start time
        entries.Sort((a, b) => a.Start.CompareTo(b.Start));

        float currentTime = 0f;

        foreach (var entry in entries)
        {
            // wait until the entry.Start relative to previous played time
            var waitBefore = Mathf.Max(0f, entry.Start - currentTime);
            if (waitBefore > 0f)
                yield return new WaitForSeconds(waitBefore);

            // Clear previous subtitle(s)
            for (int i = container.childCount - 1; i >= 0; i--)
                Destroy(container.GetChild(i).gameObject);

            // Instantiate and set text (preserve multi-line text)
            var textInstance = Instantiate(textPrefab, container);
            textInstance.text = entry.Text ?? string.Empty;

            // Optionally ensure container/viewport scroll to start
            if (scrollRect != null)
            {
                // Small animation to ensure content is visible (if content wider than viewport)
                float contentWidth = container.rect.width;
                float viewportWidth = scrollRect.viewport.rect.width;
                if (contentWidth > viewportWidth)
                    scrollRect.horizontalNormalizedPosition = 0f;
                else
                    scrollRect.horizontalNormalizedPosition = 0f;
            }

            // Show for duration (end - start). Minimum small duration to avoid zero-wait.
            var duration = Mathf.Max(0.05f, entry.End - entry.Start);
            yield return new WaitForSeconds(duration);

            currentTime = entry.End;
        }

        yield return new WaitForSeconds(0.25f);
        for (int i = container.childCount - 1; i >= 0; i--)
            Destroy(container.GetChild(i).gameObject);

        playCoroutine = null;

        isShowTextDone = true;
    }

    private bool isShowTextDone = false;

    public bool IsTextPlayDone()
    {
        if(currentSourceData == null)
        {
            return true;
        }
        return isShowTextDone;
    }
}