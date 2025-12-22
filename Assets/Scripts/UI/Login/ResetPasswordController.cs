using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;
using System.Collections;
using UnityEngine.Networking;
using System.Text;

public class ResetPasswordController : MonoBehaviour
{
    [Header("API")]
    // private string baseUrl = LmsStore.Instance.baseUrl; // Tự động đồng bộ baseUrl với LmsStore (DEV/PROD đổi 1 chỗ duy nhất)
    private string baseUrl;
    // Đừng tin Inspector 100%. Route reset sẽ thử theo danh sách dưới:
    private static readonly string[] ResetPaths = new string[] {
        "/users/password-reset",                 // chuẩn theo swagger mới
        "/api/v1/users/password-reset",          // nếu có prefix
        "/users/passwordreset",                  // fallback swagger cũ
        "/api/v1/users/passwordreset"            // fallback có prefix
    };

    [Header("Inputs")]
    public TMP_InputField passField;
    public TMP_InputField confirmField;

    [Header("Password Show/Hide")]
    public Button btnTogglePassword;
    public Image  btnTogglePasswordIcon;
    public Sprite iconShow;
    public Sprite iconHide;

    [Header("Password Show/Hide")]
    public Button btnTogglePassword2;
    public Image  btnTogglePasswordIcon2;
    public Sprite iconShow2;
    public Sprite iconHide2;

    [Header("Buttons")]
    public Button btnEnter;   // Gửi API reset
    public Button btnBack;

    [Header("Panels")]
    public GameObject currentPanel;   // panel này (Reset)
    public GameObject backPanel;      // panel quay lại (ví dụ Login)
    public GameObject successPanel;   // optional: show khi đổi mật khẩu OK
    public GameObject imageToShow;

    [Header("Optional UI")]
    public TextMeshProUGUI errorText;

    [Header("Rules")]
    [Tooltip("Tối thiểu độ dài; 0 = không kiểm tra độ dài")]
    public int minLength = 0; // đặt 6/8 nếu muốn

    [Header("Popup Warning (optional)")]
    public LoginPopupUI warningPopupPrefab;   // prefab cảnh báo nhập sai
    public Transform popupParent;             // Canvas/Panel để spawn popup

    // username (email hoặc 84...) nhận từ OTP
    private string usernameForReset = "";

    private bool passShown1 = false;
    private bool passShown2 = false;

    private void Awake()
    {
        // Lúc này Unity đã tạo xong object, gọi Instance an toàn hơn
        baseUrl = LmsStore.Instance.baseUrl;

        if (passField != null)
        {
            passField.contentType = TMP_InputField.ContentType.Password;
        }
        if (confirmField != null)
        {
            confirmField.contentType = TMP_InputField.ContentType.Password;
        }
    }

