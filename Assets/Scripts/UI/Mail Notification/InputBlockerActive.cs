using System;
using UnityEngine;

public class InputBlockerActive : MonoBehaviour
{
    private UIView uiView;

    private void Awake()
    {
        uiView = GetComponent<UIView>();
        if (uiView)
        {
            uiView.OnViewOpened += OnViewOpened;
            uiView.OnViewClosed += OnViewClosed;
        }
        
    }

    private void OnViewClosed()
    {
       InputBlocker.SetBlocked(false);
       InputBlocker.SuppressGameplayInput();
    }

    private void OnViewOpened()
    {
        InputBlocker.SetBlocked(true);
    }

    private void OnDisable()
    {
        if (uiView)
        {
            uiView.OnViewOpened -= OnViewOpened;
            uiView.OnViewClosed -= OnViewClosed;
        }
    }
}
