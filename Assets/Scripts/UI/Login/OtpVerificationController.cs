using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Networking;
using System.Text;
using System.Collections;
using System.Collections.Generic;

public class OtpVerificationController : MonoBehaviour
{
    private string baseUrl;

    private const string PREF_USERNAME84     = "REG_USERNAME_84";
    private const string PREF_OTP_BY         = "REG_OTP_BY";
    private const string PREF_OTP_IDENTIFIER = "REG_OTP_IDENTIFIER";

    [Header("Hiển thị thời gian")]
    public TextMeshProUGUI minuteText;
    public TextMeshProUGUI secondText;
    public TextMeshProUGUI textStatus;

    [Header("6 ô nhập OTP")]
    public TMP_InputField[] otpInputs = new TMP_InputField[6];

    [Header("Buttons")]
    public Button btnEnter;
    public Button btnBack;

    [Header("Timer (giây)")]
    public int totalSeconds = 60;              // sẽ bị override bởi config (limit * 60)

    [Header("Panels")]
    public GameObject successPanel;
    public GameObject imageToShow;
    public GameObject currentPanel;
    public GameObject backPanel;

    [Header("Reset Password flow (optional)")]
    public bool openResetOnSuccess = true;
    public GameObject resetPanel;
    public ResetPasswordController resetController;
    public string forgotPurposeKey = "forgot-password";

    [Header("Resend OTP")]
    public Button resendButton;
    public int resendCooldownSeconds = 60;     // sẽ bị override bởi config (limit * 60)

    [Header("Optional UI")]
    public TextMeshProUGUI errorText;

    [Header("Popup Warning (optional)")]
    public LoginPopupUI warningPopupPrefab;
    public Transform popupParent;

    [Header("Config từ server")]
    public bool useServerConfig = true;
    public string otpConfigKey = "otp-expired-time";   // key trên /config

    // ========= State =========
    private int  remainingSeconds;
    private bool isRunning       = false;
    private bool isSubmitting    = false;
    private bool _resending      = false;
    private bool _configLoaded   = false;

    [SerializeField] private string contactIdentifier = "";
    [SerializeField] private string otpByChannel      = ""; // "phone" | "email"
    [SerializeField] private string otpPurpose        = ""; // "forgot-password" | "register"

    // ======= Error mapping =======
    [System.Serializable]
    private class ErrorResponse
    {
        public bool   status;
        public string message;
        public int    remaining;
        public int    statusCode;
    }

    // Mã lỗi cho verify OTP
    private static readonly Dictionary<string, string> VerifyErrorMap =
        new Dictionary<string, string>
    {
        { "wrong_otp",          "Mã OTP không đúng hoặc đã hết hạn. Hãy nhập lại hoặc bấm Gửi lại OTP." },
        { "otp_expired",        "Mã OTP đã hết hạn. Bạn hãy bấm Gửi lại OTP để nhận mã mới." },
        { "otp_not_found",      "Mã OTP không hợp lệ. Hãy nhập lại hoặc bấm Gửi lại OTP." },
        { "otp_too_many_retry", "Bạn đã nhập sai OTP quá nhiều lần. Hãy bấm Gửi lại OTP." }
    };

    // Mã lỗi cho resend OTP
    private static readonly Dictionary<string, string> ResendErrorMap =
        new Dictionary<string, string>
    {
        { "please_wait_a_moment_to_get_new_otp", "Bạn vừa yêu cầu OTP, vui lòng chờ một lúc rồi thử lại." },
        { "otp_limit_reached",                   "Bạn đã yêu cầu OTP quá nhiều lần. Vui lòng thử lại sau ít phút." },
        { "otp_too_many_request",                "Bạn đã yêu cầu OTP quá nhiều lần. Vui lòng thử lại sau ít phút." }
    };

    private void Awake()
    {
        baseUrl = LmsStore.Instance.baseUrl;
    }