    public void SetUsername(string identifier)
    {
        usernameForReset = (identifier ?? "").Trim();
        Debug.Log($"[ResetPassword] SetUsername = '{usernameForReset}'");
    }

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(usernameForReset) && !string.IsNullOrEmpty(AuthFlowSession.LastOtpIdentifier))
            usernameForReset = AuthFlowSession.LastOtpIdentifier?.Trim();

        Debug.Log($"[ResetPassword] OnEnable usernameForReset = '{usernameForReset}'");
        Validate();
        if (errorText) errorText.text = "";
    }

    private void Start()
    {
        if (btnBack)  btnBack.onClick.AddListener(OnBack);
        if (btnEnter) btnEnter.onClick.AddListener(OnSubmit);

        if (passField)    passField.onValueChanged.AddListener(_ => Validate());
        if (confirmField) confirmField.onValueChanged.AddListener(_ => Validate());

        if (btnTogglePassword)  btnTogglePassword.onClick.AddListener(TogglePass1);
        if (btnTogglePassword2) btnTogglePassword2.onClick.AddListener(TogglePass2);

        // set icon ban đầu
        ApplyMask1(false);
        ApplyMask2(false);

        Validate();
    }

    private void OnDestroy()
    {
        if (btnBack)  btnBack.onClick.RemoveListener(OnBack);
        if (btnEnter) btnEnter.onClick.RemoveListener(OnSubmit);

        if (passField)    passField.onValueChanged.RemoveAllListeners();
        if (confirmField) confirmField.onValueChanged.RemoveAllListeners();

        if (btnTogglePassword)  btnTogglePassword.onClick.RemoveListener(TogglePass1);
        if (btnTogglePassword2) btnTogglePassword2.onClick.RemoveListener(TogglePass2);
    }

    private void OnBack()
    {
        if (currentPanel) currentPanel.SetActive(false);
        if (backPanel)    backPanel.SetActive(true);
    }

    private void OnSubmit()
    {
        if (string.IsNullOrEmpty(usernameForReset))
        {
            const string msg = "Thiếu username (email/số điện thoại).";
            if (errorText) errorText.text = msg;
            ShowWarningPopup(msg);
            return;
        }

        string p1 = passField ? passField.text : "";
        string p2 = confirmField ? confirmField.text : "";

        // ======= Kiểm tra 2 mật khẩu giống nhau + hợp lệ =======
        string msgError = null;

        if (p1 != p2)
        {
            msgError = "Mật khẩu nhập lại không khớp.";
        }
        else if (!IsValidPassword(p1, minLength))
        {
            msgError = "Mật khẩu không hợp lệ. Mật khẩu phải gồm chữ cái, số và ký tự đặc biệt.";
        }

        if (msgError != null)
        {
            if (errorText) errorText.text = msgError;
            ShowWarningPopup(msgError);   // POPUP CẢNH BÁO KHI LỖI (đặc biệt là 2 mật khẩu không giống nhau)
            return;
        }

        if (errorText) errorText.text = "";
        StartCoroutine(DoResetPassword(usernameForReset, p1, p2));
    }

    private IEnumerator DoResetPassword(string username, string pwd, string repwd)
    {
        username = (username ?? "").Trim();
        if (string.IsNullOrEmpty(username))
        {
            const string msg = "Thiếu username (email/số điện thoại).";
            if (errorText) errorText.text = msg;
            ShowWarningPopup(msg);
            yield break;
        }

        string json = "{\"username\":\"" + EscapeJson(username) + "\"," +
                      "\"password\":\"" + EscapeJson(pwd) + "\"," +
                      "\"retypePassword\":\"" + EscapeJson(repwd) + "\"}";

        if (btnEnter) btnEnter.interactable = false;

        foreach (var raw in ResetPaths)
        {
            var path = (raw ?? "").Trim();
            if (string.IsNullOrEmpty(path)) continue;

            string url = baseUrl.TrimEnd('/') + path;
            Debug.Log($"[ResetPassword] TRY PUT -> {url} body={json}");

            using (var req = new UnityWebRequest(url, "PUT"))
            {
                req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
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
                    Debug.Log("[ResetPassword] OK: " + req.downloadHandler.text);
                    if (currentPanel) currentPanel.SetActive(false);
                    if (successPanel)
                    {
                        successPanel.SetActive(true);
                    }
                    else if (backPanel) backPanel.SetActive(true);
                    if (imageToShow) imageToShow.SetActive(true);
                    if (btnEnter) btnEnter.interactable = true;
                    yield break;
                }

                Debug.LogWarning($"[ResetPassword] FAIL {req.responseCode} at {path}: {req.error}\n{req.downloadHandler.text}");

                if (req.responseCode == 404)
                {
                    // thử path tiếp theo
                    continue;
                }

                // Lỗi khác -> báo luôn
                string serverMsg = string.IsNullOrEmpty(req.downloadHandler.text)
                    ? "Đổi mật khẩu thất bại. Vui lòng thử lại."
                    : req.downloadHandler.text;

                if (errorText)
                    errorText.text = serverMsg;

                ShowWarningPopup(serverMsg);

                if (btnEnter) btnEnter.interactable = true;
                yield break;
            }
        }

        const string notFoundMsg = "Không tìm thấy endpoint reset mật khẩu (404).";
        if (errorText) errorText.text = notFoundMsg;
        ShowWarningPopup(notFoundMsg);

        if (btnEnter) btnEnter.interactable = true;
    }

    // ====== Validate ======
    private void Validate()
    {
        string p1 = passField != null ? passField.text : "";
        string p2 = confirmField != null ? confirmField.text : "";

        bool match  = p1 == p2 && p1.Length > 0;
        bool strong = IsValidPassword(p1, minLength);
        bool ok = match && strong;

        if (btnEnter) btnEnter.interactable = ok;

        if (errorText)
        {
            if (!match)       errorText.text = "Mật khẩu nhập lại không khớp.";
            else if (!strong) errorText.text = "Mật khẩu phải gồm chữ cái, số và ký tự đặc biệt.";
            else              errorText.text = "";
        }
    }

    /// Hợp lệ khi có ít nhất 1 chữ cái, 1 chữ số, 1 ký tự đặc biệt; kiểm tra độ dài nếu minLen>0
    public static bool IsValidPassword(string s, int minLen = 0)
    {
        if (string.IsNullOrEmpty(s)) return false;
        string core = @"(?=.*[A-Za-z])(?=.*\d)(?=.*[^A-Za-z0-9])";
        string len  = (minLen > 0) ? $@"(?=.{{{minLen},}})" : "";
        string pattern = $"^{len}{core}.+$";
        return Regex.IsMatch(s, pattern);
    }

    private static string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private void ShowWarningPopup(string message)
    {
        if (warningPopupPrefab == null)
        {
            Debug.LogWarning("[ResetPassword] warningPopupPrefab chưa được gán. Message: " + message);
            return;
        }

        Transform parent = popupParent;

        if (parent == null)
        {
            var canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas != null) parent = canvas.transform;
        }

        if (parent == null)
        {
            Debug.LogWarning("[ResetPassword] Không tìm thấy Canvas để hiển thị popup. Hãy gán popupParent.");
            return;
        }

        var popup = Instantiate(warningPopupPrefab, parent);
        popup.Init("Cảnh báo", message);
    }

    private void TogglePass1()
    {
        passShown1 = !passShown1;
        ApplyMask1(passShown1);
    }

    private void TogglePass2()
    {
        passShown2 = !passShown2;
        ApplyMask2(passShown2);
    }

    private void ApplyMask1(bool showPlain)
    {
        SetTMPPasswordField(passField, showPlain);
        if (btnTogglePasswordIcon)
            btnTogglePasswordIcon.sprite = showPlain ? iconShow : iconHide;
    }

    private void ApplyMask2(bool showPlain)
    {
        SetTMPPasswordField(confirmField, showPlain);
        if (btnTogglePasswordIcon2)
            btnTogglePasswordIcon2.sprite = showPlain ? iconShow2 : iconHide2;
    }

    private static void SetTMPPasswordField(TMP_InputField field, bool showPlain)
    {
        if (field == null) return;
        field.contentType = showPlain ? TMP_InputField.ContentType.Standard
                                    : TMP_InputField.ContentType.Password;
        field.asteriskChar = '*';
        field.ForceLabelUpdate();
    }

}
