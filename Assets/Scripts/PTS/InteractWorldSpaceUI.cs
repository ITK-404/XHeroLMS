using System;
using System.Collections.Generic;
using UnityEngine;

public class InteractWorldSpaceUI : MonoBehaviour
{
    [SerializeField] private PTS_WorldSpaceUI uiPrefab;
    [SerializeField] private Transform container;

    [SerializeField] private Camera playerCamera;
    [SerializeField] private List<PTS_WorldSpaceUI> uiList = new();
    [SerializeField] private Vector3 offset;

    private InteractionManagerUI interactHandle;
    
    private void Awake()
    {
        interactHandle = GetComponent<InteractionManagerUI>();
        Init();
        
        PTS_WorldSpaceUI.OnClickButtonEvent += OnClickButtonEvent;
    }

    private void OnDestroy()
    {
        PTS_WorldSpaceUI.OnClickButtonEvent -= OnClickButtonEvent;
    }

    private void OnClickButtonEvent(UIView bindingView)
    {
        interactHandle.OnEnterCourseView(bindingView);
    }

    private void Init()
    {
        foreach (var item in uiList)
        {
            if(item.gameObject.activeSelf == false) continue;
            
            item.SetCamera(playerCamera);
        }
    }

    private void LateUpdate()
    {
        foreach (var item in uiList)
        {
            item.SetOffset(offset);
        }
    }
}