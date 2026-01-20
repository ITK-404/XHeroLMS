using DG.Tweening;
using Pathfinding;
using System;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.EventSystems;

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
    [Header("Mobile")] public float rotationTouchSpeed = 1;
    private float cameraPitch = 0f;
    public float minPitch = -40f;
    public float maxPitch = 60f;

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
    }

    private bool IsBlendingCamera()
    {
        return brain != null && brain.IsBlending && BuildingCameraManager.Instance.IsFocus();
    }

    private float protectionTimer = 0f;

    void Update()
    {
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
            return;

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

            isClickMoving = false;
            HideMoveVfx(); // VFX

            // combine horizontal movement with vertical velocity
            Vector3 horizontal = forwardMove * moveSpeed;
            Vector3 totalMove = horizontal + Vector3.up * verticalVelocity;
            characterController.Move(totalMove * Time.deltaTime);

            StopWaitToMoveChair();
        }
        else
        {
            // when AI is moving the transform, still apply vertical movement for gravity
            characterController.Move(Vector3.up * (verticalVelocity * Time.deltaTime));
        }

        // Left/right rotation bằng input tay (A/D + rotateLeftRightCamera)
        var delta = TouchRotationView.deltaGlobal;
        bool isRotationActive = delta.magnitude > 0.1f;
        if (delta != Vector2.zero)
        {
            float horizontalRotation = delta.x * rotationTouchSpeed * Time.deltaTime;
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
        HandleClickMoveRotation();
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
                Debug.Log("Chặn bởi UI");
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

            // Case click vào checkpoint ghế: giữ nguyên behavior cũ
            if (PlayerChairManager.Instance)
            {
                if (MoveToChairHandle(ray))
                    return;
            }

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

    public void MoveToPosition(Vector3 position)
    {
        var node = AstarPath.active.GetNearest(new Vector3(position.x, 0, position.z)).node;

        Vector3 groundPos = (Vector3)node.position;

        if (ai != null)
        {
            ai.isStopped = false;
            ai.canMove = true;
            ai.destination = groundPos;
        }

        lastPickPosition = groundPos;
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
        if (Physics.Raycast(ray, out var chairHit, 100f, checkPointLayerMask, QueryTriggerInteraction.Ignore))
        {
            if (chairHit.collider.CompareTag("CheckPoint"))
            {
                // hardcode
                if (PlayerChairManager.Instance.playerState == PlayerChairManager.PlayerState.Sitdown)
                    return false;
                // set to global variable
                currentCheckPoint = chairHit.collider.GetComponentInParent<ChairCheckPoint>();

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
            if (TutorialHandler.Instance.IsStep(0) && TutorialHandler.Instance.IsPlayedBefore() == false)
            {
                var isNotTutorialChair = chairCheckPoint.GetComponent<TutorialChair>() == null;

                if (isNotTutorialChair)
                {
                    Debug.Log($"Player is in tutorial state and check point hit not tutorial chair");
                    return true;
                }

                TutorialHandler.Instance.worldTutorialStep.SetActive(false);
            }
        }

        waitMoveToChair = StartCoroutine(WaitForRechPos(() =>
        {
            Debug.Log("Hiện UI Xem bước chân");
            PlayerChairManager.Instance.currentCheckPoint = chairCheckPoint;
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