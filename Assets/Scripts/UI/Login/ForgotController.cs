using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;
using System.Collections;
using UnityEngine.Networking;
using System.Text;

public class ForgotController : MonoBehaviour
{
    [Header("API")]
    private string baseUrl = "https://apis-dev.xheroapp.com";

    [Tooltip("Các endpoint ứng viên để yêu cầu gửi OTP quên mật khẩu (thử lần lượt cho tới khi thành công)")]
    public string[] otpRequestPathsToTry = new string[]
    {
        "/users/otp",                   // theo tài liệu
        "/api/v1/users/otp",            // nếu backend chạy dưới /api/v1
        "/users/otp/request",           // 1 số backend đặt /request
        "/users/otp/forgot-password",   // hoặc chuyên biệt cho forgot
        "/users/otp/send"               // alias phổ biến
    };

    [Header("Payload flags")]
    public string functionName = "forgot-password"; // theo Swagger
    public string platform     = "web";             // một số hệ thống yêu cầu "lms" | "web" | "game"
    public bool   isApp        = false;             // nếu BE check cờ này

    // PlayerPrefs key dùng liên thông với màn OTP
    private const string PREF_OTP_BY         = "REG_OTP_BY";          // "phone" | "email"
    private const string PREF_OTP_IDENTIFIER = "REG_OTP_IDENTIFIER";  // 84xxxx... hoặc email

    [Header("Toggles (Chỉ chọn 1)")]
    public Toggle toggleSms;
    public Toggle toggleEmail;

    [Header("Input & Button")]
    public TMP_InputField inputField;
    public Button btnEnter;
    public Button btnBack;

    [Header("Panels")]
    public GameObject backPanel;
    public GameObject currentPanel;

    [Header("Đi tới màn nhập OTP (tuỳ chọn)")]
    public GameObject otpPanel;                     // panel OTP
    public OtpVerificationController otpController; // ref tới OTP controller

    [Header("UI Thông báo (optional)")]
    public TextMeshProUGUI errorText;

    private void Start()
    {
        if (toggleSms)   toggleSms.onValueChanged.AddListener(OnSmsToggleChanged);
        if (toggleEmail) toggleEmail.onValueChanged.AddListener(OnEmailToggleChanged);
        if (btnBack)     btnBack.onClick.AddListener(OnBack);
        if (btnEnter)    btnEnter.onClick.AddListener(OnEnter);

        // mặc định chọn Email
        if (toggleSms)   toggleSms.isOn = false;
        if (toggleEmail) toggleEmail.isOn = true;

        UpdatePlaceholder();
        if (errorText) errorText.text = "";
    }

    private void OnDestroy()
    {
        if (toggleSms)   toggleSms.onValueChanged.RemoveListener(OnSmsToggleChanged);
        if (toggleEmail) toggleEmail.onValueChanged.RemoveListener(OnEmailToggleChanged);
        if (btnBack)     btnBack.onClick.RemoveListener(OnBack);
        if (btnEnter)    btnEnter.onClick.RemoveListener(OnEnter);
    }

    private void OnSmsToggleChanged(bool isOn)
    {
        if (isOn && toggleEmail) toggleEmail.isOn = false;
        UpdatePlaceholder();
    }

    private void OnEmailToggleChanged(bool isOn)
    {
        if (isOn && toggleSms) toggleSms.isOn = false;
        UpdatePlaceholder();
    }

    private void UpdatePlaceholder()
    {
        if (inputField == null || inputField.placeholder == null) return;
        var ph = inputField.placeholder.GetComponent<TextMeshProUGUI>();
        if (!ph) return;
        ph.text = (toggleSms != null && toggleSms.isOn) ? "Số điện thoại*" : "Email*";
    }

    private void OnBack()
    {
        if (currentPanel) currentPanel.SetActive(false);
        if (backPanel)    backPanel.SetActive(true);
    }

    private void OnEnter()
    {
        if (inputField == null) return;

        string raw = (inputField.text ?? "").Trim();
        bool viaSms   = toggleSms != null && toggleSms.isOn;
        bool viaEmail = toggleEmail == null ? !viaSms : toggleEmail.isOn;

        string otpBy = viaSms ? "phone" : "email";
        string identifier;

        if (viaSms)
        {
            string digits = Regex.Replace(raw, @"\D", "");
            if (!IsValidPhone(digits))
            {
                if (errorText) errorText.text = "Số điện thoại không hợp lệ.";
                return;
            }
            identifier = ConvertPhoneTo84(digits);   // đổi về 84xxxx
        }
        else
        {
            if (!IsValidEmail(raw))
            {
                if (errorText) errorText.text = "Email không hợp lệ.";
                return;
            }
            identifier = raw;
        }

        if (errorText) errorText.text = "";
        StartCoroutine(RequestForgotOtpRoutine(identifier, otpBy));
    }

