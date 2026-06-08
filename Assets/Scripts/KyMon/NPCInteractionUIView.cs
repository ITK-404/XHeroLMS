using System;
using UnityEngine;
using UnityEngine.UI;

public class NPCInteractionUIView : UIView
{
    [SerializeField] private Button worldSpaceBtn;
    [SerializeField] private Transform worldSpaceIcon;
    [SerializeField] private Transform supportChatBox;

    public event Action OnClickWorldSpaceEvent;
    
    protected override void Awake()
    {
        base.Awake();
        worldSpaceBtn.onClick.AddListener(ClickWorldSpace);
    }

    private void OnDestroy()
    {
        worldSpaceBtn.onClick.RemoveListener(ClickWorldSpace);
    }

    private void ClickWorldSpace() => OnClickWorldSpaceEvent?.Invoke();

    public void ShowWorldSpaceIcon()
    {
        worldSpaceIcon.gameObject.SetActive(true);   
        supportChatBox.gameObject.SetActive(false);   
    }

    public void ShowSupportChatBox()
    {
        worldSpaceIcon.gameObject.SetActive(false);   
        supportChatBox.gameObject.SetActive(true);   
    }
}