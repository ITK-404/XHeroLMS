using UnityEngine;

public class RotateToCamera : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform item;
    private void LateUpdate()
    {
        if (playerCamera == null) return;
        if (item == null) return;

        var direction = playerCamera.transform.position - item.transform.position;
        item.transform.forward = direction;
    }
}
