using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UI_PanelSwitcher : MonoBehaviour
{
    [System.Serializable]
    public class ButtonSpriteState
    {
        public Button button;
        public Image targetImage;
        public Sprite defaultSprite;
        public Sprite activeSprite;
    }

    [Header("Buttons")]
    public ButtonSpriteState loginButton;
    public ButtonSpriteState registerButton;
    public ButtonSpriteState closeButton;

    [Header("Current Panels")]
    public List<GameObject> currentPanels = new List<GameObject>();

    [Header("Target Panels")]
    public GameObject loginPanel;
    public GameObject registerPanel;

    [Header("Close Config")]
    public Image targetImage;
    public GameObject targetPanel;

    private CursorGameManager cursorMgr;

private void Start()
{
    cursorMgr = FindAnyObjectByType<CursorGameManager>();

    if (loginButton.button)
        loginButton.button.onClick.AddListener(OpenLoginPanel);

    if (registerButton.button)
        registerButton.button.onClick.AddListener(OpenRegisterPanel);

    if (closeButton.button)
    {
        closeButton.button.onClick.RemoveListener(CloseUI);
        closeButton.button.onClick.AddListener(CloseUI);
    }

    // Default
    ResetStateToDefault();   
}

    private GameObject GetCurrentActivePanel()
    {
        foreach (var panel in currentPanels)
        {
            if (panel != null && panel.activeSelf)
                return panel;
        }
        return null;
    }

    private void HideCurrentPanel()
    {
        GameObject current = GetCurrentActivePanel();
        if (current != null)
            current.SetActive(false);
    }
    [SerializeField] private Color activeColor;
    [SerializeField] private Color defaultColor;
    [SerializeField] private TMP_FontAsset activeFont;
    [SerializeField] private TMP_FontAsset deActiveFont;

    // ================= RESET STATE =================

private void ResetStateToDefault()
{
    // Reset sprite + text
    ResetButtonSprite(loginButton);
    ResetButtonSprite(registerButton);
    ResetButtonSprite(closeButton);

    // Ẩn tất cả panel đang mở
    foreach (var p in currentPanels)
    {
        if (p != null) p.SetActive(false);
    }

    // Set Login là panel mặc định
    if (loginPanel != null)
        loginPanel.SetActive(true);

    // Set Login là button active
    ApplyActiveState(loginButton);
}

    private void ApplyActiveState(ButtonSpriteState active)
    {
        if (active == null) return;

        if (active.targetImage != null && active.activeSprite != null)
            active.targetImage.sprite = active.activeSprite;

        var tmp = (active.button != null)
            ? active.button.GetComponentInChildren<TextMeshProUGUI>(true)
            : null;

        HandleText(tmp, true);
    }


    private void SetButtonState(ButtonSpriteState active)
    {
        ResetButtonSprite(loginButton);
        ResetButtonSprite(registerButton);
        ResetButtonSprite(closeButton);

        if (active != null && active.targetImage != null && active.activeSprite != null)
        {
            active.targetImage.sprite = active.activeSprite;
            HandleText(active.button.GetComponentInChildren<TextMeshProUGUI>(), true);
        }
    }

    private void ResetButtonSprite(ButtonSpriteState b)
    {
        if (b == null) return;

        if (b.targetImage != null && b.defaultSprite != null)
            b.targetImage.sprite = b.defaultSprite;

        var tmp = (b.button != null)
            ? b.button.GetComponentInChildren<TextMeshProUGUI>(true)
            : null;

        HandleText(tmp, false);
    }

    private void HandleText(TextMeshProUGUI tmpText, bool isEnable)
    {
        tmpText.color = isEnable ? activeColor : defaultColor;
        tmpText.enableVertexGradient = isEnable;
        tmpText.font = isEnable ? activeFont : deActiveFont;
    }

    private void OpenLoginPanel()
    {
        HideCurrentPanel();
        if (loginPanel != null)
            loginPanel.SetActive(true);

        SetButtonState(loginButton);
    }

    private void OpenRegisterPanel()
    {
        HideCurrentPanel();
        if (registerPanel != null)
            registerPanel.SetActive(true);

        SetButtonState(registerButton);
    }

    public void CloseUI()
    {
        if (targetImage) targetImage.gameObject.SetActive(false);
        if (targetPanel) targetPanel.SetActive(false);

        if (cursorMgr)
            cursorMgr.SetUIOpen(false);

        InputBlocker.SetBlocked(false);

        ResetStateToDefault();
    }
}
