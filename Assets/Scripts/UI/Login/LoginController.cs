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

    [Header("Password Show/Hide")]
    public Button btnTogglePassword;
    public Image  btnTogglePasswordIcon;
    public Sprite iconShow;
    public Sprite iconHide;

    [Header("Options")]
    public bool autoFocusUsername = true;

    public bool showSuccessPopup = false;

    bool autoRestoreOnStart = true;

    string verifyPath = "/users/me";

    bool disableLoginWhileVerifying = true;

    bool _isLoggingIn = false;
    bool _isVerifying = false;

    OpenClosePanel openClosePanel;
    private const string PREF_LOGIN_PREFILL = "LOGIN_PREFILL_USERNAME";

    private bool _passShown = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

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

        ApplyPrefillOrFocus();

        if (btnTogglePassword != null)
            btnTogglePassword.onClick.AddListener(TogglePassword);

        ApplyPasswordMask(false); // mặc định ẩn mật khẩu

        // ===== AUTO CHECK LOGIN ON BOOT =====
        if (autoRestoreOnStart)
            StartCoroutine(AutoRestoreSession());
    }

    private void OnEnable()
    {
        ApplyPrefillOrFocus();
    }

    private void Update()
    {
        HandleKeyboardNavigation();

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
    //  AUTO RESTORE + VERIFY TOKEN
    // ============================================================
    private IEnumerator AutoRestoreSession()
    {
        if (_isVerifying) yield break;

        bool restored = TokenStore.TryRestoreFromDisk();
        if (!restored || string.IsNullOrEmpty(TokenStore.AccessToken))
        {
            ApplyPrefillOrFocus();
            yield break;
        }

        _isVerifying = true;

        if (disableLoginWhileVerifying && buttonLogin != null)
            buttonLogin.interactable = false;

        Debug.Log("[LoginController] Found saved token. Verifying...");

        yield return StartCoroutine(VerifyTokenRoutine(
            onValid: () =>
            {
                Debug.Log("[LoginController] Token valid -> auto login.");
                OnLoginComplete?.Invoke();
                if (openClosePanel != null) openClosePanel.CloseUI();
            },
            onInvalid401: (reason) =>
            {
                Debug.LogWarning("[LoginController] Token invalid (401/403) -> require login. " + reason);
                TokenStore.Clear();            // CHỈ xoá khi chắc chắn invalid
                ApplyPrefillOrFocus();
            },
            onNetworkOrServerError: (reason) =>
            {
                Debug.LogWarning("[LoginController] Verify failed (network/server). Keep token. Reason: " + reason);

                OnLoginComplete?.Invoke();
                if (openClosePanel != null) openClosePanel.CloseUI();

            }
        ));

        _isVerifying = false;

        if (disableLoginWhileVerifying && buttonLogin != null)
            buttonLogin.interactable = true;
    }

    private IEnumerator VerifyTokenRoutine(
        Action onValid,
        Action<string> onInvalid401,
        Action<string> onNetworkOrServerError
    )
    {
        string baseUrl = LmsStore.Instance.baseUrl?.TrimEnd('/');
        if (string.IsNullOrEmpty(baseUrl))
        {
            onNetworkOrServerError?.Invoke("BaseUrl empty");
            yield break;
        }

        string path = string.IsNullOrEmpty(verifyPath) ? "/users/me" : verifyPath.Trim();
        if (!path.StartsWith("/")) path = "/" + path;

        string url = baseUrl + path;

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Accept", "application/json");

            string token = TokenStore.AccessToken?.Trim();
            if (!string.IsNullOrEmpty(token))
            {
                if (!token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    token = "Bearer " + token;
                www.SetRequestHeader("Authorization", token);
            }

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                onValid?.Invoke();
                yield break;
            }

            long code = www.responseCode;
            string serverText = www.downloadHandler != null ? www.downloadHandler.text : "";

            if (code == 401 || code == 403)
            {
                onInvalid401?.Invoke($"HTTP {code} body: {serverText}");
            }
            else
            {
                onNetworkOrServerError?.Invoke($"HTTP {code} err={www.error} body={serverText}");
            }
        }
    }

    private void HandleLoginSuccess(AuthResponseRoot auth, string successMessage = "Đăng nhập thành công")
    {
        if (auth == null || auth.data == null || string.IsNullOrEmpty(auth.data.token))
        {
            Debug.LogWarning("[LoginController] HandleLoginSuccess: auth hoặc token rỗng.");
            return;
        }

        TokenStore.SetData(auth);
        Debug.Log("[LoginController] Token đã được lưu. Login success.");

        PlayerPrefs.DeleteKey(PREF_LOGIN_PREFILL);
        PlayerPrefs.Save();

        // Nếu không muốn hiện popup thành công -> đóng UI + invoke luôn
        if (!showSuccessPopup || successPopupPrefab == null)
        {
            OnLoginComplete?.Invoke();

            if (openClosePanel != null)
                openClosePanel.CloseUI();
            else
                Debug.LogWarning("[LoginController] Không tìm thấy OpenClosePanel để đóng login panel!");

            return;
        }

        // (Giữ lại nếu bạn muốn bật popup trong một số trường hợp)
        ShowPopup(
            successPopupPrefab,
            "Thành công",
            successMessage,
            () =>
            {
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
                token = jwt,
                user = hasUser ? userFromJwt : null,
                totalUnread = null
            }
        };

        if (Instance != null)
        {
            Instance.HandleLoginSuccess(auth, "Chào mừng bạn trở lại");
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
    //  LOGIN CLICK / VALIDATION (GIỮ NGUYÊN)
    // ============================================================
    private void HandleKeyboardNavigation()
    {
        if (_isVerifying) return; // đang verify thì bỏ qua keyboard login

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

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (!_isLoggingIn && !_isVerifying)
                OnLoginClicked();
        }
    }

    private void OnLoginClicked()
    {
        if (_isLoggingIn || _isVerifying) return;

        string usernameRaw = inputUsername != null ? inputUsername.text.Trim() : string.Empty;
        string password = inputPassword != null ? inputPassword.text : string.Empty;

        if (string.IsNullOrEmpty(usernameRaw) || string.IsNullOrEmpty(password))
        {
            ShowPopup(
                failPopupPrefab,
                "Đăng nhập thất bại",
                "Vui lòng nhập đầy đủ tài khoản và mật khẩu.",
                icon: LoginPopupUI.PopupIconType.Warning
            );
            return;
        }

        if (!IsValidEmail(usernameRaw) && !IsValidPhoneVN(usernameRaw))
        {
            ShowPopup(
                failPopupPrefab,
                "Đăng nhập thất bại",
                "Tên đăng nhập hoặc mật khẩu không hợp lệ. Vui lòng nhập email hoặc số điện thoại hợp lệ.",
                icon: LoginPopupUI.PopupIconType.Warning
            );
            return;
        }

        string usernameForAPI = ConvertPhoneForAPI(usernameRaw);
        StartCoroutine(LoginRoutine(usernameForAPI, password));
    }

    private bool IsValidEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return false;
        string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        return Regex.IsMatch(email, pattern);
    }

    private bool IsValidPhoneVN(string phone)
    {
        if (string.IsNullOrEmpty(phone)) return false;
        string pattern = @"^(0|\+84)(3|5|7|8|9)\d{8}$";
        return Regex.IsMatch(phone, pattern);
    }

    private string ConvertPhoneForAPI(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        if (input.StartsWith("0")) return "84" + input.Substring(1);
        return input;
    }

    private IEnumerator LoginRoutine(string username, string password)
    {
        _isLoggingIn = true;
        if (buttonLogin) buttonLogin.interactable = false;

        string url = $"{LmsStore.Instance.baseUrl}/users/authenticate";

        string jsonData = JsonUtility.ToJson(new LoginRequest { username = username, password = password });
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);

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
                    HandleLoginSuccess(auth, "Đăng nhập thành công");
                    Debug.Log($"Authorization: Bearer {auth.data.token}");
                }
                else
                {
                    ShowPopup(failPopupPrefab, "Đăng nhập thất bại", "Dữ liệu phản hồi từ máy chủ không hợp lệ. Vui lòng thử lại sau.", icon: LoginPopupUI.PopupIconType.Error);
                }
            }
            else
            {
                string serverText = www.downloadHandler != null ? www.downloadHandler.text : string.Empty;
                Debug.LogError($"Đăng nhập thất bại: {www.error}\nResponse: {serverText}");

                string errorMessage = ServerErrorConverter.Convert(serverText);
                ShowPopup(failPopupPrefab, "Đăng nhập thất bại", errorMessage, icon: LoginPopupUI.PopupIconType.Warning);
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

    public void ShowPopup(
        LoginPopupUI prefab,
        string header,
        string message,
        Action onReturn = null,
        LoginPopupUI.PopupIconType icon = LoginPopupUI.PopupIconType.None
    )
    {
        if (prefab == null)
        {
            Debug.LogWarning("[LoginController] Chưa gán prefab popup.");
            return;
        }

        Transform parent = popupParent != null ? popupParent : transform.root;
        LoginPopupUI popupInstance = Instantiate(prefab, parent);

        popupInstance.Init(header, message, icon, () => { onReturn?.Invoke(); });
    }

    public static void ShowWarning(string message, string header = "Đăng nhập thất bại")
    {
        if (Instance == null)
        {
            Debug.LogWarning("[LoginController] ShowWarning được gọi nhưng Instance == null. Message: " + message);
            return;
        }
        Instance.ShowPopup(Instance.failPopupPrefab, header, message, icon: LoginPopupUI.PopupIconType.Warning);
    }

    [Serializable]
    private class LoginRequest
    {
        public string username;
        public string password;
    }

    private void ApplyPrefillOrFocus()
    {
        if (inputUsername == null) return;

        string prefill = PlayerPrefs.GetString(PREF_LOGIN_PREFILL, "");

        if (!string.IsNullOrEmpty(prefill))
        {
            inputUsername.text = prefill;

            if (inputPassword != null)
            {
                inputPassword.text = "";
                inputPassword.ActivateInputField();
                inputPassword.Select();
            }
        }
        else if (autoFocusUsername)
        {
            inputUsername.ActivateInputField();
            inputUsername.Select();
        }
    }

    public void RefreshLoginPrefill()
    {
        ApplyPrefillOrFocus();
    }
    private void TogglePassword()
    {
        _passShown = !_passShown;
        ApplyPasswordMask(_passShown);

        // Giữ con trỏ, tránh bị chọn bôi đen text khi toggle
        ClearPasswordSelection();
    }

    private void ApplyPasswordMask(bool showPlain)
    {
        SetTMPPasswordField(inputPassword, showPlain);
        if (btnTogglePasswordIcon != null)
            btnTogglePasswordIcon.sprite = showPlain ? iconHide : iconShow;
    }

    private static void SetTMPPasswordField(TMP_InputField field, bool showPlain)
    {
        if (field == null) return;

        // TMP cần set contentType rồi ForceLabelUpdate
        field.contentType = showPlain
            ? TMP_InputField.ContentType.Standard
            : TMP_InputField.ContentType.Password;

        field.asteriskChar = '*';
        field.ForceLabelUpdate();
        
        if (field.isFocused)
            field.ActivateInputField();
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
