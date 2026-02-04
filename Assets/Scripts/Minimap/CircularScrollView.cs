using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

public class CircularScrollView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject container;
    [SerializeField] private GameObject elementContainer;
    [SerializeField] private BigAreaUIScrollElement scrollElementPrefab;
    [SerializeField] private List<BigAreaUIScrollElement> items = new();

    [Header("Settings - Layout")]
    [SerializeField] private float radius = 360f;
    [SerializeField] private float startAngle = 180f;
    [SerializeField] private float angleStep = 10f;
    [SerializeField] private Vector2 offset;
    public float scrollAngle = 0;

    [Header("Settings - Fade (screen-size friendly)")]
    [Tooltip("Pixel distance from focus where alpha stays 1.")]
    [SerializeField] private float innerPx = 180f;

    [Tooltip("Additional pixel distance where alpha goes from 1 -> 0.")]
    [SerializeField] private float fadePx = 120f;

    [Tooltip("If true, scales innerPx/fadePx based on Canvas/Screen so different resolutions feel similar.")]
    [SerializeField] private bool scaleFadeWithScreen = true;

    [Tooltip("Reference height to scale fade distances when scaleFadeWithScreen is true.")]
    [SerializeField] private float referenceHeight = 1080f;

    [Tooltip("Optional: assign the root Canvas to get accurate scale factor (CanvasScaler etc.).")]
    [SerializeField] private Canvas rootCanvas;

    private CourseMapBrowserUI courseMapBrowserUI;

    private readonly Dictionary<BigArea, float> _cachedPercentByArea = new();
    [SerializeField] private bool keepLastPercentWhenQueryReturnsZero = true;

    private void Awake()
    {
        courseMapBrowserUI = FindAnyObjectByType<CourseMapBrowserUI>();

        // Auto find canvas if user didn't assign
        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();
    }

    private void Start()
    {
        Init();
        Hide();
    }

    private void Update()
    {
        UpdateItemPositions();
    }

    private void Init()
    {
        items.Clear();

        // Cleanup old children (optional)
        if (elementContainer != null)
        {
            for (int i = elementContainer.transform.childCount - 1; i >= 0; i--)
                Destroy(elementContainer.transform.GetChild(i).gameObject);
        }

        foreach (var area in AreaDisplayManager.Instance.BigAreas)
        {
            var element = Instantiate(scrollElementPrefab, elementContainer.transform);
            element.bigArea = area;

            float percent = GetPercentWithCache(area);
            element.UpdateUI(percent);

            items.Add(element);
        }
    }

    private float GetPercentWithCache(BigArea area)
    {
        float queried = 0f;
        if (courseMapBrowserUI != null)
            queried = courseMapBrowserUI.GetBigAreaOwnedPercent(area);

        if (queried > 0f)
        {
            _cachedPercentByArea[area] = queried;
            return queried;
        }

        if (keepLastPercentWhenQueryReturnsZero &&
            area != null &&
            _cachedPercentByArea.TryGetValue(area, out var cached))
        {
            return cached;
        }

        _cachedPercentByArea[area] = 0f;
        return 0f;
    }

    private float GetFadeScale()
    {
        if (!scaleFadeWithScreen) return 1f;

        // Prefer canvas scale if available (handles CanvasScaler, render mode, etc.)
        float canvasScale = 1f;
        if (rootCanvas != null)
            canvasScale = rootCanvas.scaleFactor;

        // Also scale by screen height so different resolutions feel consistent
        float screenScale = Screen.height / referenceHeight;

        // Combine gently: canvasScale already accounts for screen scaling in many setups,
        // but not always. Multiplying both can over-scale on some setups.
        // So we take the larger one as a safer default.
        return Mathf.Max(screenScale, canvasScale);
    }

    private Vector2 GetFocusPoint()
    {
        // Focus point is the position of the item when its angle == startAngle
        float a = startAngle * Mathf.Deg2Rad;
        return offset + new Vector2(radius * Mathf.Cos(a), radius * Mathf.Sin(a));
    }

    private float GetAlphaByDistance(Vector2 itemPos)
    {
        Vector2 focus = GetFocusPoint();
        float d = Vector2.Distance(itemPos, focus);

        float scale = GetFadeScale();
        float inner = innerPx * scale;
        float outer = (innerPx + fadePx) * scale;

        if (d <= inner) return 1f;
        if (d >= outer) return 0f;

        float t = Mathf.InverseLerp(inner, outer, d);
        return Mathf.SmoothStep(1f, 0f, t);
    }

    private void UpdateItemPositions()
    {
        if (items == null) return;

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item == null) continue;

            float angle = startAngle + i * angleStep + scrollAngle;
            float angleInRadians = angle * Mathf.Deg2Rad;

            float x = radius * Mathf.Cos(angleInRadians);
            float y = radius * Mathf.Sin(angleInRadians);

            RectTransform rect = item.Rect;
            CanvasGroup canvasGroup = item.CanvasGroup;

            Vector2 pos = new Vector2(x, y) + offset;

            // alpha based on screen distance to focus point
            canvasGroup.alpha = GetAlphaByDistance(pos);

            rect.anchoredPosition = pos;
        }
    }

    public void SetAngle(float targetScrollAngle)
    {
        if (items == null || items.Count == 0)
            return;

        float boundA = 0f;
        float boundB = -(items.Count - 1) * angleStep;

        scrollAngle = Mathf.Clamp(
            targetScrollAngle,
            Mathf.Min(boundA, boundB),
            Mathf.Max(boundA, boundB)
        );
    }

    public void Show()
    {
        container.gameObject.SetActive(true);
        RefreshUI();
        TryUpdateFocusElement();
    }

    public void Hide()
    {
        container.gameObject.SetActive(false);
    }

    public void RefreshUI()
    {
        if (items == null) return;

        for (int i = 0; i < items.Count; i++)
        {
            var item = items[i];
            if (item == null) continue;

            float percent = GetPercentWithCache(item.bigArea);
            item.UpdateUI(percent);
        }
    }

    private void TryUpdateFocusElement()
    {
        if (items == null) return;

        for (int index = 0; index < items.Count; index++)
        {
            var item = items[index];
            if (item == null) continue;

            if (item.IsSelected())
            {
                SelectElement(index);
                break;
            }
        }
    }

    public void SelectElement(int index)
    {
        if (items == null || items.Count == 0)
            return;

        index = Mathf.Clamp(index, 0, items.Count - 1);

        float targetScrollAngle = -index * angleStep;
        SetAngle(targetScrollAngle);
    }
}
