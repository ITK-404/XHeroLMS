using System;
using UnityEngine;
using UnityEngine.UI;

public class ColorToggleSwitch : ToggleSwitch
{
    [Header("Color Transition")]
    [SerializeField] private Image targetImage;
    [SerializeField] private Color onColor = Color.green;
    [SerializeField] private Color offColor = Color.gray;

    protected override void Awake()
    {
        base.Awake();
        transitionEffect = UpdateColor;
    }

    private void UpdateColor()
    {
        targetImage.color = Color.Lerp(offColor, onColor, slider.value);
    }
}
