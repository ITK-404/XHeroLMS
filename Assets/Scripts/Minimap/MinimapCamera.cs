using System;
using UnityEngine;

public class MinimapCamera : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject camera;
    [SerializeField] private Vector3 offset;

    [SerializeField] private bool canLookAt = false;

    private void LateUpdate()
    {
        camera.transform.position = player.transform.position + offset;
        var direction = player.transform.position - camera.transform.position;
        camera.transform.forward = direction.normalized;
    }
}
