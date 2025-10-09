using UnityEngine;

[DisallowMultipleComponent]
public class FPSLookController : MonoBehaviour
{
    [Header("Refs")]
    public Transform pitchPivot;   // Player/YawPivot/PitchPivot
    public Camera cam;             // FP_Camera (child of pitchPivot)
    public CharacterController cc; // optional
    public Rigidbody rb;           // optional

    [Header("Mouse")]
    public float sensX = 200f;
    public float sensY = 200f;
    public float pitchMin = -89f;
    public float pitchMax =  89f;
    public bool invertX = false;
    public bool invertY = false;

    [Header("Smoothing")]
    public bool smooth = true;
    public float yawLerp = 18f;
    public float pitchLerp = 18f;

    [Header("FOV")]
    public float baseFov   = 75f;
    public float sprintFov = 82f;
    public float adsFov    = 62f;
    public float fovLerp   = 10f;
    public float sprintFovKick = 2.0f; // đá FOV khi vừa vào sprint
    public float fovKickLerp   = 12f;  // tốc độ tắt kick

    [Header("Eye Height")]
    public float eyeY       = 1.62f;
    public float crouchEyeY = 1.10f;
    public float adsEyeYOffset = 0f;
    public float eyeLerp = 10f;

    [Header("Head Stabilization (ADS)")]
    [Range(0f, 1f)] public float adsStabilize = 0.8f; // ADS -> giảm rig

    [Header("Mouse Sway")]
    public float swayYawDeg   = 1.2f;   // ảnh hưởng Mouse X (→ local yaw)
    public float swayPitchDeg = 0.6f;   // ảnh hưởng Mouse Y (→ local pitch)
    public float swayLerp     = 12f;

    [Header("Tilt from Movement (roll)")]
    public float maxRollDeg      = 4.5f;
    public float rollFromVel     = 0.05f;  // m/s -> deg
    public float rollFromAccel   = 0.10f;  // m/s² -> deg
    public float rollLerp        = 10f;

    [Header("Translational Head Lag (spring-damper)")]
    public float posLagMax = 0.06f;
    public float posStiffness = 22f;
    public float posDamping   = 10f;
    public float velOffsetScale   = 0.01f;
    public float accelOffsetScale = 0.0035f;

    [Header("Head Bob")]
    public float bobFreqWalk   = 1.8f;
    public float bobFreqSprint = 2.6f;
    public float bobAmpYWalk   = 0.015f;
    public float bobAmpYSprint = 0.025f;
    public float bobAmpXWalk   = 0.008f;
    public float bobAmpXSprint = 0.012f;
    public float bobFadeLerp   = 10f;

    [Header("Landing Kick")]
    public float landPitchKickDeg = 6f;
    public float landRiseTime = 0.06f;
    public float landFallTime = 0.12f;
    public float landMinImpactSpeed = 2.5f;

    // State (external)
    [HideInInspector] public bool IsSprinting;
    [HideInInspector] public bool IsCrouching;
    [HideInInspector] public bool IsADS;

    // Internal
    float yaw, pitch;
    Vector2 recoil;

    Vector3 prevPos;
    Vector3 velWorld, prevVelWorld, accelWorld;
    bool hadGround;
    float sprintState;    // 0..1
    float fovKickVel;     // >0 khi vừa vào sprint

    float swayYaw, swayPitch;
    float rollNow;

    Vector3 lagPos, lagVel;
    float bobPhase, bobWeight;
    Vector3 bobOffset;

    float landTimer = -1f; // <0: idle, [0..1] chạy curve
    float landDir = 1f;

    void Start()
    {
        if (!cam) cam = Camera.main;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (cam) { cam.transform.localRotation = Quaternion.identity; cam.transform.localPosition = Vector3.zero; }
        if (pitchPivot) pitchPivot.localRotation = Quaternion.identity;

        yaw = transform.eulerAngles.y;
        pitch = 0f;

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (pitchPivot)
        {
            pitchPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
            var lp = pitchPivot.localPosition; lp.y = eyeY; pitchPivot.localPosition = lp;
        }
        if (cam && cam.nearClipPlane > 0.05f) cam.nearClipPlane = 0.05f;
        if (cam) cam.fieldOfView = baseFov;

        prevPos = transform.position;
        prevVelWorld = Vector3.zero;
    }

