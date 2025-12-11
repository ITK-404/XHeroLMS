using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

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
        SetButtonState(loginButton);
        OpenLoginPanel();
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

    private void SetButtonState(ButtonSpriteState active)
    {
        ResetButtonSprite(loginButton);
        ResetButtonSprite(registerButton);
        ResetButtonSprite(closeButton);

        if (active != null && active.targetImage != null && active.activeSprite != null)
            active.targetImage.sprite = active.activeSprite;
    }

    private void ResetButtonSprite(ButtonSpriteState b)
    {
        if (b != null && b.targetImage != null && b.defaultSprite != null)
            b.targetImage.sprite = b.defaultSprite;
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

        SetButtonState(closeButton);
    }
}
