using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class AskForReplayTutorialUI : UIView
{
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button declineButton;

    // Biến này giữ "lời hứa" sẽ trả kết quả trong tương lai
    private UniTaskCompletionSource<bool> _tcs;

    protected override void Awake()
    {
        base.Awake();
        acceptButton.onClick.AddListener(OnClickedAccept);
        declineButton.onClick.AddListener(OnClickedDecline);
    }

    private void OnDestroy()
    {
        acceptButton.onClick.RemoveListener(OnClickedAccept);
        declineButton.onClick.RemoveListener(OnClickedDecline);

        _tcs?.TrySetCanceled();
    }

    public UniTask<bool> ShowAsync()
    {
        Show();

        _tcs = new UniTaskCompletionSource<bool>();
        return _tcs.Task;
    }

    private void OnClickedAccept()
    {
        _tcs?.TrySetResult(true);
        _tcs = null;
        gameObject.SetActive(false);
    }

    private void OnClickedDecline()
    {
        _tcs?.TrySetResult(false);
        _tcs = null;
        gameObject.SetActive(false);
    }
}