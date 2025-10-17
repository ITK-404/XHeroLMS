using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Text.RegularExpressions;
using UnityEngine.Networking;
using UnityEngine.EventSystems;
using System;

public class LoginController : MonoBehaviour
{
    [Header("UI References")]
    public TMP_InputField inputUsername;
    public TMP_InputField inputPassword;
    public Button buttonLogin;

    [Header("Options")]
    [Tooltip("Tự động focus vào ô username khi mở scene.")]
    public bool autoFocusUsername = true;

    bool _isLoggingIn = false;
    OpenClosePanel openClosePanel;
    public static Action OnLoginComplete;
    private void Awake()
    {
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
        openClosePanel=GameObject.FindAnyObjectByType<OpenClosePanel>();
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
            Debug.LogWarning("Vui lòng nhập đủ username và password.");
            return;
        }

        // ======== Validate username (email hoặc số điện thoại) ========
        if (!IsValidEmail(usernameRaw) && !IsValidPhoneVN(usernameRaw))
        {
            Debug.LogWarning("Tên đăng nhập không hợp lệ. Vui lòng nhập email hoặc số điện thoại hợp lệ.");
            return;
        }

        // ======== Nếu là số điện thoại thì convert 0 → 84 ========
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

        const string url = "https://apis-dev.xheroapp.com/users/authenticate";

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
                Debug.Log($"Đăng nhập thành công: {resp}");

                var auth = JsonUtility.FromJson<AuthResponseRoot>(resp);
                if (auth != null && auth.data != null)
                {
                    TokenStore.SetData(auth);
                    Debug.Log("Đăng nhập thành công, đóng panel login.");
                    OnLoginComplete?.Invoke();
                    if (openClosePanel != null)
                        openClosePanel.CloseUI();  // Đóng UI login
                    else
                        Debug.LogWarning("Không tìm thấy OpenClosePanel để đóng login panel!");
                }
                else
                {
                    Debug.LogWarning("Không thể parse dữ liệu đăng nhập hợp lệ!");
                }
            }
            else
            {
                Debug.LogError($"Đăng nhập thất bại: {www.error}\nResponse: {www.downloadHandler.text}");
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

    // ================== DTOs (match JSON) ==================
    [System.Serializable]
    private class LoginRequest
    {
        public string username;
        public string password;
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
