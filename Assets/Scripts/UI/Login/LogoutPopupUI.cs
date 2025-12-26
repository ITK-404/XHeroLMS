using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class LogoutPopupUI : MonoBehaviour
{
    public Button logoutBtn;
    public Button returnBtn;

    public static Action OnLogout;
    public static Action OnReturn;

    public void Awake()
    {
        logoutBtn.onClick.AddListener(Logout);
        returnBtn.onClick.AddListener(Return);

        _canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnDestroy()
    {
        logoutBtn.onClick.RemoveListener(Logout);
        returnBtn.onClick.RemoveListener(Return);
    }
    private void Logout()
    {
        OnLogout?.Invoke();
    }

    private void Return()
    {
        OnReturn?.Invoke();
    }

    [SerializeField] private CanvasGroup _canvasGroup;
    public void Show()
    {
        _canvasGroup.gameObject.SetActive(true);
        _canvasGroup.DOFade(0, 0);
        _canvasGroup.DOFade(1, 0.2f);
    }

    public void Hide()
    {
        _canvasGroup.gameObject.SetActive(false);
    }
}
