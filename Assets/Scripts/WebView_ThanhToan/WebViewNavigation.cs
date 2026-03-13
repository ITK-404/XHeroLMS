using System;
using UnityEngine;
using UnityEngine.UI;

public class WebViewNavigation : MonoBehaviour
{
    [SerializeField] private Button exitBtn;
    [SerializeField] private Button reloadBtn;
    [SerializeField] private Button leftBtn;
    [SerializeField] private Button rightBtn;

    public event Action OnExitClicked;
    public event Action OnReloadClicked;
    public event Action OnLeftNaviClicked;
    public event Action OnRightNaviClicked;

    private void Awake()
    {
        exitBtn.onClick.AddListener(ExitClick);
        reloadBtn.onClick.AddListener(ReloadClick);
        leftBtn.onClick.AddListener(LeftNaviClick);
        rightBtn.onClick.AddListener(RightNaviClick);
    }

    private void OnDestroy()
    {
        exitBtn.onClick.RemoveListener(ExitClick);
        reloadBtn.onClick.RemoveListener(ReloadClick);
        leftBtn.onClick.RemoveListener(LeftNaviClick);
        rightBtn.onClick.RemoveListener(RightNaviClick);
    }

    private void ExitClick() => OnExitClicked?.Invoke();
    private void ReloadClick() => OnReloadClicked?.Invoke();
    private void LeftNaviClick() => OnLeftNaviClicked?.Invoke();
    private void RightNaviClick() => OnRightNaviClicked?.Invoke();
}