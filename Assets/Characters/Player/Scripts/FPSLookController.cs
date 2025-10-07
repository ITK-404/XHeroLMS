using UnityEngine;

public class FPSLookController : MonoBehaviour
{
    [Header("Refs")]
    public Transform pitchPivot;   // gán PitchPivot
    public Camera cam;             // gán FP_Camera (MainCamera)

    [Header("Mouse")]
    public float sensX = 200f;
    public float sensY = 200f;
    public float pitchMin = -89f;
    public float pitchMax =  89f;

    [Header("Smoothing")]
    public bool smooth = true;
    public float yawLerp = 18f;
    public float pitchLerp = 18f;

    [Header("FOV")]
    public float baseFov   = 75f;
    public float sprintFov = 82f;
    public float adsFov    = 62f;
    public float fovLerp   = 10f;

    [Header("Eye Height")]
    public float eyeY       = 1.62f; // đứng
    public float crouchEyeY = 1.10f; // ngồi
    public float adsEyeYOffset = 0f; // nếu muốn hạ mắt khi ADS
    public float eyeLerp = 10f;
    public bool invertX = false;
    public bool invertY = false;

    // state điều khiển từ script khác (motor/weapon)
    [HideInInspector] public bool IsSprinting;
    [HideInInspector] public bool IsCrouching;
    [HideInInspector] public bool IsADS;

    float yaw;
    float pitch;
    Vector2 recoil; // (x=pitchKick, y=yawKick)

    void Start()
    {
        if (cam) cam.transform.localRotation = Quaternion.identity; 
if (pitchPivot) pitchPivot.localRotation = Quaternion.identity; 

        if (!cam) cam = Camera.main;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        yaw = transform.eulerAngles.y;
        pitch = 0f;

        // đặt ngay tư thế ban đầu
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (pitchPivot) pitchPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        if (pitchPivot)
        {
            var lp = pitchPivot.localPosition;
            lp.y = eyeY;
            pitchPivot.localPosition = lp;
        }
        if (cam) cam.fieldOfView = baseFov;
    }

    void Update()
    {
        float mx = Input.GetAxisRaw("Mouse X") * sensX * 0.01f;
        float my = Input.GetAxisRaw("Mouse Y") * sensY * 0.01f;

        if (invertX) mx = -mx;                 
        if (invertY) my = -my;

        yaw   += mx + recoil.y;
        pitch -= my + recoil.x;                
        recoil = Vector2.Lerp(recoil, Vector2.zero, Time.deltaTime * 10f);
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        // yaw trên YawPivot
        var yawQ = Quaternion.Euler(0f, yaw, 0f);
        transform.rotation = smooth
            ? Quaternion.Slerp(transform.rotation, yawQ, Time.deltaTime * yawLerp)
            : yawQ;

        // pitch trên PitchPivot
        if (pitchPivot)
        {
            var pitchQ = Quaternion.Euler(pitch, 0f, 0f);
            pitchPivot.localRotation = smooth
                ? Quaternion.Slerp(pitchPivot.localRotation, pitchQ, Time.deltaTime * pitchLerp)
                : pitchQ;

            var e = pitchPivot.localEulerAngles;
            pitchPivot.localRotation = Quaternion.Euler(e.x, 0f, 0f);
        }

        if (cam)
        {
            var ce = cam.transform.localEulerAngles;
            cam.transform.localRotation = Quaternion.Euler(ce.x, 0f, 0f);

            float wantFov = IsADS ? adsFov : (IsSprinting ? sprintFov : baseFov);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, wantFov, Time.deltaTime * fovLerp);
            if (cam.nearClipPlane > 0.05f) cam.nearClipPlane = 0.05f;
        }

        // Eye height
        if (pitchPivot)
        {
            float wantY = IsCrouching ? crouchEyeY : eyeY;
            if (IsADS) wantY += adsEyeYOffset;
            var lp = pitchPivot.localPosition;
            lp.y = Mathf.Lerp(lp.y, wantY, Time.deltaTime * eyeLerp);
            pitchPivot.localPosition = lp;
        }
    }

    // ======= API tiện dùng từ motor/weapon/UI =======
    public void AddRecoil(Vector2 kick) => recoil += kick; // ví dụ (-2.5f, Random.Range(-0.6f,0.6f))
    public void SetSensitivity(float sx, float sy) { sensX = sx; sensY = sy; }
    public void ResetView(float yawDeg = float.NaN, float pitchDeg = 0f)
    {
        if (!float.IsNaN(yawDeg)) yaw = yawDeg; else yaw = transform.eulerAngles.y;
        pitch = Mathf.Clamp(pitchDeg, pitchMin, pitchMax);
        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (pitchPivot) pitchPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
    public static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
