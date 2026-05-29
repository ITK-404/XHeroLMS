using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MonoBehaviour quản lý toàn bộ UI liên quan đến logout và xoá tài khoản.
/// Gắn vào cùng GameObject với PlayerPanelUI.
/// Kết nối với AuthHandler thông qua Bind().
/// </summary>
public class AuthView : MonoBehaviour
{
    // ──────────────────────────────────────────────
    // Inspector fields
    // ──────────────────────────────────────────────
    [Header("Popups")]
    public LogoutPopupUI      logoutPopupUI;
    public DeleteAccountPopup deleteAccountPopup;

    [Header("Select Function Group")]

    [Header("Config")]
    public bool disableButtonsWhileInProgress = true;

    // ──────────────────────────────────────────────
    // Private
    // ──────────────────────────────────────────────
    private AuthHandler _authHandler;

    // ──────────────────────────────────────────────
    // Unity lifecycle
    // ──────────────────────────────────────────────
    private void Awake()
    {
        logoutPopupUI.gameObject.SetActive(false);

        // Lắng nghe events từ các UI component
        LogoutPopupUI.OnReturn              += OnReturn;
        LogoutPopupUI.OnLogout              += OnLogout;
        TryLogoutButton.OnTryLogout         += OnTryLogout;
        DeleteAccountPopup.OnDeleteAccountAction = OnDeleteAccount;
    }

    private void OnDestroy()
    {
        LogoutPopupUI.OnReturn              -= OnReturn;
        LogoutPopupUI.OnLogout              -= OnLogout;
        TryLogoutButton.OnTryLogout         -= OnTryLogout;
    }

    // ──────────────────────────────────────────────
    // Public API
    // ──────────────────────────────────────────────

    /// <summary>
    /// Gọi từ PlayerPanelUI sau khi AuthHandler được tạo.
    /// Kết nối AuthHandler với AuthView thông qua callbacks.
    /// </summary>
    public void Bind(AuthHandler authHandler)
    {
        _authHandler = authHandler;

        _authHandler.OnBegin       = () => SetInteractable(false);
        _authHandler.OnRestore     = () =>
        {
            SetInteractable(true);
            InputBlocker.SetBlocked(false);
        };
        _authHandler.OnAuthComplete = HideAll;
    }

    // ──────────────────────────────────────────────
    // UI state
    // ──────────────────────────────────────────────

    public void SetInteractable(bool interactable)
    {
        if (!disableButtonsWhileInProgress) return;
        logoutPopupUI?.SetInteractable(interactable);
    }

    public void HideAll()
    {
        // GetComponent<UIView>().Hide();
        logoutPopupUI?.Hide();
    }

    // ──────────────────────────────────────────────
    // Event handlers – UI → AuthHandler
    // ──────────────────────────────────────────────

    private void OnLogout()        => _authHandler?.Logout();

    private void OnDeleteAccount() => _authHandler?.DeleteAccount();

    private void OnReturn()        => logoutPopupUI?.Hide();

    private void OnTryLogout()
    {
    }
}