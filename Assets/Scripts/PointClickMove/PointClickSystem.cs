using DG.Tweening;
using Pathfinding;
using System;
using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PointClickSystem : MonoBehaviour
{
    public CinemachineBrain brain;
    IAstarAI ai;
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
    private float rotationLerpSpeed = 2f; // độ mượt xoay

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

    private const string WarZoneObjectName = "WarZone";
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
        characterController = GetComponent<CharacterController>();

        if (ai != null)
            defaultSpeed = ai.maxSpeed;

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

        if (InputBlocker.IsBlocked() || IsBlendingCamera())
        {
            if(rotateWhenMove)
                RotateToVelocity();
            return;
        }
            

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
        h = baseInput != null ? baseInput.MoveVector.x : 0;
        v = baseInput != null ? baseInput.MoveVector.y : 0;
        // Debug.Log($"movement vector {h} {v}");
        // Forward/backward movement
        Vector3 forwardMove = transform.forward * v;
        bool isMoving = Mathf.Abs(v) > 0.1f;
        bool isRotateInput = Mathf.Abs(h) > 0.1f;

        if (isMoving || isRotateInput || TouchRotationView.IsLooking)
        {
            // Điều khiển bằng phím => tắt AI + tắt click-move
            if (ai != null)
            {
                ai.isStopped = true;
                ai.canMove = false;
            }

            rotateWhenMove = false;

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
        var delta = TouchRotationView.deltaGlobal;
        bool isRotationActive = delta.magnitude > 0.1f;
        if (delta != Vector2.zero)
        {
            float multiplier = config != null ? 1 : config.rotationMultiplier;
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
        else if (Mathf.Abs(h) > 0.1f && ai.isStopped && ai.canMove == false)
        {
            if (isRotationActive || TouchRotationView.IsLooking)
            {
                return;
            }

            Debug.Log("Đang xoay");
            float rotationAmount = h * rotationSpeed * Time.deltaTime;
            transform.Rotate(0, rotationAmount, 0);
        }

        if (!TeleMapController._mapActive)
        {
            MoveByClick();
        }

        // Vừa đi vừa xoay mượt về phía điểm đã click
        

        if (rotateWhenMove && ai.canMove && ai.reachedDestination == false)
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
            isClickMoving = false;
            HideMoveVfx(); // VFX
            return;
        }

        Quaternion targetRot = Quaternion.LookRotation(dir.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRot,
            rotationLerpSpeed * Time.deltaTime
        );

        // Khi đã tới gần destination và gần như xoay xong thì tắt cờ + tắt VFX
        if (ai.reachedEndOfPath && Quaternion.Angle(transform.rotation, targetRot) < 1f)
        {
            rotateWhenMove = false;

            isClickMoving = false;
            HideMoveVfx(); // VFX
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
       

            if (TutorialHandler.Instance != null)
            {
                if (!TutorialHandler.Instance.IsPlayedBefore())
                {
                    return;
                }
            }
            // Raycast xuống ground (maxDistance + layerMask)
            if (Physics.Raycast(ray, out var groundHit, 1000f, groundLayerMask))
            {
                Debug.Log("bắn dính mặt đất, tính điểm đứng & move tới đó, vừa đi vừa xoay nhìn vào điểm click");
                lastPickPosition = groundHit.point;

                Vector3 hitPoint = groundHit.point;
                lookTargetWorldPos = hitPoint; // xoay nhìn vào đây

                // Spawn / move VFX tới điểm click
                ShowMoveVfx(hitPoint); // VFX

                // Tính hướng từ player tới điểm click (trên mặt phẳng XZ)
                Vector3 toTarget = hitPoint - transform.position;
                toTarget.y = 0f;

                if (toTarget.sqrMagnitude < 0.001f)
                {
                    // Nếu gần như trùng nhau, chỉ xoay nhẹ về phía đó
                    isClickMoving = true;
                    if (ai != null)
                    {
                        ai.isStopped = true;
                        ai.canMove = false;
                    }

                    return;
                }

                Vector3 dirFlat = toTarget.normalized;

                // Player sẽ đứng cách hitPoint một khoảng desiredDistanceFromTarget
                float distToTarget = Mathf.Max(minDistanceFromTarget, desiredDistanceFromTarget);
                Vector3 desiredPos = hitPoint - dirFlat * distToTarget;

                // Giữ y hiện tại của player
                desiredPos.y = transform.position.y;

                // Snap vào node gần nhất của A* để tránh đi vào chỗ không walkable
                Vector3 finalDestination = desiredPos;
                if (AstarPath.active != null)
                {
                    var node = AstarPath.active.GetNearest(desiredPos);
                    finalDestination = (Vector3)node.position;
                }

                if (ai != null)
                {
                    ai.isStopped = false;
                    ai.canMove = true;
                    ai.destination = finalDestination;
                }

                // Bật chế độ vừa đi vừa xoay
                isClickMoving = true;
            }
            else
            {
                Debug.Log("Không bắn dính mặt đất rồi");
            }
        }
    }

    private bool TryHandleMoveStair(Ray ray)
    {
        if (Physics.Raycast(ray, out var stairHit, 1000f, checkPointLayerMask, QueryTriggerInteraction.Collide))
        {
            Debug.Log("bắn dính cầu thang", stairHit.collider.gameObject);
            if (stairHit.collider.CompareTag("CheckPoint"))
            {
                Debug.Log("Thử di chuyển tới cầu thang");
                var stair = stairHit.collider.GetComponentInParent<StairZone>();

                if (stair == null)
                {
                    return false;
                }

                isClickMoving = false;
                HideMoveVfx(); // VFX

                Debug.Log("Đánh dính check point");
                var position = stair.transform.position + stair.standPoint;
                MoveToPosition(position);
                return true;
            }
        }

        return false;
    }

    public void MoveToPosition(Vector3 position, bool _rotateWhenMove = true)
    {
        transform.DOKill();
        var node = AstarPath.active.GetNearest(new Vector3(position.x, 0, position.z)).node;

        Vector3 groundPos = (Vector3)node.position;

        if (ai != null)
        {
            ai.isStopped = false;
            ai.canMove = true;
            ai.destination = groundPos;
        }

        lastPickPosition = groundPos;

        rotateWhenMove = _rotateWhenMove;
    }
    
    private bool TryHandleMoveToHouse(Ray ray)
    {
        var results = Physics.RaycastAll(ray, float.MaxValue, checkPointLayerMask, QueryTriggerInteraction.Collide);
        foreach (var hit in results)
        {
            if (hit.collider.CompareTag("CheckPoint") &&
                hit.collider.TryGetComponent(out BuildingInteractable interactable))
            {
                BuildingCameraManager.Instance.FocusOnBuilding(interactable);
                Debug.Log("bắn dính ngôi nhà, đang thử di chuyển tới");
                return true;
            }
        }
     
        return false;
    }

    private bool TryMoveTest(Ray ray)
    {
        var results = Physics.RaycastAll(ray, float.MaxValue, checkPointLayerMask, QueryTriggerInteraction.Collide);
        foreach (var hit in results)
        {
            if (hit.collider.CompareTag("CheckPoint") &&
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
        var node = AstarPath.active.GetNearest(new Vector3(position.x, 0, position.z)).node;
        characterController.enabled = false;

        Vector3 groundPos = (Vector3)node.position;
        if (ai != null)
        {
            ai.isStopped = true;
            ai.canMove = false;

            ai.Teleport(groundPos);
        }

        lastPickPosition = groundPos;
        isClickMoving = false;
        HideMoveVfx(); // VFX
        Debug.Log($"Ground Pos: {groundPos} PlayerPos{transform.position}");

        characterController.enabled = true;
    }

    public void TeleportDelay(Transform hitTransform)
    {
        var position = hitTransform.position;
        var node = AstarPath.active.GetNearest(new Vector3(position.x, 0, position.z)).node;
        characterController.enabled = false;

        Vector3 groundPos = (Vector3)node.position;
        if (ai != null)
        {
            ai.isStopped = true;
            ai.canMove = false;

            ai.Teleport(groundPos);
            ai.rotation = hitTransform.rotation;
        }

        lastPickPosition = groundPos;
        isClickMoving = false;
        HideMoveVfx(); // VFX
        Debug.Log($"Ground Pos: {groundPos} PlayerPos{transform.position}");

        transform.DORotateQuaternion(hitTransform.rotation, 1);

        characterController.enabled = true;
    }

    private bool MoveToChairHandle(Ray ray)
    {
        // Choose query type based on whether PlayerChairManager singleton exists (previously duplicated code paths)
        QueryTriggerInteraction query = PlayerChairManager.Instance ? QueryTriggerInteraction.Ignore : QueryTriggerInteraction.Collide;

        if (Physics.Raycast(ray, out var chairHit, 100f, checkPointLayerMask, query))
        {
            Debug.Log("Hit check point");
            if (chairHit.collider.CompareTag("CheckPoint"))
            {
                // set to global variable
                currentCheckPoint = chairHit.collider.GetComponentInParent<ChairCheckPoint>();

                // If PlayerChairManager exists, honor the Sitdown state check (preserves previous behavior)
                if (PlayerChairManager.Instance && PlayerChairManager.Instance.playerState == PlayerChairManager.PlayerState.Sitdown)
                {
                    Debug.Log("trang thai khong phu hop de ngoi");
                    return false;
                }

                Debug.Log("Move to check point");
                return MoveToChairCheckPoint(currentCheckPoint);
            }
        }

        return false;
    }

    public void MoveToChair(ChairCheckPoint chairCheckPoint)
    {
        MoveToChairCheckPoint(chairCheckPoint);
    }
    
    public bool MoveToChairCheckPoint(ChairCheckPoint chairCheckPoint)
    {
        if (chairCheckPoint != null)
        {
            // hard code logic
            var tutorialInstance = TutorialHandler.Instance;
            if ( tutorialInstance != null)
            {
                if (tutorialInstance.IsStep(0) && tutorialInstance.IsPlayedBefore() == false)
                {
                    var isNotTutorialChair = chairCheckPoint.GetComponent<TutorialChair>() == null;

                    if (isNotTutorialChair)
                    {
                        Debug.Log($"Player is in tutorial state and check point hit not tutorial chair");
                        return true;
                    }

                    tutorialInstance.worldTutorialStep.SetActive(false);
                }
            }
            
        }
        StopWaitToMoveChair();
        waitMoveToChair = StartCoroutine(WaitForRechPos(() =>
        {
            Debug.Log("Hiện UI Xem bước chân");
            if (PlayerChairManager.Instance)
            {
                PlayerChairManager.Instance.currentCheckPoint = chairCheckPoint;
            }
            //TutorialHandler.Instance.ShowStep(1);
        }));

        // click ghế: không dùng isClickMoving + tắt VFX nếu đang bật
        isClickMoving = false;
        HideMoveVfx(); // VFX

        // moving logic
        StopWaitToMoveChair();
        Debug.Log("Đánh dính check point");

        var position = chairCheckPoint.spriteCheckPoint.transform.position;
        MoveToPosition(position);
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
                Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    targetRot,
                    rotationLerpSpeed * Time.deltaTime
                );
            }
        }
    }

    private IEnumerator WaitForRechPos(Action callback)
    {
        if (ai == null)
            yield break;

        while (!ai.reachedDestination)
        {
            RotateToVelocity();
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
        }

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
        }

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
        if (ai != null)
        {
            ai.canMove = false;
            ai.isStopped = true;
        }
    }
}
