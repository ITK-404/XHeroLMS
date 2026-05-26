using System;
using UnityEngine;

public class GraphicsSettingHandlerUI : MonoBehaviour
{
    [SerializeField] private ToggleSwitchGroupManager toggleManager;
    // dùng để chặn logic xảy ra khi update ui
    [SerializeField] private bool isEnable = false;

    [SerializeField] private UIView parent;

    private void Start()
    {
        if (parent)
        {
            parent.OnViewOpened += OnViewOpened;
            parent.OnViewClosed += OnViewClosed;
        }
      
        ReloadVisual();
    }

    private void OnDestroy()
    {
        if (parent != null)
        {
            parent.OnViewOpened -= OnViewOpened;
            parent.OnViewClosed -= OnViewClosed;
        }

    }

    private void OnViewOpened()
    {
        ReloadVisual();
        isEnable = true;
    }
    
    private void OnViewClosed()
    {
        isEnable = false;
    }
    
    private void OnValidate()
    {
        if (toggleManager == null)
        {
            toggleManager = GetComponent<ToggleSwitchGroupManager>();
        }

        if (parent == null)
        {
            parent = GetComponentInParent<UIView>();
        }
    }
    
    public void OnSelectToggle(int index)
    {
        // if (isEnable == false) return;
        GraphicsSettingsManager.Instance.ApplyPresetIndex(index);
    }

    [ContextMenu("Reload Visual")]
    private void ReloadVisual()
    {
        if (toggleManager == null) return;
        
        var activeIndex = GraphicsSettingsManager.Instance.GetActiveIndex();
        var toggleSwitches = toggleManager.ToggleSwitches;
        Debug.Log($"[ToggleGroupManager] Active Index :{activeIndex}");
        toggleSwitches[activeIndex].ToggleByGroupManager(true);
        Debug.Log($"ToggleGroupManager: ",toggleSwitches[activeIndex].gameObject);
    }
}