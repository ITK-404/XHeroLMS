using Pathfinding;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class PointClickSystem : MonoBehaviour
{

    IAstarAI ai;
    private Vector3 lastPickPosition;
    public float debugRadius = 1;
    public LayerMask groundLayerMask;
    public LayerMask chairLayerMask;
    private CharacterController characterController;
    public float moveSpeed = 5;
    public float rotationSpeed = 10f;

    private Vector3 move;
    private ChairCheckPoint currentCheckPoint;
    void OnEnable()
    {
        ai = GetComponent<IAstarAI>();
        characterController = GetComponent<CharacterController>();
    }

    void Update()
    {
        if (InputBlocker.IsBlocked())
            return;

        float h = Input.GetAxisRaw("Horizontal"); // A/D
        float v = Input.GetAxisRaw("Vertical");   // W/S

        // Forward/backward movement
        Vector3 forwardMove = transform.forward * v;
        bool isMoving = Mathf.Abs(v) > 0.1f;
        bool isRotate = Mathf.Abs(h) > 0.1f;
        if (isMoving || isRotate)
        {
            ai.isStopped = true;
            ai.canMove = false;
            ai.destination = ai.position;
            characterController.Move(forwardMove * moveSpeed * Time.deltaTime);

            StopWaitToMoveChair();
        }
        else
        {
            ai.isStopped = false;
            ai.canMove = true;
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
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePosition = Input.mousePosition;
            Ray ray = Camera.main.ScreenPointToRay(mousePosition);

            if (Physics.Raycast(ray, out var chairHit))
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
                        waitMoveToChair = StartCoroutine(WaitForRechPos());
                    }
                    
                    currentCheckPoint = chairHit.collider.GetComponentInParent<ChairCheckPoint>();
                    return;
                }
            }



            if (Physics.Raycast(ray, out var groundHit, groundLayerMask))
            {
                ai.destination = groundHit.point;
                lastPickPosition = groundHit.point;
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
        PlayerChairManager.Instance.PlayerSitdown(currentCheckPoint);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(lastPickPosition, debugRadius);
    }

}