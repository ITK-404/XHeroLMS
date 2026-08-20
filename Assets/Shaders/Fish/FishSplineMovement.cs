using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using Random = UnityEngine.Random;

public class FishSplineMovement : MonoBehaviour
{
    private enum FishState
    {
        Swimming,
        Idle,
        Turning,
        ChangingSpline
    }

    [Header("Spline")] [SerializeField] private SplineContainer splineContainer;
    [SerializeField, Min(0)] private int splineIndex;

    [SerializeField, Range(0f, 1f)] private float startProgress;

    [SerializeField] private bool loop = true;
    [SerializeField] private bool randomizeStartProgress = true;

    [Header("Direction")] [SerializeField] private bool moveForward = true;

    [Tooltip("Thỉnh thoảng cá tự đổi hướng")] [SerializeField]
    private bool canTurnAround = true;

    [SerializeField] private Vector2 turnAroundInterval = new(5f, 12f);

    [Header("Speed")] [SerializeField, Min(0f)]
    private float moveSpeed = 2f;

    [SerializeField] private Vector2 speedMultiplierRange = new(0.5f, 1.3f);

    [SerializeField, Min(0f)] private float speedChangeSmoothness = 2f;

    [SerializeField] private Vector2 speedChangeInterval = new(1.5f, 4f);

    [Header("Idle")] [SerializeField] private bool canIdle = true;

    [Range(0f, 1f)] [SerializeField] private float idleChance = 0.25f;

    [SerializeField] private Vector2 idleCheckInterval = new(3f, 7f);

    [SerializeField] private Vector2 idleDurationRange = new(0.5f, 2f);

    [Header("Rotation")] [SerializeField, Min(0f)]
    private float rotationSpeed = 8f;

    [Tooltip("Model cá nhìn theo Local X")] [SerializeField]
    private Vector3 rotationOffset = new(0f, -90f, 0f);

    [Header("Natural Movement")] [SerializeField, Min(0f)]
    private float horizontalAmount = 0.15f;

    [SerializeField, Min(0f)] private float horizontalSpeed = 0.8f;

    [SerializeField, Min(0f)] private float verticalAmount = 0.08f;

    [SerializeField, Min(0f)] private float verticalSpeed = 0.55f;

    [Header("Path Offset")] [SerializeField]
    private bool randomizePathOffset = true;

    [SerializeField] private Vector2 pathOffset;

    [SerializeField] private Vector2 horizontalOffsetRange = new(-0.5f, 0.5f);

    [SerializeField] private Vector2 verticalOffsetRange = new(-0.2f, 0.2f);

    [Header("Change Spline")] [Tooltip("Thời gian cá di chuyển sang spline mới")] [SerializeField, Min(0f)]
    private float splineTransitionDuration = 0.75f;

    [Tooltip("Số điểm dùng để tìm vị trí gần nhất")] [SerializeField, Range(10, 200)]
    private int nearestPointSamples = 60;

    [Header("Turning")] [SerializeField, Min(0f)]
    private float turnRotationSpeed = 4f;

    [SerializeField, Range(0.1f, 10f)] private float turnCompleteAngle = 2f;

    private bool pendingMoveForward;

    private FishState currentState;

    private float currentT;
    private float randomPhase;

    private float currentSpeedMultiplier = 1f;
    private float targetSpeedMultiplier = 1f;

    private float speedChangeTimer;
    private float idleCheckTimer;
    private float idleTimer;
    private float turnAroundTimer;

    private Vector3 transitionStartPosition;
    private Vector3 transitionTargetPosition;
    private float transitionTimer;

    private Spline CurrentSpline
    {
        get
        {
            if (splineContainer == null)
                return null;

            if (splineIndex < 0 ||
                splineIndex >= splineContainer.Splines.Count)
                return null;

            return splineContainer.Splines[splineIndex];
        }
    }

    private void Start()
    {
        Initialize();
    }

    private void Update()
    {
        float deltaTime = Time.deltaTime;

        switch (currentState)
        {
            case FishState.Swimming:
                UpdateSwimming(deltaTime);
                break;

            case FishState.Idle:
                UpdateIdle(deltaTime);
                break;

            case FishState.Turning:
                UpdateTurning(deltaTime);
                break;

            case FishState.ChangingSpline:
                UpdateSplineTransition(deltaTime);
                break;
        }
    }

