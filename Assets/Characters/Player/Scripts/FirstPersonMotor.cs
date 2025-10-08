using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonMotor : MonoBehaviour
{
    public Transform orientation;
    public LayerMask groundMask;

    [Header("Speed")]
    public float walk = 4.2f, sprint = 8.8f;
    public float accel = 12f, decel = 14f, airAccel = 4f, airDecel = 2f;

    [Header("Jump/Gravity")]
    public float jumpHeight = 1.2f;
    public float gravity = -22f, terminal = -50f;
    public float coyote = 0.12f, jumpBuffer = 0.12f;

    [Header("Ground Check")]
    public Transform groundCheck; // trỏ tới node GroundCheck
    public float checkRadius = 0.25f, checkOffset = 0.1f;
    public float slopeStick = 4f;

    CharacterController cc;
    Vector3 velocity;
    float lastGrounded, lastJumpPressed;

    // === Single-jump lock ===
    bool jumpUsed;        // đã nhảy 1 lần kể từ khi rời đất?
    bool wasGrounded;     // để bắt cạnh "vừa chạm đất" và reset

    void Awake(){ cc = GetComponent<CharacterController>(); }

    void Update()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        bool sprinting = Input.GetKey(KeyCode.LeftShift);

        Vector3 fwd = Vector3.ProjectOnPlane(orientation.forward, Vector3.up).normalized;
        Vector3 right = orientation.right;
        Vector3 wishDir = (fwd * v + right * h).normalized;
        float targetSpeed = sprinting ? sprint : walk;

        // === Grounded? (dùng CheckSphere để có mask chính xác) ===
        Vector3 checkPos = (groundCheck ? groundCheck.position :
            transform.position + Vector3.down * (cc.height * .5f - cc.radius + checkOffset));
        bool grounded = Physics.CheckSphere(checkPos, checkRadius, groundMask, QueryTriggerInteraction.Ignore);

        // Reset nhảy khi THỰC SỰ chạm đất
        if (grounded && !wasGrounded)
        {
            jumpUsed = false;         // cho phép nhảy lại
        }
        wasGrounded = grounded;

        if (grounded) lastGrounded = Time.time;
        if (Input.GetButtonDown("Jump")) lastJumpPressed = Time.time;

        // === Planar accel/decel ===
        Vector3 planar = new Vector3(velocity.x, 0, velocity.z);
        Vector3 wishVel = wishDir * targetSpeed;
        Vector3 delta = wishVel - planar;
        float a = grounded ? accel : airAccel;
        float d = grounded ? decel : airDecel;

        if (wishDir.sqrMagnitude > 0)
            planar += Vector3.ClampMagnitude(delta, a * Time.deltaTime);
        else
        {
            float mag = Mathf.Max(0, planar.magnitude - d * Time.deltaTime);
            planar = mag > 0 ? planar.normalized * mag : Vector3.zero;
        }

        // === Jump with coyote + buffer, nhưng KHÓA 1 LẦN ===
        bool canCoyote = Time.time - lastGrounded <= coyote;
        bool buffered  = Time.time - lastJumpPressed <= jumpBuffer;
        if (buffered && (grounded || canCoyote) && !jumpUsed)
        {
            lastJumpPressed = -999f; // consume
            velocity.y = Mathf.Sqrt(-2f * gravity * jumpHeight);
            jumpUsed = true;
        }

        // === Gravity ===
        if (grounded && velocity.y < 0)
        {
            // Hút bám dốc/mặt đất nhẹ để không "bay lơ lửng" trên nhấp nhô nhỏ
            velocity.y = -slopeStick;
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
            if (velocity.y < terminal) velocity.y = terminal;
        }

        velocity.x = planar.x; velocity.z = planar.z;
        cc.Move(velocity * Time.deltaTime);

        if (cc.isGrounded)
        {
            wasGrounded = true;
            jumpUsed = false;
        }
    }
}
