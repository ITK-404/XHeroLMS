using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class DeleteAccountPopup : MonoBehaviour
{
    [SerializeField] private GameObject container;
    [SerializeField] private CanvasGroup canvasGroup;

    [SerializeField] private Button deleteAccountBtn;
    [SerializeField] private Button continueBtn;

    public static Action OnDeleteAccountAction;
    private bool ownsGameplayLock;
    
    private void Awake()
    {
        continueBtn.onClick.AddListener(Hide);
        deleteAccountBtn.onClick.AddListener(OnDeleteAccount);
    }

    private void OnDestroy()
    {
        continueBtn.onClick.RemoveListener(Hide);
        deleteAccountBtn.onClick.RemoveListener(OnDeleteAccount);
        SetGameplayLock(false);
        
    }

    private void OnDeleteAccount()
    {
        // 
        OnDeleteAccountAction?.Invoke();
    }

    public void Show()
    {
        canvasGroup.gameObject.SetActive(true);
        canvasGroup.DOFade(0, 0);
        canvasGroup.DOFade(1, 0.2f);
        SetGameplayLock(true);
    }

    public void Hide()
    {
        canvasGroup.gameObject.SetActive(false);
        SetGameplayLock(false);
    }

    private void SetGameplayLock(bool locked)
    {
        if (ownsGameplayLock == locked)
            return;

        if (locked)
        {
            GameplayLock.Lock(GameplayLockReason.UI, GameplayLockTarget.All);
        }
        else
        {
            GameplayLock.Unlock(GameplayLockReason.UI);
        }

        ownsGameplayLock = locked;
    }
}
