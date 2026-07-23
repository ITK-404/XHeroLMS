using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tự động chọn vị trí (Top/Bottom/Left/Right) quanh 1 điểm neo (anchor)
/// sao cho label không bị tràn ra ngoài Canvas, đồng thời tự resize rect
/// theo đúng hướng đó (rect giãn ra xa điểm neo, cạnh dính anchor giữ cố định)
/// bằng cách set Pivot phù hợp — kết hợp với ContentSizeFitter có sẵn trên label.
///
/// Cách dùng:
///   1. Gắn script này lên GameObject label (có RectTransform + TMP_Text con
///      đã gắn ContentSizeFitter để tự co giãn theo nội dung).
///   2. Set anchorPoint = RectTransform của điểm muốn label bám vào.
///   3. Gọi placer.SetText("nội dung dài ngắn tuỳ ý") mỗi khi cần đổi text.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class LabelAutoPlacer : MonoBehaviour
{
    public enum Direction { Top, Bottom, Left, Right }

    [Header("References")]
    [Tooltip("Điểm neo mà label sẽ bám vào (VD: node trên UI)")]
    [SerializeField] private RectTransform anchorPoint;

    [Tooltip("Canvas gốc, dùng làm biên để check đủ chỗ hay không")]
    [SerializeField] private Canvas canvas;

    [Tooltip("Text hiển thị (TMP), cần có ContentSizeFitter trên cùng RectTransform hoặc rect cha")]
    [SerializeField] private TMP_Text label;

    [Tooltip("RectTransform sẽ bị di chuyển / đổi pivot (mặc định = chính GameObject này)")]
    [SerializeField] private RectTransform selfRect;

    [Header("Placement settings")]
    [Tooltip("Thứ tự ưu tiên khi tìm hướng đủ chỗ")]
    [SerializeField]
    private Direction[] priorityOrder =
    {
        Direction.Top, Direction.Bottom, Direction.Left, Direction.Right
    };

    [Tooltip("Khoảng cách (px, theo local space của canvas) giữa label và điểm neo")]
    [SerializeField] private float gap = 8f;

    [Tooltip("Padding tối thiểu cách mép canvas")]
    [SerializeField] private float edgePadding = 4f;

    [Header("Follow settings")]
    [Tooltip("Bật để label tự bám theo anchorPoint mỗi frame (LateUpdate). " +
             "Dùng khi anchor di chuyển liên tục (theo nhân vật, kéo thả, camera pan...).")]
    [SerializeField] private bool followAnchorEveryFrame = false;

    [Tooltip("Ngưỡng di chuyển tối thiểu (đơn vị canvas local) trước khi tính lại " +
             "hướng/vị trí. 0 = luôn tính lại mỗi frame (chính xác nhất, tốn nhất). " +
             "Tăng lên nếu cần tối ưu performance với nhiều label cùng lúc.")]
    [SerializeField] private float directionRecheckThreshold = 0f;

    private RectTransform canvasRect;
    private Vector2 cachedSize;
    private bool hasCachedSize;
    private Vector2 lastCheckedAnchorLocal;
    private bool hasLastChecked;

    private void Awake()
    {
        if (selfRect == null) selfRect = GetComponent<RectTransform>();
        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (canvas != null) canvasRect = canvas.GetComponent<RectTransform>();
    }

    private void LateUpdate()
    {
        if (!followAnchorEveryFrame) return;
        if (!hasCachedSize) return; // chưa từng SetText/Reposition lần nào thì chưa có size để dùng
        if (anchorPoint == null || canvasRect == null || selfRect == null) return;

        RecalculatePositionOnly();
    }

    /// <summary>Đổi nội dung text và tự động align + resize lại (rebuild layout đầy đủ).</summary>
    public void SetText(string text)
    {
        if (label != null) label.text = text;
        Reposition();
    }

    /// <summary>
    /// Tính lại toàn bộ: rebuild layout để đo size mới theo text, rồi chọn
    /// hướng + set vị trí. Gọi hàm này khi text thay đổi. Nếu chỉ cần bám
    /// theo anchor di chuyển (size không đổi) thì dùng followAnchorEveryFrame
    /// thay vì gọi hàm này liên tục — sẽ đỡ tốn hơn nhiều.
    /// </summary>
    [ContextMenu("Reposition Now")]
    public void Reposition()
    {
        if (anchorPoint == null || canvasRect == null || selfRect == null) return;

        // Ép layout rebuild để ContentSizeFitter tính lại size theo text mới
        // (rebuild theo rect cha của label trước, rồi tới selfRect nếu khác nhau)
        if (label != null)
            LayoutRebuilder.ForceRebuildLayoutImmediate(label.rectTransform);
        LayoutRebuilder.ForceRebuildLayoutImmediate(selfRect);

        cachedSize = selfRect.rect.size;
        hasCachedSize = true;

        RecalculatePositionOnly();
    }

    /// <summary>
    /// Chọn hướng + set vị trí dựa trên cachedSize hiện có (KHÔNG rebuild layout).
    /// Rẻ, an toàn để gọi mỗi frame trong LateUpdate.
    /// </summary>
    private void RecalculatePositionOnly()
    {
        Vector2 anchorLocal = WorldToCanvasLocal(anchorPoint.position);

        if (directionRecheckThreshold > 0f && hasLastChecked)
        {
            float sqrDist = (anchorLocal - lastCheckedAnchorLocal).sqrMagnitude;
            if (sqrDist < directionRecheckThreshold * directionRecheckThreshold)
                return; // anchor chưa di chuyển đủ nhiều, giữ nguyên hướng/vị trí hiện tại
        }
        lastCheckedAnchorLocal = anchorLocal;
        hasLastChecked = true;

        Rect bounds = canvasRect.rect;

        Direction chosen = Direction.Top;
        bool found = false;
        float bestOverlapArea = float.NegativeInfinity;
        Direction bestFallback = priorityOrder.Length > 0 ? priorityOrder[0] : Direction.Top;

        foreach (var dir in priorityOrder)
        {
            Rect candidate = GetCandidateRect(dir, anchorLocal, cachedSize);

            if (FitsInside(candidate, bounds, edgePadding))
            {
                chosen = dir;
                found = true;
                break;
            }

            float overlap = OverlapArea(candidate, bounds);
            if (overlap > bestOverlapArea)
            {
                bestOverlapArea = overlap;
                bestFallback = dir;
            }
        }

        if (!found) chosen = bestFallback;

        ApplyDirection(chosen, anchorLocal);
    }

    private Rect GetCandidateRect(Direction dir, Vector2 anchorLocal, Vector2 size)
    {
        switch (dir)
        {
            case Direction.Top:
                return new Rect(anchorLocal.x - size.x * 0.5f, anchorLocal.y + gap, size.x, size.y);
            case Direction.Bottom:
                return new Rect(anchorLocal.x - size.x * 0.5f, anchorLocal.y - gap - size.y, size.x, size.y);
            case Direction.Left:
                return new Rect(anchorLocal.x - gap - size.x, anchorLocal.y - size.y * 0.5f, size.x, size.y);
            case Direction.Right:
                return new Rect(anchorLocal.x + gap, anchorLocal.y - size.y * 0.5f, size.x, size.y);
            default:
                throw new ArgumentOutOfRangeException(nameof(dir));
        }
    }

    private static bool FitsInside(Rect candidate, Rect bounds, float padding)
    {
        return candidate.xMin >= bounds.xMin + padding
            && candidate.xMax <= bounds.xMax - padding
            && candidate.yMin >= bounds.yMin + padding
            && candidate.yMax <= bounds.yMax - padding;
    }

    private static float OverlapArea(Rect a, Rect b)
    {
        float xOverlap = Mathf.Max(0f, Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin));
        float yOverlap = Mathf.Max(0f, Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin));
        return xOverlap * yOverlap;
    }

    /// <summary>
    /// Set pivot sao cho cạnh dính điểm neo cố định, cạnh còn lại tự do giãn
    /// khi ContentSizeFitter đổi size sau này. Vị trí cuối cùng được gán qua
    /// world position (selfRect.position) thay vì anchoredPosition, để KHÔNG
    /// phụ thuộc vào selfRect đang nằm trong parent nào / anchor gì — tránh
    /// lỗi lệch vị trí khi parent thật sự của selfRect khác canvasRect.
    /// </summary>
    private void ApplyDirection(Direction dir, Vector2 anchorLocal)
    {
        Vector2 pivot;
        Vector2 targetCanvasLocalPos; // vị trí mong muốn của pivot, tính trong local space của canvasRect

        switch (dir)
        {
            case Direction.Top:
                pivot = new Vector2(0.5f, 0f);
                targetCanvasLocalPos = anchorLocal + new Vector2(0f, gap);
                break;
            case Direction.Bottom:
                pivot = new Vector2(0.5f, 1f);
                targetCanvasLocalPos = anchorLocal - new Vector2(0f, gap);
                break;
            case Direction.Left:
                pivot = new Vector2(1f, 0.5f);
                targetCanvasLocalPos = anchorLocal - new Vector2(gap, 0f);
                break;
            case Direction.Right:
            default:
                pivot = new Vector2(0f, 0.5f);
                targetCanvasLocalPos = anchorLocal + new Vector2(gap, 0f);
                break;
        }

        // Đổi pivot trước (giữ nguyên rect size, chỉ đổi origin) để bước sau
        // set world position là vị trí của đúng pivot mới này.
        selfRect.pivot = pivot;

        // canvas-local point -> world position: dùng chung hệ toạ độ ổn định
        // (canvasRect), không quan tâm selfRect thật sự đang ở parent nào.
        Vector3 worldPos = canvasRect.TransformPoint(targetCanvasLocalPos);
        selfRect.position = worldPos;

        // Lưu ý: nếu parent thật của selfRect bị resize/reposition ở các frame
        // sau (do layout khác thay đổi) mà không gọi lại Reposition(), vị trí
        // world đã set này sẽ KHÔNG tự cập nhật theo (vì không còn phụ thuộc
        // anchoredPosition). Nếu anchor điểm neo có thể di chuyển liên tục,
        // hãy gọi Reposition() mỗi khi anchorPoint đổi vị trí (LateUpdate,
        // hoặc event khi anchor di chuyển) thay vì chỉ gọi khi đổi text.
    }

    private Vector2 WorldToCanvasLocal(Vector3 worldPos)
    {
        var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPos, cam, out Vector2 localPos);
        return localPos;
    }
}