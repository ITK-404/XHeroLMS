using UnityEngine;
using UnityEngine.UI;

public class PanelSwitchController : MonoBehaviour
{
    [Header("Buttons")]
    public Button btnForgot;
    public Button btnRegister;

    [Header("Panels")]
    public GameObject forgotPanel;
    public GameObject registerPanel;
    public GameObject currentPanel;

    private void Start()
    {
        if (btnForgot != null)
            btnForgot.onClick.AddListener(ShowForgot);

        if (btnRegister != null)
            btnRegister.onClick.AddListener(ShowRegister);
    }

    private void OnDestroy()
    {
        if (btnForgot != null)
            btnForgot.onClick.RemoveListener(ShowForgot);

        if (btnRegister != null)
            btnRegister.onClick.RemoveListener(ShowRegister);
    }

    private void ShowForgot()
    {
        if (currentPanel != null) currentPanel.SetActive(false);
        if (registerPanel != null) registerPanel.SetActive(false);
        if (forgotPanel != null) forgotPanel.SetActive(true);
    }

    private void ShowRegister()
    {
        if (currentPanel != null) currentPanel.SetActive(false);
        if (forgotPanel != null) forgotPanel.SetActive(false);
        if (registerPanel != null) registerPanel.SetActive(true);
    }
}
