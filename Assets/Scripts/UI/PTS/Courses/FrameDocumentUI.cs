using System;
using UnityEngine;

public class FrameDocumentUI : MonoBehaviour
{
    [SerializeField] private UniWebView UniWebViewPrefab;
    [SerializeField] private Transform emptyUI;

    private void Start()
    {
        ShowDocument("https://drive.google.com/file/d/19yJFo1MIqQjJiE-BlU1kn6JHXkC189NN/view?usp=sharing");
    }

    public void ShowDocument(string pageUrl)
    {
        if (string.IsNullOrEmpty(pageUrl))
        {
            emptyUI.gameObject.SetActive(true);
            return;
        }

        var page = Instantiate(UniWebViewPrefab, transform);
        page.gameObject.SetActive(true);
        page.Load(pageUrl);
        page.Show();
    }
}