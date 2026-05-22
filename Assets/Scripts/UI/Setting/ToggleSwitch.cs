using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ToggleSwitch : MonoBehaviour, IPointerClickHandler
{
    [Header("Slider Setup")]
    [SerializeField, Range(0, 1f)] protected float sliderValue;
    public bool CurrentValue { get; private set; }

    protected Slider slider;

    [Header("Animation")]
    [SerializeField, Range(0, 1f)] private float animationDuration = 0.5f;
    [SerializeField] private Ease ease = Ease.InOutQuad;

    [Header("Events")]
    [SerializeField] public UnityEvent onToggleOn;
    [SerializeField] public UnityEvent onToggleOff;

    private ToggleSwitchGroupManager toggleSwitchGroupManager;
    protected Action transitionEffect;

    protected virtual void OnValidate()
    {
        SetupSliderComponent();
        if (slider) slider.value = sliderValue;
    }

    protected virtual void Awake() => SetupSliderComponent();

    private void SetupSliderComponent()
    {
        if (slider != null) return;

        slider = GetComponent<Slider>();
        if (slider == null) { Debug.Log("No slider found!", this); return; }

        slider.interactable = false;
        var colors = slider.colors;
        colors.disabledColor = Color.white;
        slider.colors = colors;
        slider.transition = Selectable.Transition.None;
    }

    public void SetupForManager(ToggleSwitchGroupManager manager) =>
        toggleSwitchGroupManager = manager;


    public void OnPointerClick(PointerEventData eventData) => Toggle();

    private void Toggle()
    {
        if (toggleSwitchGroupManager != null)
            toggleSwitchGroupManager.ToggleGroup(this);
        else
            SetState(!CurrentValue);
    }

    public void ToggleByGroupManager(bool value) => SetState(value);

    private void SetState(bool state)
    {
        bool previous = CurrentValue;
        CurrentValue = state;

        if (previous != CurrentValue)
            (CurrentValue ? onToggleOn : onToggleOff)?.Invoke();

        AnimateSlider();
    }

    private void AnimateSlider()
    {
        float target = CurrentValue ? 1f : 0f;

        slider.DOKill();
        slider.DOValue(target, animationDuration)
            .SetEase(ease)
            .OnUpdate(() =>
            {
                sliderValue = slider.value;
                transitionEffect?.Invoke();
            });
    }
}