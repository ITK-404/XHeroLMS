using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;
using System.Collections;
using UnityEngine.Networking;
using System.Collections.Generic;

public class ForgotController : MonoBehaviour
{
    [Header("API")]
    private string baseUrl;

    [Tooltip("Các endpoint ứng viên để yêu cầu gửi OTP quên mật khẩu (thử lần lượt cho tới khi thành công)")]
    public string[] otpRequestPathsToTry = new string[]
    {
        "/users/otp",
        "/api/v1/users/otp",
        "/users/otp/request",
        "/users/otp/forgot-password",
        "/users/otp/send"
    };

    [Header("Payload flags")]
    public string functionName = "forgot-password"; // theo Swagger
    public string platform     = "web";
    public bool   isApp        = false;

    // PlayerPrefs key dùng liên thông với màn OTP
    private const string PREF_OTP_BY         = "REG_OTP_BY";         
    private const string PREF_OTP_IDENTIFIER = "REG_OTP_IDENTIFIER";

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
    public GameObject otpPanel;
    public OtpVerificationController otpController;

    [Header("UI Thông báo (optional)")]
    public TextMeshProUGUI errorText;

    [Header("Popup Warning (optional)")]
    public LoginPopupUI warningPopupPrefab;
    public Transform popupParent;

    [System.Serializable]
    private class ErrorResponse
    {
        public bool   status;
        public string message;
        public int    remaining;
        public int    statusCode;
    }

    private static readonly Dictionary<string, string> ForgotErrorMessageMap =
        new Dictionary<string, string>
    {
        { "user_not_found",  "Tài khoản này không tồn tại. Bạn vui lòng kiểm tra lại thông tin." },
        { "username_not_found",  "Tài khoản này không tồn tại. Bạn vui lòng kiểm tra lại thông tin." },
        { "username_is_not_existed", "Tài khoản này không tồn tại. Bạn vui lòng kiểm tra lại thông tin." },

        { "please_wait_a_moment_to_get_new_otp", "Bạn vừa yêu cầu OTP, vui lòng chờ một lúc rồi thử lại." },
        { "otp_limit_reached", "Bạn đã yêu cầu OTP quá nhiều lần. Vui lòng thử lại sau ít phút." },
        { "otp_too_many_request", "Bạn đã yêu cầu OTP quá nhiều lần. Vui lòng thử lại sau ít phút." }
    };

    private void Awake()
    {
        baseUrl = LmsStore.Instance.baseUrl;
    }

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

