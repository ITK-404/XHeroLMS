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

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
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
            lastW = Screen.width;
            lastH = Screen.height;

            CacheOriginalSize(); // nếu layout thay đổi theo resolution
            Apply();
        }
    }

    private void CacheOriginalSize()
    {
        // Trong nhiều case layout chưa cập nhật đúng ở Awake, nên cache ở Start/Update sẽ ổn hơn.
        // sizeDelta là kích thước "logic" của RectTransform (phù hợp cho set lại).
        originalWidth = rect.rect.width;
        originalHeight = rect.rect.height;
    }

    [ContextMenu("Apply")]
    public void Apply()
    {
        float W = originalWidth;
        float H = originalHeight;

        // Guard
        if (aspectRatio.x <= 0f || aspectRatio.y <= 0f || W <= 0f || H <= 0f)
            return;

        float targetRatio = aspectRatio.x / aspectRatio.y;

        // Fit inside: thử theo width
        float hFromWidth = W / targetRatio;

        float newWidth, newHeight;

        if (hFromWidth <= H)
        {
            // giữ width, giảm height
            newWidth = W;
            newHeight = hFromWidth;
        }
        else
        {
            // giữ height, giảm width
            newHeight = H;
            newWidth = H * targetRatio;
        }

        // Apply: giữ nguyên anchor/pivot, set sizeDelta theo kích thước mới
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newHeight);
    }
    
}