    void Update()
    {
        float dt = Mathf.Max(Time.deltaTime, 1e-4f);

        // Mouse + recoil
        float mx = Input.GetAxisRaw("Mouse X") * sensX * 0.01f;
        float my = Input.GetAxisRaw("Mouse Y") * sensY * 0.01f;
        if (invertX) mx = -mx;
        if (invertY) my = -my;

        yaw   += mx + recoil.y;
        pitch -= my + recoil.x;
        recoil = Vector2.Lerp(recoil, Vector2.zero, dt * 10f);
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        // Yaw/Pitch base
        var yawQ = Quaternion.Euler(0f, yaw, 0f);
        transform.rotation = smooth ? Quaternion.Slerp(transform.rotation, yawQ, dt * yawLerp) : yawQ;

        if (pitchPivot)
        {
            var pitchQ = Quaternion.Euler(pitch, 0f, 0f);
            pitchPivot.localRotation = smooth ? Quaternion.Slerp(pitchPivot.localRotation, pitchQ, dt * pitchLerp) : pitchQ;

            // khóa yaw/roll trên pitchPivot
            var e = pitchPivot.localEulerAngles;
            pitchPivot.localRotation = Quaternion.Euler(e.x, 0f, 0f);
        }

        // Velocity/accel
        if (rb) velWorld = rb.linearVelocity;                         // FIX: không dùng linearVelocity
        else if (cc) velWorld = cc.velocity;
        else velWorld = (transform.position - prevPos) / dt;

        accelWorld = (velWorld - prevVelWorld) / dt;

        Vector3 fwd = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
        Vector3 right = new Vector3(transform.right.x, 0f, transform.right.z).normalized;
        Vector3 velH = Vector3.ProjectOnPlane(velWorld, Vector3.up);
        Vector3 accH = Vector3.ProjectOnPlane(accelWorld, Vector3.up);
        float speed = velH.magnitude;

        bool grounded = cc ? cc.isGrounded : (rb ? Mathf.Abs(rb.linearVelocity.y) < 0.05f : true);

        // Tỷ lệ giảm rig khi ADS
        float rigWeight = IsADS ? (1f - adsStabilize) : 1f;

        // Mouse sway (có áp Yaw sway)
        float targetSwayYaw   = -mx * swayYawDeg * rigWeight;
        float targetSwayPitch =  my * swayPitchDeg * rigWeight;
        swayYaw   = Mathf.Lerp(swayYaw,   targetSwayYaw, dt * swayLerp);
        swayPitch = Mathf.Lerp(swayPitch, targetSwayPitch, dt * swayLerp);

        // Roll từ chuyển động
        float rollTarget =
            Mathf.Clamp(Vector3.Dot(right, velH) * rollFromVel * Mathf.Rad2Deg
                      + Vector3.Dot(right, accH) * rollFromAccel * Mathf.Rad2Deg,
                        -maxRollDeg, maxRollDeg) * rigWeight;
        rollNow = Mathf.Lerp(rollNow, rollTarget, dt * rollLerp);

        // Translational lag (spring)
        Vector3 targetLag = Vector3.zero;
        targetLag += (-Vector3.Dot(right, velH) * velOffsetScale)   * Vector3.right;
        targetLag += (-Vector3.Dot(fwd,   velH) * velOffsetScale)   * Vector3.forward;
        targetLag += (-Vector3.Dot(right, accH) * accelOffsetScale) * Vector3.right;
        targetLag += (-Vector3.Dot(fwd,   accH) * accelOffsetScale) * Vector3.forward;

        targetLag.x = Mathf.Clamp(targetLag.x, -posLagMax, posLagMax);
        targetLag.z = Mathf.Clamp(targetLag.z, -posLagMax, posLagMax);

        // x'' = -k x - c x' + k target
        lagVel += (posStiffness * (targetLag - lagPos) - posDamping * lagVel) * dt;
        lagPos += lagVel * dt;
        lagPos *= rigWeight; // ADS giảm mạnh

        // Head bob
        float freq = IsSprinting ? bobFreqSprint : bobFreqWalk;
        float ampY = IsSprinting ? bobAmpYSprint : bobAmpYWalk;
        float ampX = IsSprinting ? bobAmpXSprint : bobAmpXWalk;

        float targetBobW = Mathf.Clamp01(Mathf.InverseLerp(0.2f, 2.0f, speed));
        if (IsCrouching) targetBobW *= 0.6f;
        targetBobW *= rigWeight; // ADS giảm
        bobWeight = Mathf.Lerp(bobWeight, targetBobW, dt * bobFadeLerp);

        bobPhase += speed * freq * dt * (grounded ? 1f : 0f);
        float sinP = Mathf.Sin(bobPhase * Mathf.PI * 2f);
        float sin2 = Mathf.Sin(bobPhase * Mathf.PI * 4f);
        bobOffset = new Vector3(sin2 * ampX, Mathf.Abs(sinP) * ampY, 0f) * bobWeight;

        // Landing kick
        if (!hadGround && grounded)
        {
            float vDown = rb ? -rb.linearVelocity.y : Mathf.Max(0f, -(accelWorld.y) * dt);
            if (vDown > landMinImpactSpeed)
            {
                landTimer = 0f; landDir = 1f;
            }
        }
        hadGround = grounded;

        float landPitch = 0f;
        if (landTimer >= 0f)
        {
            if (landDir > 0f)
            {
                landTimer += dt / Mathf.Max(landRiseTime, 1e-3f);
                float t = Mathf.Clamp01(landTimer);
                landPitch = Mathf.Lerp(0f, landPitchKickDeg, t);
                if (t >= 1f) { landDir = -1f; landTimer = 1f; }
            }
            else
            {
                landTimer -= dt / Mathf.Max(landFallTime, 1e-3f);
                float t = Mathf.Clamp01(landTimer);
                landPitch = Mathf.Lerp(0f, landPitchKickDeg, t);
                if (t <= 0f) landTimer = -1f;
            }
            landPitch *= rigWeight;
        }

        // Áp rig vào camera
        if (cam)
        {
            // đảm bảo base (chỉ pitch), rồi cộng rig
            var ce = cam.transform.localEulerAngles;
            cam.transform.localRotation = Quaternion.Euler(ce.x, 0f, 0f);

            // THÊM yaw sway vào camera (trước đây bị bỏ sót)
            Quaternion rigRot = Quaternion.Euler(swayPitch - landPitch, swayYaw, rollNow);
            cam.transform.localRotation = cam.transform.localRotation * rigRot;

            Vector3 rigPos = lagPos + bobOffset;
            cam.transform.localPosition = Vector3.Lerp(cam.transform.localPosition, rigPos, dt * 20f);

            if (cam.nearClipPlane > 0.05f) cam.nearClipPlane = 0.05f;
        }

        // FOV (đơn giản hoá, không double-lerp)
        float wantFov = IsADS ? adsFov : (IsSprinting ? sprintFov : baseFov);

        // phát hiện vừa bật sprint → bơm kickVel
        float targetSprint = IsSprinting ? 1f : 0f;
        float prevSprint = sprintState;
        sprintState = Mathf.MoveTowards(sprintState, targetSprint, dt * 10f);
        if (prevSprint < 0.5f && sprintState >= 0.5f) fovKickVel += sprintFovKick;

        // giảm dần kick về 0
        fovKickVel = Mathf.Lerp(fovKickVel, 0f, dt * fovKickLerp);
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, wantFov + fovKickVel, dt * fovLerp);

        // Eye height
        if (pitchPivot)
        {
            float wantY = IsCrouching ? crouchEyeY : eyeY;
            if (IsADS) wantY += adsEyeYOffset;
            var lp = pitchPivot.localPosition;
            lp.y = Mathf.Lerp(lp.y, wantY, dt * eyeLerp);
            pitchPivot.localPosition = lp;
        }

        // store
        prevPos = transform.position;
        prevVelWorld = velWorld;
    }
}
