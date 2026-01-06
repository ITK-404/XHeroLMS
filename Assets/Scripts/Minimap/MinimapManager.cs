using System;
using Unity.Cinemachine;
using UnityEngine;

public class MinimapManager : MonoBehaviour
{
    [SerializeField] private CinemachineCamera minimapCamera;
    [SerializeField] private GameObject player;

    private void ToggleOff()
    {
        minimapCamera.Priority.Value = 0;
        player.GetComponent<PlayerCamera>().playerCinemachineCamera.Priority.Value = 10;
    }

    private void ToggleOn()
    {
        minimapCamera.Priority.Value = 10;
        player.GetComponent<PlayerCamera>().playerCinemachineCamera.Priority.Value = 0;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.G))
        {
            ToggleOn();
        }
        else if (Input.GetKeyDown(KeyCode.H))
        {
            ToggleOff();
        }
    }
}
