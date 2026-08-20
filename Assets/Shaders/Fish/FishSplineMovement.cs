using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;
using Random = UnityEngine.Random;

public class FishSplineMovement : MonoBehaviour
{
    [Header("Spline")]
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField, Min(0)] private int splineIndex;

    [Tooltip("Vị trí bắt đầu trên spline, từ 0 đến 1")]
    [SerializeField, Range(0f, 1f)]
    private float startProgress;

    [SerializeField, Min(0f)]
    private float moveSpeed = 2f;

    [SerializeField]
    private bool loop = true;

    [Header("Rotation")]
    [SerializeField, Min(0f)]
    private float rotationSpeed = 8f;

    [Tooltip(
        "Model cá nhìn theo Local X nên mặc định xoay Y = -90"
    )]
    [SerializeField]
    private Vector3 rotationOffset = new(0f, -90f, 0f);

    [Header("Natural Movement")]
    [SerializeField, Min(0f)]
    private float horizontalAmount = 0.15f;

    [SerializeField, Min(0f)]
    private float horizontalSpeed = 0.8f;

    [SerializeField, Min(0f)]
    private float verticalAmount = 0.08f;

    [SerializeField, Min(0f)]
    private float verticalSpeed = 0.55f;

    [Header("Random")]
    [SerializeField]
    private bool randomizeOnStart = true;

    [SerializeField]
    private Vector2 speedMultiplierRange = new(0.9f, 1.1f);

    private float currentT;
    private float speedMultiplier = 1f;
    private float randomPhase;
    private bool reachedEnd;
    
    [Header("Path Offset")]
    [SerializeField]
    private Vector2 pathOffset;

// Random một lần cho từng con cá.
    [SerializeField]
    private bool randomizePathOffset = true;

    [SerializeField]
    private Vector2 horizontalOffsetRange = new(-0.5f, 0.5f);

    [SerializeField]
    private Vector2 verticalOffsetRange = new(-0.2f, 0.2f);
    
    [SerializeField]
    private bool randomizeStartProgress = true;
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
        MoveAlongSpline(Time.deltaTime);
    }

    private void Initialize()
    {
        // currentT = startProgress;
        
        currentT = randomizeStartProgress
            ? Random.Range(0f, 1f)
            : startProgress;
        
        reachedEnd = false;

        if (randomizeOnStart)
        {
            randomPhase = Random.Range(0f, Mathf.PI * 2f);

            speedMultiplier = Random.Range(
                speedMultiplierRange.x,
                speedMultiplierRange.y
            );
        }
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
        
        
        SnapToSpline();
    }

    private void MoveAlongSpline(float deltaTime)
    {
        Spline spline = CurrentSpline;

        if (spline == null || reachedEnd)
            return;

        float stepDistance =
            moveSpeed *
            speedMultiplier *
            deltaTime;

        float3 localPosition =
            SplineUtility.GetPointAtLinearDistance(
                spline,
                currentT,
                stepDistance,
                out float nextT
            );

        currentT = nextT;

        // GetPointAtLinearDistance trả về local position
        // của SplineContainer.
        Vector3 centerPosition =
            splineContainer.transform.TransformPoint(localPosition);

        Vector3 tangent =
            splineContainer.EvaluateTangent(
                splineIndex,
                currentT
            );

        Vector3 up =
            splineContainer.EvaluateUpVector(
                splineIndex,
                currentT
            );

        if (tangent.sqrMagnitude < 0.0001f)
            return;

        tangent.Normalize();

        if (up.sqrMagnitude < 0.0001f)
            up = Vector3.up;
        else
            up.Normalize();

        Vector3 right = Vector3.Cross(up, tangent).normalized;

     
        
        
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

        Vector2 currentOffset = GetCurrentOffset();
        Vector3 targetPosition =
            centerPosition +
            right * currentOffset.x +
            up * currentOffset.y;
        
        transform.position = targetPosition;

        RotateTowards(tangent, up, deltaTime);
        CheckEnd();
    }

    private void RotateTowards(
        Vector3 tangent,
        Vector3 up,
        float deltaTime
    )
    {
        Quaternion splineRotation =
            Quaternion.LookRotation(tangent, up);

        Quaternion modelOffset =
            Quaternion.Euler(rotationOffset);

        Quaternion targetRotation =
            splineRotation * modelOffset;

        float rotationLerp =
            1f - Mathf.Exp(-rotationSpeed * deltaTime);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationLerp
        );
    }

    private void CheckEnd()
    {
        if (currentT < 0.9999f)
            return;

        if (loop)
        {
            currentT = 0f;
            return;
        }

        reachedEnd = true;
    }

    [ContextMenu("Snap To Spline")]
    private void SnapToSpline()
    {
        Spline spline = CurrentSpline;

        if (spline == null)
            return;

        currentT = Mathf.Clamp01(currentT);

        float3 localPosition =
            spline.EvaluatePosition(currentT);

        Vector3 position =
            splineContainer.transform.TransformPoint(localPosition);

        Vector3 tangent =
            splineContainer.EvaluateTangent(
                splineIndex,
                currentT
            );

        Vector3 up =
            splineContainer.EvaluateUpVector(
                splineIndex,
                currentT
            );

        transform.position = position;

        if (tangent.sqrMagnitude > 0.0001f)
        {
            tangent.Normalize();

            up = up.sqrMagnitude > 0.0001f
                ? up.normalized
                : Vector3.up;

            transform.rotation =
                Quaternion.LookRotation(tangent, up) *
                Quaternion.Euler(rotationOffset);
        }
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
    
    public void Restart(float normalizedProgress = 0f)
    {
        currentT = Mathf.Clamp01(normalizedProgress);
        reachedEnd = false;
        SnapToSpline();
    }
    
    public void SetSpline(
        SplineContainer newContainer,
        int newSplineIndex = 0,
        float startT = 0f,
        bool snapImmediately = true
    )
    {
        if (newContainer == null)
        {
            Debug.LogWarning(
                "Cannot change spline: container is null.",
                this
            );

            return;
        }

        if (newSplineIndex < 0 ||
            newSplineIndex >= newContainer.Splines.Count)
        {
            Debug.LogWarning(
                $"Spline index {newSplineIndex} is invalid.",
                this
            );

            return;
        }

        splineContainer = newContainer;
        splineIndex = newSplineIndex;
        currentT = Mathf.Clamp01(startT);
        reachedEnd = false;

        if (snapImmediately)
            SnapToSpline();
    }
}