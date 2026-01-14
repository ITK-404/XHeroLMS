using System;
using UnityEngine;
using System.Collections.Generic;
using JetBrains.Annotations;

public class CircularScrollView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject container;
    [SerializeField] private GameObject elementContainer;
    [SerializeField] private BigAreaUIScrollElement scrollElementPrefab;
    [SerializeField] private List<BigAreaUIScrollElement> items = new();
    
    [Header("Settings")]
    [SerializeField] private float radius = 360f;
    [SerializeField] private float startAngle = 180f;
    [SerializeField] private float angleStep = 10f;
    [SerializeField] private Vector2 offset;
    [SerializeField] private float fadeMinAngle;
    [SerializeField] private float fadeMaxAngle;
    public float scrollAngle = 0;
    
    private void Start()
    {
        Init();
        Hide();
    }

    private void Init()
    {
        foreach (var item in AreaDisplayManager.Instance.BigAreas)
        {
            var element = Instantiate(scrollElementPrefab, elementContainer.transform);
            element.bigArea = item;
            element.UpdateUI();
            
            items.Add(element);
        }
    }
    
    private void Update()
    {
        UpdateItemPositions();
    }


    void UpdateItemPositions()
    {
        for (int i = 0; i < items.Count; i++)
        {
            // calculation
            float angle = startAngle + i * angleStep + scrollAngle;
            float angleInRadians = angle * Mathf.Deg2Rad;
            
            // calculation alpha
            float delta = Mathf.Abs(Mathf.DeltaAngle(startAngle, angle));
            float alpha = 1;
            float fadeAngle = startAngle + fadeMaxAngle;
            if (delta > fadeAngle)
            {
                alpha = 0f;
            }
            else
            {
                alpha = 1f - Mathf.InverseLerp(0f, fadeAngle, delta);
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
        UpdateHighlightElement();
        TryUpdateFocusElement();
    }

    public void Hide()
    {
        container.gameObject.SetActive(false);
    }

    private void UpdateHighlightElement()
    {
        foreach (var item in items)
        {
            item.UpdateUI();
        }
    }

    private void TryUpdateFocusElement()
    {
        if (items == null)return;
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