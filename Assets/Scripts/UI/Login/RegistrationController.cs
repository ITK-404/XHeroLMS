using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

public class RegistrationController : MonoBehaviour
{
    [Header("API")]
    private string baseUrl;
    private const string RegisterPath = "/users";
    private const string PREF_USERNAME84 = "REG_USERNAME_84";

    [Header("Inputs")]
    public TMP_InputField phoneField;
    public TMP_InputField passwordField;
    public TMP_InputField referralCodeField; // Mã giới thiệu (nếu có, tạm chưa dùng)

    [Header("Password Show/Hide")]
    public Button btnTogglePassword;
    public Image  btnTogglePasswordIcon;
    public Sprite iconShow;
    public Sprite iconHide;
    private bool isPasswordShown = false;

    [Header("Xác nhận điều khoản")]
    public Toggle confirmToggle;

    [Header("Buttons")]
    public Button btnRegister;

    [Header("Panels")]
    public GameObject currentPanel;
    public GameObject backPanel;
    public GameObject otpPanel;

    [Header("Optional UI")]
    public TextMeshProUGUI errorText;

    [Header("Rules")]
    public int passwordMinLen = 0; // ví dụ set = 6 trong Inspector

    [Header("Refs (optional)")]
    public OtpVerificationController otpController;

    [Header("Popup Warning (optional)")]
    public LoginPopupUI warningPopupPrefab;
    public Transform popupParent;

    private bool confirmed = false;

    private void Awake()
    {
        baseUrl = LmsStore.Instance.baseUrl;
    }

    private void Start()
    {
        // Bàn phím số cho SĐT (mobile)
        if (phoneField != null)
        {
            phoneField.contentType = TMP_InputField.ContentType.IntegerNumber;
#if UNITY_ANDROID || UNITY_IOS
            phoneField.keyboardType = TouchScreenKeyboardType.NumberPad;
#endif
            phoneField.ForceLabelUpdate();
        }

        ApplyPasswordMask(false);

        if (btnTogglePassword) btnTogglePassword.onClick.AddListener(TogglePassword);

        if (confirmToggle)
        {
            confirmToggle.isOn = false;
            confirmToggle.onValueChanged.AddListener(isOn =>
            {
                confirmed = isOn;
                ValidateAll();
            });
        }

        if (btnRegister) btnRegister.onClick.AddListener(OnRegisterClick);

        if (phoneField)    phoneField.onValueChanged.AddListener(_ => ValidateAll());
        if (passwordField) passwordField.onValueChanged.AddListener(_ => ValidateAll());

        ValidateAll();
    }

    private void OnDestroy()
    {
        if (btnTogglePassword) btnTogglePassword.onClick.RemoveListener(TogglePassword);
        if (btnRegister)       btnRegister.onClick.RemoveListener(OnRegisterClick);
        if (confirmToggle)     confirmToggle.onValueChanged.RemoveAllListeners();
    }

    // =========================
    // PASSWORD
    // =========================

    private void TogglePassword()
    {
        isPasswordShown = !isPasswordShown;
        ApplyPasswordMask(isPasswordShown);
    }

    private void ApplyPasswordMask(bool showPlain)
    {
        SetTMPPasswordField(passwordField, showPlain);
        if (btnTogglePasswordIcon) btnTogglePasswordIcon.sprite = showPlain ? iconShow : iconHide;
    }

    private void SetTMPPasswordField(TMP_InputField field, bool showPlain)
    {
        if (field == null) return;
        field.contentType = showPlain ? TMP_InputField.ContentType.Standard
                                      : TMP_InputField.ContentType.Password;
        field.asteriskChar = '*';
        field.ForceLabelUpdate();
    }

    // =========================
    // REGISTER FLOW
    // =========================

