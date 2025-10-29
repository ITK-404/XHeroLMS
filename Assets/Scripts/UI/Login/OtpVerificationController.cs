using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Networking;
using System.Text;

public class OtpVerificationController : MonoBehaviour
{
    [Header("API")]
    public string baseUrl = "https://apis-dev.xheroapp.com";

    // Danh sách các endpoint dự phòng; có thể thêm/bớt trong Inspector
    public string[] otpPathsToTry = new string[]
    {
        "/users/otpverification",      // snake kiểu không gạch
        "/users/otp-verification",     // có gạch
        "/users/otpVerification"       // camelCase
    };

    private const string PREF_USERNAME84 = "REG_USERNAME_84";

    [Header("Texts hiển thị thời gian")]
    public TextMeshProUGUI minuteText;
    public TextMeshProUGUI secondText;

    [Header("6 ô nhập OTP")]
    public TMP_InputField[] otpInputs = new TMP_InputField[6];

    [Header("Buttons")]
    public Button btnEnter; // gọi API xác thực
    public Button btnBack;

    [Header("Cấu hình thời gian (giây)")]
    public int totalSeconds = 60; // ví dụ: 1 phút

    [Header("Panels")]
    public GameObject currentPanel;
    public GameObject backPanel;
    public GameObject successPanel;

    [Header("Optional UI")]
    public TextMeshProUGUI errorText;

    // ========= State =========
    private int remainingSeconds;
    private bool isRunning = false;
    private bool isSubmitting = false;

    // Username 84... từ bước Đăng ký (truyền vào ở RegistrationController khi đăng ký thành công)
    [SerializeField] private string username84 = "";

    /// <summary>Được gọi từ RegistrationController sau khi đăng ký OK.</summary>
    public void SetUsername(string username84FromRegister)
    {
        username84 = (username84FromRegister ?? "").Trim();
        AuthFlowSession.LastRegUsername84 = username84; // RAM session
    }

    private void OnEnable()
    {
        EnsureUsernameFromSessionOrPrefs();

        // Clear 6 ô và focus
        for (int i = 0; i < otpInputs.Length; i++)
            if (otpInputs[i] != null) otpInputs[i].text = "";

        if (otpInputs.Length > 0 && otpInputs[0] != null) otpInputs[0].Select();
        if (errorText) errorText.text = "";
    }

    private void Start()
    {
        EnsureUsernameFromSessionOrPrefs();
        ResetTimer();

        // Setup input OTP & listeners
        for (int i = 0; i < otpInputs.Length; i++)
        {
            int index = i;
            if (otpInputs[i] == null) continue;
            otpInputs[i].characterLimit = 1;
            otpInputs[i].contentType = TMP_InputField.ContentType.IntegerNumber;
            otpInputs[i].onValueChanged.AddListener((value) => OnInputChanged(index, value));
        }

        if (otpInputs.Length > 0 && otpInputs[0] != null) otpInputs[0].Select();

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
    }

