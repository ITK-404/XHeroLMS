using System;
using UnityEngine;
using UnityEngine.UI;

public class AskForReplayTutorialUI : UIView
{
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button declineButton;

    public event Action OnClickedAcceptEvent;
    public event Action OnClickedDeclineEvent;

    protected override void Awake()
    {
        base.Awake();
        acceptButton.onClick.AddListener(OnClickedAccept);
        declineButton.onClick.AddListener(OnClickedDecline);
    }

    private void OnDestroy()
    {
        acceptButton.onClick.RemoveListener(OnClickedAccept);
        declineButton.onClick.RemoveListener(OnClickedDecline);    }

    private void OnClickedDecline()
    {
        OnClickedDeclineEvent?.Invoke();
    }

    private void OnClickedAccept()
    {
        OnClickedAcceptEvent?.Invoke();
    }
}