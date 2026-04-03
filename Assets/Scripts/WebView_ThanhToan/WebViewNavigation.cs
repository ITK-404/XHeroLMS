using System;
using TMPro;
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

    [SerializeField] private CanvasGroup leftNavCanvas;
    [SerializeField] private CanvasGroup rightNavCanvas;
    [SerializeField] private TextMeshProUGUI title;
    private void Awake()
    {
        exitBtn.onClick.AddListener(ExitClick);
        reloadBtn.onClick.AddListener(ReloadClick);
        leftBtn.onClick.AddListener(LeftNaviClick);
        rightBtn.onClick.AddListener(RightNaviClick);

        // Set title ngay khi mở WebView
        InitTitle();
    }

    private void InitTitle()
    {
        string t = WebViewTest.StoreTitleCourse;

        if (string.IsNullOrWhiteSpace(t))
            t = "Web View"; // fallback

        title.text = t;
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

    public void SetNavigationState(bool leftNavActive, bool rightNavActive)
    {
        leftNavCanvas.alpha = leftNavActive ? 1 : 0.5f;
        rightNavCanvas.alpha = rightNavActive ? 1 : 0.5f;
    }

    public void SetTitle(string title) => this.title.text = title;
}