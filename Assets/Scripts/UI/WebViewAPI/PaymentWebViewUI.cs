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
    [SerializeField] private Sprite unCompletePaymentTxt;
    [SerializeField] private Sprite completePaymentTxt;

    [SerializeField] private string paymentUnCompleteString;
    [SerializeField] private string paymentCompleteString;

    public Button ReturnButton => returnBtn;
    public Button ActionButton => actionBtn;

    public void ShowPayment(bool isComplete, string price)
    {
        Show();
        UpdateByState(isComplete);

        if (paymentPriceTmp != null)
            paymentPriceTmp.text = price;

        if (paymentCompleteTmp != null)
            paymentCompleteTmp.text = isComplete ? paymentCompleteString : paymentUnCompleteString;

        SetActionButtonText(isComplete);
    }

    public void SetActionButtonText(bool isComplete)
    {
        if (actionBtn == null) return;

        var tmp = actionBtn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp != null)
            tmp.text = isComplete ? "VÀO HỌC NGAY" : "THANH TOÁN LẠI";
    }

    private void UpdateByState(bool isComplete)
    {
        if (paymentStateTxtImg != null)
            paymentStateTxtImg.sprite = isComplete ? completePaymentTxt : unCompletePaymentTxt;

        if (paymentNotifyImg != null)
            paymentNotifyImg.sprite = isComplete ? completePayment : unCompletePayment;
    }
}