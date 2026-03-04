using System;
using UnityEngine;
using UnityEngine.UI;

public class PTS_CourseDetailManager : PTS_CourseSectionBase
{
    [SerializeField] private Toggle[] toggles;
    [SerializeField] private PanelBaseUI[] panelBaseUIList;
    [SerializeField] private GameObject itemNeedUI;
    [SerializeField] private GameObject leftSide;
    private void Start()
    {
        Binding();
    }

    private void Binding()
    {
        for (int i = 0; i < toggles.Length; i++)
        {
            var toggle = toggles[i];
            var index = i;
            toggle.onValueChanged.AddListener((isOn) =>
            {
                ShowPanel(index);
            });
        }
    }
    
    private void ShowPanel(int index)
    {
        if (index == -1)
        {
            HideAll();
            return;
        }
        // show select panel
        var newPanel = panelBaseUIList[index];
        newPanel.Show();
        // hide other else panel
        foreach (var item in panelBaseUIList)
        {
            if (item != newPanel)
            {
                item.Hide();
            }
        }
    }

    private void HideAll()
    {
        foreach (var item in panelBaseUIList)
            item.Hide();
    }

    public override void Show()
    {
        ShowPanel(0);
        itemNeedUI.gameObject.SetActive(true);
        leftSide.gameObject.SetActive(true);
        // call to show button group
    }

    public override void Hide()
    {
        itemNeedUI.gameObject.SetActive(false);
        leftSide.gameObject.SetActive(false);
        ShowPanel(-1);
    }
}