    private void Start()
    {
        EnsureContactFromSessionOrPrefs();

        // Lấy config OTP từ server (limit = phút)
        if (useServerConfig)
        {
            StartCoroutine(LoadOtpConfigFromServer());
        }

        // OTP inputs
        for (int i = 0; i < otpInputs.Length; i++)
        {
            int index = i;
            if (otpInputs[i] == null) continue;
            otpInputs[i].characterLimit = 1;
            otpInputs[i].contentType = TMP_InputField.ContentType.IntegerNumber;
            otpInputs[i].onValueChanged.AddListener((value) => OnInputChanged(index, value));
        }

        if (otpInputs.Length > 0 && otpInputs[0] != null)
            otpInputs[0].Select();

        // Buttons
        if (btnEnter) btnEnter.onClick.AddListener(OnEnterClicked);
        if (btnBack)  btnBack.onClick.AddListener(OnBackClicked);

        if (errorText) errorText.text = "";
    }

    public void SetContact(string identifier, string otpBy, string purpose = "")
    {
        contactIdentifier = (identifier ?? "").Trim();
        otpByChannel      = (otpBy ?? "").Trim();
        otpPurpose        = (purpose ?? "").Trim();

        AuthFlowSession.LastOtpIdentifier = contactIdentifier;
        AuthFlowSession.LastOtpBy         = otpByChannel;
        if (!string.IsNullOrEmpty(otpPurpose))
            AuthFlowSession.LastOtpPurpose = otpPurpose;

        // Cập nhật status text ngay khi được set từ màn Register
        UpdateStatusLabel();
    }

    public void SetUsername(string username84FromRegister) =>
        SetContact(username84FromRegister, "phone", "register");

    private void OnEnable()
    {
        EnsureContactFromSessionOrPrefs();

        // Clear 6 ô và focus
        for (int i = 0; i < otpInputs.Length; i++)
            if (otpInputs[i] != null) otpInputs[i].text = "";

        if (otpInputs.Length > 0 && otpInputs[0] != null)
            otpInputs[0].Select();

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

    private void OnDestroy()
    {
        foreach (var input in otpInputs)
            if (input != null) input.onValueChanged.RemoveAllListeners();

        if (btnEnter)     btnEnter.onClick.RemoveListener(OnEnterClicked);
        if (btnBack)      btnBack.onClick.RemoveListener(OnBackClicked);
        if (resendButton) resendButton.onClick.RemoveListener(OnClickResend);
    }

    private void Update()
    {
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

    // Gọi từ màn Register / Forgot khi OTP đã được gửi thành công
    public void BeginCountdown()
    {
        ResetTimer();
    }

    // ================= Contact sourcing =================
    private void EnsureContactFromSessionOrPrefs()
    {
        if (!string.IsNullOrEmpty(contactIdentifier)) return;

        if (!string.IsNullOrEmpty(AuthFlowSession.LastOtpIdentifier))
            contactIdentifier = AuthFlowSession.LastOtpIdentifier.Trim();

        if (string.IsNullOrEmpty(otpByChannel))
            otpByChannel = AuthFlowSession.LastOtpBy;
        if (string.IsNullOrEmpty(otpPurpose))
            otpPurpose = AuthFlowSession.LastOtpPurpose;

        if (!string.IsNullOrEmpty(contactIdentifier)) return;

        contactIdentifier = PlayerPrefs.GetString(PREF_OTP_IDENTIFIER, "").Trim();
        if (string.IsNullOrEmpty(contactIdentifier))
            contactIdentifier = PlayerPrefs.GetString(PREF_USERNAME84, "").Trim();

        if (string.IsNullOrEmpty(otpByChannel))
            otpByChannel = PlayerPrefs.GetString(PREF_OTP_BY, otpByChannel);
    }

    // ================= OTP inputs =================
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
            const string msg = "Thiếu thông tin liên hệ (email/số điện thoại).";
            if (errorText) errorText.text = msg;
            ShowWarningPopup(msg);
            Debug.LogWarning("[OTP] contactIdentifier rỗng.");
            return;
        }

        if (otpCode.Length != 6)
        {
            const string msg = "Mã OTP phải gồm 6 chữ số.";
            if (errorText) errorText.text = msg;
            ShowWarningPopup(msg);
            return;
        }

        StartCoroutine(VerifyOtpRoutine(contactIdentifier, otpCode));
    }

