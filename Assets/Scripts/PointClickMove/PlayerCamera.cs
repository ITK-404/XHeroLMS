using Unity.Cinemachine;
using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    public static PlayerCamera Instance;
    public Camera playerUICamera;
    public Camera mainCamera;
    public CinemachineCamera playerCinemachineCamera;

    private void Awake()
    {
        Instance = this;
    }
}