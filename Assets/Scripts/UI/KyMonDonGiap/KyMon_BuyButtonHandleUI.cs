using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KyMon_BuyButtonHandleUI : MonoBehaviour
{
    [SerializeField] private Button registerBtn;
    [SerializeField] private Button buyBtn;

    public event Action OnBuyClickedEvent;
    public event Action OnRegisterClickedEvent;

    private void Awake()
    {
        registerBtn.onClick.AddListener(RegisterButtonClicked);
        buyBtn.onClick.AddListener(BuyButtonClicked);
    }

    private void OnDestroy()
    {
        registerBtn.onClick.RemoveListener(RegisterButtonClicked);
        buyBtn.onClick.RemoveListener(BuyButtonClicked);
    }

    private void Start()
    {
        HideBothButtons();
    }

    private void RegisterButtonClicked()
    {
        OnRegisterClickedEvent?.Invoke();
    }

    private void BuyButtonClicked()
    {
        OnBuyClickedEvent?.Invoke();
    }

    public void SetBuyText(string buyText)
    {
        var tmp = buyBtn.GetComponentInChildren<TMP_Text>();
        if (tmp == null)
        {
            Debug.LogError("KyMon_BuyButtonHandleUI: button does not contain text");
            return;
        }

        tmp.text = buyText;
    }
    
    public void ShowBuyButton()
    {
        buyBtn.gameObject.SetActive(true);
        registerBtn.gameObject.SetActive(false);
    }

    public void ShowRegisterButton()
    {
        buyBtn.gameObject.SetActive(false);
        registerBtn.gameObject.SetActive(true);
    }

    public void BothButtons()
    {
        ShowBuyButton();
        ShowRegisterButton();
    }

    public void HideBothButtons()
    {
        buyBtn.gameObject.SetActive(false);
        registerBtn.gameObject.SetActive(false);
    }
}