    private IEnumerator VerifyOtpRoutine(string username, string otp)
    {
        if (errorText) errorText.text = "";
        isSubmitting = true;
        if (btnEnter) btnEnter.interactable = false;

        bool isForgot = !string.IsNullOrEmpty(otpPurpose) && otpPurpose == forgotPurposeKey;

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
        string lastLog = null;

        foreach (var rawPath in pathList)
        {
            var path = (rawPath ?? "").Trim();
            if (string.IsNullOrEmpty(path)) continue;

            string url  = baseUrl.TrimEnd('/') + path;
            string json = "{\"username\":\"" + EscapeJson(username) + "\",\"otp\":\"" + EscapeJson(otp) + "\"}";
            Debug.Log($"[OTP] Try ({(isForgot ? "FORGOT" : "REGISTER")}) -> {url}");

            using (var req = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
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

                string raw = req.downloadHandler.text;
                lastLog =
                    $"[OTP] FAIL {req.responseCode} at {path}: {req.error}\n{raw}";
                Debug.LogWarning(lastLog);

                // 404 -> thử endpoint tiếp theo
                if (req.responseCode == 404)
                    continue;

                // Các lỗi khác -> map message thân thiện
                string friendly = BuildVerifyFriendlyMessage(req, raw);
                if (friendly.Contains("Mã OTP không đúng") ||
                    friendly.Contains("OTP đã hết hạn"))
                {
                    ClearOtpInputs();
                }

                if (errorText) errorText.text = friendly;
                ShowWarningPopup(friendly);

                isSubmitting = false;
                if (btnEnter) btnEnter.interactable = true;
                yield break;
            }
        }

        Debug.LogWarning("[OTP] All verify endpoints failed or returned 404. Last: " + lastLog);

        const string finalMsg =
            "Hệ thống đang gặp sự cố khi xác thực OTP. Bạn vui lòng thử lại sau hoặc liên hệ bộ phận hỗ trợ.";
        if (errorText) errorText.text = finalMsg;
        ShowWarningPopup(finalMsg);

        isSubmitting = false;
        if (btnEnter) btnEnter.interactable = true;
    }

