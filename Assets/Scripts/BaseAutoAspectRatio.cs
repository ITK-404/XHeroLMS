using UnityEngine;

public abstract class BaseAutoAspectRatio : MonoBehaviour
{
    protected RectTransform rect;

    [Header("Target aspect (x:y) e.g. 16:9")]
    public Vector2 aspectRatio = new Vector2(16, 9);

    [Header("Apply on start (safer for layouts)")]
    public bool applyOnStart = true;

    [Header("If true, size will update when screen size changes (cheap check)")]
    public bool updateOnScreenChange;

    protected float originalWidth, originalHeight;
    protected int lastW, lastH;
    protected bool hasCachedSize;

    protected virtual void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    // protected virtual void OnEnable()
    // {
    //     if (hasCachedSize && applyOnStart) Apply();
    // }

    protected virtual void Start()
    {
        CacheOriginalSize();
        if (applyOnStart) Apply();
        lastW = Screen.width;
        lastH = Screen.height;
    }

    protected virtual void Update()
    {
        if (!updateOnScreenChange) return;
        if (Screen.width != lastW || Screen.height != lastH)
        {
            OnScreenSizeChanged();
            lastW = Screen.width;
            lastH = Screen.height;
        }
    }

    // Called to (re)compute originalWidth/originalHeight. Default uses this rect's rect.
    protected virtual void CacheOriginalSize()
    {
        if (hasCachedSize) return;
        if (rect != null)
        {
            originalWidth = rect.rect.width;
            originalHeight = rect.rect.height;
            if (originalWidth > 0f && originalHeight > 0f) hasCachedSize = true;
        }
    }

    // Called when the screen size changed and updateOnScreenChange is true.
    // Default behavior: just Apply() -- subclasses can override to recompute original size first.
    protected virtual void OnScreenSizeChanged()
    {
        Apply();
    }

    [ContextMenu("Apply")]
    public virtual void Apply()
    {
        float w = originalWidth;
        float h = originalHeight;
        if (aspectRatio.x <= 0f || aspectRatio.y <= 0f || w <= 0f || h <= 0f) return;
        float targetRatio = aspectRatio.x / aspectRatio.y;
        float hFromWidth = w / targetRatio;
        float newWidth, newHeight;
        if (hFromWidth <= h)
        {
            newWidth = w;
            newHeight = hFromWidth;
        }
        else
        {
            newHeight = h;
            newWidth = h * targetRatio;
        }

        if (rect != null)
        {
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, newWidth);
            rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, newHeight);
        }
    }

    public virtual void ResetCachedSize()
    {
        hasCachedSize = false;
        CacheOriginalSize();
        Apply();
    }
}