        string sdt = "Số điện thoại<color=##E95F18>*</color>";
        string email = "Email<color=##E95F18>*</color>";
        if (!ph) return;
        ph.text = (toggleSms != null && toggleSms.isOn) ? sdt : email;
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
                const string msg = "Số điện thoại không hợp lệ.";
                if (errorText) errorText.text = msg;
                ShowWarningPopup(msg);
                return;
            }
            identifier = ConvertPhoneTo84(digits);
        }
        else
        {
            if (!IsValidEmail(raw))
            {
                const string msg = "Email không hợp lệ.";
                if (errorText) errorText.text = msg;
                ShowWarningPopup(msg);
                return;
            }
            identifier = raw;
        }

        if (string.IsNullOrEmpty(identifier))
        {
            const string msg = "Vui lòng nhập thông tin tài khoản.";
            if (errorText) errorText.text = msg;
            ShowWarningPopup(msg);
            return;
        }

        if (errorText) errorText.text = "";
        StartCoroutine(RequestForgotOtpRoutine(identifier, otpBy));
    }

    private IEnumerator RequestForgotOtpRoutine(string identifier, string otpBy)
    {
        if (btnEnter) btnEnter.interactable = false;

        string lastErrLog = null;

        string query =
            $"?username={UnityWebRequest.EscapeURL(identifier)}" +
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
                bool ok = req.result == UnityWebRequest.Result.Success ||
                          (req.responseCode >= 200 && req.responseCode < 300);
#else
                bool ok = !req.isNetworkError && !req.isHttpError &&
                          (req.responseCode >= 200 && req.responseCode < 300);
#endif

                if (ok)
                {
                    Debug.Log("[Forgot] OTP requested OK: " + req.downloadHandler.text);

                    // Lưu info OTP
                    PlayerPrefs.SetString(PREF_OTP_BY, otpBy);
                    PlayerPrefs.SetString(PREF_OTP_IDENTIFIER, identifier);
                    PlayerPrefs.Save();

                    AuthFlowSession.LastOtpBy         = otpBy;
                    AuthFlowSession.LastOtpIdentifier = identifier;
                    AuthFlowSession.LastOtpPurpose    = functionName;

                    // Chuyển panel
                    if (otpPanel)     otpPanel.SetActive(true);
                    if (currentPanel) currentPanel.SetActive(false);

                    if (otpController)
                    {
                        // set contact để label trên màn OTP đúng
                        otpController.SetContact(identifier, otpBy, functionName);
                        // bắt đầu đếm ngược khi BE đã gửi OTP thành công
                        otpController.BeginCountdown();
                    }

                    // Nếu không có panel OTP riêng thì show text thông báo
                    if (errorText && otpPanel == null)
                    {
                        errorText.text = otpBy == "email"
                            ? "Đã gửi mã OTP tới email. Vui lòng kiểm tra hộp thư."
                            : "Đã gửi mã OTP tới số điện thoại. Vui lòng kiểm tra tin nhắn.";
                    }

                    if (btnEnter) btnEnter.interactable = true;
                    yield break;
                }

                // 404 -> thử route tiếp theo
                if (req.responseCode == 404)
                {
                    lastErrLog =
                        $"OTP request 404 at path '{path}': {req.error}\n{req.downloadHandler.text}";
                    Debug.LogWarning("[Forgot] " + lastErrLog);
                    continue;
                }

                string raw = req.downloadHandler.text;
                lastErrLog =
                    $"OTP request FAIL ({req.responseCode}) at path '{path}': {req.error}\n{raw}";
                Debug.LogWarning("[Forgot] " + lastErrLog);

                string friendly = BuildForgotFriendlyMessage(req, raw);

                if (errorText) errorText.text = friendly;
                ShowWarningPopup(friendly);

                if (btnEnter) btnEnter.interactable = true;
                yield break;
            }
        }

        Debug.LogWarning("[Forgot] All candidate OTP routes returned 404. Last: " + lastErrLog);

        const string finalMsg =
            "Hệ thống đang gặp sự cố khi gửi mã OTP. Bạn vui lòng thử lại sau hoặc liên hệ bộ phận hỗ trợ.";
        if (errorText) errorText.text = finalMsg;
        ShowWarningPopup(finalMsg);

        if (btnEnter) btnEnter.interactable = true;
    }

    private string BuildForgotFriendlyMessage(UnityWebRequest req, string raw)
    {
#if UNITY_2020_2_OR_NEWER
        if (req.result == UnityWebRequest.Result.ConnectionError)
#else
        if (req.isNetworkError)
#endif
        {
            return "Lỗi mạng, bạn vui lòng kiểm tra kết nối internet và thử lại.";
        }

        if (req.responseCode >= 500 && req.responseCode < 600)
        {
            return "Hệ thống đang bận hoặc bảo trì. Bạn vui lòng thử lại sau giây lát.";
        }

        if (!string.IsNullOrEmpty(raw))
        {
            try
            {
                var err = JsonUtility.FromJson<ErrorResponse>(raw);
                if (err != null && !string.IsNullOrEmpty(err.message))
                {
                    if (ForgotErrorMessageMap.TryGetValue(err.message, out var mapped))
                    {
                        return mapped;
                    }

                    Debug.LogWarning("[Forgot] Unmapped backend error code: " + err.message);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Forgot] Parse error when reading backend error JSON: " + e.Message +
                                 " | raw: " + raw);
            }
        }

        if (req.responseCode >= 400 && req.responseCode < 500)
        {
            return "Gửi mã OTP thất bại. Bạn vui lòng kiểm tra lại thông tin và thử lại.";
        }

        return "Gửi mã OTP thất bại. Bạn vui lòng thử lại sau giây lát.";
    }

    public static bool IsValidPhone(string phoneDigits)
    {
        return Regex.IsMatch(phoneDigits ?? "", @"^(84\d{9}|0?\d{9,10})$");
    }

    public static bool IsValidEmail(string email) =>
        Regex.IsMatch(email ?? "", @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

    public static string ConvertPhoneTo84(string phone)
    {
        if (string.IsNullOrEmpty(phone)) return "";
        phone = Regex.Replace(phone, @"\D", "");

        if (phone.StartsWith("84")) return phone;
        if (phone.StartsWith("0"))  return "84" + phone[1..];
        return "84" + phone;
    }

    private void ShowWarningPopup(string message)
    {
        if (warningPopupPrefab == null)
        {
            Debug.LogWarning("[Forgot] warningPopupPrefab chưa được gán. Message: " + message);
            return;
        }

        Transform parent = popupParent != null ? popupParent : transform.root;
        var popup = Instantiate(warningPopupPrefab, parent);
        popup.Init("Cảnh báo", message);
    }
}
