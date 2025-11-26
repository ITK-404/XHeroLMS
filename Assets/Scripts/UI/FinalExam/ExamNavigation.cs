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

        CenterOnItem(target.GetComponent<RectTransform>());
    }

    public void ClampTarget(int index)
    {
        var target = spawnContainer.GetChild(index);
        if (target == null)
        {
            Debug.Log("Navigation không tồn tại index này");
            return;
        }

        ClampItemByIndex(target.GetComponent<RectTransform>());
    }

    public void CenterOnItem(RectTransform target)
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
        if (tween != null)
        {
            tween.Kill();
        }

        tween = DOTween.To(
            () => scrollRect.horizontalNormalizedPosition,
            x => scrollRect.horizontalNormalizedPosition = x,
            normalized,
            0.3f
        ).SetEase(Ease.OutCubic);
    }

    public void ClampItemByIndex(RectTransform target)
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
            return;
        }

        // 6. Clamp để không kéo vượt quá content
        desiredPos = Mathf.Clamp(desiredPos, 0, contentWidth - viewportWidth);

        // 7. Convert sang normalized
        float normalized = desiredPos / (contentWidth - viewportWidth);

        if (tween != null)
            tween.Kill();

        tween = DOTween.To(
            () => scrollRect.horizontalNormalizedPosition,
            x => scrollRect.horizontalNormalizedPosition = x,
            normalized,
            0.25f
        ).SetEase(Ease.OutCubic);
    }

    public void Show() => container.gameObject.SetActive(true);
    public void Hide() => container.gameObject.SetActive(false);
}