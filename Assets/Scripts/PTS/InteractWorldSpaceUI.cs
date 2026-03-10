using System;
using System.Collections.Generic;
using UnityEngine;

public class InteractWorldSpaceUI : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private List<PTS_WorldSpaceUI> uiList = new();
    [SerializeField] private Vector3 offset;

    private PTS_ViewManager viewManager;
    private void Awake()
    {
        viewManager = GetComponent<PTS_ViewManager>();
        Init();
    }

    private void Init()
    {
        foreach (var item in uiList)
        {
            if(item.gameObject.activeSelf == false) continue;
            item.OnPressedButton = ShowTarget;
            item.SetCamera(playerCamera);
        }
    }

    private void ShowTarget(string target)
    {
        viewManager.TryShow(target);
    }

    private void LateUpdate()
    {
        foreach (var item in uiList)
        {
            item.SetOffset(offset);
        }
    }
}