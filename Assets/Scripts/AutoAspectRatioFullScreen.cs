using UnityEngine;

public class AutoAspectRatioFullScreen : BaseAutoAspectRatio
{
    protected override void CacheOriginalSize()
    {
        if (hasCachedSize) return;
        if (rect != null)
        {
            RectTransform parent = rect.parent as RectTransform;
            if (parent != null)
            {
                originalWidth = parent.rect.width;
                originalHeight = parent.rect.height;
            }
            else
            {
                originalWidth = Screen.width;
                originalHeight = Screen.height;
            }

            hasCachedSize = originalWidth > 0f && originalHeight > 0f;
        }
    }

    protected override void OnScreenSizeChanged()
    {
        // Force recompute from parent/screen then apply
        hasCachedSize = false;
        CacheOriginalSize();
        Apply();
    }
}