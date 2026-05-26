using System;
using UnityEngine;

public class FPSUIHandler : MonoBehaviour
{
    [SerializeField] private ToggleSwitch toggle30;
    [SerializeField] private ToggleSwitch toggle60;

    [SerializeField] private UIView parent;

    private void Start()
    {
        SyncToggleUI();
        
        toggle30.onToggleOn.AddListener(OnToggle30);
        toggle60.onToggleOn.AddListener(OnToggle60);
    }

    private void OnDestroy()
    {
        toggle30.onToggleOn.RemoveListener(OnToggle30);
        toggle60.onToggleOn.RemoveListener(OnToggle60);    }

    private void OnValidate()
    {
        if (parent == null)
        {
            parent = GetComponentInParent<UIView>();
        }
    }

    private void OnEnable()
    {
        if (parent != null)
        {
            parent.OnViewOpened += OnViewOpened;
        }
    }

    private void OnDisable()
    {
        if (parent != null)
        {
            parent.OnViewOpened -= OnViewOpened;
        }
    }

    private void OnViewOpened()
    {
        SyncToggleUI();
    }

    public void OnToggle30() => FPSHandler.SetFPS(30);
    public void OnToggle60() => FPSHandler.SetFPS(60);

    private void SyncToggleUI()
    {
        toggle30?.ToggleByGroupManager(FPSHandler.CurrentFPS == 30);
        toggle60?.ToggleByGroupManager(FPSHandler.CurrentFPS == 60);
    }
}