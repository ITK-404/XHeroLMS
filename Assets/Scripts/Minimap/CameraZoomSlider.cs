using System;
using UnityEngine;
using UnityEngine.UI;

public class CameraZoomSlider : MonoBehaviour
{
    [SerializeField] private Slider _slider;
    [SerializeField] private GameObject container;
    [SerializeField] private Button zoomInBtn;
    [SerializeField] private Button zoomOutBtn;
    public Action<float> OnSliderValueChanged;
    
    private void Awake()
    {
        if (_slider != null)
        {
            _slider.onValueChanged.AddListener(OnValueChanged);
        }
        ClampZeroToOne();
    }
    
    private void OnDestroy()
    {
        if (_slider != null)
        {
            _slider.onValueChanged.RemoveListener(OnValueChanged);
        }
        zoomInBtn.onClick.RemoveListener(ZoomIn);
        zoomOutBtn.onClick.RemoveListener(ZoomOut);
    }

    private void ZoomIn() => OnValueChanged(_slider.value += 0.1f);
    private void ZoomOut() => OnValueChanged(_slider.value -= 0.1f);
    

    private void ClampZeroToOne()
    {
        _slider.minValue = 0;
        _slider.maxValue = 1;
        _slider.value = 0.5f;
    }

    private void OnValueChanged(float valueChanged)
    {
        valueChanged = Mathf.Clamp(valueChanged, _slider.minValue, _slider.maxValue);
        OnSliderValueChanged?.Invoke(valueChanged);
    }

    public void Show()
    {
        container.gameObject.SetActive(true);
    }

    public void Hide()
    {
        container.gameObject.SetActive(false);
    }
}