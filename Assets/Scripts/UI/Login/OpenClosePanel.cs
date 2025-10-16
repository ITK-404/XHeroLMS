using UnityEngine;
using UnityEngine.UI;

public class OpenClosePanel : MonoBehaviour
{
    [Header("UI References")]
    public Button buttonOpen;
    public Button buttonClose;
    public Image targetImage; 
    public GameObject targetPanel;

    CursorGameManager cursorMgr;

    private void Start()
    {
        cursorMgr = FindAnyObjectByType<CursorGameManager>();

        if (buttonOpen != null)
            buttonOpen.onClick.AddListener(OpenUI);

        if (buttonClose != null)
            buttonClose.onClick.AddListener(CloseUI);

        // Khởi đầu: tắt hết nếu muốn
        if (targetImage != null) targetImage.gameObject.SetActive(false);
        if (targetPanel != null) targetPanel.SetActive(false);
    }

    void OpenUI()
    {
        if (targetImage != null) targetImage.gameObject.SetActive(true);
        if (targetPanel != null) targetPanel.SetActive(true);
        if (cursorMgr) cursorMgr.SetUIOpen(true);
        InputBlocker.SetBlocked(true);
    }

    public void CloseUI()
    {
        if (targetImage != null) targetImage.gameObject.SetActive(false);
        if (targetPanel != null) targetPanel.SetActive(false);
        if (cursorMgr) cursorMgr.SetUIOpen(false);
        InputBlocker.SetBlocked(false);
    }
}
