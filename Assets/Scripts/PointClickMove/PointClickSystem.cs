using Pathfinding;
using UnityEngine;

public class PointClickSystem : MonoBehaviour
{
    IAstarAI ai;
    void OnEnable()
    {
        // Get a reference to our movement script.
        // We use the IAstarAI interface to make the code work with all movement scripts
        // You can alternatively use the concrete FollowerEntity class,
        // but that would make the code less flexible
        ai = GetComponent<IAstarAI>();
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Get the mouse position
            var mousePosition = Input.mousePosition;

            // Create a ray from the camera to the mouse position
            var ray = Camera.main.ScreenPointToRay(mousePosition);

            // Check if the ray hits something
            if (Physics.Raycast(ray, out var hit))
            {
                // Set the destination for the AI to move towards
                Debug.Log("Position hit point: " + hit);
                ai.destination = hit.point;
            }
        }
    }
}
