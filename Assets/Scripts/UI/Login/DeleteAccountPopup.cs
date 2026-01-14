using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class DeleteAccountPopup : MonoBehaviour
{
    [SerializeField] private GameObject container;
    [SerializeField] private CanvasGroup canvasGroup;

    [SerializeField] private Button deleteAccountBtn;
    [SerializeField] private Button continueBtn;

    private void Awake()
    {
        continueBtn.onClick.AddListener(Hide);
        deleteAccountBtn.onClick.AddListener(OnDeleteAccount);
    }

    private void OnDestroy()
    {
        continueBtn.onClick.RemoveListener(Hide);
        deleteAccountBtn.onClick.RemoveListener(OnDeleteAccount);
        
    }

    private void OnDeleteAccount()
    {
        // 
    }

    public void Show()
    {
        canvasGroup.gameObject.SetActive(true);
        canvasGroup.DOFade(0, 0);
        canvasGroup.DOFade(1, 0.2f);
    }

    public void Hide()
    {
        canvasGroup.gameObject.SetActive(false);
    }
}