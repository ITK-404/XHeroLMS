using System;
using UnityEngine;
using System.Collections.Generic;

public class CircularScrollView : MonoBehaviour
{
    [Header("UI")] [SerializeField] private GameObject container;
    [SerializeField] private GameObject elementContainer;
    [SerializeField] private BigAreaUIScrollElement scrollElementPrefab;
    [SerializeField] private List<BigAreaUIScrollElement> items = new();

    [Header("Settings")] [SerializeField] private float radius = 360f;
    [SerializeField] private float startAngle = 180f;
    [SerializeField] private float angleStep = 10f;
    [SerializeField] private Vector2 offset;
    [SerializeField] private float fadeMinAngle;
    [SerializeField] private float fadeMaxAngle;
    public float scrollAngle = 0;

    private CourseMapBrowserUI courseMapBrowserUI;

    private readonly Dictionary<BigArea, float> _cachedPercentByArea = new();
    [SerializeField] private bool keepLastPercentWhenQueryReturnsZero = true;

    private void Awake()
    {
        courseMapBrowserUI = FindAnyObjectByType<CourseMapBrowserUI>();
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

        // dọn con cũ trong container (optional)
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

    [SerializeField] private float innerRange = 50f;
    [SerializeField] private float fadeRange = 30f;
    void UpdateItemPositions()
    {
        for (int i = 0; i < items.Count; i++)
        {
            // calculation
            float angle = startAngle + i * angleStep + scrollAngle;
            float angleInRadians = angle * Mathf.Deg2Rad;

            // calculation alpha
            float centerAngle = startAngle;     
            float outerRange = innerRange + fadeRange;

            float delta = Mathf.Abs(Mathf.DeltaAngle(centerAngle, angle)); // 0..180

            float alpha;
            if (delta <= innerRange)
            {
                alpha = 1f;
            }
            else if (delta >= outerRange)
            {
                alpha = 0f;
            }
            else
            {
                // alpha = 1f - Mathf.InverseLerp(innerRange, outerRange, delta);
                alpha = Mathf.SmoothStep(1f, 0f, Mathf.InverseLerp(innerRange, outerRange, delta));
            }

            float x = radius * Mathf.Cos(angleInRadians);
            float y = radius * Mathf.Sin(angleInRadians);

            RectTransform rect = items[i].Rect;
            CanvasGroup canvasGroup = items[i].CanvasGroup;

            canvasGroup.alpha = alpha;
            rect.anchoredPosition = new Vector2(x, y) + offset;
        }
    }
    

    public void SetAngle(float targetScrollAngle)
    {
        if (items == null || items.Count == 0)
            return;

        // float max = 0f;
        // float min = -(items.Count - 1) * angleStep;
        //
        // scrollAngle = Mathf.Clamp(targetScrollAngle, Mathf.Min(min, max), Mathf.Max(min, max));

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
            var isItemSelected = item.IsSelected();
            if (isItemSelected)
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