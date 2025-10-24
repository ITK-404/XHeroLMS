using Pathfinding;
using System;
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

    private void MoveByClick()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var mousePosition = Input.mousePosition;
            var ray = Camera.main.ScreenPointToRay(mousePosition);


            if (Physics.Raycast(ray, out var chairHit, chairLayerMask))
            {
                var position = chairHit.transform.position;
                var node = AstarPath.active.GetNearest(new Vector3(position.x, 0, position.z)).node;
                Vector3 groundPos = (Vector3)node.position;
                ai.destination = groundPos;
                lastPickPosition = groundPos;
                return;
            }



            if (Physics.Raycast(ray, out var groundHit, groundLayerMask))
            {
                Debug.Log("Position hit point: " + chairHit);
                ai.destination = chairHit.point;
            }
            lastPickPosition = mousePosition;
        }
    }
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(lastPickPosition, debugRadius);
    }

}