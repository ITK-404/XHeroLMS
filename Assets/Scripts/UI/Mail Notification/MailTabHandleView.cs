using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class MailTabHandleView : MonoBehaviour
{
    [SerializeField] private MailViewUI[] views;

    [SerializeField] private Button[] _buttons;

    private void Awake()
    {
        Binding();
        ShowTab(0);
    }

    private void OnDestroy()
    {
        UnBinding();
    }

    private void HandleButtonVisual(int index)
    {
        for (int i = 0; i < _buttons.Length; i++)
        {
            var btn = _buttons[i];
            btn.image.DOFade(index == i ? 1 : 0, 0);
        }
    }
    
    private void Binding()
    {
        for (int i = 0; i < _buttons.Length; i++)
        {
            var btn = _buttons[i];
            var index = i;
            btn.onClick.AddListener(() =>
            {
                ShowTab(index);
            });
        }
    }

    private void UnBinding()
    {
        for (int i = 0; i < _buttons.Length; i++)
        {
            var btn = _buttons[i];
            var index = i;
            btn.onClick.RemoveListener(() =>
            {
                ShowTab(index);
            });
        }
    }

    private void ShowTab(int index)
    {
        for (int i = 0; i < views.Length; i++)
        {
            var view = views[i];
            bool isShow = index == i;
            if (isShow)
                view.Show();
            else
                view.Hide();
        }

        HandleButtonVisual(index);
    }
}