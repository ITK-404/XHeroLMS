using System;
using UnityEngine;
using UnityEngine.UI;

public class PTS_FrameArrowNavigation : MonoBehaviour
{
    [SerializeField] private Button leftBtn;
    [SerializeField] private Button rightBtn;

    private static Action leftCallback;
    private static Action rightCallback;

    private void Awake()
    {
        leftBtn.onClick.AddListener(RaiseLeftCallback);
        rightBtn.onClick.AddListener(RaiseRightCallback);
    }

    private void OnDestroy()
    {
        leftBtn.onClick.RemoveListener(RaiseLeftCallback);
        rightBtn.onClick.RemoveListener(RaiseRightCallback);
    }

    private void RaiseLeftCallback() => leftCallback?.Invoke();
    private void RaiseRightCallback() => rightCallback?.Invoke();

    public static void AssignCallback(Action _leftCallback, Action _rightCallback)
    {
        leftCallback = _leftCallback;
        rightCallback = _rightCallback;
    }
}