    private void OnRegisterClick()
    {
        if (!btnRegister || !btnRegister.interactable) return;

        string phoneRaw    = phoneField ? phoneField.text : "";
        string phoneDigits = NormalizeDigits(phoneRaw);

        string pass1 = passwordField ? passwordField.text : "";

        // Chỉ dùng phone để gửi OTP
        string otpBy      = "phone";
        string username84 = ConvertPhoneTo84(phoneDigits).Trim();

        // validate
        bool phoneOk     = IsValidPhone(phoneDigits);
        bool passOk      = IsValidPassword(pass1, passwordMinLen);
        bool termsAgreed = confirmed;

        string message = null;
        if (!phoneOk)
            message = "Số điện thoại không hợp lệ.";
        else if (!passOk)
            message = "Mật khẩu không hợp lệ.";
        else if (!termsAgreed)
            message = "Hãy đồng ý điều khoản để tiếp tục.";

        if (message != null)
        {
            if (errorText) errorText.text = message;
            ShowWarningPopup(message);
            return;
        }

        // Payload đơn giản – BE sẽ gửi OTP theo phone
        var payload = new RegisterPayload
        {
            username          = username84,
            isUsernameEmail   = false,
            isApp             = false,
            password          = pass1,
            retypePassword    = pass1,
            otpBy             = otpBy,          // "phone"
            registerPlatform  = "web",
            isFromGame        = false,

            fullName          = phoneDigits,
            email             = "",
            province          = "",
            gender            = "male"
        };

        StartCoroutine(RegisterRoutine(payload, username84));

        Debug.Log($"[Register F] phoneRaw='{phoneRaw}', phoneDigits='{phoneDigits}', username84='{username84}'");

    }

    private IEnumerator RegisterRoutine(RegisterPayload payload, string username84)
    {
        btnRegister.interactable = false;
        if (errorText) errorText.text = "";

        string url  = baseUrl.TrimEnd('/') + RegisterPath;
        string json = JsonUtility.ToJson(payload);

        using (var req = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            req.uploadHandler   = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

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
                Debug.Log("Register OK: " + req.downloadHandler.text);

                // Lưu lại username 84 để dùng sau
                PlayerPrefs.SetString(PREF_USERNAME84, username84);
                PlayerPrefs.Save();
                AuthFlowSession.LastRegUsername84 = username84;

                if (currentPanel) currentPanel.SetActive(false);
                if (otpPanel)     otpPanel.SetActive(true);

                OtpVerificationController targetOtp = otpController;
                if (targetOtp == null && otpPanel != null)
                    targetOtp = otpPanel.GetComponentsInChildren<OtpVerificationController>(true).FirstOrDefault();
                if (targetOtp == null)
                    targetOtp = FindFirstObjectByType<OtpVerificationController>(FindObjectsInactive.Include);

                if (targetOtp != null)
                {
                    // Luôn dùng phone
                    string contact = username84;
                    targetOtp.SetContact(contact, "phone", "register");
                    targetOtp.BeginCountdown();
                }

                AuthFlowSession.LastOtpIdentifier = username84;
                AuthFlowSession.LastOtpBy         = "phone";
                AuthFlowSession.LastOtpPurpose    = "register";
            }
            else
            {
                string raw    = req.downloadHandler.text;
                string msgLog = $"Register FAIL ({req.responseCode}): {req.error}\n{raw}";
                Debug.LogWarning(msgLog);

                string friendly = BuildFriendlyErrorMessage(req, raw);

                if (errorText) errorText.text = friendly;
                ShowWarningPopup(friendly);
            }
        }

