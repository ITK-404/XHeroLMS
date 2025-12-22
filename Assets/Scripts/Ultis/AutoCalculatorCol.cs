using System;
using UnityEngine;
using UnityEngine.UI;

public class AutoCalculatorCol : MonoBehaviour
{
    [SerializeField] private RectTransform container;
    [SerializeField] private GridLayoutGroup gridLayout;

    private void OnEnable()
    {
        Calculator();
    }

    private void Calculator()
    {
        if (container == null || gridLayout == null) return;

        // 1. Lấy chiều cao thực tế khả dụng của container (đã trừ padding trên dưới)
        float totalAvailableHeight = container.rect.height - gridLayout.padding.top - gridLayout.padding.bottom;

        // 2. Lấy kích thước cell và khoảng cách spacing theo trục dọc (Y)
        float cellHeight = gridLayout.cellSize.y;
        float spacingHeight = gridLayout.spacing.y;

        // 3. Tính toán số hàng tối đa
        // Công thức tương tự: (Tổng chiều cao + Spacing) / (Chiều cao Cell + Spacing)
        int maxRows = Mathf.FloorToInt((totalAvailableHeight + spacingHeight) / (cellHeight + spacingHeight));

        // Đảm bảo tối thiểu có 1 hàng
        maxRows = Mathf.Max(1, maxRows);

        // 4. Cập nhật Grid Layout 
        // Nếu bạn muốn giới hạn số hàng, bạn chỉnh Constraint sang FixedRowCount
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedRowCount;
        gridLayout.constraintCount = maxRows;

        Debug.Log($"Số hàng tối đa có thể hiển thị: {maxRows}");
    }
}
