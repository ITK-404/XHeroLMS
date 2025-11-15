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

    public void Show() => container.gameObject.SetActive(true);
    public void Hide() => container.gameObject.SetActive(false);
}