    private void Update()
    {
        // Backspace: nếu ô đang focus rỗng -> nhảy về ô trước và xoá nó
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

    // ===== Username sourcing =====
    private void EnsureUsernameFromSessionOrPrefs()
    {
        if (!string.IsNullOrEmpty(username84)) return;

        if (!string.IsNullOrEmpty(AuthFlowSession.LastRegUsername84))
        {
            username84 = AuthFlowSession.LastRegUsername84.Trim();
            if (!string.IsNullOrEmpty(username84)) return;
        }

        username84 = PlayerPrefs.GetString(PREF_USERNAME84, "").Trim();
        // nếu vẫn rỗng, sẽ báo khi bấm Enter
    }

    // ===== OTP inputs =====
    private void OnInputChanged(int index, string value)
    {
        if (value.Length > 0 && index < otpInputs.Length - 1 && otpInputs[index + 1] != null)
        {
            otpInputs[index + 1].Select(); // qua ô kế
        }
        else if (value.Length == 0 && index > 0 && otpInputs[index - 1] != null)
        {
            otpInputs[index - 1].Select(); // quay về ô trước
        }
    }

    private void OnEnterClicked()
    {
        if (isSubmitting) return;

        EnsureUsernameFromSessionOrPrefs();

        string otpCode = GetOtp().Trim();

        if (string.IsNullOrEmpty(username84))
        {
            if (errorText) errorText.text = "Thiếu số điện thoại (username). Vui lòng thử lại.";
            Debug.LogWarning("[OTP] username rỗng. Hãy đảm bảo Registration đã gọi SetUsername hoặc Prefs đã lưu.");
            return;
        }

        if (otpCode.Length != 6)
        {
            if (errorText) errorText.text = "OTP phải gồm 6 chữ số.";
            return;
        }

        StartCoroutine(VerifyOtpRoutine(username84, otpCode));
    }

    private IEnumerator VerifyOtpRoutine(string username, string otp)
    {
        if (errorText) errorText.text = "";
        isSubmitting = true;
        if (btnEnter) btnEnter.interactable = false;

        // Thử từng endpoint trong otpPathsToTry
        string lastErrText = null;
        for (int i = 0; i < otpPathsToTry.Length; i++)
        {
            string path = otpPathsToTry[i]?.Trim();
            if (string.IsNullOrEmpty(path)) continue;

            string url  = baseUrl.TrimEnd('/') + path;
            string json = "{\"username\":\"" + EscapeJson(username) + "\",\"otp\":\"" + EscapeJson(otp) + "\"}";

            Debug.Log($"[OTP] Calling: {url} payload={json}");

            using (var req = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
                req.uploadHandler   = new UploadHandlerRaw(bodyRaw);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");

                yield return req.SendWebRequest();

#if UNITY_2020_2_OR_NEWER
                bool ok = req.result == UnityWebRequest.Result.Success || (req.responseCode >= 200 && req.responseCode < 300);
#else
                bool ok = !req.isNetworkError && !req.isHttpError && (req.responseCode >= 200 && req.responseCode < 300);
#endif

                // Thành công -> break ngay
                if (ok)
                {
                    Debug.Log("[OTP] Verify OK via " + path + " -> " + req.downloadHandler.text);

                    if (isRunning)
                    {
                        StopAllCoroutines();
                        isRunning = false;
                    }
                    if (currentPanel) currentPanel.SetActive(false);
                    if (successPanel) successPanel.SetActive(true);

                    isSubmitting = false;
                    if (btnEnter) btnEnter.interactable = true;
                    yield break;
                }

                // Nếu là 404 -> thử endpoint tiếp theo
                if (req.responseCode == 404)
                {
                    Debug.LogWarning($"[OTP] 404 Not Found at {path}. Trying next candidate if any...");
                    lastErrText = $"404 Not Found at {path}";
                    continue;
                }

                // Nếu lỗi khác 404: dừng thử và báo lỗi
                lastErrText = $"OTP Verify FAIL ({req.responseCode}): {req.error}\n{req.downloadHandler.text}";
                Debug.LogWarning(lastErrText);
                if (errorText)
                {
                    errorText.text = string.IsNullOrEmpty(req.downloadHandler.text)
                        ? "Xác thực OTP thất bại. Vui lòng thử lại."
                        : req.downloadHandler.text;
                }

                isSubmitting = false;
                if (btnEnter) btnEnter.interactable = true;
                yield break;
            }
        }

        // Nếu chạy hết mảng mà vẫn không thành công (thường do 404 tất cả)
        if (errorText)
        {
            errorText.text = "Không tìm thấy endpoint OTP (404). Hãy kiểm tra đường dẫn trên server hoặc chỉnh otpPathsToTry.";
        }
        Debug.LogWarning("[OTP] All candidates returned 404. Kiểm tra lại route trên server/Swagger.");

        isSubmitting = false;
        if (btnEnter) btnEnter.interactable = true;
    }

    // ==== Nút quay lại ====
    private void OnBackClicked()
    {
        Debug.Log("Quay lại panel trước");

        if (isRunning)
        {
            StopAllCoroutines();
            isRunning = false;
        }

        if (currentPanel) currentPanel.SetActive(false);
        if (backPanel)    backPanel.SetActive(true);
    }

    // ==== Đếm ngược ====
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

    // ==== Helpers ====
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
