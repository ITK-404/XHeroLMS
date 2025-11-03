using System;
using Unity.VisualScripting;
using UnityEngine;

public class ToggleVideoType : ToggleBaseUI
{
    public ViewState watchVideoState;
    public Action<ViewState> OnClickVideoAction;

    public override void OnClickButton()
    {
        Debug.Log("Bắt đầu show danh sách video");
        base.OnClickButton();
        OnClickVideoAction?.Invoke(watchVideoState);
    }
}