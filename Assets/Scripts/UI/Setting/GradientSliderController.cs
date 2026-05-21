using UnityEngine;
using UnityEngine.UI;

public class GradientSliderController : MonoBehaviour
{
    [SerializeField] private Slider targetSlider;
    [SerializeField] private Image targetImg;

    private static readonly int ValueID = Shader.PropertyToID("_Value");
    private Material _instanceMaterial;

    void Awake()
    {
        if (targetImg != null)
        {
            _instanceMaterial = Instantiate(targetImg.material);
            targetImg.material = _instanceMaterial;
        }
    }

    void OnEnable()
    {
        if (targetSlider != null)
            targetSlider.onValueChanged.AddListener(OnSliderChanged);

        float initial = targetSlider != null ? targetSlider.value : 1f;
        SetValue(initial);
    }

    void OnDisable()
    {
        if (targetSlider != null)
            targetSlider.onValueChanged.RemoveListener(OnSliderChanged);
    }

    void OnDestroy()
    {
        if (_instanceMaterial != null)
            Destroy(_instanceMaterial);
    }

    private void OnSliderChanged(float value)
    {
        SetValue(value);
    }

    public void SetValue(float value)
    {
        if (_instanceMaterial != null)
            _instanceMaterial.SetFloat(ValueID, value);
        
        targetImg.enabled = false;
        targetImg.enabled = true;
    }
}