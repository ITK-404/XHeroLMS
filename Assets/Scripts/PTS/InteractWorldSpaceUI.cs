using System;
using System.Collections.Generic;
using UnityEngine;

public class InteractWorldSpaceUI : MonoBehaviour
{
    [SerializeField] private PTS_WorldSpaceUI uiPrefab;
    [SerializeField] private Transform container;
    [SerializeField] private List<Transform> items = new();

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

    private void OnClickButtonEvent()
    {
        interactHandle.OnEnterCourseView();
    }

    private void Init()
    {
        foreach (var item in items)
        {
            if(item.gameObject.activeSelf == false) continue;
            
            var ui = Instantiate(uiPrefab, container);
            ui.SetCamera(playerCamera);
            ui.SetTarget(item);
            uiList.Add(ui);
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