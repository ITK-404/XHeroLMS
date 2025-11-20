using UnityEngine;

public class ModelRotator : MonoBehaviour
{
    public Transform model;                  // gán model 3D ở Inspector
    public float sensitivity = 0.3f;         // độ nhạy xoay
    public float minPitch = -80f;            // giới hạn nhìn lên
    public float maxPitch = 80f;             // giới hạn nhìn xuống

    float yaw = 0f;
    float pitch = 0f;
    Vector3 lastMousePos;
    bool dragging = false;

    private Quaternion originalQuaternion;
    
    void Start()
    {
        if (model)
        {
            Vector3 e = model.rotation.eulerAngles;
            yaw = e.y;
            pitch = e.x;

            originalQuaternion = model.rotation;
        }
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            dragging = true;
            lastMousePos = Input.mousePosition;
        }

        if (Input.GetMouseButtonUp(0))
            dragging = false;

        if (dragging && model != null)
        {
            Vector3 delta = (Vector3)Input.mousePosition - lastMousePos;
            yaw += delta.x * sensitivity;
            pitch -= delta.y * sensitivity; // trừ để kéo lên thì nhìn lên
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            model.rotation = Quaternion.Euler(pitch, yaw, 0f);
            lastMousePos = Input.mousePosition;
        }
        else
        {
            model.rotation = Quaternion.Lerp(model.rotation,originalQuaternion,Time.deltaTime * 5);
        }
    }
}