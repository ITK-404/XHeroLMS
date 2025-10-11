using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Speeds")]
    public float walkSpeed = 5.5f;
    public float sprintSpeed = 9.5f;
    public float airMaxSpeed = 7f;

    [Header("Acceleration (m/s²)")]
    public float accel = 30f;          // khi đang nhập hướng
    public float decel = 20f;          // khi thả phím
    public float airAccel = 8f;        // control trên không (thấp hơn)
    public float counterFriction = 4f; // kéo giảm trượt nhỏ khi gần đứng yên

    [Header("Jumping")]
    public float jumpImpulse = 7.2f;
    public float coyoteTime = 0.12f;      // nhảy được sau khi rời đất một chút
    public float jumpBuffer = 0.12f;      // bấm sớm vẫn nhảy khi vừa chạm đất
    public bool variableJumpHeight = true;
    public float lowJumpGravityMul = 2.0f; // thả Space sớm => rơi nhanh hơn
    public float fallGravityMul = 2.3f;    // đang rơi => rơi nặng tay

    [Header("Grounding")]
    public Transform orientation;      // forward/right lấy từ camera yaw
    public float playerHeight = 2f;    // cho spherecast
    public float groundCheckRadius = 0.25f;
    public float groundSnapMaxDist = 0.5f; // bám mặt đất khi ở sát
    public float maxSlopeAngle = 50f;
    public LayerMask groundMask;

    [Header("Physics")]
    public float maxGroundSpeed = 10f; // clamp an toàn
    public float maxFallSpeed = 50f;   // terminal velocity
    public float bodyDragGround = 0f;  // dùng lực hãm thay vì drag cao
    public float bodyDragAir = 0f;

    Rigidbody rb;
    float hor, ver;
    bool grounded;
    Vector3 groundNormal = Vector3.up;

    float lastGroundedTime;
    float lastJumpPressedTime;
    bool jumpHeld;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    void Update()
    {
        // --- input ---
        hor = Input.GetAxisRaw("Horizontal");
        ver = Input.GetAxisRaw("Vertical");
        if (Input.GetKeyDown(KeyCode.Space)) lastJumpPressedTime = Time.time;
        jumpHeld = Input.GetKey(KeyCode.Space);

        GroundCheck();

        // --- jump consume (buffer + coyote) ---
        bool canJump = (Time.time - lastGroundedTime) <= coyoteTime;
        bool hasBufferedJump = (Time.time - lastJumpPressedTime) <= jumpBuffer;

        if (hasBufferedJump && canJump)
        {
            DoJump();
            lastJumpPressedTime = -99f; // consume
        }

        ApplyBetterGravity();

        // drag
        rb.linearDamping = grounded ? bodyDragGround : bodyDragAir;
    }

    void FixedUpdate()
    {
        Move();
        GroundSnapIfNeeded();
        ClampFallSpeed();
    }

    void GroundCheck()
    {
        // spherecast từ tâm xuống dưới
        float castDist = (playerHeight * 0.5f) + 0.2f;
        Ray ray = new Ray(transform.position + Vector3.up * 0.05f, Vector3.down);
        grounded = Physics.SphereCast(ray, groundCheckRadius, out RaycastHit hit, castDist, groundMask, QueryTriggerInteraction.Ignore);

        if (grounded)
        {
            groundNormal = hit.normal;
            lastGroundedTime = Time.time;
        }
        else
        {
            groundNormal = Vector3.up;
        }
    }

    void Move()
    {
        // hướng di chuyển theo mặt đất (project trên mặt dốc)
        Vector3 inputDir = (orientation.forward * ver + orientation.right * hor).normalized;
        Vector3 moveOnPlane = Vector3.ProjectOnPlane(inputDir, groundNormal).normalized;

        bool isSprinting = Input.GetKey(KeyCode.LeftShift);
        float targetSpeed = isSprinting ? sprintSpeed : walkSpeed;

        // tốc độ hiện tại theo phương ngang
        Vector3 vel = rb.linearVelocity;
        Vector3 velHorizontal = Vector3.ProjectOnPlane(vel, Vector3.up);

        // chọn gia tốc phù hợp
        float usedAccel = grounded ? (inputDir.sqrMagnitude > 0.01f ? accel : decel) : airAccel;

        // tạo "desired" velocity
        Vector3 desiredVel;
        if (grounded)
            desiredVel = moveOnPlane * targetSpeed;
        else
            desiredVel = inputDir * Mathf.Min(targetSpeed, airMaxSpeed);

        // force để tiến về desired velocity (PID tối giản)
        Vector3 velError = desiredVel - velHorizontal;
        Vector3 accelVec = velError * usedAccel; // m/s² * kg (sẽ dùng VelocityChange)

        // hạn chế trượt nhỏ khi gần đứng yên và không input
        if (grounded && inputDir.sqrMagnitude < 0.01f && velHorizontal.magnitude < 2f)
        {
            Vector3 counter = -velHorizontal * counterFriction;
            accelVec += counter;
        }

        // cấm leo dốc quá gắt
        float slopeAngle = Vector3.Angle(groundNormal, Vector3.up);
        if (grounded && slopeAngle > maxSlopeAngle)
        {
            // trên dốc > max, không cho bơm lực tiến lên (chỉ cho trượt xuống theo trọng lực)
            accelVec = Vector3.ProjectOnPlane(accelVec, groundNormal);
            if (Vector3.Dot(accelVec, Vector3.ProjectOnPlane(moveOnPlane, Vector3.up)) > 0f)
                accelVec = Vector3.zero;
        }

        // áp lực velocity-change (độc lập khối lượng -> feel ổn định)
        rb.AddForce(accelVec * Time.fixedDeltaTime, ForceMode.VelocityChange);

        // clamp tốc độ ngang khi trên đất
        if (grounded && rb.linearVelocity.magnitude > maxGroundSpeed)
        {
            Vector3 clamped = rb.linearVelocity.normalized * maxGroundSpeed;
            clamped.y = rb.linearVelocity.y;
            rb.linearVelocity = clamped;
        }
    }

    void DoJump()
    {
        // xoá thành phần Y để nhảy ổn định
        Vector3 v = rb.linearVelocity;
        v.y = 0f;
        rb.linearVelocity = v;

        rb.AddForce(Vector3.up * jumpImpulse, ForceMode.VelocityChange);
    }

    void ApplyBetterGravity()
    {
        // tăng gravity khi rơi; nếu bật variableJumpHeight, thả Space sớm => rơi mạnh hơn
        bool falling = rb.linearVelocity.y < -0.01f;
        if (falling)
        {
            rb.AddForce(Physics.gravity * (fallGravityMul - 1f), ForceMode.Acceleration);
        }
        else if (variableJumpHeight && !jumpHeld && rb.linearVelocity.y > 0.01f)
        {
            rb.AddForce(Physics.gravity * (lowJumpGravityMul - 1f), ForceMode.Acceleration);
        }
    }

    void GroundSnapIfNeeded()
    {
        // bám mặt đất khi vừa xuống dốc/nhấp nhô nhẹ (tránh “bay nhẹ”)
        if (grounded) return;
        if (rb.linearVelocity.y > 0.5f) return;

        if (Physics.SphereCast(transform.position + Vector3.up * 0.1f, groundCheckRadius,
                               Vector3.down, out RaycastHit hit, groundSnapMaxDist, groundMask,
                               QueryTriggerInteraction.Ignore))
        {
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (slopeAngle <= maxSlopeAngle)
            {
                // kéo xuống mặt đất nhẹ nhàng
                rb.position = hit.point + Vector3.up * 0.01f;
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
                grounded = true;
                groundNormal = hit.normal;
                lastGroundedTime = Time.time;
            }
        }
    }

    void ClampFallSpeed()
    {
        if (rb.linearVelocity.y < -maxFallSpeed)
        {
            Vector3 v = rb.linearVelocity;
            v.y = -maxFallSpeed;
            rb.linearVelocity = v;
        }
    }

    void OnDrawGizmosSelected()
    {
        // visualize ground check
        Gizmos.color = grounded ? Color.green : Color.red;
        Vector3 c = transform.position + Vector3.down * (playerHeight * 0.5f - 0.1f);
        Gizmos.DrawWireSphere(c, groundCheckRadius);
    }
}
