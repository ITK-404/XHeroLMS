using System;
using System.Collections;
using UnityEngine;

public class SettingTabManagerUI : MonoBehaviour
{
    [SerializeField] private SettingTabToggleUI[] toggles;
    [SerializeField] private UIView[] views;
    public event Action<int> OnTabChanged;

    private int currentIndex = -1;
    public int CurrentIndex
    {
        get => currentIndex;
    }
    private void Start()
    {
        SetupViews();
        SetupTabs();
        ShowTab(0);
    }

    private void SetupTabs()
    {
        for (int i = 0; i < views.Length; i++)
        {
            toggles[i].Init(i,this);
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
        if (currentIndex == index)
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

        currentIndex = index;
        
        OnTabChanged?.Invoke(currentIndex);
    }

}