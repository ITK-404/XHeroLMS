using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ExamNavigation : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private Transform spawnContainer;
    [SerializeField] private Transform container;
    [SerializeField] private ExamNavigationElement examNavigationElementPrefab;
    
    private Tween tween;

    private void Awake()
    {
        EventHub.OnExamIndexClampChanged += CenterItemByIndex;
        EventHub.OnExamIndexCenterChanged += ClampTarget;
    }

    private void OnDestroy()
    {
        EventHub.OnExamIndexClampChanged -= CenterItemByIndex;
        EventHub.OnExamIndexCenterChanged -= ClampTarget;
    }

    public void CenterItemByIndex(int index)
    {
        var target = spawnContainer.GetChild(index);
        if(target == null)
        {
            Debug.Log("Navigation không tồn tại index này");
            return;
        }
        moveTween?.Kill();
        moveTween = scrollRect.CenterOnItem(target.GetComponent<RectTransform>());
    }

    private Tween moveTween;
    public void ClampTarget(int index)
    {
        var target = spawnContainer.GetChild(index);
        if (target == null)
        {
            Debug.Log("Navigation không tồn tại index này");
            return;
        }
        moveTween?.Kill();
        moveTween = scrollRect.ClampItemByIndex(target.GetComponent<RectTransform>());
    }

    public void Show() => container.gameObject.SetActive(true);
    public void Hide() => container.gameObject.SetActive(false);

    public void CenterOnItem(RectTransform rectTransform)
    {
        moveTween?.Kill();
        moveTween = scrollRect.CenterOnItem(rectTransform);
    }
}

public static class ScrollRectSupport
{
     public static Tween CenterOnItem(this ScrollRect scrollRect,RectTransform target)
    {
        Debug.Log("Set center of item");
        var content = scrollRect.content;
        var viewport = scrollRect.viewport;

        // 1. Lấy vị trí target trong content space
        var targetBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(content, target);

        // 2. Lấy kích thước
        float contentWidth = content.rect.width;
        float viewportWidth = viewport.rect.width;

        // 3. Vị trí giữa của target
        float targetCenter = targetBounds.center.x;

        // 4. Tính vị trí mong muốn để target nằm giữa viewport
        float desiredPos = targetCenter - viewportWidth / 2f;

        // 5. Clamp để không trượt ra ngoài content
        desiredPos = Mathf.Clamp(desiredPos, 0, contentWidth - viewportWidth);

        // 6. Đổi sang normalized position
        float normalized = desiredPos / (contentWidth - viewportWidth);

        var tween = DOTween.To(
            () => scrollRect.horizontalNormalizedPosition,
            x => scrollRect.horizontalNormalizedPosition = x,
            normalized,
            0.3f
        ).SetEase(Ease.OutCubic);

        return tween;
    }

    public static Tween ClampItemByIndex(this ScrollRect scrollRect,RectTransform target)
    {
        var content = scrollRect.content;
        var viewport = scrollRect.viewport;

        // 1. Bounds của item trong toạ độ content
        var bounds = RectTransformUtility.CalculateRelativeRectTransformBounds(content, target);

        float contentWidth = content.rect.width;
        float viewportWidth = viewport.rect.width;

        // 2. Lấy vị trí item trong content space
        float itemLeft = bounds.min.x;              // cạnh trái của item
        float itemRight = bounds.max.x;             // cạnh phải của item

        // 3. Lấy vùng nhìn hiện tại của viewport trong content space
        float viewportLeft = scrollRect.horizontalNormalizedPosition * (contentWidth - viewportWidth);
        float viewportRight = viewportLeft + viewportWidth;

        float desiredPos = viewportLeft;

        // 4. Kiểm tra nếu item bị nằm ngoài bên TRÁI viewport
        if (itemLeft < viewportLeft)
        {
            desiredPos = itemLeft;     // đẩy viewport sang trái để item lọt vào
        }
        // 5. Kiểm tra nếu item bị nằm ngoài bên PHẢI viewport
        else if (itemRight > viewportRight)
        {
            desiredPos = itemRight - viewportWidth; // đẩy viewport sang phải để item lọt vào
        }
        else
        {
            // Item đã nằm trong viewport -> không cần scroll
            return null;
        }

        // 6. Clamp để không kéo vượt quá content
        desiredPos = Mathf.Clamp(desiredPos, 0, contentWidth - viewportWidth);

        // 7. Convert sang normalized
        float normalized = desiredPos / (contentWidth - viewportWidth);

        var tween = DOTween.To(
            () => scrollRect.horizontalNormalizedPosition,
            x => scrollRect.horizontalNormalizedPosition = x,
            normalized,
            0.25f
        ).SetEase(Ease.OutCubic);

        return tween;
    }

    // New: force center without clamping — returns a Tween that moves content so target is centered even if that requires moving content beyond its normal scroll bounds
    public static Tween ForceCenterOnItem(this ScrollRect scrollRect, RectTransform target, float duration = 0.25f, bool preserveMovementType = true)
    {
        var content = scrollRect.content;
        var viewport = scrollRect.viewport;
        if (content == null || viewport == null || target == null) return null;

        // Ensure layout/Rect sizes are up-to-date before calculating bounds
        Canvas.ForceUpdateCanvases();

        // target bounds in content local space
        var targetBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(content, target);

        float contentWidth = content.rect.width;
        float viewportWidth = viewport.rect.width;

        // If there's no scrollable width, nothing to do
        if (Mathf.Approximately(contentWidth, 0f) || Mathf.Approximately(viewportWidth, 0f)) return null;

        float targetCenter = targetBounds.center.x;

        // desired left position in content space so that target is centered
        float desiredPos = targetCenter - viewportWidth / 2f;

        // current viewport left in content space
        float currentViewportLeft = 0f;
        if (!Mathf.Approximately(contentWidth - viewportWidth, 0f))
            currentViewportLeft = scrollRect.horizontalNormalizedPosition * (contentWidth - viewportWidth);

        float delta = desiredPos - currentViewportLeft;

        // Compute target anchoredPosition.x by shifting content by -delta
        float currentAnchoredX = content.anchoredPosition.x;
        float targetAnchoredX = currentAnchoredX - delta;

        // Temporarily set movement type to Unrestricted so ScrollRect won't clamp/override anchoredPosition during the tween
        var prevMovement = scrollRect.movementType;
        scrollRect.movementType = ScrollRect.MovementType.Unrestricted;

        Tween tween = null;
        if (duration <= 0f)
        {
            // instant
            Vector2 a = content.anchoredPosition;
            a.x = targetAnchoredX;
            content.anchoredPosition = a;

            if (preserveMovementType)
                scrollRect.movementType = prevMovement;

            return null;
        }

        // animate anchoredPosition.x to targetAnchoredX
        tween = DOTween.To(
            () => content.anchoredPosition.x,
            x => {
                var aa = content.anchoredPosition;
                aa.x = x;
                content.anchoredPosition = aa;
            },
            targetAnchoredX,
            duration
        ).SetEase(Ease.OutSine);

        if (preserveMovementType)
        {
            tween.OnComplete(() => scrollRect.movementType = prevMovement);
            tween.OnKill(() => { if (scrollRect != null) scrollRect.movementType = prevMovement; });
        }

        return tween;
    }
}