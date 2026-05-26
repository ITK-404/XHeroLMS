using System;
using UnityEngine;
using UnityEngine.UI;

public class UIView_ResetScrollRect : MonoBehaviour
{
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private UIView uiView;

    private void OnValidate()
    {
        if (scrollRect == null)
        {
            scrollRect = GetComponent<ScrollRect>();
        }
    }

    private void Awake()
    {
        if (uiView != null)
        {
            uiView.OnViewClosed += ResetOnClose;
        }
    }

    private void OnDestroy()
    {
        if (uiView != null)
        {
            uiView.OnViewClosed -= ResetOnClose;
        }
    }

    private void ResetOnClose()
    {
        if (scrollRect != null)
        {
            scrollRect.horizontalNormalizedPosition = 1;
            scrollRect.verticalNormalizedPosition = 1;
        }
    }
}