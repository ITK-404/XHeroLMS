using UnityEngine;
using UnityEngine.UI;

public class UI_PanelSwitcher : MonoBehaviour
{
    [Header("Buttons")]
    public Button btnLogin;
    public Button btnRegister;
    public Button buttonClose;

    [Header("Panels")]
    public GameObject currentPanel;
    public GameObject loginPanel;
    public GameObject registerPanel;
    public Image  targetImage;          // nền mờ (optional)
    public GameObject targetPanel;      // panel đăng nhập
    CursorGameManager cursorMgr;

    private void Start()
    {
        cursorMgr = FindAnyObjectByType<CursorGameManager>();
        // Gắn sự kiện
        if (btnLogin != null)
            btnLogin.onClick.AddListener(OpenLoginPanel);

        if (btnRegister != null)
            btnRegister.onClick.AddListener(OpenRegisterPanel);

        if (buttonClose != null)
        {
            buttonClose.onClick.RemoveListener(CloseUI);
            buttonClose.onClick.AddListener(CloseUI);
        }
    }

    private void OpenLoginPanel()
    {
        if (currentPanel != null)
            currentPanel.SetActive(false);

        if (loginPanel != null)
            loginPanel.SetActive(true);
    }

    private void OpenRegisterPanel()
    {
        if (currentPanel != null)
            currentPanel.SetActive(false);

        if (registerPanel != null)
            registerPanel.SetActive(true);
    }

        public void CloseUI()
    {
        if (targetImage) targetImage.gameObject.SetActive(false);
        if (targetPanel) targetPanel.SetActive(false);
        if (cursorMgr) cursorMgr.SetUIOpen(false);
        InputBlocker.SetBlocked(false);
    }
}
