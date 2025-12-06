using UnityEngine;

public class MouseScrollRotate : MonoBehaviour
{
    [Header("Target quay (nếu để trống sẽ dùng chính object này)")]
    public Transform target;

    [Header("Cấu hình")]
    public float rotationSpeed = 10f;
    public float maxAngle = 45f; // giới hạn ±45 độ

    private float currentXRotation;

    private void Start()
    {
        if (target == null)
            target = transform;

        // Lấy giá trị xoay ban đầu
        currentXRotation = target.localEulerAngles.x;
        currentXRotation = NormalizeAngle(currentXRotation);
    }

    private void Update()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");

        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentXRotation -= scroll * rotationSpeed;
            currentXRotation = Mathf.Clamp(currentXRotation, -maxAngle, maxAngle);

            target.localEulerAngles = new Vector3(currentXRotation, 
                                                  target.localEulerAngles.y, 
                                                  target.localEulerAngles.z);
        }
    }

    // Chống lỗi góc Euler vượt quá 360°
    private float NormalizeAngle(float angle)
    {
        if (angle > 180f) angle -= 360f;
        return angle;
    }
}
