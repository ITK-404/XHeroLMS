using System;
using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;

public class MinimapManager : MonoBehaviour
{
    [SerializeField] private CinemachineCamera minimapCamera;
    [SerializeField] private GameObject player;

    [SerializeField] private MinimapUI minimapUI;
    private void Awake()
    {
        minimapUI.turnOnBtn.onClick.AddListener(ToggleOn);
        minimapUI.turnOffBtn.onClick.AddListener(ToggleOff);
    }

    private void OnDestroy()
    {
        minimapUI.turnOnBtn.onClick.RemoveListener(ToggleOn);
        minimapUI.turnOffBtn.onClick.RemoveListener(ToggleOff);
    }
    

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