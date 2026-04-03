using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PaymentWebViewUI : UIView
{
    [SerializeField] private Button returnBtn;
    [SerializeField] private Button actionBtn;

    [SerializeField] private TextMeshProUGUI paymentCompleteTmp;
    [SerializeField] private TextMeshProUGUI paymentPriceTmp;

    [SerializeField] private Image paymentStateTxtImg;
    [SerializeField] private Image paymentNotifyImg;


    [SerializeField] private Sprite unCompletePayment;
    [SerializeField] private Sprite completePayment;
    // Text
    [SerializeField] private Sprite unCompletePaymentTxt;
    [SerializeField] private Sprite completePaymentTxt;

    [SerializeField] private string paymentUnCompleteString;
    [SerializeField] private string paymentCompleteString;
    
    public void ShowPayment(bool isComplete, string price)
    {
        Show();
        UpdateByState(isComplete);
        
        paymentPriceTmp.text = price;
        paymentCompleteTmp.text = isComplete ? paymentCompleteString : paymentUnCompleteString;

        actionBtn.GetComponentInChildren<TextMeshProUGUI>().text = isComplete ? "Vào học ngay" : "Thanh toán lại";
    }

    private void UpdateByState(bool isComplete)
    {
        paymentStateTxtImg.sprite = isComplete ? completePaymentTxt : unCompletePaymentTxt;
        paymentNotifyImg.sprite = isComplete ? completePayment : unCompletePayment;
    }
}
