using System;
using UnityEngine;

public class MinimapCamera : MonoBehaviour
{
    [SerializeField] private GameObject player;
    [SerializeField] private GameObject camera;
    [SerializeField] private Vector3 offset;

    private void LateUpdate()
    {
        camera.transform.position = player.transform.position + offset;
    }
}
