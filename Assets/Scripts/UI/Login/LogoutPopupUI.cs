using System;
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
}
