using System;
using UnityEngine;
using UnityEngine.UI;

public class SettingTabToggleUI : MonoBehaviour
{
    [SerializeField] private Image backgroundImg;
    [SerializeField] private Image iconImg;
    [Header("Background Sprite")]
    [SerializeField] private Sprite activeSprite;
    [SerializeField] private Sprite deActiveSprite;

    [Header("Icon Sprite")] 
    [SerializeField] private Sprite activeIcSprite;
    [SerializeField] private Sprite deActiveIcSprite;
    
    [SerializeField] private Button btn;

    [SerializeField] private int tabIndex;

    private bool isSelectTab = false;
    private SettingTabManagerUI manager;

    private void Awake()
    {
        btn.onClick.AddListener(OnSelectTab);
    }

    private void OnDestroy()
    {
        btn.onClick.RemoveListener(OnSelectTab);

        if (manager)
        {
            manager.OnTabChanged -= OnCurrentTabChange;
        }
    }

    private void OnSelectTab()
    {
        Debug.Log($"Select Tab Index: {tabIndex}");
        manager.ShowTab(tabIndex);
    }


    private void UpdateVisualTab(bool isSelect)
    {
        backgroundImg.sprite = isSelect ? activeSprite : deActiveSprite;
        iconImg.sprite = isSelect ? activeIcSprite : deActiveIcSprite;
    }

    public void Init(int index, SettingTabManagerUI _manager)
    {
        tabIndex = index;
        manager = _manager;
        
        if (manager)
        {
            manager.OnTabChanged += OnCurrentTabChange;
        }
    }
    
    private void OnCurrentTabChange(int tabSelectIndex)
    {
        UpdateVisualTab(tabSelectIndex == tabIndex);
    }
}