using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine.Networking;
using UnityEngine.EventSystems;
using System;
using System.Text;

public class LoginController : MonoBehaviour
{
    // ===== SINGLETON ĐƠN GIẢN CHO LOGIN UI =====
    public static LoginController Instance { get; private set; }
    public static Action OnLoginComplete;

    [Header("UI References")]
    public TMP_InputField inputUsername;
    public TMP_InputField inputPassword;
    public Button buttonLogin;

    [Header("Popup Prefabs")]
    public LoginPopupUI successPopupPrefab;
    public LoginPopupUI failPopupPrefab;
    public Transform popupParent;

    [Header("Options")]
    [Tooltip("Tự động focus vào ô username khi mở scene.")]
    public bool autoFocusUsername = true;

    bool _isLoggingIn = false;
    OpenClosePanel openClosePanel;

    private void Awake()
    {
        // Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Ẩn ký tự mật khẩu + chặn copy/cut
        if (inputPassword != null)
        {
            inputPassword.contentType = TMP_InputField.ContentType.Password;
            inputPassword.ForceLabelUpdate();

            inputPassword.onSelect.AddListener((_) => ClearPasswordSelection());
            inputPassword.onDeselect.AddListener((_) => ClearPasswordSelection());
        }
    }

    private void Start()
    {
        openClosePanel = GameObject.FindAnyObjectByType<OpenClosePanel>();
        if (buttonLogin != null)
            buttonLogin.onClick.AddListener(OnLoginClicked);

        if (autoFocusUsername && inputUsername != null)
        {
            inputUsername.ActivateInputField();
            inputUsername.Select();
        }
    }

    private void Update()
    {
        HandleKeyboardNavigation();

        // Nếu password đang focus, chặn Ctrl+C / Ctrl+X
        if (inputPassword != null && inputPassword.isFocused)
        {
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl)
                        || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand);

            if (ctrl && (Input.GetKeyDown(KeyCode.C) || Input.GetKeyDown(KeyCode.X)))
            {
                GUIUtility.systemCopyBuffer = string.Empty;
                Debug.Log("[LoginController] Copy/Cut mật khẩu đã bị chặn.");
                ClearPasswordSelection();
            }

            if (inputPassword.selectionAnchorPosition != inputPassword.selectionFocusPosition)
                ClearPasswordSelection();
        }
    }

    // ============================================================
    //  HÀM DÙNG CHUNG KHI LOGIN THÀNH CÔNG (PASSWORD HOẶC QR)
    // ============================================================
    private void HandleLoginSuccess(AuthResponseRoot auth, string successMessage = "Đăng nhập thành công")
    {
        if (auth == null || auth.data == null || string.IsNullOrEmpty(auth.data.token))
        {
            Debug.LogWarning("[LoginController] HandleLoginSuccess: auth hoặc token rỗng.");
            return;
        }

        // Lưu token + user vào TokenStore
        TokenStore.SetData(auth);
        Debug.Log("[LoginController] Token đã được lưu, chuẩn bị đóng UI login.");

        // POPUP THÀNH CÔNG
        ShowPopup(
            successPopupPrefab,
            "Thành công",
            successMessage,
            () =>
            {
                // Sau khi bấm OK trong popup
                OnLoginComplete?.Invoke();

                if (openClosePanel != null)
                    openClosePanel.CloseUI();
                else
                    Debug.LogWarning("[LoginController] Không tìm thấy OpenClosePanel để đóng login panel!");
            });
    }

    /// <summary>
    /// Hàm static để các kênh khác (QR, OTP, v.v.) gọi vào khi đã có JWT token.
    /// </summary>