    private IEnumerator RequestForgotOtpRoutine(string identifier, string otpBy)
    {
        if (btnEnter) btnEnter.interactable = false;

        string lastErr = null;

        // Build query string (GET)
        string query = $"?username={UnityWebRequest.EscapeURL(identifier)}" +
                       $"&otpBy={UnityWebRequest.EscapeURL(otpBy)}" +
                       $"&isApp={isApp.ToString().ToLower()}" +
                       $"&functionName={UnityWebRequest.EscapeURL(functionName)}" +
                       $"&platform={UnityWebRequest.EscapeURL(platform)}";

        for (int i = 0; i < otpRequestPathsToTry.Length; i++)
        {
            var path = (otpRequestPathsToTry[i] ?? "").Trim();
            if (string.IsNullOrEmpty(path)) continue;

            var url = baseUrl.TrimEnd('/') + path + query;
            Debug.Log($"[Forgot] Try OTP request -> {url}");

            using (var req = UnityWebRequest.Get(url))
            {
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Accept", "application/json");

                yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
                bool ok = req.result == UnityWebRequest.Result.Success || (req.responseCode >= 200 && req.responseCode < 300);
#else
            bool ok = !req.isNetworkError && !req.isHttpError && (req.responseCode >= 200 && req.responseCode < 300);
#endif
                if (ok)
                {
                    Debug.Log("[Forgot] OTP requested OK: " + req.downloadHandler.text);

                    PlayerPrefs.SetString(PREF_OTP_BY, otpBy);
                    PlayerPrefs.SetString(PREF_OTP_IDENTIFIER, identifier);
                    PlayerPrefs.Save();

                    AuthFlowSession.LastOtpBy = otpBy;
                    AuthFlowSession.LastOtpIdentifier = identifier;
                    AuthFlowSession.LastOtpPurpose = functionName;

                    if (otpPanel) otpPanel.SetActive(true);
                    if (currentPanel) currentPanel.SetActive(false);

                    if (otpController)
                        otpController.SetContact(identifier, otpBy, functionName);

                    if (errorText && otpPanel == null)
                    {
                        errorText.text = otpBy == "email"
                            ? "Đã gửi OTP tới email. Vui lòng kiểm tra hộp thư."
                            : "Đã gửi OTP tới số điện thoại. Vui lòng kiểm tra tin nhắn.";
                    }

                    if (btnEnter) btnEnter.interactable = true;
                    yield break;
                }

                if (req.responseCode == 404)
                {
                    Debug.LogWarning($"[Forgot] 404 Not Found at {path}. Trying next candidate...");
                    lastErr = $"404 at {path}";
                    continue;
                }

                lastErr = $"OTP request FAIL ({req.responseCode}): {req.error}\n{req.downloadHandler.text}";
                Debug.LogWarning("[Forgot] " + lastErr);

                if (errorText)
                {
                    errorText.text = string.IsNullOrEmpty(req.downloadHandler.text)
                        ? "Gửi OTP thất bại. Vui lòng thử lại."
                        : req.downloadHandler.text;
                }

                if (btnEnter) btnEnter.interactable = true;
                yield break;
            }
        }

        if (errorText)
            errorText.text = "Không tìm thấy route gửi OTP (404). Kiểm tra lại đường dẫn trên server/Swagger (ví dụ có thể là /api/v1/users/otp).";

        Debug.LogWarning("[Forgot] All candidates returned 404. Last: " + lastErr);

        if (btnEnter) btnEnter.interactable = true;
    }

    // ======= Validators =======
    // public static bool IsValidPhone(string phoneDigits) => Regex.IsMatch(phoneDigits ?? "", @"^(0?\d{9,10})$");
    public static bool IsValidPhone(string phoneDigits)
    {
        // Cho phép: 84xxxxxxxxx (11 số), 0xxxxxxxxx (10/11), xxxxxxxxx (9/10)
        return Regex.IsMatch(phoneDigits ?? "", @"^(84\d{9}|0?\d{9,10})$");
    }

    public static bool IsValidEmail(string email)       => Regex.IsMatch(email ?? "", @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

    public static string ConvertPhoneTo84(string phone)
    {
        if (string.IsNullOrEmpty(phone)) return "";
        phone = Regex.Replace(phone, @"\D", ""); // chỉ còn số

        if (phone.StartsWith("84")) return phone;      // đã là 84...
        if (phone.StartsWith("0")) return "84" + phone.Substring(1); // 0xxxxxxxxx -> 84xxxxxxxxx
        return "84" + phone; // trường hợp user gõ 9-10 số không leading 0
    }

    private static string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}