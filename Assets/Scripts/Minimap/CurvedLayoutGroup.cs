using UnityEngine;
using UnityEngine.UI;

public class CurvedLayoutGroup : LayoutGroup
{
    public float radius = 200f;
    public float angleStep = 20f;

    // Hàm bắt buộc khi kế thừa LayoutGroup
    public override void CalculateLayoutInputHorizontal() => base.SetLayoutInputForAxis(0, 0, 0, 0);
    public override void CalculateLayoutInputVertical() { }

    public override void SetLayoutHorizontal() => UpdateLayout();
    public override void SetLayoutVertical() => UpdateLayout();

    void UpdateLayout()
    {
        for (int i = 0; i < rectChildren.Count; i++)
        {
            RectTransform child = rectChildren[i];
            
            // Tính toán góc dựa trên index
            float angle = i * angleStep * Mathf.Deg2Rad;
            
            // Tính toán vị trí theo công thức hình tròn
            float x = Mathf.Sin(angle) * radius;
            float y = Mathf.Cos(angle) * radius;

            // Áp dụng vào child
            child.localPosition = new Vector3(x, y, 0);
            
            // Tùy chỉnh rotation để xoay mặt vào tâm
            child.localRotation = Quaternion.Euler(0, 0, -i * angleStep);
        }
    }
}