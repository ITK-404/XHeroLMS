using System;
using System.Collections;
using UnityEngine;

public class SettingTabManagerUI : MonoBehaviour
{
    [Header("Buttons")]

    [Header("Toggles")]
    [SerializeField] private SettingTabToggleUI[] toggles;
    [SerializeField] private UIView[] views;
    public event Action<int> OnTabChanged;

    private int _currentTabIndex = -1;
    public int CurrentTabIndex
    {
        get => _currentTabIndex;
    }
    private void Start()
    {
        SetupViews();
        SetupTabs();
        ShowTab(0);
    }

    private void SetupTabs()
    {
        for (int i = 0; i < toggles.Length; i++)
        {
            toggles[i].Init(this);
            toggles[i].SetIndex(i);
        }
    }

    private void SetupViews()
    {
        foreach (var item in views)
        {
            item.Hide();
        }
    }

    public void ShowTab(int index)
    {
        if (_currentTabIndex == index)
        {
            return;
        }
        Debug.Log($"[SettingTabManagerUI] Show tab for {index}");
        for (int i = 0; i < views.Length; i++)
        {
            if (index == i)
            {
                Debug.Log("Show tab",views[i].gameObject);
                views[i].Show();
            }
            else
            {
                Debug.Log("Hide tab",views[i].gameObject);
                views[i].Hide();
            }
        }

        _currentTabIndex = index;
        
        OnTabChanged?.Invoke(_currentTabIndex);
    }

}