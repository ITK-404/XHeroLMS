using DG.Tweening;
using Pathfinding;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PointClickSystem : MonoBehaviour
{
    public event Action<bool> OnPathStopped;

    public CinemachineBrain brain;
    IAstarAI ai;
    private Seeker seeker;
    private Vector3 lastPickPosition;
    public float debugRadius = 1;
    public LayerMask groundLayerMask;
    public LayerMask checkPointLayerMask;
    private CharacterController characterController;
    public float moveSpeed = 5;

    public float rotationSpeed = 70;

    // gravity
    public float gravity = -9.81f;
    public float gravityMultiplier = 1f;
    private float verticalVelocity = 0f;

    private Vector3 move;
    private ChairCheckPoint currentCheckPoint;

    private PlayerCamera playerCamera;

    // ===== Click focus config =====
    private float desiredDistanceFromTarget = 6f; // player sẽ đứng cách điểm click khoảng này
    private float minDistanceFromTarget = 3f; // khoảng cách tối thiểu
    [SerializeField, Min(30f)] private float autoTurnDegreesPerSecond = 90f; // giới hạn tốc độ xoay tự động để camera không quăng gắt

    [Header("Pathfinding")]
    private bool constrainAiInsideGraph = false;
    [SerializeField] private bool searchPathImmediately = true;
    [SerializeField, Min(0.25f)] private float maxDestinationSnapDistance = 3f;
    [SerializeField, Range(8, 32)] private int clickDestinationAngleSamples = 16;
    [SerializeField, Range(1, 6)] private int clickDestinationDistanceSamples = 4;
    [SerializeField, Min(0.1f)] private float destinationPreferenceWeight = 0.35f;
    [SerializeField, Min(0.1f)] private float arrivalStopDistance = 0.5f;
    [SerializeField, Min(0.2f)] private float stuckStopSeconds = 1.25f;
    [SerializeField, Min(0.05f)] private float stationaryRadius = 0.5f;
    [SerializeField, Min(0.2f)] private float stationaryStopSeconds = 1.5f;
    [SerializeField, Min(0.01f)] private float progressEpsilon = 0.05f;
    [SerializeField, Min(0f)] private float minPathAgeBeforeStop = 0.15f;
    [SerializeField, Range(0f, 1f)] private float minGroundNormalY = 0.35f;

    private bool isClickMoving = false;

    public bool IsClickMoving
    {
        get => isClickMoving;
        set => isClickMoving = value;
    }
    private Vector3 lookTargetWorldPos;
    private float defaultSpeed;

    // ===== VFX move indicator =====
    [Header("Move VFX")] [SerializeField] private GameObject moveVfxPrefab; // drag prefab vào đây
    private GameObject moveVfxInstance;
    private BaseInput baseInput;
    [Header("Mobile")] 
    [SerializeField] private PlayerRotationConfig config;
    public float rotationTouchSpeed = 1;

    private float cameraPitch = 0f;
    public float minPitch = -40f;
    public float maxPitch = 60f;

    private bool rotateWhenMove = false;
    private bool hasActiveMoveDestination = false;
    private bool lastPathStopWasArrival = false;
    private Vector3 activeMoveDestination;
    private Vector3 activeMoveStationaryAnchor;
    private float bestActiveMoveMetric = float.PositiveInfinity;
    private float activeMoveStuckTimer = 0f;
    private float activeMoveStationaryTimer = 0f;
    private float activeMoveStartRealtime = 0f;
    private int cachedGroundFallbackMask = -1;
    [SerializeField, Min(0f)] private float navigationRefreshWaitTimeout = 10f;
    private Coroutine pendingNavigationMoveRoutine;

    private const int RaycastBufferSize = 128;
    private const float GroundRaycastDistance = 1000f;
    private const float GroundProjectionHeight = 3f;
    private const float GroundProjectionDistance = 12f;
    private readonly RaycastHit[] groundRaycastHits = new RaycastHit[RaycastBufferSize];
    private readonly RaycastHit[] checkpointRaycastHits = new RaycastHit[RaycastBufferSize];
    private static readonly RaycastHitDistanceComparer HitDistanceComparer = new RaycastHitDistanceComparer();

    private const string WarZoneObjectName = "WarnZone";
    private const string WarZoneNoticeMessage = "Phía trước là cấm địa còn đang phong ấn, tạm thời chưa thể tiến vào.";
    private const float WarZoneRetreatDistance = 3f;
    private const float WarZoneRetreatSpeedMultiplier = 0.85f;
    private const float WarZoneRetreatRotationLerpSpeed = 0.8f;
    private const float WarZoneNoticeVisibleSeconds = 3f;
    private const float WarZoneNoticeY = -80f;
    private const float WarZoneNoticeHiddenY = -132f;
    private const float WarZoneNoticeIdleY = -86f;
    private const int WarZoneNoticeSortingOrder = 32761;

    private bool isInWarZone = false;
    private bool isWarZoneRetreating = false;
    private bool warZoneRetreatNeedsTriggerExit = false;
    private int warZoneTriggerContactCount = 0;
    private Vector3 lastSafePosition;
    private Vector3 previousSafePosition;
    private Vector3 warZoneRetreatStartPosition;
    private Vector3 warZoneRetreatDirection;
    private Vector3 lastMoveDirection = Vector3.forward;
    private GameObject warZoneNoticeRoot;
    private CanvasGroup warZoneNoticeCanvasGroup;
    private RectTransform warZoneNoticePanelRect;
    private Sequence warZoneNoticeShowTween;
    private Sequence warZoneNoticeHideTween;
    private Tween warZoneNoticeIdleTween;
    private bool warZoneNoticeActive = false;
    private float warZoneNoticeTimer = 0f;
    
    private void Awake()
    {
        playerCamera = GetComponent<PlayerCamera>();
        baseInput = GetComponent<BaseInput>();
    }

    void OnEnable()
    {
        ai = GetComponent<IAstarAI>();
        seeker = GetComponent<Seeker>();
        characterController = GetComponent<CharacterController>();

        if (ai != null)
            defaultSpeed = ai.maxSpeed;

        ConfigureAstarAgent();
        SetAstarAgentIdle(true);
        ClearActiveMoveTracking(false);

        SetSafePosition(transform.position);
        warZoneRetreatStartPosition = transform.position;
    }

    private void OnDisable()
    {
        isInWarZone = false;
        isWarZoneRetreating = false;
        warZoneRetreatNeedsTriggerExit = false;
        warZoneTriggerContactCount = 0;
        warZoneNoticeTimer = 0f;
        CancelPendingNavigationMove();
        ClearActiveMoveTracking(false);
        HideWarZoneNotice(true);
    }

    private bool IsBlendingCamera()
    {
        return brain != null && brain.IsBlending && BuildingCameraManager.Instance.IsFocus();
    }

    private float protectionTimer = 0f;

    void Update()
    {
        HandleWarZoneNoticeTimer();
        UpdatePathCompletionGuard();

        if (TeleMapController._mapActive)
        {
            protectionTimer = .5f;
            return;
        }

        if (protectionTimer > 0f)
        {
            protectionTimer -= Time.deltaTime;
            return;
        }

        if (IsBlendingCamera())
        {
            if(rotateWhenMove)
                RotateToVelocity();
            return;
        }

        bool movementLocked = GameplayLock.IsLocked(GameplayLockTarget.Movement);
        bool interactLocked = GameplayLock.IsLocked(GameplayLockTarget.Interact);
        bool cameraLocked = GameplayLock.IsLocked(GameplayLockTarget.Camera);

        // Gravity update
        bool isGrounded = characterController != null && characterController.isGrounded;
        if (isGrounded && verticalVelocity < 0f)
        {
            // small negative to keep contact with ground
            verticalVelocity = -1f;
        }
        else
        {
            verticalVelocity += gravity * gravityMultiplier * Time.deltaTime;
        }

        if (isWarZoneRetreating)
        {
            HandleWarZoneRetreat();
            return;
        }

        float h = 0; // A/D
        float v = 0; // W/S
        //h = Input.GetAxisRaw("Horizontal") + rotateLeftRightCamera.vertical; // A/D
        //v = Input.GetAxisRaw("Vertical"); // W/S
        h = !movementLocked && baseInput != null ? baseInput.MoveVector.x : 0;
        v = !movementLocked && baseInput != null ? baseInput.MoveVector.y : 0;
        // Debug.Log($"movement vector {h} {v}");
        // Forward/backward movement
        Vector3 forwardMove = transform.forward * v;
        bool isMoving = Mathf.Abs(v) > 0.1f;
        bool isRotateInput = Mathf.Abs(h) > 0.1f;
        bool isLooking = !cameraLocked && TouchRotationView.IsLooking;

        if (isMoving || isRotateInput || isLooking)
        {
            // Điều khiển bằng phím => tắt AI + tắt click-move
            if (ai != null)
                SetAstarAgentIdle(true);

            rotateWhenMove = false;
            CancelPendingNavigationMove();
            ClearActiveMoveTracking(false);

            isClickMoving = false;
            HideMoveVfx(); // VFX

            // combine horizontal movement with vertical velocity
            Vector3 horizontal = forwardMove * moveSpeed;
            RememberMoveDirection(horizontal);
            Vector3 totalMove = horizontal + Vector3.up * verticalVelocity;
            characterController.Move(totalMove * Time.deltaTime);

            StopWaitToMoveChair();
        }
        else
        {
            // when AI is moving the transform, still apply vertical movement for gravity
            RememberMoveDirection(ai != null ? ai.velocity : Vector3.zero);
            characterController.Move(Vector3.up * (verticalVelocity * Time.deltaTime));
        }

        // Left/right rotation bằng input tay (A/D + rotateLeftRightCamera)
        var delta = cameraLocked ? Vector2.zero : TouchRotationView.deltaGlobal;
        bool isRotationActive = !cameraLocked && delta.magnitude > 0.1f;
        if (delta != Vector2.zero)
        {
            float multiplier = config != null ? config.rotationMultiplier : 1f;
            float horizontalRotation = delta.x * rotationTouchSpeed * Time.deltaTime * multiplier;
            transform.Rotate(0, horizontalRotation, 0);

            if (playerCamera != null && playerCamera.playerCinemachineCamera != null)
            {
                //var camTransform = playerCamera.playerCinemachineCamera.transform;
                //float verticalRotation = -delta.y * rotationTouchSpeed * Time.deltaTime;
                //camTransform.Rotate(verticalRotation, 0, 0, Space.Self);
                var camTransform = playerCamera.playerCinemachineCamera.transform;
                // Cộng dồn pitch, clamp lại
                cameraPitch -= delta.y * rotationTouchSpeed * Time.deltaTime;
                cameraPitch = Mathf.Clamp(cameraPitch, minPitch, maxPitch);

                // Giữ nguyên yaw, chỉ thay đổi pitch
                Vector3 euler = camTransform.localEulerAngles;
                euler.x = cameraPitch;
                camTransform.localEulerAngles = euler;
            }
        }
        else if (Mathf.Abs(h) > 0.1f && ai != null && ai.isStopped && ai.canMove == false)
        {
            if (isRotationActive || isLooking)
            {
                return;
            }

            // Debug.Log("Đang xoay");
            float rotationAmount = h * rotationSpeed * Time.deltaTime;
            transform.Rotate(0, rotationAmount, 0);
        }

        if (!TeleMapController._mapActive && !movementLocked && !interactLocked)
        {
            MoveByClick();
        }

        // Vừa đi vừa xoay mượt về phía điểm đã click
        

        if (rotateWhenMove && ai != null && ai.canMove && ai.reachedDestination == false)
        {
            // Debug.Log($"Velocity {ai.velocity}");
            RotateToVelocity();
        }
        else
        {
            rotateWhenMove = false;
            HandleClickMoveRotation();
        }

        CacheSafePosition();
    }

    private void HandleClickMoveRotation()
    {
        if (!isClickMoving || playerCamera == null)
            return;

        if (ai == null)
        {
            isClickMoving = false;
            HideMoveVfx(); // VFX
            return;
        }

        // Nếu AI đã bị dừng (do chỗ khác), thôi không xoay nữa
        if (!ai.canMove || ai.isStopped)
        {
            isClickMoving = false;
            HideMoveVfx(); // VFX
            return;
        }

        // Xoay dần về phía lookTargetWorldPos (trên mặt phẳng XZ)
        Vector3 dir = lookTargetWorldPos - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f)
        {
            StopActivePath(true);
            return;
        }

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        RotateTowards(targetRot, autoTurnDegreesPerSecond);

        // Khi đã tới gần destination và gần như xoay xong thì tắt cờ + tắt VFX
        if (ai.reachedEndOfPath && Quaternion.Angle(transform.rotation, targetRot) < 1f)
        {
            StopActivePath(true);
        }
    }

    private void StopWaitToMoveChair()
    {
        if (waitMoveToChair != null)
        {
            StopCoroutine(waitMoveToChair);
            waitMoveToChair = null;
        }
    }

    private Coroutine waitMoveToChair;

    private void MoveByClick()
    {
        if (playerCamera == null)
        {
            Debug.LogError("Player camera is null");
            return;
        }

        bool isPlayerClick = false;
        isPlayerClick = baseInput != null ? baseInput.IsClicked : false;

        if (isPlayerClick)
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                // Debug.Log("Chặn bởi UI");
                return;
            }

            Vector3 mousePosition = Input.mousePosition;
            Ray ray = playerCamera.mainCamera.ScreenPointToRay(mousePosition);

            if (TryHandleMoveStair(ray))
            {
                return;
            }

            if (TryHandleMoveToHouse(ray))
            {
                return;
            }

            if (TryMoveTest(ray))
            {
                return;
            }
            
            // normal check
            if (MoveToChairHandle(ray))
                return;
            // Case click vào checkpoint ghế: giữ nguyên behavior cũ
       
            // Raycast xuống ground (maxDistance + layerMask)
            if (TryRaycastGround(ray, out var groundHit, GroundRaycastDistance))
            {
                Debug.Log("bắn dính mặt đất, tính điểm đứng & move tới đó, vừa đi vừa xoay nhìn vào điểm click");
                lastPickPosition = groundHit.point;

                Vector3 hitPoint = groundHit.point;
                lookTargetWorldPos = hitPoint; // xoay nhìn vào đây

                // Tính hướng từ player tới điểm click (trên mặt phẳng XZ)
                Vector3 toTarget = hitPoint - transform.position;
                toTarget.y = 0f;

                if (toTarget.sqrMagnitude < 0.001f)
                {
                    // Nếu gần như trùng nhau, chỉ xoay nhẹ về phía đó
                    isClickMoving = true;
                    StopActivePath(true);

                    return;
                }

                Vector3 dirFlat = toTarget.normalized;

                // Player sẽ đứng cách hitPoint một khoảng desiredDistanceFromTarget
                float distToTarget = Mathf.Max(minDistanceFromTarget, desiredDistanceFromTarget);
                Vector3 desiredPos = hitPoint - dirFlat * distToTarget;

                // Giữ y hiện tại của player
                desiredPos.y = transform.position.y;

                if (IsNavigationGraphRefreshing())
                {
                    QueueClickMoveAfterNavigationReady(hitPoint, desiredPos);
                    return;
                }

                TryStartClickMove(hitPoint, desiredPos);
            }
            else
            {
                Debug.Log("Không bắn dính mặt đất rồi");
            }
        }
    }

    private bool TryStartClickMove(Vector3 hitPoint, Vector3 preferredDestination)
    {
        lookTargetWorldPos = hitPoint;
        lastPickPosition = hitPoint;

        if (!TryResolveClickDestination(hitPoint, preferredDestination, out Vector3 finalDestination))
        {
            Debug.LogWarning($"[PointClick] Không tìm được đường hợp lệ tới điểm click: {hitPoint}");
            HideMoveVfx();
            isClickMoving = false;
            return false;
        }

        ShowMoveVfx(hitPoint);

        if (!MoveAgentToResolvedDestination(finalDestination))
        {
            HideMoveVfx();
            isClickMoving = false;
            return false;
        }

        isClickMoving = true;
        return true;
    }

    private void QueueClickMoveAfterNavigationReady(Vector3 hitPoint, Vector3 preferredDestination)
    {
        CancelPendingNavigationMove();
        ShowMoveVfx(hitPoint);
        isClickMoving = false;
        pendingNavigationMoveRoutine = StartCoroutine(StartClickMoveWhenNavigationReady(hitPoint, preferredDestination));
    }

    private IEnumerator StartClickMoveWhenNavigationReady(Vector3 hitPoint, Vector3 preferredDestination)
    {
        yield return WaitForNavigationGraphReady();
        pendingNavigationMoveRoutine = null;

        if (IsNavigationGraphRefreshing())
        {
            Debug.LogWarning("[PointClick] A* graph vẫn đang refresh, bỏ qua lệnh click để tránh tạo path sai.");
            HideMoveVfx();
            isClickMoving = false;
            yield break;
        }

        TryStartClickMove(hitPoint, preferredDestination);
    }

    private bool IsNavigationGraphRefreshing()
    {
        return AddressableAdditiveSceneLoader.IsAstarGraphRefreshInProgress
               || (AstarPath.active != null && AstarPath.active.isScanning);
    }

    private IEnumerator WaitForNavigationGraphReady()
    {
        float startedAt = Time.realtimeSinceStartup;

        while (IsNavigationGraphRefreshing()
               && Time.realtimeSinceStartup - startedAt < navigationRefreshWaitTimeout)
        {
            yield return null;
        }
    }

    private void CancelPendingNavigationMove()
    {
        if (pendingNavigationMoveRoutine == null)
            return;

        StopCoroutine(pendingNavigationMoveRoutine);
        pendingNavigationMoveRoutine = null;
    }

    private void ConfigureAstarAgent()
    {
        if (ai is AIPath aiPath)
        {
            if (constrainAiInsideGraph)
                aiPath.constrainInsideGraph = true;

        }
    }

    private void SetAstarAgentIdle(bool clearPath)
    {
        if (ai == null)
            return;

        ai.destination = transform.position;
        ai.canSearch = false;
        ai.isStopped = true;
        ai.canMove = false;

        if (clearPath)
            ai.SetPath(null);
    }

    private bool TryRaycastGround(Ray ray, out RaycastHit groundHit, float maxDistance)
    {
        if (TryRaycastGroundInMask(ray, groundLayerMask.value, maxDistance, out groundHit))
            return true;

        int fallbackMask = GetGroundFallbackMask() & ~groundLayerMask.value;
        return TryRaycastGroundInMask(ray, fallbackMask, maxDistance, out groundHit);
    }

    private bool TryRaycastGroundInMask(Ray ray, int layerMask, float maxDistance, out RaycastHit groundHit)
    {
        groundHit = default;

        if (layerMask == 0)
            return false;

        int hitCount = Physics.RaycastNonAlloc(ray, groundRaycastHits, maxDistance, layerMask, QueryTriggerInteraction.Ignore);
        if (hitCount <= 0)
            return false;

        SortRaycastHits(groundRaycastHits, hitCount);

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = groundRaycastHits[i];
            if (hit.collider == null || hit.normal.y < minGroundNormalY)
                continue;

            groundHit = hit;
            return true;
        }

        return false;
    }

    private int GetGroundFallbackMask()
    {
        if (cachedGroundFallbackMask >= 0)
            return cachedGroundFallbackMask;

        int mask = groundLayerMask.value;
        AddLayerToMask(ref mask, "Default");
        RemoveLayerFromMask(ref mask, "Ignore Raycast");
        RemoveLayerFromMask(ref mask, "UI");
        RemoveLayerFromMask(ref mask, "Obstacle");
        RemoveLayerFromMask(ref mask, "IgnoreGenerateCollider");

        cachedGroundFallbackMask = mask;
        return cachedGroundFallbackMask;
    }

    private static void AddLayerToMask(ref int mask, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer >= 0)
            mask |= 1 << layer;
    }

    private static void RemoveLayerFromMask(ref int mask, string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer >= 0)
            mask &= ~(1 << layer);
    }

    private bool TryProjectToGround(Vector3 position, out Vector3 groundPosition)
    {
        Vector3 rayOrigin = position + Vector3.up * GroundProjectionHeight;
        Ray ray = new Ray(rayOrigin, Vector3.down);

        if (TryRaycastGround(ray, out RaycastHit hit, GroundProjectionHeight + GroundProjectionDistance))
        {
            groundPosition = hit.point;
            return true;
        }

        groundPosition = position;
        return false;
    }

    private bool TryResolveTeleportDestination(Vector3 requestedDestination, out Vector3 finalDestination)
    {
        if (TryProjectToGround(requestedDestination, out finalDestination))
            return true;

        return TryResolveReachableDestination(requestedDestination, out finalDestination);
    }

    private int RaycastCheckpointHits(Ray ray, float maxDistance, QueryTriggerInteraction query)
    {
        int hitCount = Physics.RaycastNonAlloc(ray, checkpointRaycastHits, maxDistance, checkPointLayerMask, query);
        SortRaycastHits(checkpointRaycastHits, hitCount);
        return hitCount;
    }

    private static void SortRaycastHits(RaycastHit[] hits, int hitCount)
    {
        if (hitCount > 1)
            Array.Sort(hits, 0, hitCount, HitDistanceComparer);
    }

    private bool TryResolveClickDestination(Vector3 hitPoint, Vector3 preferredDestination, out Vector3 finalDestination)
    {
        if (AstarPath.active == null)
        {
            finalDestination = preferredDestination;
            return true;
        }

        if (!TryGetStartNode(out GraphNode startNode))
        {
            finalDestination = default;
            return false;
        }

        bool found = false;
        float bestScore = float.PositiveInfinity;
        Vector3 bestDestination = default;

        EvaluateDestinationCandidate(preferredDestination, preferredDestination, startNode, ref found, ref bestScore, ref bestDestination);
        EvaluateDestinationCandidate(hitPoint, preferredDestination, startNode, ref found, ref bestScore, ref bestDestination);

        Vector3 preferredOffsetDirection = preferredDestination - hitPoint;
        preferredOffsetDirection.y = 0f;
        if (preferredOffsetDirection.sqrMagnitude < 0.001f)
            preferredOffsetDirection = transform.position - hitPoint;

        preferredOffsetDirection.y = 0f;
        if (preferredOffsetDirection.sqrMagnitude < 0.001f)
            preferredOffsetDirection = -transform.forward;

        preferredOffsetDirection.Normalize();

        float minDistance = Mathf.Max(0.25f, minDistanceFromTarget);
        float maxDistance = Mathf.Max(minDistance, Vector3.Distance(FlattenXZ(hitPoint), FlattenXZ(preferredDestination)));
        int distanceSamples = Mathf.Max(1, clickDestinationDistanceSamples);
        int angleSamples = Mathf.Max(8, clickDestinationAngleSamples);

        for (int distanceIndex = 0; distanceIndex < distanceSamples; distanceIndex++)
        {
            float t = distanceSamples == 1 ? 1f : distanceIndex / (float)(distanceSamples - 1);
            float distance = Mathf.Lerp(minDistance, maxDistance, t);

            for (int angleIndex = 0; angleIndex < angleSamples; angleIndex++)
            {
                float angle = 360f * angleIndex / angleSamples;
                Vector3 offsetDirection = Quaternion.AngleAxis(angle, Vector3.up) * preferredOffsetDirection;
                Vector3 candidate = hitPoint + offsetDirection * distance;
                candidate.y = preferredDestination.y;

                EvaluateDestinationCandidate(candidate, preferredDestination, startNode, ref found, ref bestScore, ref bestDestination);
            }
        }

        finalDestination = bestDestination;
        return found;
    }

    private bool TryResolveReachableDestination(Vector3 requestedDestination, out Vector3 finalDestination)
    {
        if (AstarPath.active == null)
        {
            finalDestination = requestedDestination;
            return true;
        }

        if (!TryGetStartNode(out GraphNode startNode))
        {
            finalDestination = default;
            return false;
        }

        bool found = false;
        float bestScore = float.PositiveInfinity;
        Vector3 bestDestination = default;

        EvaluateDestinationCandidate(requestedDestination, requestedDestination, startNode, ref found, ref bestScore, ref bestDestination);

        finalDestination = bestDestination;
        return found;
    }

    private void EvaluateDestinationCandidate(
        Vector3 candidate,
        Vector3 preferredDestination,
        GraphNode startNode,
        ref bool found,
        ref float bestScore,
        ref Vector3 bestDestination)
    {
        if (!TryGetReachableNearest(candidate, startNode, out NNInfo nearest))
            return;

        float snapDistance = Vector3.Distance(FlattenXZ(candidate), FlattenXZ(nearest.position));
        if (snapDistance > maxDestinationSnapDistance)
            return;

        float travelDistance = Vector3.Distance(FlattenXZ(transform.position), FlattenXZ(nearest.position));
        float preferenceDistance = Vector3.Distance(FlattenXZ(preferredDestination), FlattenXZ(nearest.position));
        float score = travelDistance + preferenceDistance * Mathf.Max(0.01f, destinationPreferenceWeight) + snapDistance;

        if (score >= bestScore)
            return;

        found = true;
        bestScore = score;
        bestDestination = nearest.position;
    }

    private bool TryGetStartNode(out GraphNode startNode)
    {
        startNode = null;

        if (AstarPath.active == null)
            return false;

        NNConstraint constraint = BuildNearestNodeConstraint(null);
        NNInfo nearest = AstarPath.active.GetNearest(transform.position, constraint);
        startNode = nearest.node;

        return startNode != null && startNode.Walkable;
    }

    private bool TryGetReachableNearest(Vector3 position, GraphNode startNode, out NNInfo nearest)
    {
        nearest = default;

        if (AstarPath.active == null)
            return false;

        NNConstraint constraint = BuildNearestNodeConstraint(startNode);
        nearest = AstarPath.active.GetNearest(position, constraint);

        return nearest.node != null &&
               nearest.node.Walkable &&
               (startNode == null || PathUtilities.IsPathPossible(startNode, nearest.node));
    }

    private NNConstraint BuildNearestNodeConstraint(GraphNode startNode)
    {
        NNConstraint constraint = NNConstraint.Default;
        constraint.distanceXZ = true;

        if (seeker != null)
        {
            constraint.tags = seeker.traversableTags;
            constraint.graphMask = seeker.graphMask;
        }

        if (startNode != null)
        {
            constraint.constrainArea = true;
            constraint.area = (int)startNode.Area;
        }

        return constraint;
    }

    private bool MoveAgentToResolvedDestination(Vector3 destination)
    {
        if (ai == null)
            return false;

        ai.isStopped = false;
        ai.canMove = true;
        ai.canSearch = !searchPathImmediately;
        ai.destination = destination;

        hasActiveMoveDestination = true;
        lastPathStopWasArrival = false;
        activeMoveDestination = destination;
        activeMoveStationaryAnchor = transform.position;
        bestActiveMoveMetric = float.PositiveInfinity;
        activeMoveStuckTimer = 0f;
        activeMoveStationaryTimer = 0f;
        activeMoveStartRealtime = Time.realtimeSinceStartup;

        if (searchPathImmediately)
            ai.SearchPath();

        return true;
    }

    private void UpdatePathCompletionGuard()
    {
        if (!hasActiveMoveDestination || ai == null)
            return;

        if (!ai.canMove || ai.isStopped)
        {
            ClearActiveMoveTracking(false);
            return;
        }

        if (Time.realtimeSinceStartup - activeMoveStartRealtime < minPathAgeBeforeStop)
            return;

        if (HasArrivedAtActiveDestination())
        {
            StopActivePath(true);
            return;
        }

        if (ShouldStopBecauseStationary())
        {
            StopActivePath(HasArrivedAtActiveDestination());
            return;
        }

        if (ai.pathPending)
            return;

        float metric = GetActiveMoveMetric();
        if (bestActiveMoveMetric - metric > progressEpsilon)
        {
            bestActiveMoveMetric = metric;
            activeMoveStuckTimer = 0f;
            return;
        }

        activeMoveStuckTimer += Time.deltaTime;
        if (activeMoveStuckTimer >= stuckStopSeconds)
        {
            StopActivePath(false);
        }
    }

    private bool HasArrivedAtActiveDestination()
    {
        if (lastPathStopWasArrival)
            return true;

        if (!hasActiveMoveDestination || ai == null)
            return false;

        float horizontalDistance = Vector3.Distance(FlattenXZ(transform.position), FlattenXZ(activeMoveDestination));
        if (horizontalDistance <= arrivalStopDistance)
            return true;

        float remainingDistance = ai.remainingDistance;
        if (IsUsableDistance(remainingDistance) && remainingDistance <= arrivalStopDistance)
            return true;

        return ai.reachedDestination && horizontalDistance <= arrivalStopDistance;
    }

    private bool ShouldStopBecauseStationary()
    {
        float movedDistance = Vector3.Distance(FlattenXZ(transform.position), FlattenXZ(activeMoveStationaryAnchor));
        if (movedDistance > stationaryRadius)
        {
            activeMoveStationaryAnchor = transform.position;
            activeMoveStationaryTimer = 0f;
            return false;
        }

        activeMoveStationaryTimer += Time.deltaTime;
        return activeMoveStationaryTimer >= stationaryStopSeconds;
    }

    private float GetActiveMoveMetric()
    {
        if (ai != null && IsUsableDistance(ai.remainingDistance))
            return ai.remainingDistance;

        return Vector3.Distance(FlattenXZ(transform.position), FlattenXZ(activeMoveDestination));
    }

    private static bool IsUsableDistance(float distance)
    {
        return !float.IsNaN(distance) && !float.IsInfinity(distance);
    }

    private void StopActivePath(bool arrived)
    {
        bool wasActive = hasActiveMoveDestination || isClickMoving || rotateWhenMove;

        ClearActiveMoveTracking(arrived);
        isClickMoving = false;
        rotateWhenMove = false;
        SetAstarAgentIdle(true);

        HideMoveVfx();

        if (wasActive)
            OnPathStopped?.Invoke(arrived);
    }

    private void ClearActiveMoveTracking(bool arrived)
    {
        hasActiveMoveDestination = false;
        lastPathStopWasArrival = arrived;
        activeMoveStuckTimer = 0f;
        activeMoveStationaryTimer = 0f;
        activeMoveStationaryAnchor = transform.position;
        bestActiveMoveMetric = float.PositiveInfinity;
    }

    private static Vector3 FlattenXZ(Vector3 value)
    {
        value.y = 0f;
        return value;
    }

    private bool TryHandleMoveStair(Ray ray)
    {
        int hitCount = RaycastCheckpointHits(ray, GroundRaycastDistance, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit stairHit = checkpointRaycastHits[i];
            if (stairHit.collider == null || !stairHit.collider.CompareTag("CheckPoint"))
                continue;

            Debug.Log("bắn dính cầu thang", stairHit.collider.gameObject);

            Debug.Log("Thử di chuyển tới cầu thang");
            var stair = stairHit.collider.GetComponentInParent<StairZone>();

            if (stair == null)
            {
                continue;
            }

            isClickMoving = false;
            HideMoveVfx(); // VFX

            Debug.Log("Đánh dính check point");
            var position = stair.transform.position + stair.standPoint;
            MoveToPosition(position);
            return true;
        }

        return false;
    }

    public bool MoveToPosition(Vector3 position, bool _rotateWhenMove = true)
    {
        transform.DOKill();

        if (IsNavigationGraphRefreshing())
        {
            QueueMoveToPositionAfterNavigationReady(position, _rotateWhenMove);
            return true;
        }

        return TryStartMoveToPosition(position, _rotateWhenMove);
    }

    private bool TryStartMoveToPosition(Vector3 position, bool _rotateWhenMove)
    {
        if (!TryResolveReachableDestination(position, out Vector3 groundPos))
        {
            Debug.LogWarning($"[PointClick] Không tìm được đường hợp lệ tới vị trí: {position}");
            return false;
        }

        if (!MoveAgentToResolvedDestination(groundPos))
            return false;

        lastPickPosition = groundPos;

        rotateWhenMove = _rotateWhenMove;
        return true;
    }

    private void QueueMoveToPositionAfterNavigationReady(Vector3 position, bool rotateWhenMoveAfterStart)
    {
        CancelPendingNavigationMove();
        pendingNavigationMoveRoutine = StartCoroutine(StartPositionMoveWhenNavigationReady(position, rotateWhenMoveAfterStart));
    }

    private IEnumerator StartPositionMoveWhenNavigationReady(Vector3 position, bool rotateWhenMoveAfterStart)
    {
        yield return WaitForNavigationGraphReady();
        pendingNavigationMoveRoutine = null;

        if (IsNavigationGraphRefreshing())
        {
            Debug.LogWarning("[PointClick] A* graph vẫn đang refresh, bỏ qua lệnh move để tránh tạo path sai.");
            yield break;
        }

        TryStartMoveToPosition(position, rotateWhenMoveAfterStart);
    }
    
    private bool TryHandleMoveToHouse(Ray ray)
    {
        int hitCount = RaycastCheckpointHits(ray, float.MaxValue, QueryTriggerInteraction.Collide);

        List<Transform> blockerTransform = new();
        List<BuildingInteractable> targetHits = new();
        BuildingInteractable targetArea = null;
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = checkpointRaycastHits[i];
            if (hit.collider != null)
            {
                if (hit.collider.CompareTag("CheckPoint") &&
                    hit.collider.TryGetComponent(out BuildingInteractable hitTarget))
                {
                    Debug.Log("MoveToHouse cast to target area");
                    targetHits.Add(hitTarget);
                }
                else
                { 
                    blockerTransform.Add(hit.collider.transform);
                }
            }
        }
        // find shortest area
        var currentPosition = transform.position;
        var shortestDistance = float.MaxValue;

        foreach (var item in targetHits)
        {
            var distance = Vector3.Distance(item.transform.position, currentPosition);

            if (distance < shortestDistance)
            {
                targetArea = item;
                shortestDistance = distance;
            }
        }
        // purpose is prevent different blocker collider outside building
        // blocker in same building will not affect this current check
        if (targetArea == null) return false;

        var targetParentBuilding = targetArea.transform.root;
        foreach (var blocker in blockerTransform)
        {
            var rootOfBlocker = blocker.transform.root;
            if (rootOfBlocker != targetParentBuilding)
            {
                Debug.Log($"MoveToHouse: this blocker not in same with building");
                return false;
            }
        }
        Debug.Log($"MoveToHouse: all blocker is place in same with target building");
        
        BuildingCameraManager.Instance.FocusOnBuilding(targetArea);
        return true;
    }
   

    private bool TryMoveTest(Ray ray)
    {
        int hitCount = RaycastCheckpointHits(ray, float.MaxValue, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = checkpointRaycastHits[i];
            if (hit.collider != null &&
                hit.collider.CompareTag("CheckPoint") &&
                hit.collider.TryGetComponent(out InteractionPoint interactionPoint))
            {
                interactionPoint.Interact(this);
                Debug.Log("bắn dính ngôi nhà, đang thử di chuyển tới");
                return true;
            }
        }

        return false;
    }

    public void TeleportDelay(Vector3 targetPos)
    {
        var position = targetPos;
        characterController.enabled = false;

        if (!TryResolveTeleportDestination(position, out Vector3 groundPos))
            groundPos = position;

        if (ai != null)
        {
            ai.isStopped = true;
            ai.canMove = false;
            ai.canSearch = false;

            ai.Teleport(groundPos);
        }

        lastPickPosition = groundPos;
        isClickMoving = false;
        ClearActiveMoveTracking(false);
        HideMoveVfx(); // VFX
        Debug.Log($"Ground Pos: {groundPos} PlayerPos{transform.position}");

        characterController.enabled = true;
    }

    public void TeleportDelay(Transform hitTransform)
    {
        var position = hitTransform.position;
        characterController.enabled = false;

        if (!TryResolveTeleportDestination(position, out Vector3 groundPos))
            groundPos = position;

        if (ai != null)
        {
            ai.isStopped = true;
            ai.canMove = false;
            ai.canSearch = false;

            ai.Teleport(groundPos);
            ai.rotation = hitTransform.rotation;
        }

        lastPickPosition = groundPos;
        isClickMoving = false;
        ClearActiveMoveTracking(false);
        HideMoveVfx(); // VFX
        Debug.Log($"Ground Pos: {groundPos} PlayerPos{transform.position}");

        transform.DORotateQuaternion(hitTransform.rotation, 1);

        characterController.enabled = true;
    }

    private bool MoveToChairHandle(Ray ray)
    {
        // Choose query type based on whether PlayerChairManager singleton exists (previously duplicated code paths)
        QueryTriggerInteraction query = PlayerChairManager.Instance ? QueryTriggerInteraction.Ignore : QueryTriggerInteraction.Collide;

        int hitCount = RaycastCheckpointHits(ray, 100f, query);
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit chairHit = checkpointRaycastHits[i];
            if (chairHit.collider == null || !chairHit.collider.CompareTag("CheckPoint"))
                continue;

            Debug.Log("Hit check point");

            // set to global variable
            currentCheckPoint = chairHit.collider.GetComponentInParent<ChairCheckPoint>();
            if (currentCheckPoint == null)
                continue;

            // If PlayerChairManager exists, honor the Sitdown state check (preserves previous behavior)
            if (PlayerChairManager.Instance && PlayerChairManager.Instance.playerState == PlayerChairManager.PlayerState.Sitdown)
            {
                Debug.Log("trang thai khong phu hop de ngoi");
                return false;
            }

            Debug.Log("Move to check point");
            return MoveToChairCheckPoint(currentCheckPoint);
        }

        return false;
    }

    public void MoveToChair(ChairCheckPoint chairCheckPoint)
    {
        MoveToChairCheckPoint(chairCheckPoint);
    }
    
    public bool MoveToChairCheckPoint(ChairCheckPoint chairCheckPoint)
    {
        StopWaitToMoveChair();

        // click ghế: không dùng isClickMoving + tắt VFX nếu đang bật
        isClickMoving = false;
        HideMoveVfx(); // VFX

        // moving logic
        Debug.Log("Đánh dính check point");

        var position = chairCheckPoint.spriteCheckPoint.transform.position;
        if (!MoveToPosition(position, false))
            return false;

        waitMoveToChair = StartCoroutine(WaitForRechPos(() =>
        {
            Debug.Log("Hiện UI Xem bước chân");
            if (PlayerChairManager.Instance)
            {
                PlayerChairManager.Instance.currentCheckPoint = chairCheckPoint;
            }
            //TutorialHandler.Instance.ShowStep(1);
        }));

        return true;
    }
    
    private void RotateToVelocity()
    {
        if (ai != null && ai.canMove && !ai.isStopped)
        {
            Vector3 moveDir = ai.velocity;
            moveDir.y = 0f; // Ignore vertical component for rotation

            if (moveDir.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(moveDir.normalized, Vector3.up);
                RotateTowards(targetRot, autoTurnDegreesPerSecond);
            }
        }
    }

    private void RotateTowards(Quaternion targetRot, float degreesPerSecond)
    {
        float maxDegreesDelta = Mathf.Max(1f, degreesPerSecond) * Time.deltaTime;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRot, maxDegreesDelta);
    }

    private IEnumerator WaitForRechPos(Action callback)
    {
        if (ai == null)
            yield break;

        while (pendingNavigationMoveRoutine != null)
            yield return null;

        while (!HasArrivedAtActiveDestination())
        {
            if (!hasActiveMoveDestination && !lastPathStopWasArrival)
                yield break;

            yield return null;
        }

        ai.maxSpeed = defaultSpeed;

        Debug.Log("Đã tới vị trí ngồi");
        callback?.Invoke();
    }

    // thông báo Trigger

    private void OnTriggerEnter(Collider other)
    {
        if (IsWarZone(other))
        {
            warZoneTriggerContactCount++;
            isInWarZone = true;
            StartWarZoneRetreat(other);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!IsWarZone(other))
            return;

        isInWarZone = true;

        if (!isWarZoneRetreating)
        {
            if (warZoneTriggerContactCount <= 0)
                warZoneTriggerContactCount = 1;

            StartWarZoneRetreat(other);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsWarZone(other))
            return;

        warZoneTriggerContactCount = Mathf.Max(0, warZoneTriggerContactCount - 1);
        isInWarZone = warZoneTriggerContactCount > 0;

        if (isWarZoneRetreating)
        {
            return;
        }

        if (!isInWarZone)
        {
            SetSafePosition(transform.position);
        }
    }

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit != null && IsWarZone(hit.collider))
            StartWarZoneRetreat(hit.collider);
    }

    private bool IsWarZone(Collider collider)
    {
        if (collider == null)
            return false;

        Transform current = collider.transform;
        while (current != null)
        {
            if (string.Equals(current.name, WarZoneObjectName, StringComparison.Ordinal))
                return true;

            current = current.parent;
        }

        return false;
    }

    private void StartWarZoneRetreat(Collider warZoneCollider)
    {
        if (isWarZoneRetreating)
            return;

        isInWarZone = true;
        isWarZoneRetreating = true;
        warZoneRetreatNeedsTriggerExit = warZoneCollider != null && warZoneCollider.isTrigger;
        isClickMoving = false;
        rotateWhenMove = false;

        transform.DOKill();
        CancelPendingNavigationMove();
        HideMoveVfx();
        StopWaitToMoveChair();

        warZoneRetreatStartPosition = transform.position;

        warZoneRetreatDirection = lastSafePosition - transform.position;
        warZoneRetreatDirection.y = 0f;

        if (warZoneRetreatDirection.sqrMagnitude < 0.001f)
            warZoneRetreatDirection = -lastMoveDirection;

        warZoneRetreatDirection.y = 0f;

        if (warZoneRetreatDirection.sqrMagnitude < 0.001f)
            warZoneRetreatDirection = -transform.forward;

        warZoneRetreatDirection.Normalize();

        if (ai != null)
        {
            ai.destination = transform.position;
            ai.isStopped = true;
            ai.canMove = false;
            ai.canSearch = false;
            ai.SetPath(null);
        }

        ClearActiveMoveTracking(false);
        ShowWarZoneNotice();
    }

    private void HandleWarZoneRetreat()
    {
        Vector3 moveDirection = warZoneRetreatDirection;
        float maxMoveDistance = moveSpeed * WarZoneRetreatSpeedMultiplier * Time.deltaTime;
        Vector3 horizontalMove = moveDirection * maxMoveDistance;

        Vector3 verticalMove = Vector3.up * (verticalVelocity * Time.deltaTime);
        if (characterController != null && characterController.enabled)
        {
            characterController.Move(horizontalMove + verticalMove);
        }
        else
        {
            transform.position += horizontalMove + verticalMove;
        }

        Quaternion targetRot = Quaternion.LookRotation(moveDirection, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, WarZoneRetreatRotationLerpSpeed * Time.deltaTime);

        Vector3 retreatedDistance = transform.position - warZoneRetreatStartPosition;
        retreatedDistance.y = 0f;

        float retreatProgress = Vector3.Dot(retreatedDistance, warZoneRetreatDirection);
        bool retreatedFarEnough = retreatProgress >= WarZoneRetreatDistance;
        bool clearedWarZone = !warZoneRetreatNeedsTriggerExit || !isInWarZone;
        if (retreatedFarEnough && clearedWarZone)
        {
            FinishWarZoneRetreat();
        }
    }

    private void FinishWarZoneRetreat()
    {
        isWarZoneRetreating = false;
        isInWarZone = false;
        warZoneRetreatNeedsTriggerExit = false;
        warZoneTriggerContactCount = 0;
        isClickMoving = false;
        rotateWhenMove = false;

        if (ai != null)
        {
            ai.destination = transform.position;
            ai.canMove = false;
            ai.isStopped = true;
            ai.canSearch = false;
            ai.SetPath(null);
        }

        ClearActiveMoveTracking(false);
        SetSafePosition(transform.position);
        HideMoveVfx();
    }

    private void ShowWarZoneNotice()
    {
        warZoneNoticeActive = true;
        warZoneNoticeTimer = WarZoneNoticeVisibleSeconds;

        if (warZoneNoticeRoot != null)
        {
            warZoneNoticeRoot.SetActive(true);
            PlayWarZoneNoticeShowAnimation();
            return;
        }

        warZoneNoticeRoot = new GameObject("~WarZoneNoticeCanvas", typeof(Canvas), typeof(CanvasScaler));

        var canvas = warZoneNoticeRoot.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = WarZoneNoticeSortingOrder;

        var scaler = warZoneNoticeRoot.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        warZoneNoticeCanvasGroup = warZoneNoticeRoot.AddComponent<CanvasGroup>();
        warZoneNoticeCanvasGroup.interactable = false;
        warZoneNoticeCanvasGroup.blocksRaycasts = false;

        var panel = new GameObject("Notice", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Outline));
        panel.transform.SetParent(warZoneNoticeRoot.transform, false);

        warZoneNoticePanelRect = panel.GetComponent<RectTransform>();
        warZoneNoticePanelRect.anchorMin = new Vector2(0.5f, 1f);
        warZoneNoticePanelRect.anchorMax = new Vector2(0.5f, 1f);
        warZoneNoticePanelRect.pivot = new Vector2(0.5f, 1f);
        warZoneNoticePanelRect.anchoredPosition = new Vector2(0f, WarZoneNoticeY);
        warZoneNoticePanelRect.sizeDelta = new Vector2(1120f, 118f);

        var panelImage = panel.GetComponent<Image>();
        panelImage.color = new Color(0.055f, 0.04f, 0.025f, 0.92f);
        panelImage.raycastTarget = false;

        var panelOutline = panel.GetComponent<Outline>();
        panelOutline.effectColor = new Color(0.95f, 0.68f, 0.22f, 0.72f);
        panelOutline.effectDistance = new Vector2(2f, -2f);
        panelOutline.useGraphicAlpha = false;

        var textObject = new GameObject("Message", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panel.transform, false);

        var textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(32f, 16f);
        textRect.offsetMax = new Vector2(-32f, -16f);

        var messageText = textObject.GetComponent<TextMeshProUGUI>();
        messageText.text = WarZoneNoticeMessage;
        messageText.alignment = TextAlignmentOptions.Center;
        messageText.color = new Color(1f, 0.9f, 0.62f, 1f);
        messageText.fontSize = 30f;
        messageText.fontStyle = FontStyles.Bold;
        messageText.textWrappingMode = TextWrappingModes.Normal;
        messageText.raycastTarget = false;

        PlayWarZoneNoticeShowAnimation();
    }

    private void HideWarZoneNotice(bool immediate = false)
    {
        if (!warZoneNoticeActive && warZoneNoticeRoot == null)
            return;

        warZoneNoticeActive = false;
        warZoneNoticeTimer = 0f;

        if (immediate || warZoneNoticeRoot == null || warZoneNoticeCanvasGroup == null || warZoneNoticePanelRect == null)
        {
            KillWarZoneNoticeTweens();
            DestroyWarZoneNoticeObject();
            return;
        }

        warZoneNoticeShowTween?.Kill();
        warZoneNoticeShowTween = null;
        warZoneNoticeIdleTween?.Kill();
        warZoneNoticeIdleTween = null;
        warZoneNoticeHideTween?.Kill();

        warZoneNoticeHideTween = DOTween.Sequence();
        warZoneNoticeHideTween
            .Join(warZoneNoticeCanvasGroup.DOFade(0f, 0.22f).SetEase(Ease.InSine))
            .Join(warZoneNoticePanelRect.DOAnchorPosY(WarZoneNoticeHiddenY, 0.28f).SetEase(Ease.InSine))
            .Join(warZoneNoticePanelRect.DOScale(new Vector3(0.94f, 0.88f, 1f), 0.24f).SetEase(Ease.InSine))
            .OnComplete(() =>
            {
                DestroyWarZoneNoticeObject();
                warZoneNoticeHideTween = null;
            });
    }

    private void HandleWarZoneNoticeTimer()
    {
        if (!warZoneNoticeActive || isInWarZone || isWarZoneRetreating)
            return;

        warZoneNoticeTimer -= Time.deltaTime;
        if (warZoneNoticeTimer <= 0f)
            HideWarZoneNotice();
    }

    private void PlayWarZoneNoticeShowAnimation()
    {
        if (warZoneNoticeRoot == null || warZoneNoticeCanvasGroup == null || warZoneNoticePanelRect == null)
            return;

        warZoneNoticeShowTween?.Kill();
        warZoneNoticeHideTween?.Kill();
        warZoneNoticeIdleTween?.Kill();
        warZoneNoticeShowTween = null;
        warZoneNoticeHideTween = null;
        warZoneNoticeIdleTween = null;

        warZoneNoticeCanvasGroup.alpha = 0f;
        warZoneNoticePanelRect.anchoredPosition = new Vector2(0f, WarZoneNoticeHiddenY);
        warZoneNoticePanelRect.localScale = new Vector3(0.92f, 0.86f, 1f);

        warZoneNoticeShowTween = DOTween.Sequence();
        warZoneNoticeShowTween
            .Join(warZoneNoticeCanvasGroup.DOFade(1f, 0.18f).SetEase(Ease.OutSine))
            .Join(warZoneNoticePanelRect.DOAnchorPosY(WarZoneNoticeY, 0.42f).SetEase(Ease.OutBack))
            .Join(warZoneNoticePanelRect.DOScale(Vector3.one, 0.34f).SetEase(Ease.OutBack))
            .OnComplete(() =>
            {
                warZoneNoticeShowTween = null;
                PlayWarZoneNoticeIdleAnimation();
            });
    }

    private void PlayWarZoneNoticeIdleAnimation()
    {
        if (warZoneNoticePanelRect == null)
            return;

        warZoneNoticeIdleTween?.Kill();
        warZoneNoticeIdleTween = warZoneNoticePanelRect
            .DOAnchorPosY(WarZoneNoticeIdleY, 1.05f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void KillWarZoneNoticeTweens()
    {
        warZoneNoticeShowTween?.Kill();
        warZoneNoticeHideTween?.Kill();
        warZoneNoticeIdleTween?.Kill();
        warZoneNoticeShowTween = null;
        warZoneNoticeHideTween = null;
        warZoneNoticeIdleTween = null;
    }

    private void DestroyWarZoneNoticeObject()
    {
        if (warZoneNoticeRoot != null)
            Destroy(warZoneNoticeRoot);

        warZoneNoticeRoot = null;
        warZoneNoticeCanvasGroup = null;
        warZoneNoticePanelRect = null;
    }

    private void RememberMoveDirection(Vector3 direction)
    {
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
            lastMoveDirection = direction.normalized;
    }

    private void CacheSafePosition()
    {
        if (isInWarZone || isWarZoneRetreating)
            return;

        lastSafePosition = previousSafePosition;
        previousSafePosition = transform.position;
    }

    private void SetSafePosition(Vector3 position)
    {
        lastSafePosition = position;
        previousSafePosition = position;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(lastPickPosition, debugRadius);
    }

    // ====== VFX ======
    private void ShowMoveVfx(Vector3 position)
    {
        if (moveVfxPrefab == null) return;

        if (moveVfxInstance == null)
        {
            moveVfxInstance = Instantiate(moveVfxPrefab, position, Quaternion.identity);
            moveVfxInstance.GetComponent<RotateToCamera>().SetCamera(playerCamera.mainCamera);
        }
        else
        {
            moveVfxInstance.transform.position = position;
            if (!moveVfxInstance.activeSelf)
                moveVfxInstance.SetActive(true);
        }
    }

    private void HideMoveVfx()
    {
        if (moveVfxInstance != null && moveVfxInstance.activeSelf)
        {
            moveVfxInstance.SetActive(false);
        }
    }

    public void StopMoving()
    {
        StopActivePath(false);
    }

    private sealed class RaycastHitDistanceComparer : IComparer<RaycastHit>
    {
        public int Compare(RaycastHit x, RaycastHit y)
        {
            return x.distance.CompareTo(y.distance);
        }
    }
}
