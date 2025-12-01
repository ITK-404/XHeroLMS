using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;
using System.Collections;
using UnityEngine.Networking;
using System;
using System.Collections.Generic;
using System.Linq;

public class RegistrationController : MonoBehaviour
{
    [Header("API")]
    // private string baseUrl = LmsStore.Instance.baseUrl; // Tự động đồng bộ baseUrl với LmsStore (DEV/PROD đổi 1 chỗ duy nhất)
    private string baseUrl;
    private const string RegisterPath = "/users";
    private const string ProvincePath = "/api/v1/province/0"; // parentId = 0

    private const string PREF_USERNAME84 = "REG_USERNAME_84";

    [Header("Inputs")]
    public TMP_InputField fullNameField;
    public TMP_InputField phoneField;
    public TMP_InputField emailField;
    public TMP_InputField passwordField;
    public TMP_InputField confirmPasswordField;

    [Header("Password Show/Hide")]
    public Button btnTogglePassword;
    public Button btnToggleConfirmPassword;
    public Image  btnTogglePasswordIcon;
    public Image  btnToggleConfirmPasswordIcon;
    public Sprite iconShow;
    public Sprite iconHide;
    private bool isPasswordShown = false;

    [Header("Dropdown Khu vực (Tỉnh/Thành phố)")]
    public TMP_Dropdown regionDropdown;

    // Mapping id <-> name provinces
    private readonly List<string> _provinceIds   = new List<string>();
    private readonly List<string> _provinceNames = new List<string>();

    [Header("Toggles – Giới tính (chỉ chọn 1)")]
    public Toggle toggleMale;
    public Toggle toggleFemale;

    [Header("Toggles – OTP (chỉ chọn 1)")]
    public Toggle toggleEmail;   // chọn gửi OTP qua email
    public Toggle toggleSms;     // chọn gửi OTP qua SMS (phone)

    [Header("Xác nhận điều khoản (Toggle)")]
    public Toggle confirmToggle;

    [Header("Buttons")]
    public Button btnRegister;
    public Button btnBack;

    [Header("Panels")]
    public GameObject currentPanel;
    public GameObject backPanel;
    public GameObject otpPanel;  // bật khi đăng ký OK

    [Header("Optional UI")]
    public TextMeshProUGUI errorText;

    [Header("Rules")]
    public int passwordMinLen = 0; // gợi ý 6/8+

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
        // Fill provinces
        StartCoroutine(FetchProvincesAndFillDropdown());

        InitToggles();
        ApplyPasswordMask(false);

        if (btnTogglePassword) btnTogglePassword.onClick.AddListener(TogglePassword);
        if (btnToggleConfirmPassword) btnToggleConfirmPassword.onClick.AddListener(TogglePassword);

        if (toggleMale) toggleMale.onValueChanged.AddListener(v => { if (v && toggleFemale) toggleFemale.isOn = false; });
        if (toggleFemale) toggleFemale.onValueChanged.AddListener(v => { if (v && toggleMale) toggleMale.isOn = false; });

        if (toggleEmail) toggleEmail.onValueChanged.AddListener(v => { if (v && toggleSms) toggleSms.isOn = false; });
        if (toggleSms) toggleSms.onValueChanged.AddListener(v => { if (v && toggleEmail) toggleEmail.isOn = false; });

        if (confirmToggle)
        {
            confirmToggle.isOn = false;
            confirmToggle.onValueChanged.AddListener(isOn => { confirmed = isOn; ValidateAll(); });
        }

        if (btnRegister) btnRegister.onClick.AddListener(OnRegisterClick);
        if (btnBack) btnBack.onClick.AddListener(BackAndReset);

        if (fullNameField) fullNameField.onValueChanged.AddListener(_ => ValidateAll());
        if (phoneField) phoneField.onValueChanged.AddListener(_ => ValidateAll());
        if (emailField) emailField.onValueChanged.AddListener(_ => ValidateAll());
        if (passwordField) passwordField.onValueChanged.AddListener(_ => ValidateAll());
        if (confirmPasswordField) confirmPasswordField.onValueChanged.AddListener(_ => ValidateAll());

