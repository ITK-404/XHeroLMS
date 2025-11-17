using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Text;
using System.Collections;

public class OtpVerificationController : MonoBehaviour
{
    [Header("API")]
    // private string baseUrl = LmsStore.Instance.baseUrl; // Tự động đồng bộ baseUrl với LmsStore (DEV/PROD đổi 1 chỗ duy nhất)
    private string baseUrl;

    private const string PREF_USERNAME84     = "REG_USERNAME_84";
    private const string PREF_OTP_BY         = "REG_OTP_BY";
    private const string PREF_OTP_IDENTIFIER = "REG_OTP_IDENTIFIER";

    [Header("Hiển thị thời gian")]
    public TextMeshProUGUI minuteText;
    public TextMeshProUGUI secondText;

    [Header("6 ô nhập OTP")]
    public TMP_InputField[] otpInputs = new TMP_InputField[6];

    [Header("Buttons")]
    public Button btnEnter;
    public Button btnBack;

    [Header("Timer (giây)")]
    public int totalSeconds = 60;

    [Header("Panels")]
    public GameObject successPanel; // dùng cho flow đăng ký
    public GameObject imageToShow;  // optional: show logo/ảnh khi verify OK
    public GameObject currentPanel;
    public GameObject backPanel;

    [Header("Reset Password flow (optional)")]
    [Tooltip("Mở panel Reset Password khi OTP thuộc flow quên mật khẩu")]
    public bool openResetOnSuccess = true;
    public GameObject resetPanel;
    public ResetPasswordController resetController;
    [Tooltip("Giá trị purpose cho flow forgot (đồng bộ với ForgotController)")]
    public string forgotPurposeKey = "forgot-password";

    [Header("Resend OTP")]
    public Button resendButton;
    public int resendCooldownSeconds = 60;

    [Header("Optional UI")]
    public TextMeshProUGUI errorText;

    // ========= State =========
    private int  remainingSeconds;
    private bool isRunning       = false;
    private bool isSubmitting    = false;
    private bool _resending      = false;

    // Liên hệ xác thực (email hoặc 84xxxxx)
    [SerializeField] private string contactIdentifier = "";
    [SerializeField] private string otpByChannel      = ""; // "phone" | "email"
    [SerializeField] private string otpPurpose        = ""; // "forgot-password" | "register"

    private void Awake()
    {
        baseUrl = LmsStore.Instance.baseUrl;
    }
    /// <summary>Gọi khi chuyển từ Register/Forgot sang OTP</summary>
    public void SetContact(string identifier, string otpBy, string purpose = "")
    {
        contactIdentifier = (identifier ?? "").Trim();
        otpByChannel = (otpBy ?? "").Trim();
        otpPurpose = (purpose ?? "").Trim();

        AuthFlowSession.LastOtpIdentifier = contactIdentifier;
        AuthFlowSession.LastOtpBy = otpByChannel;
        if (!string.IsNullOrEmpty(otpPurpose)) AuthFlowSession.LastOtpPurpose = otpPurpose;
    }

    /// <summary>Giữ tương thích cũ (đăng ký qua SMS: nhận số 84...)</summary>
    public void SetUsername(string username84FromRegister) => SetContact(username84FromRegister, "phone", "register");

    private void OnEnable()
    {
        EnsureContactFromSessionOrPrefs();

        // Clear 6 ô và focus
        for (int i = 0; i < otpInputs.Length; i++)
            if (otpInputs[i] != null) otpInputs[i].text = "";

        if (otpInputs.Length > 0 && otpInputs[0] != null) otpInputs[0].Select();
        if (errorText) errorText.text = "";

        // Bind resend
        if (resendButton)
        {
            resendButton.onClick.RemoveListener(OnClickResend);
            resendButton.onClick.AddListener(OnClickResend);
            SetButtonLabel(resendButton, "Gửi lại OTP");
            resendButton.interactable = true;
        }
    }

