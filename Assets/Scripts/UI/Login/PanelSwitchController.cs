using UnityEngine;
using UnityEngine.UI;

public class PanelSwitchController : MonoBehaviour
{
    [Header("Buttons")]
    public Button btnForgot;
    public Button btnLoginQR;

    [Header("Panels")]
    public GameObject forgotPanel;
    public GameObject currentPanel;
    public GameObject loginQrPanel;

    [Header("QR UI")]
    public LmsQrAuthUI loginQrUI;   // <-- kéo thả LmsQrAuthUI vào đây trong Inspector
    public LmsDeepLinkAuthUI deepLinkAuthUI;

    private void Start()
    {
        if (btnForgot != null)
            btnForgot.onClick.AddListener(ShowForgot);

        if (btnLoginQR != null)
            btnLoginQR.onClick.AddListener(OnClickLoginViaApp);
    }

    private void OnDestroy()
    {
        if (btnForgot != null)
            btnForgot.onClick.RemoveListener(ShowForgot);

        if (btnLoginQR != null)
            btnLoginQR.onClick.RemoveListener(OnClickLoginViaApp);
    }

    private void ShowForgot()
    {
        if (currentPanel != null) currentPanel.SetActive(false);
        if (loginQrPanel != null) loginQrPanel.SetActive(false);
        if (forgotPanel != null) forgotPanel.SetActive(true);
    }

    private void ShowRegister()
    {
        if (currentPanel != null) currentPanel.SetActive(false);
        if (forgotPanel != null) forgotPanel.SetActive(false);
        if (loginQrPanel != null) loginQrPanel.SetActive(false);
    }

    private void ShowLoginQr()
    {
        if (currentPanel != null) currentPanel.SetActive(false);
        if (forgotPanel != null) forgotPanel.SetActive(false);

        if (loginQrPanel != null)
            loginQrPanel.SetActive(true);

        LoadingUI.Show();

        // Gọi flow QR khi user mở panel
        if (loginQrUI != null)
        {
            loginQrUI.StartQrLogin();
        }
        else
        {
            Debug.LogWarning("[PanelSwitchController] loginQrUI chưa được gán trong Inspector.");
        }
    }
    private void OnClickLoginViaApp()
    {
        LoadingUI.Show();

        if (deepLinkAuthUI != null)
        {
            deepLinkAuthUI.StartDeepLinkLogin();
        }
        else
        {
            LoadingUI.Hide();
            Debug.LogWarning("[PanelSwitchController] deepLinkAuthUI chưa được gán trong Inspector.");
            LoginController.ShowWarning("Thiếu cấu hình đăng nhập qua App (deepLinkAuthUI).");
        }
    }
}