    private void UpdateTurning(float deltaTime)
    {
        GetSplineOrientation(
            out Vector3 tangent,
            out Vector3 up,
            out _
        );

        Vector3 targetDirection =
            pendingMoveForward ? tangent : -tangent;

        Quaternion targetRotation =
            Quaternion.LookRotation(targetDirection, up) *
            Quaternion.Euler(rotationOffset);

        float rotationLerp =
            1f - Mathf.Exp(-turnRotationSpeed * deltaTime);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationLerp
        );

        // Vẫn cập nhật wobble nhưng không di chuyển dọc spline.
        UpdatePositionWithoutRotation();

        float remainingAngle = Quaternion.Angle(
            transform.rotation,
            targetRotation
        );

        if (remainingAngle <= turnCompleteAngle)
        {
            transform.rotation = targetRotation;
            moveForward = pendingMoveForward;

            ResetBehaviourTimers();
            ChangeState(FishState.Swimming);
        }
    }

    private void UpdatePositionWithoutRotation()
    {
        Spline spline = CurrentSpline;

        if (spline == null)
            return;

        currentT = Mathf.Clamp01(currentT);

        float3 localPosition =
            spline.EvaluatePosition(currentT);

        Vector3 centerPosition =
            splineContainer.transform.TransformPoint(localPosition);

        GetSplineOrientation(
            out _,
            out Vector3 up,
            out Vector3 right
        );

        Vector2 offset = GetCurrentOffset();

        transform.position =
            centerPosition +
            right * offset.x +
            up * offset.y;
    }

    private void Initialize()
    {
        currentT = randomizeStartProgress
            ? Random.Range(0f, 1f)
            : startProgress;

        randomPhase = Random.Range(0f, Mathf.PI * 2f);

        targetSpeedMultiplier = Random.Range(
            speedMultiplierRange.x,
            speedMultiplierRange.y
        );

        currentSpeedMultiplier = targetSpeedMultiplier;

        if (randomizePathOffset)
        {
            pathOffset = new Vector2(
                Random.Range(
                    horizontalOffsetRange.x,
                    horizontalOffsetRange.y
                ),
                Random.Range(
                    verticalOffsetRange.x,
                    verticalOffsetRange.y
                )
            );
        }

        ResetBehaviourTimers();
        ChangeState(FishState.Swimming);
        SnapToSpline();
    }

    private void UpdateSwimming(float deltaTime)
    {
        Spline spline = CurrentSpline;

        if (spline == null)
            return;

        UpdateSpeed(deltaTime);
        UpdateBehaviourTimers(deltaTime);

        float stepDistance =
            moveSpeed *
            currentSpeedMultiplier *
            deltaTime;

        MoveProgressByDistance(
            spline,
            stepDistance,
            moveForward
        );

        UpdateTransformOnSpline(deltaTime);
        CheckSplineEnd();
    }

    private void UpdateIdle(float deltaTime)
    {
        idleTimer -= deltaTime;

        // Cá đứng tại chỗ nhưng vẫn lắc nhẹ.
        UpdateTransformOnSpline(deltaTime);

        if (idleTimer <= 0f)
        {
            ResetBehaviourTimers();
            ChangeState(FishState.Swimming);
        }
    }

    private void UpdateSplineTransition(float deltaTime)
    {
        if (splineTransitionDuration <= 0f)
        {
            CompleteSplineTransition();
            return;
        }

        transitionTimer += deltaTime;

        float normalizedTime = Mathf.Clamp01(
            transitionTimer / splineTransitionDuration
        );

        // SmoothStep giúp chuyển spline mềm hơn.
        float smoothTime = normalizedTime *
                           normalizedTime *
                           (3f - 2f * normalizedTime);

        transform.position = Vector3.Lerp(
            transitionStartPosition,
            transitionTargetPosition,
            smoothTime
        );

        RotateTowardsSpline(deltaTime);

        if (normalizedTime >= 1f)
            CompleteSplineTransition();
    }

    private void UpdateSpeed(float deltaTime)
    {
        speedChangeTimer -= deltaTime;

        if (speedChangeTimer <= 0f)
        {
            targetSpeedMultiplier = Random.Range(
                speedMultiplierRange.x,
                speedMultiplierRange.y
            );

            speedChangeTimer = Random.Range(
                speedChangeInterval.x,
                speedChangeInterval.y
            );
        }

        float lerpValue =
            1f - Mathf.Exp(-speedChangeSmoothness * deltaTime);

        currentSpeedMultiplier = Mathf.Lerp(
            currentSpeedMultiplier,
            targetSpeedMultiplier,
            lerpValue
        );
    }

    private void UpdateBehaviourTimers(float deltaTime)
    {
        if (canIdle)
        {
            idleCheckTimer -= deltaTime;

            if (idleCheckTimer <= 0f)
            {
                idleCheckTimer = Random.Range(
                    idleCheckInterval.x,
                    idleCheckInterval.y
                );

                if (Random.value <= idleChance)
                {
                    idleTimer = Random.Range(
                        idleDurationRange.x,
                        idleDurationRange.y
                    );

                    ChangeState(FishState.Idle);
                    return;
                }
            }
        }

        if (canTurnAround)
        {
            turnAroundTimer -= deltaTime;

            if (turnAroundTimer <= 0f)
            {
                ReverseDirection();

                turnAroundTimer = Random.Range(
                    turnAroundInterval.x,
                    turnAroundInterval.y
                );
            }
        }
    }

    private void MoveProgressByDistance(
        Spline spline,
        float distance,
        bool forward
    )
    {
        if (forward)
        {
            SplineUtility.GetPointAtLinearDistance(
                spline,
                currentT,
                distance,
                out float nextT
            );

            currentT = nextT;
        }
        else
        {
            MoveBackwardByDistance(spline, distance);
        }
    }

    private void MoveBackwardByDistance(
        Spline spline,
        float distance
    )
    {
        // GetPointAtLinearDistance chủ yếu đi theo chiều tăng T,
        // nên khi đi ngược ta tự giảm T dựa trên chiều dài spline.
        float splineLength = spline.GetLength();

        if (splineLength <= 0.0001f)
            return;

        float normalizedStep = distance / splineLength;
        currentT -= normalizedStep;
    }

    private void UpdateTransformOnSpline(float deltaTime)
    {
        Spline spline = CurrentSpline;

        if (spline == null)
            return;

        currentT = Mathf.Clamp01(currentT);

        float3 localPosition = spline.EvaluatePosition(currentT);

        Vector3 centerPosition =
            splineContainer.transform.TransformPoint(localPosition);

        GetSplineOrientation(
            out Vector3 tangent,
            out Vector3 up,
            out Vector3 right
        );

        Vector2 offset = GetCurrentOffset();

        transform.position =
            centerPosition +
            right * offset.x +
            up * offset.y;

        Vector3 moveDirection =
            moveForward ? tangent : -tangent;

        RotateTowards(
            moveDirection,
            up,
            deltaTime
        );
    }

    private void GetSplineOrientation(
        out Vector3 tangent,
        out Vector3 up,
        out Vector3 right
    )
    {
        tangent = splineContainer.EvaluateTangent(
            splineIndex,
            currentT
        );

        up = splineContainer.EvaluateUpVector(
            splineIndex,
            currentT
        );

        tangent = tangent.sqrMagnitude > 0.0001f
            ? tangent.normalized
            : transform.forward;

        up = up.sqrMagnitude > 0.0001f
            ? up.normalized
            : Vector3.up;

        right = Vector3.Cross(up, tangent);

        if (right.sqrMagnitude > 0.0001f)
            right.Normalize();
        else
            right = transform.right;
    }

    private void RotateTowardsSpline(float deltaTime)
    {
        GetSplineOrientation(
            out Vector3 tangent,
            out Vector3 up,
            out _
        );

        Vector3 direction =
            moveForward ? tangent : -tangent;

        RotateTowards(direction, up, deltaTime);
    }

    private void RotateTowards(
        Vector3 direction,
        Vector3 up,
        float deltaTime
    )
    {
        if (direction.sqrMagnitude < 0.0001f)
            return;

        Quaternion splineRotation =
            Quaternion.LookRotation(direction, up);

        Quaternion targetRotation =
            splineRotation *
            Quaternion.Euler(rotationOffset);

        float rotationLerp =
            1f - Mathf.Exp(-rotationSpeed * deltaTime);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationLerp
        );
    }

    private void CheckSplineEnd()
    {
        bool reachedSplineEnd =
            moveForward
                ? currentT >= 0.9999f
                : currentT <= 0.0001f;

        if (!reachedSplineEnd)
            return;

        currentT = Mathf.Clamp01(currentT);

        if (loop)
        {
            currentT = moveForward ? 0f : 1f;
            return;
        }

        ReverseDirection();
    }

    private Vector2 GetCurrentOffset()
    {
        float horizontalWobble =
            Mathf.Sin(
                Time.time * horizontalSpeed +
                randomPhase
            ) * horizontalAmount;

        float verticalWobble =
            Mathf.Sin(
                Time.time * verticalSpeed +
                randomPhase * 1.731f
            ) * verticalAmount;

        return new Vector2(
            pathOffset.x + horizontalWobble,
            pathOffset.y + verticalWobble
        );
    }

    public void ReverseDirection()
    {
        if (currentState == FishState.Turning)
            return;

        pendingMoveForward = !moveForward;
        ChangeState(FishState.Turning);
    }

    public void SetDirection(bool forward)
    {
        if (forward == moveForward)
            return;

        pendingMoveForward = forward;
        ChangeState(FishState.Turning);
    }

    public void ChangeSplineToNearestPoint(
        SplineContainer newContainer,
        int newSplineIndex = 0
    )
    {
        if (!IsValidSpline(newContainer, newSplineIndex))
            return;

        Vector3 currentPosition = transform.position;

        splineContainer = newContainer;
        splineIndex = newSplineIndex;

        currentT = FindNearestProgress(
            currentPosition,
            newContainer,
            newSplineIndex
        );

        transitionStartPosition = currentPosition;
        transitionTargetPosition =
            GetSplineWorldPosition(currentT);

        transitionTimer = 0f;
        ChangeState(FishState.ChangingSpline);
    }

    private float FindNearestProgress(
        Vector3 worldPosition,
        SplineContainer container,
        int index
    )
    {
        Spline spline = container.Splines[index];

        float nearestT = 0f;
        float nearestDistanceSqr = float.MaxValue;

        // Sampling đủ ổn cho cá và ít phụ thuộc phiên bản
        // Unity Splines hơn GetNearestPoint.
        for (int i = 0; i <= nearestPointSamples; i++)
        {
            float t = i / (float)nearestPointSamples;

            float3 localPoint = spline.EvaluatePosition(t);

            Vector3 worldPoint =
                container.transform.TransformPoint(localPoint);

            float distanceSqr =
                (worldPosition - worldPoint).sqrMagnitude;

            if (distanceSqr < nearestDistanceSqr)
            {
                nearestDistanceSqr = distanceSqr;
                nearestT = t;
            }
        }

        return nearestT;
    }

    private Vector3 GetSplineWorldPosition(float t)
    {
        Spline spline = CurrentSpline;

        if (spline == null)
            return transform.position;

        float3 localPosition = spline.EvaluatePosition(t);

        return splineContainer.transform.TransformPoint(
            localPosition
        );
    }

    private bool IsValidSpline(
        SplineContainer container,
        int index
    )
    {
        if (container == null)
        {
            Debug.LogWarning(
                "Cannot change spline: container is null.",
                this
            );

            return false;
        }

        if (index < 0 || index >= container.Splines.Count)
        {
            Debug.LogWarning(
                $"Spline index {index} is invalid.",
                this
            );

            return false;
        }

        return true;
    }

    private void CompleteSplineTransition()
    {
        transitionTimer = 0f;
        ResetBehaviourTimers();
        ChangeState(FishState.Swimming);
        UpdateTransformOnSpline(0f);
    }

    private void ChangeState(FishState newState)
    {
        currentState = newState;
    }

    private void ResetBehaviourTimers()
    {
        speedChangeTimer = Random.Range(
            speedChangeInterval.x,
            speedChangeInterval.y
        );

        idleCheckTimer = Random.Range(
            idleCheckInterval.x,
            idleCheckInterval.y
        );

        turnAroundTimer = Random.Range(
            turnAroundInterval.x,
            turnAroundInterval.y
        );
    }

    [ContextMenu("Snap To Spline")]
    private void SnapToSpline()
    {
        if (CurrentSpline == null)
            return;

        currentT = Mathf.Clamp01(currentT);
        UpdateTransformOnSpline(0f);
    }

    public void Restart(float normalizedProgress = 0f)
    {
        currentT = Mathf.Clamp01(normalizedProgress);
        ResetBehaviourTimers();
        ChangeState(FishState.Swimming);
        SnapToSpline();
    }
}