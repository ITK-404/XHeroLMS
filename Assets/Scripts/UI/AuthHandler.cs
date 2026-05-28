using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Handles authentication logic: logout and delete account.
/// Attach to the same GameObject as PlayerPanelUI, or instantiate via new AuthHandler(...).
/// </summary>
public class AuthHandler
{
    // ──────────────────────────────────────────────
    // Config
    // ──────────────────────────────────────────────
    private readonly string _baseUrl;
    private readonly string _logoutPath;
    private readonly string _fromPlatform;
    private readonly string _defaultLoadScene;

    // ──────────────────────────────────────────────
    // Dependencies (injected)
    // ──────────────────────────────────────────────
    private readonly MonoBehaviour _coroutineRunner;   // dùng để StartCoroutine
    private readonly DeleteAccountApi _deleteAccountApi;

    // ──────────────────────────────────────────────
    // State
    // ──────────────────────────────────────────────
    private bool _isLoggingOut = false;
    private bool _isDeleting   = false;
    private bool _isLoaded     = false;   // chặn action sau khi đã chuyển scene

    // ──────────────────────────────────────────────
    // Callbacks (set bởi PlayerPanelUI)
    // ──────────────────────────────────────────────
    public Action OnBegin;          // disable UI buttons
    public Action OnRestore;        // enable  UI buttons
    public Action OnAuthComplete;   // chuyển scene / dọn dẹp

    // ──────────────────────────────────────────────
    // Constructor
    // ──────────────────────────────────────────────
    public AuthHandler(
        MonoBehaviour coroutineRunner,
        string baseUrl,
        string logoutPath,
        string fromPlatform,
        string defaultLoadScene,
        DeleteAccountApi deleteAccountApi)
    {
        _coroutineRunner   = coroutineRunner;
        _baseUrl           = baseUrl?.TrimEnd('/');
        _logoutPath        = logoutPath;
        _fromPlatform      = fromPlatform;
        _defaultLoadScene  = defaultLoadScene;
        _deleteAccountApi  = deleteAccountApi;
    }

    // ──────────────────────────────────────────────
    // Public API
    // ──────────────────────────────────────────────

    /// <summary>Bắt đầu quá trình logout. Gọi từ PlayerPanelUI.</summary>
    public void Logout()
    {
        if (_isLoaded || _isLoggingOut) return;

        if (!TokenStore.IsAuthenticated || string.IsNullOrEmpty(TokenStore.AccessToken))
        {
            Debug.LogWarning("[AuthHandler] Cannot logout: not authenticated.");
            return;
        }

        _coroutineRunner.StartCoroutine(CoLogout());
    }

    /// <summary>Bắt đầu quá trình xoá tài khoản. Gọi từ PlayerPanelUI.</summary>
    public void DeleteAccount()
    {
        if (_isDeleting || _isLoaded) return;

        if (!TokenStore.IsAuthenticated || string.IsNullOrEmpty(TokenStore.AccessToken))
        {
            Debug.LogWarning("[AuthHandler] Cannot delete account: not authenticated.");
            return;
        }

        _isDeleting = true;
        OnBegin?.Invoke();

        _coroutineRunner.StartCoroutine(_deleteAccountApi.DeleteAccountRoutine(
            onSuccess: HandleDeleteSuccess,
            onFail:    HandleDeleteFail
        ));
    }

    // ──────────────────────────────────────────────
    // Private – Logout
    // ──────────────────────────────────────────────

    private IEnumerator CoLogout()
    {
        _isLoggingOut = true;
        OnBegin?.Invoke();

        if (string.IsNullOrEmpty(_baseUrl))
        {
            Debug.LogError("[AuthHandler] Logout failed: baseUrl is null or empty.");
            RestoreFromLogout();
            yield break;
        }

        // Lưu session trước khi logout
        yield return GameInitializer.Instance.GameSessionHandler.SaveSession();

        string url = $"{_baseUrl}{_logoutPath}?fromPlatform={UnityWebRequest.EscapeURL(_fromPlatform)}";

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("authorization", $"Bearer {TokenStore.AccessToken}");
            request.timeout = 20;

            yield return request.SendWebRequest();

#if UNITY_2020_1_OR_NEWER
            bool success = request.result == UnityWebRequest.Result.Success;
#else
            bool success = !request.isNetworkError && !request.isHttpError;
#endif

            if (success)
            {
                Debug.Log("[AuthHandler] Logout API success.");
                HandleAuthComplete(raiseLogout: true);
            }
            else
            {
                Debug.LogError($"[AuthHandler] Logout API failed: {request.error}\n{request.downloadHandler.text}");
                RestoreFromLogout();
            }
        }
    }

    private void RestoreFromLogout()
    {
        _isLoggingOut = false;
        OnRestore?.Invoke();
    }

    // ──────────────────────────────────────────────
    // Private – Delete Account callbacks
    // ──────────────────────────────────────────────

    private void HandleDeleteSuccess()
    {
        _isDeleting = false;
        OnRestore?.Invoke();
        HandleAuthComplete(raiseLogout: false);
    }

    private void HandleDeleteFail(string error)
    {
        _isDeleting = false;
        OnRestore?.Invoke();
        Debug.LogError("[AuthHandler] Delete account failed: " + error);
    }

    // ──────────────────────────────────────────────
    // Private – Shared completion
    // ──────────────────────────────────────────────

    private void HandleAuthComplete(bool raiseLogout)
    {
        _isLoaded = true;
        TokenStore.Clear();

        if (raiseLogout)
            EventHub.RaisePlayerLogout();
        else
            EventHub.RaisePlayerDeleteAccount();

        InputBlocker.SetBlocked(false);
        OnAuthComplete?.Invoke();
        LoadingTransition.Load_Scene(_defaultLoadScene, false);
    }
}