        ValidateAll();
    }

    private void OnDestroy()
    {
        if (btnTogglePassword)        btnTogglePassword.onClick.RemoveListener(TogglePassword);
        if (btnToggleConfirmPassword) btnToggleConfirmPassword.onClick.RemoveListener(TogglePassword);
        if (btnRegister)              btnRegister.onClick.RemoveListener(OnRegisterClick);
        if (btnBack)                  btnBack.onClick.RemoveListener(BackAndReset);
        if (confirmToggle)            confirmToggle.onValueChanged.RemoveAllListeners();
        if (regionDropdown)           regionDropdown.onValueChanged.RemoveAllListeners();
    }

    // =========================
    // PROVINCES (API)
    // =========================

    [Serializable] private class ProvinceItem { public string id; public string name; }
    [Serializable] private class ProvinceResponse { public bool status; public ProvinceItem[] data; }

    private IEnumerator FetchProvincesAndFillDropdown()
    {
        if (regionDropdown == null) yield break;

        string url = baseUrl.TrimEnd('/') + ProvincePath;
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
            if (!ok)
            {
                Debug.LogWarning($"[Province] FAIL ({req.responseCode}): {req.error}\n{req.downloadHandler.text}");
                FillDropdownWithFallback();
                yield break;
            }

            ProvinceResponse resp = null;
            try { resp = JsonUtility.FromJson<ProvinceResponse>(req.downloadHandler.text); }
            catch (Exception e) { Debug.LogWarning($"[Province] Parse error: {e.Message}"); }

            if (resp == null || resp.data == null || resp.data.Length == 0)
            {
                Debug.LogWarning("[Province] Response rỗng hoặc sai cấu trúc. Dùng fallback.");
                FillDropdownWithFallback();
                yield break;
            }

            _provinceIds.Clear();
            _provinceNames.Clear();
            foreach (var p in resp.data)
            {
                if (p == null) continue;
                _provinceIds.Add(p.id ?? "");
                _provinceNames.Add(p.name ?? "");
            }

            regionDropdown.ClearOptions();
            regionDropdown.AddOptions(_provinceNames);
            regionDropdown.value = 0;
            regionDropdown.RefreshShownValue();
        }
    }

    private void FillDropdownWithFallback()
    {
        _provinceIds.Clear();
        _provinceNames.Clear();
        string[] fallback = { "Hà Nội","TP. Hồ Chí Minh","Đà Nẵng","Bình Dương","Đồng Nai" };
        foreach (var n in fallback) { _provinceIds.Add(""); _provinceNames.Add(n); }

        if (regionDropdown)
        {
            regionDropdown.ClearOptions();
            regionDropdown.AddOptions(_provinceNames);
            regionDropdown.value = 0;
            regionDropdown.RefreshShownValue();
        }
    }

    public string GetSelectedProvinceId()
    {
        if (regionDropdown == null) return "";
        int idx = regionDropdown.value;
        if (idx < 0 || idx >= _provinceIds.Count) return "";
        return _provinceIds[idx];
    }

    public string GetSelectedProvinceName()
    {
        if (regionDropdown == null) return "";
        int idx = regionDropdown.value;
        if (idx < 0 || idx >= _provinceNames.Count) return "";
        return _provinceNames[idx];
    }

    // =========================
    // UI INIT / TOGGLES
    // =========================

    private void InitToggles()
    {
        if (toggleMale && toggleFemale)
        {
            toggleFemale.isOn = false;
            toggleMale.isOn   = true;
        }
        if (toggleEmail && toggleSms)
        {
            toggleSms.isOn   = false;
            toggleEmail.isOn = true; // mặc định dùng email
        }
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
        SetTMPPasswordField(confirmPasswordField, showPlain);
        if (btnTogglePasswordIcon)        btnTogglePasswordIcon.sprite        = showPlain ? iconShow : iconHide;
        if (btnToggleConfirmPasswordIcon) btnToggleConfirmPasswordIcon.sprite = showPlain ? iconShow : iconHide;
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
    // REGISTER
    // =========================

    private void OnRegisterClick()
    {
        if (!btnRegister || !btnRegister.interactable) return;

        string fullName = fullNameField ? fullNameField.text.Trim() : "";
        string email    = emailField    ? emailField.text.Trim()    : "";

        string phoneRaw    = phoneField ? phoneField.text : "";
        string phoneDigits = NormalizeDigits(phoneRaw);
        string username    = ConvertPhoneTo84(phoneDigits).Trim();

        string pass1 = passwordField ? passwordField.text : "";
        string pass2 = confirmPasswordField ? confirmPasswordField.text : "";

        // gender by toggles
        string gender = "other";
        if (toggleMale   != null && toggleMale.isOn)   gender = "male";
        if (toggleFemale != null && toggleFemale.isOn) gender = "female";

        // otpBy by toggles (email/sms)
        string otpBy = (toggleSms != null && toggleSms.isOn) ? "phone" : "email";
        // username chuẩn cho SMS
        string username84 = ConvertPhoneTo84(phoneDigits).Trim();

        string idForOtp = (otpBy == "email") ? email : username84;

        // province (id) – optional
        // string provinceId = GetSelectedProvinceId();
        string provinceId = GetSelectedProvinceName();

        // validate cơ bản (giữ giống trước)
        bool nameOk  = !string.IsNullOrEmpty(fullName);
        bool phoneOk = IsValidPhone(phoneDigits);
        bool emailOk = IsValidEmail(email);
        bool passOk  = IsValidPassword(pass1, passwordMinLen);
        bool matchOk = pass1 == pass2 && pass1.Length > 0;

        if (!(nameOk && phoneOk && emailOk && passOk && matchOk))
        {
            if (errorText)
                errorText.text = "Thông tin chưa hợp lệ. Vui lòng kiểm tra lại.";

            ShowWarningPopup(errorText.text);
            return;
        }

        var payload = new RegisterPayload
        {
            username          = idForOtp,                // << dùng email khi otpBy=email
            isUsernameEmail   = (otpBy == "email"),      // << bật cờ cho BE
            isApp             = false,
            password          = pass1,
            retypePassword    = pass2,
            otpBy             = otpBy,                   // "email" | "phone"
            registerPlatform  = "web",
            isFromGame        = false,

            fullName          = fullName,
            email             = email,
            province          = GetSelectedProvinceName(),
            gender            = gender
        };
        StartCoroutine(RegisterRoutine(payload, username84));
    }

    private IEnumerator RegisterRoutine(RegisterPayload payload, string username84)
    {
        btnRegister.interactable = false;
        if (errorText) errorText.text = ""; 

        string url = baseUrl.TrimEnd('/') + RegisterPath;
        string json = JsonUtility.ToJson(payload);

        using (var req = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            req.uploadHandler   = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool ok = req.result == UnityWebRequest.Result.Success || (req.responseCode >= 200 && req.responseCode < 300);
#else
            bool ok = !req.isNetworkError && !req.isHttpError && (req.responseCode >= 200 && req.responseCode < 300);
#endif

            if (ok)
            {
                Debug.Log("Register OK: " + req.downloadHandler.text);

                PlayerPrefs.SetString(PREF_USERNAME84, username84);
                PlayerPrefs.Save();
                AuthFlowSession.LastRegUsername84 = username84;

                if (currentPanel) currentPanel.SetActive(false);
                if (otpPanel) otpPanel.SetActive(true);

                OtpVerificationController targetOtp = otpController;
                if (targetOtp == null && otpPanel != null)
                    targetOtp = otpPanel.GetComponentsInChildren<OtpVerificationController>(true).FirstOrDefault();
                if (targetOtp == null)
                    targetOtp = FindFirstObjectByType<OtpVerificationController>(FindObjectsInactive.Include);

                // if (targetOtp != null) targetOtp.SetUsername(username84);
                // else Debug.LogWarning("[Register] Không tìm thấy OtpVerificationController để SetUsername.");
                if (targetOtp != null)
                {
                    // truyền đúng contact theo kênh đã chọn
                    string contact = (payload.otpBy == "email") ? payload.email : username84;
                    targetOtp.SetContact(contact, payload.otpBy, "register");
                }

                // (tuỳ chọn) lưu session để các panel khác dùng
                AuthFlowSession.LastOtpIdentifier = (payload.otpBy == "email") ? payload.email : username84;
                AuthFlowSession.LastOtpBy = payload.otpBy;
                AuthFlowSession.LastOtpPurpose = "register";

            }
            else
            {
                string raw = req.downloadHandler.text;
                string msgLog = $"Register FAIL ({req.responseCode}): {req.error}\n{raw}";
                Debug.LogWarning(msgLog);

                // ---- parse message từ BE (tuỳ chọn) ----
                string friendly = BuildFriendlyErrorMessage(req, raw);

                if (errorText) errorText.text = friendly;

                // Gọi popup luôn
                ShowWarningPopup(friendly);
            }
        }

        btnRegister.interactable = true;
    }

    // =========================
    // BACK & RESET
    // =========================

    private void BackAndReset()
    {
        ResetForm();
        if (currentPanel) currentPanel.SetActive(false);
        if (backPanel)    backPanel.SetActive(true);
    }

    private void ResetForm()
    {
        if (fullNameField)        fullNameField.text = "";
        if (phoneField)           phoneField.text    = "";
        if (emailField)           emailField.text    = "";
        if (passwordField)        passwordField.text = "";
        if (confirmPasswordField) confirmPasswordField.text = "";

        InitToggles();
        ApplyPasswordMask(false);

        if (regionDropdown) regionDropdown.value = 0;
        confirmed = false;

        if (errorText) errorText.text = "";
        ValidateAll();
    }

    // =========================
    // VALIDATION
    // =========================

    private void ValidateAll()
    {
        string fullName = fullNameField ? fullNameField.text.Trim() : "";
        string phone = phoneField ? phoneField.text.Trim() : "";
        string email = emailField ? emailField.text.Trim() : "";
        string pass1 = passwordField ? passwordField.text : "";
        string pass2 = confirmPasswordField ? confirmPasswordField.text : "";

        string normalizedPhone = NormalizeDigits(phone);

        bool nameOk = !string.IsNullOrEmpty(fullName);
        bool phoneOk = IsValidPhone(normalizedPhone);
        bool emailOk = IsValidEmail(email);
        bool passOk = IsValidPassword(pass1, passwordMinLen);
        bool matchOk = pass1 == pass2 && pass1.Length > 0;

        bool formOk = nameOk && phoneOk && emailOk && passOk && matchOk;

        if (btnRegister) btnRegister.interactable = formOk && confirmed;

        if (errorText)
        {
            if (!nameOk)
            {
                errorText.text = "Vui lòng nhập Họ và tên.";
                ShowWarningPopup(errorText.text);
            }
            else if (!phoneOk)
            {
                errorText.text = "Số điện thoại không hợp lệ.";
                ShowWarningPopup(errorText.text);
            }
            else if (!emailOk)
            {
                errorText.text = "Email không hợp lệ.";
                ShowWarningPopup(errorText.text);
            }
            else if (!passOk)
            {
                errorText.text = "Mật khẩu phải gồm chữ, số và ký tự đặc biệt.";
                ShowWarningPopup(errorText.text);
            }
            else if (!matchOk)
            {
                errorText.text = "Mật khẩu nhập lại không khớp.";
                ShowWarningPopup(errorText.text);
            }
            else if (!confirmed)
            {
                errorText.text = "Hãy đồng ý điều khoản để tiếp tục.";
                ShowWarningPopup(errorText.text);
            }
            else
            {
                errorText.text = "";
            }
        }
    }
    
    private string NormalizeDigits(string s) => Regex.Replace(s ?? "", @"\D", "");
    public static bool IsValidPhone(string digitsOnly) => Regex.IsMatch(digitsOnly ?? "", @"^(0?\d{9,10})$");
    public static bool IsValidEmail(string email)      => Regex.IsMatch(email ?? "", @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

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
        popup.Init("Cảnh báo", message);
    }
    // ---------- Payload ----------
    [Serializable]
    private class RegisterPayload
    {
        public string username;
        public bool isApp;
        public string password;
        public string retypePassword;
        public string otpBy;               // "phone" | "email"
        public string registerPlatform;
        public bool isFromGame;
        public bool isUsernameEmail;

        // bổ sung
        public string fullName;
        public string email;
        public string province;           // id tỉnh (hoặc đổi sang name nếu backend yêu cầu)
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
        // 1) Lỗi mạng
#if UNITY_2020_2_OR_NEWER
        if (req.result == UnityWebRequest.Result.ConnectionError)
#else
    if (req.isNetworkError)
#endif
        {
            return "Lỗi mạng, bạn vui lòng kiểm tra kết nối và thử lại sau giây lát.";
        }

        // 2) Thử parse JSON từ BE
        if (!string.IsNullOrEmpty(raw))
        {
            try
            {
                var err = JsonUtility.FromJson<ErrorResponse>(raw);
                if (err != null && !string.IsNullOrEmpty(err.message))
                {
                    // Nếu có map -> trả message đẹp
                    if (ErrorMessageMap.TryGetValue(err.message, out var friendly))
                    {
                        return friendly;
                    }

                    // Không có map -> generic, không quăng code thô vào mặt user
                    Debug.LogWarning("[Register] Unmapped error message code: " + err.message);
                    return "Đăng ký thất bại. Bạn vui lòng thử lại sau giây lát.";
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Register] Parse error: " + e.Message + "\nRaw: " + raw);
            }
        }

        // 3) Fallback cuối cùng
        return "Đăng ký thất bại. Bạn vui lòng thử lại sau giây lát.";
    }

    // Map mã lỗi backend -> message hiển thị
    private static readonly Dictionary<string, string> ErrorMessageMap = new Dictionary<string, string>
    {
        // Đăng ký
        { "username_is_existed", "Tài khoản này đã được đăng ký. Bạn hãy dùng số điện thoại / email khác hoặc đăng nhập." },
        { "email_is_existed",    "Email này đã được sử dụng. Bạn hãy dùng email khác hoặc đăng nhập." },
        { "phone_is_existed",    "Số điện thoại này đã được sử dụng. Bạn hãy dùng số khác hoặc đăng nhập." },

        // OTP
        { "please_wait_a_moment_to_get_new_otp", "Bạn vừa yêu cầu OTP, vui lòng chờ một lúc rồi thử lại." },
        { "otp_incorrect",  "Mã OTP không đúng. Bạn hãy kiểm tra lại." },
        { "otp_expired",    "Mã OTP đã hết hạn, bạn hãy yêu cầu mã mới." },

        // Thêm tiếp các mã BE hay trả về...
    };
}
