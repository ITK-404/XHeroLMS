using System;
using UnityEngine;
using UnityEngine.UI;

public class AdvanceKyMonCourseUIView : UIView
{
    [SerializeField] private Button returnBtn;
    public event Action OnClickReturnEvent;

    protected override void Awake()
    {
        base.Awake();
        returnBtn.onClick.AddListener(OnClickReturnButton);
    }

    private void OnDestroy()
    {
        returnBtn.onClick.RemoveListener(OnClickReturnButton);
    }

    private void OnClickReturnButton()
    {
        OnClickReturnEvent?.Invoke();
    }
}