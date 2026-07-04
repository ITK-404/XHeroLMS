using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public class OtpVerificationController : MonoBehaviour
{
    private string baseUrl;

    private const string PREF_USERNAME84     = "REG_USERNAME_84";
    private const string PREF_OTP_BY         = "REG_OTP_BY";
    private const string PREF_OTP_IDENTIFIER = "REG_OTP_IDENTIFIER";
    public string otpConfigKey = "otp-expired-time";   // key trên /config
    private const string PREF_LOGIN_PREFILL  = "LOGIN_PREFILL_USERNAME";

    [Header("Hiển thị thời gian")]
    public TextMeshProUGUI minuteText;
    public TextMeshProUGUI secondText;
    public TextMeshProUGUI textStatus;

    [Header("6 ô nhập OTP")]
    public TMP_InputField[] otpInputs = new TMP_InputField[6];

    [Header("OTP input UX")]
    public bool useSingleKeyboardInput = true;
    public bool focusOtpOnEnable = false;
    public bool autoSubmitWhenOtpComplete = false;

    [Header("Android SMS OTP Auto Fill")]
    public bool enableAndroidSmsOtpAutoFill = true;
    public bool enableAndroidSmsUserConsentFallback = true;
    public bool logAndroidSmsAppHash = true;
    public string androidSmsOtpRegex = @"(?<!\d)\d{6}(?!\d)";

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

    // ========= State =========
    private int  remainingSeconds;
    private bool isRunning       = false;
    private bool isSubmitting    = false;
    private bool _resending      = false;
    private bool _configLoaded = false;

    [SerializeField] private string contactIdentifier = "";
    [SerializeField] private string otpByChannel      = ""; // "phone" | "email"
    [SerializeField] private string otpPurpose        = ""; // "forgot-password" | "register"

    private TMP_InputField _otpKeyboardInput;
    private bool _otpUiConfigured = false;
    private bool _updatingOtpVisuals = false;
    private bool _androidSmsListening = false;
    private bool _androidSmsAppHashLogged = false;
    private Coroutine _focusOtpRoutine;
    private Coroutine _countdownRoutine;
    private Coroutine _beginCountdownRoutine;
    private bool _configLoading = false;

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
        ConfigureOtpInputs();
    }

    private void Start()
    {
        EnsureContactFromSessionOrPrefs();

        // Lấy config OTP từ server (limit = phút)
        if (useServerConfig)
        {
            EnsureOtpConfigLoadStarted();
        }

        // Buttons
        if (btnEnter) btnEnter.onClick.AddListener(OnEnterClicked);
        if (btnBack)  btnBack.onClick.AddListener(OnBackClicked);

        if (errorText) errorText.text = "";
    }

    public void SetContact(string identifier, string otpBy, string purpose = "")
    {
        contactIdentifier = (identifier ?? "").Trim();
        otpByChannel      = (otpBy ?? "").Trim().ToLowerInvariant();
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

    public void PrepareForIncomingOtp(string identifier, string otpBy, string purpose = "")
    {
        SetContact(identifier, otpBy, purpose);
        StartAndroidSmsOtpAutoFillIfNeeded(true);
    }

    private void OnEnable()
    {
        EnsureContactFromSessionOrPrefs();
        UpdateStatusLabel();

        EnsureOtpConfigLoadStarted();
        ClearOtpInputs(focusOtpOnEnable);
        StartAndroidSmsOtpAutoFillIfNeeded();

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

    private void OnDisable()
    {
        if (_focusOtpRoutine != null)
        {
            StopCoroutine(_focusOtpRoutine);
            _focusOtpRoutine = null;
        }

        StopAndroidSmsOtpAutoFill();
    }

    private void OnDestroy()
    {
        StopAndroidSmsOtpAutoFill();

        if (_otpKeyboardInput != null)
            _otpKeyboardInput.onValueChanged.RemoveListener(OnKeyboardOtpChanged);

        if (otpInputs != null)
        {
            foreach (var input in otpInputs)
                if (input != null) input.onValueChanged.RemoveAllListeners();
        }

        if (btnEnter)     btnEnter.onClick.RemoveListener(OnEnterClicked);
        if (btnBack)      btnBack.onClick.RemoveListener(OnBackClicked);
        if (resendButton) resendButton.onClick.RemoveListener(OnClickResend);
    }

    private void Update()
    {
        if (useSingleKeyboardInput)
        {
            if (Input.GetKeyDown(KeyCode.Backspace) && _otpKeyboardInput != null && !_otpKeyboardInput.isFocused)
                FocusOtpAt(GetNextEditIndex(), false);

            return;
        }

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
        if (_beginCountdownRoutine != null)
            StopCoroutine(_beginCountdownRoutine);

        _beginCountdownRoutine = StartCoroutine(BeginCountdownWhenConfigReady());
    }

    private IEnumerator BeginCountdownWhenConfigReady()
    {
        EnsureOtpConfigLoadStarted();

        if (useServerConfig && _configLoading)
        {
            Debug.Log("[OTP Config] Waiting for server config before starting countdown.");
            while (_configLoading)
                yield return null;
        }

        _beginCountdownRoutine = null;
        ResetTimer();
    }

    private IEnumerator WaitForOtpConfigIfNeeded()
    {
        EnsureOtpConfigLoadStarted();

        if (useServerConfig && _configLoading)
        {
            Debug.Log("[OTP Config] Waiting for server config before applying OTP timer.");
            while (_configLoading)
                yield return null;
        }
    }

    // ================= Contact sourcing =================
    private void EnsureContactFromSessionOrPrefs()
    {
        if (!string.IsNullOrEmpty(contactIdentifier))
        {
            NormalizeOtpChannel();
            return;
        }

        if (!string.IsNullOrEmpty(AuthFlowSession.LastOtpIdentifier))
            contactIdentifier = AuthFlowSession.LastOtpIdentifier.Trim();

        if (string.IsNullOrEmpty(otpByChannel))
            otpByChannel = AuthFlowSession.LastOtpBy;
        if (string.IsNullOrEmpty(otpPurpose))
            otpPurpose = AuthFlowSession.LastOtpPurpose;

        if (!string.IsNullOrEmpty(contactIdentifier))
        {
            NormalizeOtpChannel();
            return;
        }

        contactIdentifier = PlayerPrefs.GetString(PREF_OTP_IDENTIFIER, "").Trim();
        if (string.IsNullOrEmpty(contactIdentifier))
            contactIdentifier = PlayerPrefs.GetString(PREF_USERNAME84, "").Trim();

        if (string.IsNullOrEmpty(otpByChannel))
            otpByChannel = PlayerPrefs.GetString(PREF_OTP_BY, otpByChannel);

        NormalizeOtpChannel();
    }

    private void NormalizeOtpChannel()
    {
        if (!string.IsNullOrEmpty(otpByChannel))
            otpByChannel = otpByChannel.Trim().ToLowerInvariant();
    }

    // ================= OTP inputs =================
    private void ConfigureOtpInputs()
    {
        if (_otpUiConfigured) return;
        _otpUiConfigured = true;

        if (useSingleKeyboardInput)
            EnsureOtpKeyboardInput();

        int count = GetOtpSlotCount();
        for (int i = 0; i < count; i++)
        {
            var input = otpInputs[i];
            if (input == null) continue;

            if (useSingleKeyboardInput)
            {
                PrepareOtpVisualInput(input, i);
            }
            else
            {
                int index = i;
                ConfigureOtpKeyboardType(input, 1);
                input.onValueChanged.AddListener((value) => OnInputChanged(index, value));
                input.onSelect.AddListener(_ => SelectOtpFallback(index, true));
            }
        }
    }

    private void EnsureOtpKeyboardInput()
    {
        if (_otpKeyboardInput != null) return;

        var go = new GameObject("OtpKeyboardInput", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TMP_InputField));
        go.transform.SetParent(transform, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(-10000f, -10000f);
        rect.sizeDelta = new Vector2(1f, 1f);

        var image = go.GetComponent<Image>();
        image.color = Color.clear;
        image.raycastTarget = false;

        var textGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textGo.transform.SetParent(go.transform, false);

        var textRect = (RectTransform)textGo.transform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        var text = textGo.GetComponent<TextMeshProUGUI>();
        text.text = "";
        text.fontSize = 1f;
        text.color = Color.clear;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;

        var sourceText = GetFirstOtpTextComponent();
        if (sourceText != null)
        {
            text.font = sourceText.font;
            text.fontSharedMaterial = sourceText.fontSharedMaterial;
        }

        _otpKeyboardInput = go.GetComponent<TMP_InputField>();
        _otpKeyboardInput.textViewport = rect;
        _otpKeyboardInput.textComponent = text;
        _otpKeyboardInput.targetGraphic = image;
        _otpKeyboardInput.caretWidth = 0;
        _otpKeyboardInput.selectionColor = Color.clear;
        ConfigureOtpKeyboardType(_otpKeyboardInput, GetOtpSlotCount());
        _otpKeyboardInput.onValueChanged.AddListener(OnKeyboardOtpChanged);
    }

    private TMP_Text GetFirstOtpTextComponent()
    {
        if (otpInputs == null) return null;

        foreach (var input in otpInputs)
        {
            if (input != null && input.textComponent != null)
                return input.textComponent;
        }

        return null;
    }

    private void ConfigureOtpKeyboardType(TMP_InputField input, int limit)
    {
        if (input == null) return;

        input.characterLimit = Mathf.Max(1, limit);
        input.contentType = TMP_InputField.ContentType.Custom;
        input.characterValidation = TMP_InputField.CharacterValidation.Digit;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.richText = false;
        input.shouldHideMobileInput = true;

#if UNITY_IOS && !UNITY_EDITOR
        try
        {
            input.keyboardType = (TouchScreenKeyboardType)System.Enum.Parse(typeof(TouchScreenKeyboardType), "OneTimeCode");
        }
        catch
        {
            input.keyboardType = TouchScreenKeyboardType.NumberPad;
        }
#else
        input.keyboardType = TouchScreenKeyboardType.NumberPad;
#endif
        input.ForceLabelUpdate();
    }

    private void PrepareOtpVisualInput(TMP_InputField input, int index)
    {
        ConfigureOtpKeyboardType(input, 1);
        input.readOnly = true;
        input.caretWidth = 0;
        input.selectionColor = Color.clear;
        input.SetTextWithoutNotify("");

        var colors = input.colors;
        colors.disabledColor = colors.normalColor;
        input.colors = colors;
        input.interactable = false;

        AddOtpPointerTrigger(input, index);
    }

    private void AddOtpPointerTrigger(TMP_InputField input, int index)
    {
        var trigger = input.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = input.gameObject.AddComponent<EventTrigger>();

        var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
        entry.callback.AddListener(_ => FocusOtpAt(index, true));
        trigger.triggers.Add(entry);
    }

    private void FocusOtpAt(int index, bool replaceCurrent)
    {
        int count = GetOtpSlotCount();
        if (count <= 0) return;

        index = Mathf.Clamp(index, 0, count - 1);

        if (!useSingleKeyboardInput || _otpKeyboardInput == null)
        {
            SelectOtpFallback(index, replaceCurrent);
            return;
        }

        if (!isActiveAndEnabled)
            return;

        if (_focusOtpRoutine != null)
            StopCoroutine(_focusOtpRoutine);

        _focusOtpRoutine = StartCoroutine(FocusOtpNextFrame(index, replaceCurrent));
    }

    private IEnumerator FocusOtpNextFrame(int index, bool replaceCurrent)
    {
        yield return null;

        _focusOtpRoutine = null;
        if (_otpKeyboardInput == null || !isActiveAndEnabled)
            yield break;

        _otpKeyboardInput.Select();
        _otpKeyboardInput.ActivateInputField();
        ApplyProxySelection(index, replaceCurrent);
    }

    private void ApplyProxySelection(int index, bool replaceCurrent)
    {
        if (_otpKeyboardInput == null) return;

        string text = _otpKeyboardInput.text ?? "";
        int start = Mathf.Clamp(index, 0, text.Length);
        int end = (replaceCurrent && start < text.Length) ? start + 1 : start;

        _otpKeyboardInput.selectionStringAnchorPosition = start;
        _otpKeyboardInput.selectionStringFocusPosition = end;
        _otpKeyboardInput.ForceLabelUpdate();
    }

    private void OnKeyboardOtpChanged(string value)
    {
        if (_updatingOtpVisuals) return;

        int count = GetOtpSlotCount();
        string digits = KeepOnlyDigits(value);
        if (digits.Length > count)
            digits = digits.Substring(0, count);

        if (value != digits && _otpKeyboardInput != null)
        {
            int caret = Mathf.Clamp(_otpKeyboardInput.stringPosition, 0, digits.Length);
            _otpKeyboardInput.SetTextWithoutNotify(digits);
            ApplyProxySelection(caret, false);
        }

        SetOtpVisuals(digits);

        if (count > 0 && autoSubmitWhenOtpComplete && digits.Length == count)
            OnEnterClicked();
    }

    private void OnInputChanged(int index, string value)
    {
        if (_updatingOtpVisuals || useSingleKeyboardInput) return;

        string digits = KeepOnlyDigits(value);

        if (digits.Length > 1)
        {
            FillOtpFromIndex(index, digits);
            return;
        }

        var input = otpInputs[index];
        if (input != null && input.text != digits)
            input.SetTextWithoutNotify(digits);

        if (digits.Length > 0)
        {
            if (index < GetOtpSlotCount() - 1)
                SelectOtpFallback(index + 1, true);
            else if (autoSubmitWhenOtpComplete)
                OnEnterClicked();
        }
        else if (index > 0)
        {
            SelectOtpFallback(index - 1, true);
        }
    }

    private void FillOtpFromIndex(int startIndex, string value)
    {
        string digits = KeepOnlyDigits(value);
        if (string.IsNullOrEmpty(digits)) return;

        _updatingOtpVisuals = true;
        int writeIndex = Mathf.Clamp(startIndex, 0, GetOtpSlotCount() - 1);
        for (int i = 0; i < digits.Length && writeIndex < GetOtpSlotCount(); i++, writeIndex++)
        {
            if (otpInputs[writeIndex] != null)
                otpInputs[writeIndex].SetTextWithoutNotify(digits[i].ToString());
        }
        _updatingOtpVisuals = false;

        SelectOtpFallback(Mathf.Min(writeIndex, GetOtpSlotCount() - 1), true);

        if (autoSubmitWhenOtpComplete && GetOtp().Length == GetOtpSlotCount())
            OnEnterClicked();
    }

    private void SelectOtpFallback(int index, bool selectAll)
    {
        int count = GetOtpSlotCount();
        if (count <= 0) return;

        index = Mathf.Clamp(index, 0, count - 1);
        var input = otpInputs[index];
        if (input == null) return;

        input.Select();
        input.ActivateInputField();

        if (selectAll)
        {
            int length = (input.text ?? "").Length;
            input.selectionStringAnchorPosition = 0;
            input.selectionStringFocusPosition = length;
        }
    }

    private void FillOtpCode(string value, bool submitAfterFill)
    {
        string digits = KeepOnlyDigits(value);
        int count = GetOtpSlotCount();
        if (digits.Length > count)
            digits = digits.Substring(0, count);

        if (useSingleKeyboardInput && _otpKeyboardInput != null)
        {
            _otpKeyboardInput.SetTextWithoutNotify(digits);
            SetOtpVisuals(digits);
            ApplyProxySelection(GetNextEditIndex(), false);
        }
        else
        {
            SetOtpVisuals(digits);
            SelectOtpFallback(GetNextEditIndex(), true);
        }

        if (count > 0 && submitAfterFill && digits.Length == count)
            OnEnterClicked();
    }

    private void SetOtpVisuals(string digits)
    {
        _updatingOtpVisuals = true;
        int count = GetOtpSlotCount();
        for (int i = 0; i < count; i++)
        {
            if (otpInputs[i] == null) continue;
            string next = i < digits.Length ? digits[i].ToString() : "";
            otpInputs[i].SetTextWithoutNotify(next);
            otpInputs[i].ForceLabelUpdate();
        }
        _updatingOtpVisuals = false;
    }

    private int GetNextEditIndex()
    {
        string otp = GetOtp();
        int count = GetOtpSlotCount();
        if (count <= 0) return 0;
        return Mathf.Clamp(otp.Length, 0, count - 1);
    }

    private int GetOtpSlotCount()
    {
        return otpInputs == null ? 0 : otpInputs.Length;
    }

    private string KeepOnlyDigits(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        return Regex.Replace(value, @"\D", "");
    }

    private void StartAndroidSmsOtpAutoFillIfNeeded(bool forceRestart = false)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (otpByChannel != "phone")
        {
            if (!string.IsNullOrEmpty(otpByChannel))
                Debug.Log($"[OTP SMS] Skip Android SMS listener because otpByChannel='{otpByChannel}'.");
            return;
        }

        if (!enableAndroidSmsOtpAutoFill && !enableAndroidSmsUserConsentFallback)
        {
            Debug.LogWarning("[OTP SMS] Android SMS OTP autofill is disabled in Inspector.");
            return;
        }

        if (_androidSmsListening && !forceRestart)
        {
            Debug.Log("[OTP SMS] Android SMS listener is already running.");
            return;
        }

        try
        {
            if (_androidSmsListening)
                StopAndroidSmsOtpAutoFill();

            using (var smsRetriever = new AndroidJavaClass("com.xherozone.otp.SmsOtpRetriever"))
            {
                var bridge = AndroidSmsOtpBridge.Ensure(this);
                string callbackTarget = bridge != null
                    ? AndroidSmsOtpBridge.BridgeGameObjectName
                    : gameObject.name;

                if (logAndroidSmsAppHash && !_androidSmsAppHashLogged)
                {
                    string appHash = smsRetriever.CallStatic<string>("getAppSignatureHash");
                    if (!string.IsNullOrEmpty(appHash))
                        Debug.Log("[OTP SMS] Android SMS Retriever app hash: " + appHash);
                    _androidSmsAppHashLogged = true;
                }

                bool useUserConsent = enableAndroidSmsUserConsentFallback;
                Debug.Log($"[OTP SMS] Start listening. forceRestart={forceRestart}, consentFallback={useUserConsent}, contact='{contactIdentifier}', callbackTarget='{callbackTarget}'");

                smsRetriever.CallStatic(
                    "startListening",
                    callbackTarget,
                    nameof(OnAndroidSmsOtpReceived),
                    nameof(OnAndroidSmsOtpError),
                    string.IsNullOrEmpty(androidSmsOtpRegex) ? @"(?<!\d)\d{6}(?!\d)" : androidSmsOtpRegex,
                    useUserConsent,
                    (string)null
                );
                _androidSmsListening = true;
                Debug.Log("[OTP SMS] Native Android SMS listener call completed.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[OTP SMS] Start Android SMS listener failed: " + e.Message);
            _androidSmsListening = false;
        }
#endif
    }

    private void StopAndroidSmsOtpAutoFill()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!_androidSmsListening)
            return;

        try
        {
            using (var smsRetriever = new AndroidJavaClass("com.xherozone.otp.SmsOtpRetriever"))
            {
                smsRetriever.CallStatic("stopListening");
            }
            Debug.Log("[OTP SMS] Android SMS listener stopped.");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[OTP SMS] Stop Android SMS listener failed: " + e.Message);
        }

        _androidSmsListening = false;
#endif
    }

    public void OnAndroidSmsOtpReceived(string code)
    {
        _androidSmsListening = false;

        string digits = KeepOnlyDigits(code);
        if (digits.Length < GetOtpSlotCount())
        {
            Debug.LogWarning("[OTP SMS] Android SMS listener received a message without a full OTP.");
            return;
        }

        Debug.Log("[OTP SMS] Android SMS OTP received. Auto filling OTP.");
        FillOtpCode(digits, autoSubmitWhenOtpComplete);
    }

    public void OnAndroidSmsOtpError(string error)
    {
        _androidSmsListening = false;

        if (string.IsNullOrEmpty(error))
            return;

        if (error == "timeout")
            Debug.Log("[OTP SMS] Android SMS listener timed out before a matching SMS arrived.");
        else if (error == "consent_cancelled")
            Debug.Log("[OTP SMS] Android SMS consent dialog was cancelled.");
        else
            Debug.LogWarning("[OTP SMS] Android SMS listener callback: " + error);
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

                    // 1) Lưu username cho màn Login
                    SetLoginPrefillFromVerifiedContact();
                    StopAndroidSmsOtpAutoFill();

                    // 2) Dừng timer như cũ
                    StopCountdownTimer();

                    // 3) Điều hướng như cũ
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
                        if (imageToShow) imageToShow.gameObject.SetActive(true);
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

        StartAndroidSmsOtpAutoFillIfNeeded(true);
        StartCoroutine(ResendOtpRoutine(contactIdentifier, otpByChannel, otpPurpose));
    }

    private IEnumerator ResendOtpRoutine(string identifier, string otpBy, string purpose)
    {
        _resending = true;
        if (resendButton) resendButton.interactable = false;
        int serverRemainingSeconds = 0;

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
                yield return WaitForOtpConfigIfNeeded();
                ResetTimer();
                if (errorText) errorText.text = "Đã gửi lại OTP. Vui lòng kiểm tra hộp thư/tin nhắn.";
            }
            else
            {
                string raw = req.downloadHandler.text;
                Debug.LogWarning($"[OTP] Resend FAIL {req.responseCode}: {req.error}\n{raw}");
                serverRemainingSeconds = TryReadServerRemainingSeconds(raw);
                if (serverRemainingSeconds > 0)
                {
                    totalSeconds = Mathf.Max(totalSeconds, serverRemainingSeconds);
                    resendCooldownSeconds = Mathf.Max(resendCooldownSeconds, serverRemainingSeconds);
                    ResetTimer(serverRemainingSeconds);
                    Debug.Log($"[OTP] Server resend remaining = {serverRemainingSeconds}s, sync countdown/cooldown.");
                }

                string friendly = BuildResendFriendlyMessage(req, raw);
                if (errorText) errorText.text = friendly;
                ShowWarningPopup(friendly);
            }
        }

        // Cooldown chống spam resend — dùng limit * 60 nếu server trả về
        float cd = Mathf.Max(5, serverRemainingSeconds > 0 ? serverRemainingSeconds : resendCooldownSeconds);
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

    private int TryReadServerRemainingSeconds(string raw)
    {
        if (string.IsNullOrEmpty(raw))
            return 0;

        try
        {
            var err = JsonUtility.FromJson<ErrorResponse>(raw);
            if (err != null && err.remaining > 0)
                return err.remaining;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[OTP] Parse remaining seconds fail: " + e.Message + " | raw: " + raw);
        }

        return 0;
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

    private void ClearOtpInputs(bool focusFirst = true)
    {
        FillOtpCode("", false);

        if (focusFirst)
            FocusOtpAt(0, false);
    }

    private void OnBackClicked()
    {
        StopAndroidSmsOtpAutoFill();
        StopCountdownTimer();
        if (currentPanel) currentPanel.SetActive(false);
        if (backPanel)    backPanel.SetActive(true);
    }

    private void ResetTimer()
    {
        ResetTimer(totalSeconds);
    }

    private void ResetTimer(int seconds)
    {
        remainingSeconds = Mathf.Max(1, seconds);
        StopCountdownTimer();
        _countdownRoutine = StartCoroutine(CountdownTimer());
    }

    private void StopCountdownTimer()
    {
        if (_countdownRoutine != null)
        {
            StopCoroutine(_countdownRoutine);
            _countdownRoutine = null;
        }

        isRunning = false;
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
        _countdownRoutine = null;
        Debug.Log("Hết thời gian OTP!");
    }

    private string GetOtp()
    {
        if (useSingleKeyboardInput && _otpKeyboardInput != null)
            return KeepOnlyDigits(_otpKeyboardInput.text);

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

        _ = purposeLabel;

        textStatus.text =
            $"Nhập xác minh gồm 6 số vừa được gửi đến \n {channelLabel} <color #ff7b00ff>{contactIdentifier}</color>. ";
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

    private void EnsureOtpConfigLoadStarted()
    {
        if (!useServerConfig || _configLoaded || _configLoading)
            return;

        if (!isActiveAndEnabled)
            return;

        StartCoroutine(LoadOtpConfigFromServer());
    }

    private void FinishOtpConfigLoad(bool loaded)
    {
        _configLoaded = loaded;
        _configLoading = false;
    }

    private IEnumerator LoadOtpConfigFromServer()
    {
        string url = baseUrl.TrimEnd('/') + "/config?key=" + otpConfigKey;
        _configLoading = true;

        Debug.Log("[OTP Config] GET " + url);

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
                FinishOtpConfigLoad(false);
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
                FinishOtpConfigLoad(false);
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
                totalSeconds = seconds;   // thời gian hiệu lực OTP
                resendCooldownSeconds = seconds;  // thời gian chờ gửi lại OTP

                Debug.Log($"[OTP Config] Loaded limit = {limitMinutes} phút => {seconds} giây.");
            }
            else
            {
                Debug.LogWarning("[OTP Config] limit <= 0, giữ nguyên giá trị mặc định trong Inspector.");
            }

            FinishOtpConfigLoad(limitMinutes > 0);
        }
    }
    private void SetLoginPrefillFromVerifiedContact()
    {
        if (string.IsNullOrEmpty(contactIdentifier))
            return;

        string display = contactIdentifier.Trim();

        // Nếu là phone thì convert 84xxxxxxxxx -> 0xxxxxxxxx cho dễ nhìn
        if (otpByChannel == "phone")
        {
            string digits = display.Replace(" ", "").Replace("-", "");
            if (digits.StartsWith("84") && digits.Length > 2)
            {
                display = "0" + digits.Substring(2);
            }
            else
            {
                display = digits;
            }
        }

        PlayerPrefs.SetString(PREF_LOGIN_PREFILL, display);
        PlayerPrefs.Save();
        Debug.Log($"[OTP] Set LOGIN_PREFILL_USERNAME = {display}");

        // Nếu login đang tồn tại trên scene thì cập nhật luôn
        if (LoginController.Instance != null)
        {
            LoginController.Instance.RefreshLoginPrefill();
        }
    }

}
