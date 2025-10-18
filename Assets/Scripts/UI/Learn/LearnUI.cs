using System;
using UnityEngine;
using UnityEngine.UI;

public class LearnUI : MonoBehaviour
{
    public GameObject container;

    public Button returnBtn;

    public Action OnClickReturnBtn;

    private void Awake()
    {
        returnBtn.onClick.AddListener(ClickReturnBtn);
        Hide();
    }

    private void OnDestroy()
    {
        returnBtn.onClick.AddListener(ClickReturnBtn);
    }

    private void ClickReturnBtn()
    {
        OnClickReturnBtn?.Invoke();
    }

    public void Show()
    {
        container.gameObject.SetActive(true);
    }

    public void Hide()
    {
        container.gameObject.SetActive(false);
    }
}