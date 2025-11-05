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
public string baseUrl = "https://apis-dev.xheroapp.com";
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

    // username (email hoặc 84...) nhận từ OTP
    private string usernameForReset = "";

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

    private IEnumerator DoResetPassword(string username, string pwd, string repwd)
    {
        username = (username ?? "").Trim();
        if (string.IsNullOrEmpty(username))
        {
            if (errorText) errorText.text = "Thiếu username (email/số điện thoại).";
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
                if (errorText)
                    errorText.text = string.IsNullOrEmpty(req.downloadHandler.text)
                        ? "Đổi mật khẩu thất bại. Vui lòng thử lại."
                        : req.downloadHandler.text;

                if (btnEnter) btnEnter.interactable = true;
                yield break;
            }
        }

        if (errorText) errorText.text = "Không tìm thấy endpoint reset mật khẩu (404).";
        if (btnEnter) btnEnter.interactable = true;
    }

    private void Start()
    {
        if (btnBack)  btnBack.onClick.AddListener(OnBack);
        if (btnEnter) btnEnter.onClick.AddListener(OnSubmit);

        if (passField)    passField.onValueChanged.AddListener(_ => Validate());
        if (confirmField) confirmField.onValueChanged.AddListener(_ => Validate());

        Validate();
    }

    private void OnDestroy()
    {
        if (btnBack)  btnBack.onClick.RemoveListener(OnBack);
        if (btnEnter) btnEnter.onClick.RemoveListener(OnSubmit);

        if (passField)    passField.onValueChanged.RemoveAllListeners();
        if (confirmField) confirmField.onValueChanged.RemoveAllListeners();
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
            if (errorText) errorText.text = "Thiếu username (email/số điện thoại).";
            return;
        }

        string p1 = passField ? passField.text : "";
        string p2 = confirmField ? confirmField.text : "";

        if (!IsValidPassword(p1, minLength) || p1 != p2)
        {
            if (errorText) errorText.text = "Mật khẩu không hợp lệ hoặc không khớp.";
            return;
        }

        if (errorText) errorText.text = "";
        StartCoroutine(DoResetPassword(usernameForReset, p1, p2));
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
}
