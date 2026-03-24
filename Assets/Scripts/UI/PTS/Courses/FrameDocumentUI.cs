using System;
using UnityEngine;

public class FrameDocumentUI : PanelBaseUI
{
    [SerializeField] private UniWebView UniWebViewPrefab;
    [SerializeField] private Transform emptyUI;

    private UniWebView page;
    
    private void Start()
    {
        // ShowDocument("https://drive.google.com/file/d/19yJFo1MIqQjJiE-BlU1kn6JHXkC189NN/view?usp=sharing");
    }

    public override void Show()
    {
        base.Show();
        ShowDocument("");
    }

    public override void Hide()
    {
        base.Hide();
        if (page != null)
        {
            page.Hide();
        }
    }

    public void ShowDocument(string pageUrl)
    {
        if (string.IsNullOrEmpty(pageUrl))
        {
            emptyUI.gameObject.SetActive(false);
            return;
        }

        if (page == null)
        {
            page = Instantiate(UniWebViewPrefab, transform);
            page.gameObject.SetActive(true);
        }
            
        page.Load(pageUrl);
        page.Show();
    }
}