using System;
using UnityEngine;
using UnityEngine.UI;

public class FindCourseTypeOptionUI : MonoBehaviour
{
    [SerializeField] private Toggle toggle;
    public Toggle Toggle => toggle;
    [Header("UI")] 
    [SerializeField] private Image backgroundImg;

    [SerializeField] private Image circleImg;
    [Header("Sprite")]  
    [SerializeField] private Sprite backgroundActive;
    [SerializeField] private Sprite backgroundDeActive;
    [SerializeField] private Sprite circleActive;
    [SerializeField] private Sprite circleDeActive;
  
    private void Awake()
    {
        toggle.onValueChanged.AddListener(OnValueChanged);
        UpdateUI(toggle.isOn);
    }

    private void OnDestroy()
    {
        toggle.onValueChanged.RemoveListener(OnValueChanged);
    }

    private void OnValueChanged(bool value)
    {
        // update ui by state
        UpdateUI(toggle.isOn);
    }

    private void UpdateUI(bool isEnable)
    {
        backgroundImg.sprite = isEnable ? backgroundActive : backgroundDeActive;
        circleImg.sprite = isEnable ? circleActive : circleDeActive;
    }
}