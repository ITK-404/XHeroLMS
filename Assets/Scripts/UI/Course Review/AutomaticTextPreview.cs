using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AutomaticTextPreview : MonoBehaviour
{
    public ScrollRect scrollRect;
    public TextMeshProUGUI textPrefab;
    public RectTransform container;
    [TextArea]public string description;
    public int time = 30;

    private void Start()
    {
        CreateText();
    }

    [ContextMenu("Create Text")]
    public void CreateText()
    {
        // Clear previous items
        if (container != null)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
                Destroy(container.GetChild(i).gameObject);
        }

        StartCoroutine(CreateTextCoroutine());
    }

    private IEnumerator CreateTextCoroutine()
    {
        if (string.IsNullOrWhiteSpace(description) || textPrefab == null || container == null)
            yield break;

        var words = description.Split(new[] { ' ', '\t', '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            yield break;

        float totalSeconds = Mathf.Max(1f, time);
        float timePerWord = totalSeconds / words.Length;

        for (int i = 0; i < words.Length; i++)
        {
            var textInstance = Instantiate(textPrefab, container);
            textInstance.text = words[i];
            
            float contentWidth = container.rect.width;
            float viewportWidth = scrollRect.viewport.rect.width;
            
            if(contentWidth > viewportWidth)
            {
                scrollRect.DOHorizontalNormalizedPos(1, 0.1f);
            }
           
            yield return new WaitForSeconds(timePerWord);
        }
    }
}
