using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;

public class ForgotController : MonoBehaviour
{
    [Header("Toggles (Chỉ chọn 1)")]
    public Toggle toggleSms;
    public Toggle toggleEmail;

    [Header("Input & Button")]
    public TMP_InputField inputField;
    public Button btnEnter;
    public Button btnBack;

    [Header("Panel quay về")]
    public GameObject backPanel;
    public GameObject currentPanel;

    private void Start()
    {
        toggleSms.onValueChanged.AddListener(OnSmsToggleChanged);
        toggleEmail.onValueChanged.AddListener(OnEmailToggleChanged);
        btnBack.onClick.AddListener(OnBack);

        toggleSms.isOn = false;
        toggleEmail.isOn = true; // mặc định chọn Email

        UpdatePlaceholder();
    }

    private void OnDestroy()
    {
        toggleSms.onValueChanged.RemoveListener(OnSmsToggleChanged);
        toggleEmail.onValueChanged.RemoveListener(OnEmailToggleChanged);
        btnBack.onClick.RemoveListener(OnBack);
    }

    private void OnSmsToggleChanged(bool isOn)
    {
        if (isOn)
        {
            toggleEmail.isOn = false;
            UpdatePlaceholder();
        }
    }

    private void OnEmailToggleChanged(bool isOn)
    {
        if (isOn)
        {
            toggleSms.isOn = false;
            UpdatePlaceholder();
        }
    }

    private void UpdatePlaceholder()
    {
        if (toggleSms.isOn)
            inputField.placeholder.GetComponent<TextMeshProUGUI>().text = "Số điện thoại*";
        else
            inputField.placeholder.GetComponent<TextMeshProUGUI>().text = "Email*";
    }

    private void OnBack()
    {
        if (currentPanel != null) currentPanel.SetActive(false);
        if (backPanel != null) backPanel.SetActive(true);
    }

    public static bool IsValidPhone(string phone)
    {
        // Chấp nhận 0xxxxxxxxx hoặc xxxxxxxxx
        return Regex.IsMatch(phone, @"^(0?\d{9,10})$");
    }

    public static bool IsValidEmail(string email)
    {
        return Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");
    }

    // định dạng số điện thoại sang 84
    public static string ConvertPhoneTo84(string phone)
    {
        if (string.IsNullOrEmpty(phone)) return "";

        // Bỏ khoảng trắng, ký tự lạ
        phone = Regex.Replace(phone, @"\D", "");

        // Nếu bắt đầu bằng 0 → thay bằng 84
        if (phone.StartsWith("0"))
            phone = "84" + phone.Substring(1);
        else if (!phone.StartsWith("84"))
            phone = "84" + phone;

        return phone;
    }
}