        btnRegister.interactable = true;
        Debug.Log($"[Register F] POST {url} body={json}");

    }

    private void ResetForm()
    {
        if (phoneField)    phoneField.text    = "";
        if (passwordField) passwordField.text = "";
        if (referralCodeField) referralCodeField.text = "";

        ApplyPasswordMask(false);

        if (confirmToggle) confirmToggle.isOn = false;
        confirmed = false;

        if (errorText) errorText.text = "";
        ValidateAll();
    }

    // =========================
    // VALIDATION HELPERS
    // =========================

    private void ValidateAll()
    {
        if (errorText) errorText.text = "";
        if (btnRegister) btnRegister.interactable = true;
    }

    private string NormalizeDigits(string s) => Regex.Replace(s ?? "", @"\D", "");

    public static bool IsValidPhone(string digitsOnly)
        => Regex.IsMatch(digitsOnly ?? "", @"^(0?\d{9,10})$");

    public static bool IsValidPassword(string s, int minLen = 0)
    {
        if (string.IsNullOrEmpty(s)) return false;
string core = @"(?=.*[A-Za-z])(?=.*\d)(?=.*[^A-Za-z0-9])";
        string len  = (minLen > 0) ? $@"(?=.{{{minLen},}})" : "";
        return Regex.IsMatch(s, $"^{len}{core}.+$");
    }

    public static string ConvertPhoneTo84(string phoneRaw)
    {
        if (string.IsNullOrEmpty(phoneRaw)) return "";
        string p = Regex.Replace(phoneRaw, @"\D", "");
        if (p.StartsWith("0"))        p = "84" + p.Substring(1);
        else if (!p.StartsWith("84")) p = "84" + p;
        return p;
    }

    private void ShowWarningPopup(string message)
    {
        if (warningPopupPrefab == null)
        {
            Debug.LogWarning("[Register] warningPopupPrefab chưa được gán. Msg: " + message);
            return;
        }

        Transform parent = popupParent != null ? popupParent : transform.root;
        var popup = Instantiate(warningPopupPrefab, parent);
        popup.Init("Cảnh báo", message, LoginPopupUI.PopupIconType.Warning);
    }

    // ---------- Payload ----------
    [Serializable]
    private class RegisterPayload
    {
        public string username;
        public bool isApp;
        public string password;
        public string retypePassword;
        public string otpBy;               // "phone"
        public string registerPlatform;
        public bool isFromGame;
        public bool isUsernameEmail;

        // Optional fields cho BE
        public string fullName;
        public string email;
        public string province;
        public string gender;
    }

    [Serializable]
    private class ErrorResponse
    {
        public bool status;
        public string message;
        public int remaining;
        public int statusCode;
    }

    private string BuildFriendlyErrorMessage(UnityWebRequest req, string raw)
    {
#if UNITY_2020_2_OR_NEWER
        if (req.result == UnityWebRequest.Result.ConnectionError)
#else
        if (req.isNetworkError)
#endif
        {
            return "Lỗi mạng, bạn vui lòng kiểm tra kết nối và thử lại.";
        }

        if (!string.IsNullOrEmpty(raw))
        {
            try
            {
                var err = JsonUtility.FromJson<ErrorResponse>(raw);
                if (err != null && !string.IsNullOrEmpty(err.message))
                {
                    if (ErrorMessageMap.TryGetValue(err.message, out var friendly))
                        return friendly;

                    Debug.LogWarning("[Register] Unmapped error message code: " + err.message);
                    return "Đăng ký thất bại. Bạn vui lòng thử lại sau.";
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Register] Parse error: " + e.Message + "\nRaw: " + raw);
            }
        }

        return "Đăng ký thất bại. Bạn vui lòng thử lại sau.";
    }

    private static readonly Dictionary<string, string> ErrorMessageMap = new Dictionary<string, string>
    {
        { "username_is_existed", "Số điện thoại này đã được đăng ký. Bạn hãy dùng số khác hoặc đăng nhập." },
        { "please_wait_a_moment_to_get_new_otp", "Bạn vừa yêu cầu OTP, vui lòng chờ một lúc rồi thử lại." },
        { "otp_incorrect", "Mã OTP không đúng. Bạn hãy kiểm tra lại." },
        { "otp_expired",   "Mã OTP đã hết hạn, bạn hãy yêu cầu mã mới." },
    };
}