    private void Start()
    {
        EnsureContactFromSessionOrPrefs();
        ResetTimer();

        // OTP inputs
        for (int i = 0; i < otpInputs.Length; i++)
        {
            int index = i;
            if (otpInputs[i] == null) continue;
            otpInputs[i].characterLimit = 1;
            otpInputs[i].contentType = TMP_InputField.ContentType.IntegerNumber;
            otpInputs[i].onValueChanged.AddListener((value) => OnInputChanged(index, value));
        }

        if (otpInputs.Length > 0 && otpInputs[0] != null) otpInputs[0].Select();

        // Buttons
        if (btnEnter) btnEnter.onClick.AddListener(OnEnterClicked);
        if (btnBack)  btnBack.onClick.AddListener(OnBackClicked);

        if (errorText) errorText.text = "";
    }

    private void OnDestroy()
    {
        foreach (var input in otpInputs)
            if (input != null) input.onValueChanged.RemoveAllListeners();

        if (btnEnter) btnEnter.onClick.RemoveListener(OnEnterClicked);
        if (btnBack)  btnBack.onClick.RemoveListener(OnBackClicked);
        if (resendButton) resendButton.onClick.RemoveListener(OnClickResend);
    }

    private void Update()
    {
        // Backspace: lùi focus
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            for (int i = 0; i < otpInputs.Length; i++)
            {
                if (otpInputs[i] != null && otpInputs[i].isFocused)
                {
                    if (otpInputs[i].text.Length == 0 && i > 0 && otpInputs[i - 1] != null)
                    {
                        otpInputs[i - 1].Select();
                        otpInputs[i - 1].text = "";
                    }
                    break;
                }
            }
        }
    }

    // ===== Contact sourcing =====
    private void EnsureContactFromSessionOrPrefs()
    {
        if (!string.IsNullOrEmpty(contactIdentifier)) return;

        // RAM session (ưu tiên forgot; fallback register)
        if (!string.IsNullOrEmpty(AuthFlowSession.LastOtpIdentifier))
        {
            contactIdentifier = AuthFlowSession.LastOtpIdentifier.Trim();
        }
        if (string.IsNullOrEmpty(otpByChannel)) otpByChannel = AuthFlowSession.LastOtpBy;
        if (string.IsNullOrEmpty(otpPurpose))   otpPurpose   = AuthFlowSession.LastOtpPurpose;

        if (!string.IsNullOrEmpty(contactIdentifier)) return;

        // PlayerPrefs
        contactIdentifier = PlayerPrefs.GetString(PREF_OTP_IDENTIFIER, "").Trim();
        if (string.IsNullOrEmpty(contactIdentifier))
            contactIdentifier = PlayerPrefs.GetString(PREF_USERNAME84, "").Trim(); // từ đăng ký

        if (string.IsNullOrEmpty(otpByChannel))
            otpByChannel = PlayerPrefs.GetString(PREF_OTP_BY, otpByChannel);
    }

    // ===== OTP inputs =====
    private void OnInputChanged(int index, string value)
    {
        if (value.Length > 0 && index < otpInputs.Length - 1 && otpInputs[index + 1] != null)
            otpInputs[index + 1].Select();
        else if (value.Length == 0 && index > 0 && otpInputs[index - 1] != null)
            otpInputs[index - 1].Select();
    }

    private void OnEnterClicked()
    {
        if (isSubmitting) return;

        EnsureContactFromSessionOrPrefs();
        string otpCode = GetOtp().Trim();

        if (string.IsNullOrEmpty(contactIdentifier))
        {
            if (errorText) errorText.text = "Thiếu thông tin liên hệ (email/số điện thoại).";
            Debug.LogWarning("[OTP] contactIdentifier rỗng.");
            return;
        }

        if (otpCode.Length != 6)
        {
            if (errorText) errorText.text = "OTP phải gồm 6 chữ số.";
            return;
        }

        StartCoroutine(VerifyOtpRoutine(contactIdentifier, otpCode));
    }

    private IEnumerator VerifyOtpRoutine(string username, string otp)
    {
        if (errorText) errorText.text = "";
        isSubmitting = true;
        if (btnEnter) btnEnter.interactable = false;

        Debug.Log($"[OTP] purpose='{otpPurpose}' forgotKey='{forgotPurposeKey}' username='{username}' otp='{otp}'");

        // CHỐT luồng theo purpose
        bool isForgot = !string.IsNullOrEmpty(otpPurpose) && otpPurpose == forgotPurposeKey;

        // Endpoint theo luồng (đừng chỉnh trong Inspector để tránh sai)
        string[] registerPaths = {
            "/users/otpverification",
            "/users/otp-verification",
            "/users/otpVerification"
        };
        string[] forgotPaths = {
            "/users/password-reset/otp-verification",
            "/api/v1/users/password-reset/otp-verification"
        };

        var pathList = isForgot ? forgotPaths : registerPaths;

        foreach (var rawPath in pathList)
        {
            var path = (rawPath ?? "").Trim();
            if (string.IsNullOrEmpty(path)) continue;

            string url = baseUrl.TrimEnd('/') + path;
            string json = "{\"username\":\"" + EscapeJson(username) + "\",\"otp\":\"" + EscapeJson(otp) + "\"}";
            Debug.Log($"[OTP] Try ({(isForgot ? "FORGOT" : "REGISTER")}) -> {url} ; body={json}");

            using (var req = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                req.uploadHandler = new UploadHandlerRaw(bodyRaw);
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
                    Debug.Log("[OTP] Verify OK -> " + req.downloadHandler.text);

                    if (isRunning) { StopAllCoroutines(); isRunning = false; }

                    if (isForgot && openResetOnSuccess && resetPanel != null)
                    {
                        if (resetController != null) resetController.SetUsername(username);
                        if (currentPanel) currentPanel.SetActive(false);
                        resetPanel.SetActive(true);
                    }
                    else
                    {
                        if (currentPanel) currentPanel.SetActive(false);
                        if (successPanel) successPanel.SetActive(true);
                        if (imageToShow)  imageToShow.gameObject.SetActive(true);
                    }

                    isSubmitting = false;
                    if (btnEnter) btnEnter.interactable = true;
                    yield break;
                }

                Debug.LogWarning($"[OTP] FAIL {req.responseCode} at {path}: {req.error}\n{req.downloadHandler.text}");

                // Sai endpoint -> thử cái tiếp theo
                if (req.responseCode == 404) continue;

                // Sai mã -> dọn input + gợi ý resend
                if (req.responseCode == 400 && (req.downloadHandler.text ?? "").Contains("wrong_otp"))
                {
                    ClearOtpInputs();
                    if (errorText) errorText.text = "Mã OTP không đúng hoặc đã hết hạn. Hãy nhập lại hoặc bấm Gửi lại OTP.";
                }
                else
                {
                    if (errorText)
                        errorText.text = string.IsNullOrEmpty(req.downloadHandler.text)
                            ? "Xác thực OTP thất bại. Vui lòng thử lại."
                            : req.downloadHandler.text;
                }

                isSubmitting = false;
                if (btnEnter) btnEnter.interactable = true;
                yield break;
            }
        }

        if (errorText) errorText.text = "Không tìm thấy endpoint OTP verify (404).";
        isSubmitting = false;
        if (btnEnter) btnEnter.interactable = true;
    }

    // ==== Resend ====
    private void OnClickResend()
    {
        if (_resending) return;
        EnsureContactFromSessionOrPrefs();

        if (string.IsNullOrEmpty(contactIdentifier))
        {
            if (errorText) errorText.text = "Thiếu email/số điện thoại để gửi lại OTP.";
            return;
        }

        StartCoroutine(ResendOtpRoutine(contactIdentifier, otpByChannel, otpPurpose));
    }

    private IEnumerator ResendOtpRoutine(string identifier, string otpBy, string purpose)
    {
        _resending = true;
        if (resendButton) resendButton.interactable = false;

        // functionName theo luồng
        string functionName = (purpose == forgotPurposeKey) ? "forgot-password" : "register";

        // /users/otp?username=...&otpBy=...&isApp=false&functionName=...&platform=web
        string baseU = baseUrl.TrimEnd('/');
        string qUser = UnityWebRequest.EscapeURL(identifier);
        string qBy   = UnityWebRequest.EscapeURL(otpBy);
        string qFun  = UnityWebRequest.EscapeURL(functionName);
        string url   = $"{baseU}/users/otp?username={qUser}&otpBy={qBy}&isApp=false&functionName={qFun}&platform=web";

        using (var req = UnityWebRequest.Get(url))
        {
            req.downloadHandler = new DownloadHandlerBuffer();
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool ok = req.result == UnityWebRequest.Result.Success || (req.responseCode >= 200 && req.responseCode < 300);
#else
            bool ok = !req.isNetworkError && !req.isHttpError && (req.responseCode >= 200 && req.responseCode < 300);
#endif
            if (ok)
            {
                Debug.Log($"[OTP] Resend OK ({otpBy}/{functionName}) -> {req.downloadHandler.text}");
                // Reset đếm ngược mỗi lần gửi lại
                ResetTimer();
                if (errorText) errorText.text = "Đã gửi lại OTP. Vui lòng kiểm tra hộp thư/tin nhắn.";
            }
            else
            {
                var body = req.downloadHandler.text;
                Debug.LogWarning($"[OTP] Resend FAIL {req.responseCode}: {req.error}\n{body}");
                if (errorText) errorText.text = string.IsNullOrEmpty(body) ? "Gửi lại OTP thất bại." : body;
            }
        }

        // Cooldown chống spam resend
        float cd = Mathf.Max(5, resendCooldownSeconds);
        float t = cd;
        while (t > 0f)
        {
            SetButtonLabel(resendButton, $"Gửi lại ({Mathf.CeilToInt(t)}s)");
            yield return new WaitForSecondsRealtime(1f);
            t -= 1f;
        }

        if (resendButton)
        {
            resendButton.interactable = true;
            SetButtonLabel(resendButton, "Gửi lại OTP");
        }

        _resending = false;
    }

    private void SetButtonLabel(Button btn, string text)
    {
        if (!btn) return;
        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp) tmp.text = text;
    }

    private void ClearOtpInputs()
    {
        foreach (var f in otpInputs) if (f) f.text = "";
        if (otpInputs != null && otpInputs.Length > 0 && otpInputs[0]) otpInputs[0].Select();
    }

    // ==== Back ====
    private void OnBackClicked()
    {
        if (isRunning) { StopAllCoroutines(); isRunning = false; }
        if (currentPanel) currentPanel.SetActive(false);
        if (backPanel)    backPanel.SetActive(true);
    }

    // ==== Timer ====
    private void ResetTimer()
    {
        remainingSeconds = totalSeconds;
        if (isRunning) StopAllCoroutines();
        StartCoroutine(CountdownTimer());
    }

    private IEnumerator CountdownTimer()
    {
        isRunning = true;
        while (remainingSeconds > 0)
        {
            int minutes = remainingSeconds / 60;
            int seconds = remainingSeconds % 60;

            if (minuteText) minuteText.text = minutes.ToString("00");
            if (secondText) secondText.text = seconds.ToString("00");

            yield return new WaitForSeconds(1);
            remainingSeconds--;
        }
        if (minuteText) minuteText.text = "00";
        if (secondText) secondText.text = "00";
        isRunning = false;
        Debug.Log("Hết thời gian OTP!");
    }
    
    private string GetOtp()
    {
        var sb = new StringBuilder(6);
        foreach (var input in otpInputs)
        {
            if (input == null) continue;
            sb.Append(input.text);
        }
        return sb.ToString();
    }

    private string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }
}
