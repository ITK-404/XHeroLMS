using Pathfinding;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class PointClickSystem : MonoBehaviour
{
    IAstarAI ai;
    private Vector3 lastPickPosition;
    public float debugRadius = 1;
    public LayerMask groundLayerMask;
    public LayerMask checkPointLayerMask;
    private CharacterController characterController;
    public float moveSpeed = 5;
    public float rotationSpeed = 10f;

    // gravity
    public float gravity = -9.81f;
    public float gravityMultiplier = 1f;
    private float verticalVelocity = 0f;

    private Vector3 move;
    private ChairCheckPoint currentCheckPoint;

    private PlayerCamera playerCamera;
    
    private void Awake()
    {
        playerCamera = GetComponent<PlayerCamera>();
    }

    void OnEnable()
    {
        ai = GetComponent<IAstarAI>();
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (InputBlocker.IsBlocked())
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

        float h = Input.GetAxisRaw("Horizontal"); // A/D
        float v = Input.GetAxisRaw("Vertical"); // W/S

        // Forward/backward movement
        Vector3 forwardMove = transform.forward * v;

        bool isMoving = Mathf.Abs(v) > 0.1f;
        bool isRotate = Mathf.Abs(h) > 0.1f;
        if (isMoving || isRotate)
        {
            ai.isStopped = true;
            ai.canMove = false;
            ai.destination = ai.position;

            // combine horizontal movement with vertical velocity
            Vector3 horizontal = forwardMove * moveSpeed;
            Vector3 totalMove = horizontal + Vector3.up * verticalVelocity;
            characterController.Move(totalMove * Time.deltaTime);

            StopWaitToMoveChair();
        }
        else
        {
            ai.isStopped = false;
            ai.canMove = true;

            // when AI is moving the transform, still apply vertical movement for gravity
            characterController.Move(Vector3.up * verticalVelocity * Time.deltaTime);
        }

        // Left/right rotation
        if (Mathf.Abs(h) > 0.1f)
        {
            float rotationAmount = h * rotationSpeed * Time.deltaTime;
            transform.Rotate(0, rotationAmount, 0);
        }

        MoveByClick();
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
        
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject())
                return;
            
            Vector3 mousePosition = Input.mousePosition;
            Ray ray = playerCamera.mainCamera.ScreenPointToRay(mousePosition);
            if (PlayerChairManager.Instance)
            {
                MoveToChairHandle(ray);
                return;
            }

            if (Physics.Raycast(ray, out var groundHit, groundLayerMask))
            {
                Debug.Log("bắn dính mặt đất, bắt đầu di chuyển");
                ai.destination = groundHit.point;
                lastPickPosition = groundHit.point;
            }
            else
            {
                Debug.Log("Không bắn dính mặt đất rồi");
            }
        }
    }

    private void MoveToChairHandle(Ray ray)
    {
        if (Physics.Raycast(ray, out var chairHit, 100f, checkPointLayerMask, QueryTriggerInteraction.Ignore))
        {
            if (chairHit.collider.CompareTag("CheckPoint"))
            {
                StopWaitToMoveChair();
                Debug.Log("Đánh dính check point");
                var position = chairHit.transform.position;
                var node = AstarPath.active.GetNearest(new Vector3(position.x, 0, position.z)).node;

                Vector3 groundPos = (Vector3)node.position;

                ai.destination = groundPos;
                lastPickPosition = groundPos;

                if (PlayerChairManager.Instance.playerState == PlayerChairManager.PlayerState.Sitdown) return;
                if (currentCheckPoint != null)
                {
                    currentCheckPoint = chairHit.collider.GetComponentInParent<ChairCheckPoint>();
                    waitMoveToChair = StartCoroutine(WaitForRechPos());
                }

                return;
            }
        }
    }

    private IEnumerator WaitForRechPos()
    {
        while (!ai.reachedDestination)
        {
            yield return null;
        }

        Debug.Log("Đã tới vị trí ngồi");
        PlayerChairManager.Instance.currentCheckPoint = currentCheckPoint;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(lastPickPosition, debugRadius);
    }
}