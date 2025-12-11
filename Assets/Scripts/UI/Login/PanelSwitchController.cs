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

    private void Start()
    {
        if (btnForgot != null)
            btnForgot.onClick.AddListener(ShowForgot);

        if (btnLoginQR != null)
            btnLoginQR.onClick.AddListener(ShowLoginQr);
    }

    private void OnDestroy()
    {
        if (btnForgot != null)
            btnForgot.onClick.RemoveListener(ShowForgot);

        if (btnLoginQR != null)
            btnLoginQR.onClick.RemoveListener(ShowLoginQr);
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
}
