using UnityEngine;
using UnityEngine.UI;

public class RotationSensitivityUI : MonoBehaviour
{
    [SerializeField] private Slider sensitivitySlider;
    [SerializeField] private PlayerRotationConfig config;

    private void OnValidate()
    {
        if (sensitivitySlider == null)
        {
            sensitivitySlider = GetComponent<Slider>();
        }
    }


    private void OnEnable()
    {
        SetSliderByConfig();

        if (sensitivitySlider != null)
        {
            sensitivitySlider.onValueChanged.AddListener(OnSliderValueChange);
        }
    }

    private void OnDisable()
    {
        if (sensitivitySlider != null)
        {
            sensitivitySlider.onValueChanged.RemoveListener(OnSliderValueChange);
        }
    }

    private void OnSliderValueChange(float value)
    {
        if (config == null) return;
        float min = config.minRotationMultiplier;
        float max = config.maxRotationMultiplier;
    
        // Map 0→1 back to min→max
        config.rotationMultiplier = Mathf.Lerp(min, max, value);
    }

    private void SetSliderByConfig()
    {
        if (config == null || sensitivitySlider == null) return;

        sensitivitySlider.minValue = 0f;
        sensitivitySlider.maxValue = 1f;

        float min = config.minRotationMultiplier;
        float max = config.maxRotationMultiplier;

        // Map min→max to 0→1
        sensitivitySlider.value = Mathf.InverseLerp(min, max, config.rotationMultiplier);
    }
}