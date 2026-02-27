using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayerControllerUI : MonoBehaviour
{
    [SerializeField] public Button loginBtn;

    public Action OnLoginBtnClicked;

    private void Awake()
    {
        loginBtn.onClick.AddListener(ClickLoginBtn);
    }

    private void OnDestroy()
    {
        loginBtn.onClick.RemoveListener(ClickLoginBtn);
    }

    private void ClickLoginBtn()
    {
        OnLoginBtnClicked?.Invoke();
    }
}