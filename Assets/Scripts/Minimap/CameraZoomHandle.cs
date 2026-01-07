using System;
using Unity.Cinemachine;
using UnityEngine;

public class CameraZoomHandle : MonoBehaviour
{
    [SerializeField] private CinemachineCamera minimapCamera;

    [SerializeField] private float minZoom;
    [SerializeField] private float maxZoom;
    [SerializeField] private CameraZoomSlider cameraZoomSlider;
    private void Awake()
    {
        cameraZoomSlider.Hide();
        cameraZoomSlider.OnSliderValueChanged += Zoom;
    }

    private void OnDestroy()
    {
        cameraZoomSlider.OnSliderValueChanged -= Zoom;
    }

    public void Zoom(float normalize)
    {
        minimapCamera.Lens.FieldOfView = Mathf.Lerp(minZoom, maxZoom, normalize);
    }
}
