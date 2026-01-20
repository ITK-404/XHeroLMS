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
        targetValue = minimapCamera.Lens.FieldOfView;
    }

    private void OnDestroy()
    {
        cameraZoomSlider.OnSliderValueChanged -= Zoom;
    }

    public void Zoom(float normalize)
    {
        targetValue = Mathf.Lerp(maxZoom, minZoom, normalize);
    }

    [SerializeField] private float targetValue;
    private void Update()
    {
        minimapCamera.Lens.FieldOfView = Mathf.Lerp(minimapCamera.Lens.FieldOfView, targetValue, Time.deltaTime * 5);
    }
}
