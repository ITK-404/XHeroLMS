using UnityEngine;

public class UIPositionClamper : MonoBehaviour
{
    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    private void Update()
    {
        ClampWithAnchoredPosition();
    }

    void ClampWithAnchoredPosition()
    {
        Vector2 anchoredPos = rectTransform.anchoredPosition;

        // Lấy kích thước của parent (Canvas)
        RectTransform parentRect = rectTransform.parent as RectTransform;
        Vector2 parentSize = parentRect.rect.size;
        Vector2 size = rectTransform.rect.size;

        // Tính giới hạn dựa trên pivot và anchor
        float minX = -parentSize.x / 2f + size.x / 2f;
        float maxX = parentSize.x / 2f - size.x / 2f;
        float minY = -parentSize.y / 2f + size.y / 2f;
        float maxY = parentSize.y / 2f - size.y / 2f;

        anchoredPos.x = Mathf.Clamp(anchoredPos.x, minX, maxX);
        anchoredPos.y = Mathf.Clamp(anchoredPos.y, minY, maxY);

        rectTransform.anchoredPosition = anchoredPos;
    }
}