public static void LoginWithQrToken(string raw)
{
    if (string.IsNullOrEmpty(raw))
    {
        Debug.LogWarning("[LoginController] LoginWithQrToken: token rỗng.");
        return;
    }

    // Trường hợp Firebase gửi cả JSON:
    // {"status":true,"data":{"token":"<jwt>"}}
    if (raw[0] == '{')
    {
        try
        {
            var resp = JsonUtility.FromJson<AuthResponseRoot>(raw);
            if (resp != null && resp.data != null && !string.IsNullOrEmpty(resp.data.token))
            {
                raw = resp.data.token;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[LoginController] Không parse được JSON token từ QR: " + e);
        }
    }

    string jwt = raw;

    // Decode JWT -> lấy user
    AuthUser userFromJwt = null;
    bool hasUser = TryGetUserFromJwt(jwt, out userFromJwt);

    var auth = new AuthResponseRoot
    {
        status = true,
        data = new AuthData
        {
            token       = jwt,
            user        = hasUser ? userFromJwt : null,
            totalUnread = null
        }
    };

    if (Instance != null)
    {
        Instance.HandleLoginSuccess(auth, "Đăng nhập bằng QR thành công");
    }
    else
    {
        TokenStore.SetData(auth);
        OnLoginComplete?.Invoke();
    }
}
private static bool TryGetUserFromJwt(string jwt, out AuthUser userOut)
{
    userOut = null;

    if (string.IsNullOrEmpty(jwt))
        return false;

    var parts = jwt.Split('.');
    if (parts.Length != 3)
    {
        Debug.LogWarning("[LoginController] JWT không đúng định dạng 3 phần.");
        return false;
    }

    string payload = parts[1];

    // Base64Url -> Base64
    payload = payload.Replace('-', '+').Replace('_', '/');
    switch (payload.Length % 4)
    {
        case 2: payload += "=="; break;
        case 3: payload += "="; break;
        case 0: break;
        default:
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4), '=');
            break;
    }

    byte[] jsonBytes;
    try
    {
        jsonBytes = Convert.FromBase64String(payload);
    }
    catch (Exception e)
    {
        Debug.LogError("[LoginController] Base64 decode JWT payload fail: " + e);
        return false;
    }

    string json = Encoding.UTF8.GetString(jsonBytes);
    Debug.Log("[LoginController] JWT payload = " + json);

    JwtPayload payloadObj = null;
    try
    {
        payloadObj = JsonUtility.FromJson<JwtPayload>(json);
    }
    catch (Exception e)
    {
        Debug.LogError("[LoginController] Parse JWT payload JSON fail: " + e);
        return false;
    }

    if (payloadObj == null || payloadObj.user == null)
    {
        Debug.LogWarning("[LoginController] JWT payload không có field user.");
        return false;
    }

    // Map sang AuthUser cho thống nhất với login thường
    var u = payloadObj.user;
    userOut = new AuthUser
    {
        id           = u.id,
        username     = u.username,
        fullName     = u.fullName,
        gender       = u.gender,
        role         = u.role,
        email        = u.email,
        status       = u.status,
        avatar       = u.avatar,
        referralCode = u.referralCode,
        jit          = u.jit
    };

    return true;
}

    // ============================================================
    //  LOGIN BẰNG USERNAME/PASSWORD (GIỮ Y NHƯ CŨ, CHỈ GỌI HandleLoginSuccess)
    // ============================================================
    private void HandleKeyboardNavigation()
    {
        // Nhấn Tab để chuyển input
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (inputUsername != null && inputUsername.isFocused)
            {
                inputPassword.ActivateInputField();
                inputPassword.Select();
            }
            else if (inputPassword != null && inputPassword.isFocused)
            {
                EventSystem.current.SetSelectedGameObject(buttonLogin.gameObject);
            }
        }

        // Nhấn Enter để login
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (!_isLoggingIn)
                OnLoginClicked();
        }
    }

    private void OnLoginClicked()
    {
        if (_isLoggingIn) return;

        string usernameRaw = inputUsername != null ? inputUsername.text.Trim() : string.Empty;
        string password = inputPassword != null ? inputPassword.text : string.Empty;

        // ======== Validate cơ bản ========
        if (string.IsNullOrEmpty(usernameRaw) || string.IsNullOrEmpty(password))
        {
            ShowPopup(
                failPopupPrefab,
                "Cảnh báo",
                "Vui lòng nhập đầy đủ tài khoản và mật khẩu."
            );
            return;
        }

        // ======== Validate username (email hoặc số điện thoại) ========
        if (!IsValidEmail(usernameRaw) && !IsValidPhoneVN(usernameRaw))
        {
            ShowPopup(
                failPopupPrefab,
                "Cảnh báo",
                "Tên đăng nhập không hợp lệ. Vui lòng nhập email hoặc số điện thoại hợp lệ."
            );
            return;
        }

        // ======== Nếu là số điện thoại thì convert 0 -> 84 ========
        string usernameForAPI = ConvertPhoneForAPI(usernameRaw);

        StartCoroutine(LoginRoutine(usernameForAPI, password));
    }

    // ================== Regex validation ==================
    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return false;
        string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        return Regex.IsMatch(email, pattern);
    }

    private bool IsValidPhoneVN(string phone)
    {
        if (string.IsNullOrEmpty(phone)) return false;
        // Cho phép: 0xxxxxxxxx hoặc +84xxxxxxxxx (3|5|7|8|9)
        string pattern = @"^(0|\+84)(3|5|7|8|9)\d{8}$";
        return Regex.IsMatch(phone, pattern);
    }

    private string ConvertPhoneForAPI(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        if (input.StartsWith("0")) return "84" + input.Substring(1);
        return input;
    }

    // ================== Gửi request đăng nhập ==================
    private IEnumerator LoginRoutine(string username, string password)
    {
        _isLoggingIn = true;
        if (buttonLogin) buttonLogin.interactable = false;

        string url = $"{LmsStore.Instance.baseUrl}/users/authenticate";

        string jsonData = JsonUtility.ToJson(new LoginRequest { username = username, password = password });
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);

        using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.SetRequestHeader("Accept", "application/json");

            Debug.Log($"Đang đăng nhập bằng username (API): {username}");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string resp = www.downloadHandler.text;
                Debug.Log($"Đăng nhập thành công (raw): {resp}");

                var auth = JsonUtility.FromJson<AuthResponseRoot>(resp);
                if (auth != null && auth.data != null)
                {
                    // Gọi hàm dùng chung
                    HandleLoginSuccess(auth, "Đăng nhập thành công");
                }
                else
                {
                    Debug.LogWarning("Không thể parse dữ liệu đăng nhập hợp lệ!");

                    // POPUP THẤT BẠI – lỗi parse dữ liệu
                    ShowPopup(
                        failPopupPrefab,
                        "Cảnh báo",
                        "Dữ liệu phản hồi từ máy chủ không hợp lệ. Vui lòng thử lại sau.");
                }
            }
            else
            {
                string serverText = www.downloadHandler != null
                    ? www.downloadHandler.text
                    : string.Empty;

                Debug.LogError($"Đăng nhập thất bại: {www.error}\nResponse: {serverText}");

                string errorMessage = ServerErrorConverter.Convert(serverText);

                ShowPopup(
                    failPopupPrefab,
                    "Cảnh báo",
                    errorMessage);
            }
        }

        _isLoggingIn = false;
        if (buttonLogin) buttonLogin.interactable = true;
    }

    private void ClearPasswordSelection()
    {
        if (inputPassword == null) return;
        int pos = inputPassword.stringPosition;
        inputPassword.selectionAnchorPosition = pos;
        inputPassword.selectionFocusPosition = pos;
    }

    private void ShowPopup(LoginPopupUI prefab, string header, string message, Action onReturn = null)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[LoginController] Chưa gán prefab popup.");
            return;
        }

        Transform parent = popupParent != null ? popupParent : transform.root;
        LoginPopupUI popupInstance = Instantiate(prefab, parent);

        // Gọi Init để gán text + callback
        popupInstance.Init(header, message, () =>
        {
            onReturn?.Invoke();
        });
    }

    // ================== DTOs (match JSON) ==================
    [System.Serializable]
    private class LoginRequest
    {
        public string username;
        public string password;
    }

    [Serializable]
    private class ErrorResponse
    {
        public string message;
    }
}

// ====== Models (đặt riêng file cũng được) ======
[System.Serializable]
public class AuthResponseRoot
{
    public bool status;
    public AuthData data;
}

[System.Serializable]
public class AuthData
{
    public string token;
    public AuthUser user;
    public AuthUnread totalUnread;
}

[System.Serializable]
public class AuthUser
{
    public string id;
    public string username;
    public string fullName;
    public string gender;
    public string role;
    public string email;
    public string status;
    public string avatar;
    public string referralCode;
    public string jit;
}

[System.Serializable]
public class AuthUnread
{
    public string all;
    public string personal;
    public string system;
}
[Serializable]
public class JwtPayloadUser
{
    public string id;
    public string username;
    public string fullName;
    public string gender;
    public string role;
    public string email;
    public string status;
    public string avatar;
    public string referralCode;
    public string jit;
}

[Serializable]
public class JwtPayload
{
    public JwtPayloadUser user;
}
