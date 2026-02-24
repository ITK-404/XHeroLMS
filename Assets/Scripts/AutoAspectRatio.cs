using System;
using UnityEngine;

public class AutoAspectRatio : MonoBehaviour
{
    private RectTransform rect;

    [Header("Target aspect (x:y) e.g. 16:9")]
    public Vector2 aspectRatio = new Vector2(16, 9);

    [Header("Apply on start (safer for layouts)")]
    public bool applyOnStart = true;

    [Header("If true, size will update when screen size changes (cheap check)")]
    public bool updateOnScreenChange = false;

    private float originalWidth, originalHeight;
    private int lastW, lastH;
    private bool hasCachedSize = false; 

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    private void OnEnable()
    {
        if (hasCachedSize && applyOnStart)
            Apply();
    }

    private void Start()
    {
        CacheOriginalSize();
        if (applyOnStart) Apply();
        lastW = Screen.width;
        lastH = Screen.height;
    }

    private void Update()
    {
        if (!updateOnScreenChange) return;

        if (Screen.width != lastW || Screen.height != lastH)
        {
            Apply();
            lastW = Screen.width;
            lastH = Screen.height;
        }
    }

    private void CacheOriginalSize()
    {
        if (hasCachedSize) return; 

        originalWidth = rect.rect.width;
        originalHeight = rect.rect.height;

        if (originalWidth > 0f && originalHeight > 0f)
            hasCachedSize = true;
    }

    [ContextMenu("Apply")]
    public void Apply()
    {
        float W = originalWidth;
        float H = originalHeight;

        if (aspectRatio.x <= 0f || aspectRatio.y <= 0f || W <= 0f || H <= 0f)
            return;

        float targetRatio = aspectRatio.x / aspectRatio.y;

        float hFromWidth = W / targetRatio;

        float newWidth, newHeight;

        if (hFromWidth <= H)
        {
            newWidth = W;
            newHeight = hFromWidth;
        }
        else
        {
            newHeight = H;
            newWidth = H * targetRatio;
        }

        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newHeight);
    }

    public void ResetCachedSize()
    {
        hasCachedSize = false;
        CacheOriginalSize();
        Apply();
    }
}