    private string BuildVerifyFriendlyMessage(UnityWebRequest req, string raw)
    {
#if UNITY_2020_2_OR_NEWER
        if (req.result == UnityWebRequest.Result.ConnectionError)
#else
        if (req.isNetworkError)
#endif
            return "Lỗi mạng, bạn vui lòng kiểm tra kết nối và thử lại.";

        if (req.responseCode >= 500 && req.responseCode < 600)
            return "Hệ thống đang bận hoặc bảo trì. Bạn vui lòng thử lại sau giây lát.";

        if (!string.IsNullOrEmpty(raw))
        {
            try
            {
                var err = JsonUtility.FromJson<ErrorResponse>(raw);
                if (err != null && !string.IsNullOrEmpty(err.message))
                {
                    if (VerifyErrorMap.TryGetValue(err.message, out var mapped))
                        return mapped;

                    Debug.LogWarning("[OTP] Unmapped verify error code: " + err.message);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[OTP] Parse verify error JSON fail: " + e.Message + " | raw: " + raw);
            }
        }

        if (req.responseCode >= 400 && req.responseCode < 500)
            return "Mã OTP không hợp lệ hoặc đã hết hạn. Bạn vui lòng nhập lại hoặc bấm Gửi lại OTP.";

        return "Xác thực OTP thất bại. Bạn vui lòng thử lại sau giây lát.";
    }

    // =============== Resend ===============
    private void OnClickResend()
    {
        if (_resending) return;
        EnsureContactFromSessionOrPrefs();

        if (string.IsNullOrEmpty(contactIdentifier))
        {
            const string msg = "Thiếu email/số điện thoại để gửi lại OTP.";
            if (errorText) errorText.text = msg;
            ShowWarningPopup(msg);
            return;
        }

        StartCoroutine(ResendOtpRoutine(contactIdentifier, otpByChannel, otpPurpose));
    }

    private IEnumerator ResendOtpRoutine(string identifier, string otpBy, string purpose)
    {
        _resending = true;
        if (resendButton) resendButton.interactable = false;

        string functionName = (purpose == forgotPurposeKey) ? "forgot-password" : "register";

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
            bool ok = req.result == UnityWebRequest.Result.Success ||
                      (req.responseCode >= 200 && req.responseCode < 300);
#else
            bool ok = !req.isNetworkError && !req.isHttpError &&
                      (req.responseCode >= 200 && req.responseCode < 300);
#endif
            if (ok)
            {
                Debug.Log($"[OTP] Resend OK ({otpBy}/{functionName}) -> {req.downloadHandler.text}");
                ResetTimer();
                if (errorText) errorText.text = "Đã gửi lại OTP. Vui lòng kiểm tra hộp thư/tin nhắn.";
            }
            else
            {
                string raw = req.downloadHandler.text;
                Debug.LogWarning($"[OTP] Resend FAIL {req.responseCode}: {req.error}\n{raw}");

                string friendly = BuildResendFriendlyMessage(req, raw);
                if (errorText) errorText.text = friendly;
                ShowWarningPopup(friendly);
            }
        }

        // Cooldown chống spam resend — dùng limit * 60 nếu server trả về
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

    private string BuildResendFriendlyMessage(UnityWebRequest req, string raw)
    {
#if UNITY_2020_2_OR_NEWER
        if (req.result == UnityWebRequest.Result.ConnectionError)
#else
        if (req.isNetworkError)
#endif
            return "Lỗi mạng, bạn vui lòng kiểm tra kết nối và thử lại.";

        if (req.responseCode >= 500 && req.responseCode < 600)
            return "Hệ thống đang bận hoặc bảo trì. Bạn vui lòng thử lại sau giây lát.";

        if (!string.IsNullOrEmpty(raw))
        {
            try
            {
                var err = JsonUtility.FromJson<ErrorResponse>(raw);
                if (err != null && !string.IsNullOrEmpty(err.message))
                {
                    if (ResendErrorMap.TryGetValue(err.message, out var mapped))
                        return mapped;

                    Debug.LogWarning("[OTP] Unmapped resend error code: " + err.message);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[OTP] Parse resend error JSON fail: " + e.Message + " | raw: " + raw);
            }
        }

        if (req.responseCode >= 400 && req.responseCode < 500)
            return "Gửi lại OTP thất bại. Bạn vui lòng thử lại sau ít phút.";

        return "Gửi lại OTP thất bại. Bạn vui lòng thử lại sau giây lát.";
    }

    // =============== UI helpers ===============
    private void SetButtonLabel(Button btn, string text)
    {
        if (!btn) return;
        var tmp = btn.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmp) tmp.text = text;
    }

    private void ClearOtpInputs()
    {
        foreach (var f in otpInputs) if (f) f.text = "";
        if (otpInputs != null && otpInputs.Length > 0 && otpInputs[0])
            otpInputs[0].Select();
    }

    private void OnBackClicked()
    {
        if (isRunning) { StopAllCoroutines(); isRunning = false; }
        if (currentPanel) currentPanel.SetActive(false);
        if (backPanel)    backPanel.SetActive(true);
    }

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

    private void ShowWarningPopup(string message)
    {
        if (warningPopupPrefab == null)
        {
            Debug.LogWarning("[OTP] warningPopupPrefab chưa được gán. Msg: " + message);
            return;
        }

        Transform parent = popupParent != null ? popupParent : transform.root;
        var popup = Instantiate(warningPopupPrefab, parent);
        popup.Init("Cảnh báo", message);
    }

    private void UpdateStatusLabel()
    {
        if (textStatus == null)
            return;

        if (string.IsNullOrEmpty(contactIdentifier))
        {
            textStatus.text = "";
            return;
        }

        string channelLabel = (otpByChannel == "phone")
            ? "số điện thoại"
            : "email";

        string purposeLabel;
        if (!string.IsNullOrEmpty(otpPurpose))
        {
            if (otpPurpose == forgotPurposeKey)
                purposeLabel = "đặt lại mật khẩu";
            else if (otpPurpose == "register")
                purposeLabel = "hoàn tất đăng ký";
            else
                purposeLabel = "tiếp tục";
        }
        else
        {
            purposeLabel = "tiếp tục";
        }

        textStatus.text =
            $"Mã xác minh gồm 6 số vừa được gửi đến {channelLabel} {contactIdentifier}. " +
            $"\nVui lòng nhập mã OTP để {purposeLabel}.";
    }

    // =============== CONFIG /config?key=otp-expired-time ===============
    [System.Serializable]
    private class OtpConfigItem
    {
        public string type;   // vd: "order"
        public int    limit;  // phút
    }

    [System.Serializable]
    private class OtpConfigData
    {
        public string        _id;
        public string        key;
        public OtpConfigItem[] data;
    }

    [System.Serializable]
    private class OtpConfigResponse
    {
        public bool          status;
        public OtpConfigData data;
    }

    private IEnumerator LoadOtpConfigFromServer()
    {
        string url = baseUrl.TrimEnd('/') + "/config?key=" + otpConfigKey;

        using (var req = UnityWebRequest.Get(url))
        {
            req.downloadHandler = new DownloadHandlerBuffer();
            yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
            bool ok = req.result == UnityWebRequest.Result.Success ||
                      (req.responseCode >= 200 && req.responseCode < 300);
#else
            bool ok = !req.isNetworkError && !req.isHttpError &&
                      (req.responseCode >= 200 && req.responseCode < 300);
#endif
            if (!ok)
            {
                Debug.LogWarning($"[OTP Config] FAIL {req.responseCode}: {req.error}\n{req.downloadHandler.text}");
                yield break;
            }

            var json = req.downloadHandler.text;
            OtpConfigResponse resp = null;
            try
            {
                resp = JsonUtility.FromJson<OtpConfigResponse>(json);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[OTP Config] Parse JSON fail: " + e.Message + "\nRaw: " + json);
            }

            if (resp == null || resp.data == null || resp.data.data == null || resp.data.data.Length == 0)
            {
                Debug.LogWarning("[OTP Config] Response rỗng hoặc sai cấu trúc.");
                yield break;
            }

            // Lấy phần tử đầu tiên (hoặc có type = "order")
            OtpConfigItem item = resp.data.data[0];
            for (int i = 0; i < resp.data.data.Length; i++)
            {
                if (resp.data.data[i] != null && resp.data.data[i].type == "order")
                {
                    item = resp.data.data[i];
                    break;
                }
            }

            int limitMinutes = item != null ? item.limit : 0;
            if (limitMinutes > 0)
            {
                int seconds = limitMinutes * 60;
                totalSeconds         = seconds;   // thời gian hiệu lực OTP
                resendCooldownSeconds = seconds;  // thời gian chờ gửi lại OTP

                Debug.Log($"[OTP Config] Loaded limit = {limitMinutes} phút => {seconds} giây.");
            }
            else
            {
                Debug.LogWarning("[OTP Config] limit <= 0, giữ nguyên giá trị mặc định trong Inspector.");
            }

            _configLoaded = true;
        }
    }
}
