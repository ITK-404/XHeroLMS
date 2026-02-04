using System;
using TMPro;
using UnityEngine;

public class PlotArea : MonoBehaviour
{
    public AreaMapLocation Location;

    [SceneSeoDropdown] public string seo_url;

    public PlotAreaUI plotAreaUI;

    [HideInInspector]public PlotAreaData plotAreaData;

    [SerializeField] private string titleName;
    [SerializeField] private string displayName;

    [SerializeField] private float ratioUI = 1;

    private void Awake()
    {
        if (Location == null)
        {
            Location = GetComponent<AreaMapLocation>();
        }
    }

    public void Initialize()
    {
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (plotAreaUI == null)
        {
            Debug.LogError("This Plot Area UI is null");
            return;
        }
        plotAreaUI.titleText.text = titleName;
        plotAreaUI.displayText.text = displayName;
    }

    [ContextMenu("Load For Debug")]
    private void LoadForDebug()
    {
        if (string.IsNullOrEmpty(seo_url))
        {
            gameObject.name = $"Plot Area: 'Empty' {titleName} {displayName}";
        }
        else
        {
            gameObject.name = "Plot Area: " + seo_url;
        }
    }

    public void Show(bool isEnable)
    {
        if (isEnable)
            plotAreaUI.Show();
        else
            plotAreaUI.Hide();
    }
}

