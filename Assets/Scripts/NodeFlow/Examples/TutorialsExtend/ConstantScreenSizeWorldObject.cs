using UnityEngine;

public class ConstantScreenSizeWorldObject : MonoBehaviour
{
    [Tooltip("Kích thước mong muốn tính theo % chiều cao màn hình")]
    private const float screenSizePercent = 0.25f; // 10% chiều cao viewport

    [Tooltip("Kích thước gốc của mesh (world units) khi scale = 1")]
    public float baseObjectSize = 1f;

    private static Camera cam;

    void Start()
    {
        if (cam == null)
        {
            cam = PlayerCamera.Instance.mainCamera;
        }
    }

    void LateUpdate()
    {
        if (cam == null) return;

        float distance = Vector3.Distance(cam.transform.position, transform.position);

        float scale;
        if (cam.orthographic)
        {
            // Orthographic: kích thước không phụ thuộc distance
            scale = (cam.orthographicSize * 2f * screenSizePercent) / baseObjectSize;
        }
        else
        {
            // Perspective: bù trừ theo FOV và distance
            float fovRad = cam.fieldOfView * Mathf.Deg2Rad;
            float frustumHeight = 2f * distance * Mathf.Tan(fovRad * 0.5f);
            scale = (frustumHeight * screenSizePercent) / baseObjectSize;
        }

        transform.localScale = Vector3.one * scale;
        
    }
}