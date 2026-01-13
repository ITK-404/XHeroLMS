using System;
using UnityEngine;
using UnityEngine.UI;

public class CameraZoomSlider : MonoBehaviour
{
    [SerializeField] private Slider _slider;
    [SerializeField] private GameObject container;
    [SerializeField] private Button minBtn;
    [SerializeField] private Button maxBtn;
    public Action<float> OnSliderValueChanged;
    
    private void Awake()
    {
        if (_slider != null)
        {
            _slider.onValueChanged.AddListener(OnValueChanged);
        }
        
        minBtn.onClick.AddListener(DecreaseZoom);
        maxBtn.onClick.AddListener(IncreaseZoom);
        ClampZeroToOne();
    }

    private void IncreaseZoom() => AdjustSlider(0.1f);
    private void DecreaseZoom() => AdjustSlider(-0.1f);

    private void AdjustSlider(float value) => _slider.value += value;
    
    private void OnDestroy()
    {
        if (_slider != null)
        {
            _slider.onValueChanged.RemoveListener(OnValueChanged);
        }
        
        minBtn.onClick.RemoveListener(DecreaseZoom);
        maxBtn.onClick.RemoveListener(IncreaseZoom);
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