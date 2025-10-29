using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;

public class ResetPasswordController : MonoBehaviour
{
    [Header("Inputs")]
    public TMP_InputField passField;
    public TMP_InputField confirmField;

    [Header("Buttons")]
    public Button btnEnter;       // Hiện tại chỉ bật/tắt theo validate
    public Button btnBack;

    [Header("Panels")]
    public GameObject currentPanel;
    public GameObject backPanel;

    [Header("Optional UI")]
    public TextMeshProUGUI errorText;  // có thể bỏ trống

    [Header("Rules")]
    [Tooltip("Tối thiểu độ dài; 0 = không kiểm tra độ dài")]
    public int minLength = 0; // đặt 6/8 nếu muốn

    private void Start()
    {
        if (btnBack != null) btnBack.onClick.AddListener(OnBack);

        if (passField != null) passField.onValueChanged.AddListener(_ => Validate());
        if (confirmField != null) confirmField.onValueChanged.AddListener(_ => Validate());

        Validate(); // chạy lần đầu
    }

    private void OnDestroy()
    {
        if (btnBack != null) btnBack.onClick.RemoveListener(OnBack);

        if (passField != null) passField.onValueChanged.RemoveAllListeners();
        if (confirmField != null) confirmField.onValueChanged.RemoveAllListeners();
    }

    private void OnBack()
    {
        if (currentPanel != null) currentPanel.SetActive(false);
        if (backPanel != null) backPanel.SetActive(true);
    }

    // ====== Validate ======

    private void Validate()
    {
        string p1 = passField != null ? passField.text : "";
        string p2 = confirmField != null ? confirmField.text : "";

        bool match = p1 == p2 && p1.Length > 0;
        bool strong = IsValidPassword(p1, minLength);

        bool ok = match && strong;

        if (btnEnter != null) btnEnter.interactable = ok;

        if (errorText != null)
        {
            if (!match) errorText.text = "Mật khẩu nhập lại không khớp.";
            else if (!strong) errorText.text = "Mật khẩu phải gồm chữ cái, số và ký tự đặc biệt.";
            else errorText.text = "";
        }
    }

    /// <summary>
    /// Hợp lệ khi: có ít nhất 1 chữ cái, 1 chữ số, 1 ký tự đặc biệt.
    /// Nếu minLen > 0 thì yêu cầu thêm độ dài tối thiểu.
    /// </summary>
    public static bool IsValidPassword(string s, int minLen = 0)
    {
        if (string.IsNullOrEmpty(s)) return false;

        // Lookaheads: 1 chữ cái, 1 số, 1 ký tự đặc biệt (không phải chữ/số)
        string core = @"(?=.*[A-Za-z])(?=.*\d)(?=.*[^A-Za-z0-9])";

        string len = (minLen > 0) ? $@"(?=.{{{minLen},}})" : "";
        string pattern = $"^{len}{core}.+$";

        return Regex.IsMatch(s, pattern);
    }
}
