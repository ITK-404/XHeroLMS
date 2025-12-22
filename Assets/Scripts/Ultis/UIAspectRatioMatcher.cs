using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasScaler))]
public class UIAspectRatioMatcher : MonoBehaviour
{
    private void Awake()
    {
        CanvasScaler scaler = GetComponent<CanvasScaler>();
        
        // Tính tỉ lệ màn hình hiện tại
        float aspectRatio = (float)Screen.width / Screen.height;

        // Tỉ lệ thiết kế (Reference Resolution), ví dụ 1920x1080 là ~1.77
        float targetRatio = scaler.referenceResolution.x / scaler.referenceResolution.y;

        if (aspectRatio < targetRatio)
        {
            // Màn hình "vuông" hơn thiết kế (như iPad)
            // Ưu tiên giữ chiều rộng để không bị mất UI hai bên
            scaler.matchWidthOrHeight = 0f;
        }
        else
        {
            // Màn hình "dài" hơn thiết kế (như iPhone X, Sony Xperia)
            // Ưu tiên giữ chiều cao để không mất UI trên dưới
            scaler.matchWidthOrHeight = 0.5f;
        }
        
    }
}