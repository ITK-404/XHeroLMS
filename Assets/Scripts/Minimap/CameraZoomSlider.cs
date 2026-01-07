using System;
using UnityEngine;
using UnityEngine.UI;

public class CameraZoomSlider : MonoBehaviour
{
    [SerializeField] private Slider _slider;
    [SerializeField] private GameObject container;
   
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
    }

    private void ClampZeroToOne()
    {
        _slider.minValue = 0;
        _slider.maxValue = 1;
        _slider.value = 0.5f;
    }

    private void OnValueChanged(float valueChanged)
    {
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