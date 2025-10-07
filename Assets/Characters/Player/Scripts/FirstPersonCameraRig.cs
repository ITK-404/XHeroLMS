using UnityEngine;

public class FirstPersonCameraRig : MonoBehaviour
{
    public Transform yawPivot;
    public Transform pitchPivot;
    public Transform orientation; // thường = yawPivot
    public Camera cam;

    [Header("Mouse")]
    public float sensX = 200f, sensY = 200f;
    public float pitchMin = -89f, pitchMax = 89f;
    [Header("Smoothing")]
    public float yawLerp = 18f, pitchLerp = 18f;
    public bool smooth = true;

    [Header("FOV & Eye")]
    public float baseFov = 75f, sprintFov = 82f, fovLerp = 10f;
    public float eyeY = 1.62f, crouchEyeY = 1.1f, eyeLerp = 10f;

    float yaw, pitch;
    float targetFov;
    float targetEyeY;
    Vector2 recoil; // cộng dồn từ weapon

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
        if (!cam) cam = Camera.main;
        if (!orientation) orientation = yawPivot;

        yaw = yawPivot.eulerAngles.y; pitch = 0f;
        targetFov = baseFov; targetEyeY = eyeY;
        ApplyInstant();
    }

    void Update()
    {
        float mx = Input.GetAxisRaw("Mouse X") * sensX * 0.01f;
        float my = Input.GetAxisRaw("Mouse Y") * sensY * 0.01f;

        yaw   += mx + recoil.y;
        pitch -= my + recoil.x;
        recoil = Vector2.Lerp(recoil, Vector2.zero, Time.deltaTime * 10f);
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        var yawQ = Quaternion.Euler(0, yaw, 0);
        var pitchQ = Quaternion.Euler(pitch, 0, 0);

        yawPivot.rotation = smooth ? Quaternion.Slerp(yawPivot.rotation, yawQ, Time.deltaTime * yawLerp) : yawQ;
        pitchPivot.localRotation = smooth ? Quaternion.Slerp(pitchPivot.localRotation, pitchQ, Time.deltaTime * pitchLerp) : pitchQ;

        // FOV sprint (ví dụ)
        bool sprint = Input.GetKey(KeyCode.LeftShift);
        targetFov = Mathf.Lerp(targetFov, sprint ? sprintFov : baseFov, Time.deltaTime * fovLerp);
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFov, Time.deltaTime * fovLerp);

        // Eye height (crouch)
        bool crouch = Input.GetKey(KeyCode.LeftControl);
        float wantY = crouch ? crouchEyeY : eyeY;
        targetEyeY = Mathf.Lerp(targetEyeY, wantY, Time.deltaTime * eyeLerp);
        var lp = pitchPivot.localPosition; lp.y = targetEyeY; pitchPivot.localPosition = lp;
    }

    public void AddRecoil(Vector2 kick) => recoil += kick;

    void ApplyInstant()
    {
        yawPivot.rotation = Quaternion.Euler(0, yaw, 0);
        pitchPivot.localRotation = Quaternion.Euler(pitch, 0, 0);
        cam.fieldOfView = baseFov;
        var lp = pitchPivot.localPosition; lp.y = eyeY; pitchPivot.localPosition = lp;
    }
}
