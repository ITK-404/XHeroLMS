using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class OpenClosePanel : MonoBehaviour
{
    [Header("UI References")]
    public Button buttonOpen;
    public Button buttonLogout;

    public Button buttonTryLogout;
    public Button buttonCloseWarning;
    
    public Button buttonClose;
    public Image  targetImage;          // nền mờ (optional)
    public GameObject targetPanel;      // panel đăng nhập

    public GameObject[] showWhenLoggedIn;
    
    public GameObject[] showWhenLoggedOut;

    [Header("Logout behavior")]
    public bool reloadSceneOnLogout = true;
    public string sceneNameAfterLogout = "NewScene";
    public float loadDelay = 0f;
    public Transform warningPopup;
    CursorGameManager cursorMgr;

    void OnEnable()
    {
        LoginController.OnLoginComplete -= HandleLoginComplete;
        LoginController.OnLoginComplete += HandleLoginComplete;

        // gán click handler an toàn
        if (buttonOpen != null)
        {
            buttonOpen.onClick.RemoveListener(OnOpenButtonClicked);
            buttonOpen.onClick.AddListener(OnOpenButtonClicked);
        }
        if (buttonClose != null)
        {
            buttonClose.onClick.RemoveListener(CloseUI);
            buttonClose.onClick.AddListener(CloseUI);
        }
        buttonLogout.onClick.AddListener(DoLogout);

        buttonTryLogout.onClick.AddListener(OnShowWarning);
        buttonCloseWarning.onClick.AddListener(HideWarning);
        
        UpdateVisualState();
    }

    private void OnShowWarning()
    {
        warningPopup.gameObject.SetActive(true);
    }

    private void HideWarning()
    {
        warningPopup.gameObject.SetActive(false);
    }

    void OnDisable()
    {
        LoginController.OnLoginComplete -= HandleLoginComplete;

        if (buttonOpen != null)  buttonOpen.onClick.RemoveListener(OnOpenButtonClicked);
        if (buttonClose != null) buttonClose.onClick.RemoveListener(CloseUI);

        buttonLogout.onClick.RemoveListener(DoLogout);

        buttonTryLogout.onClick.RemoveListener(OnShowWarning);
        buttonCloseWarning.onClick.RemoveListener(HideWarning);

    }

    void Start()
    {
        cursorMgr = FindAnyObjectByType<CursorGameManager>();

        // bảo đảm UI khởi tạo đúng
        if (targetImage) targetImage.gameObject.SetActive(false);
        if (targetPanel) targetPanel.SetActive(false);

        UpdateVisualState();
    }
    
    bool IsLoggedIn()
    {
        return TokenStore.IsAuthenticated && !string.IsNullOrEmpty(TokenStore.AccessToken);
    }


    void ToggleGroup(GameObject[] objs, bool on)
    {
        if (objs == null) return;
        for (int i = 0; i < objs.Length; i++)
            if (objs[i]) objs[i].SetActive(on);
    }

    void UpdateVisualState()
    {
        bool loggedIn = IsLoggedIn();

        // Tự ẩn/hiện các nhóm UI nếu có cấu hình
        ToggleGroup(showWhenLoggedIn, loggedIn);
        ToggleGroup(showWhenLoggedOut, !loggedIn);

        // Nếu đã login mà panel vẫn mở, đóng lại cho chắc
        if (loggedIn && targetPanel && targetPanel.activeSelf)
            CloseUI();
    }
    
    void HandleLoginComplete()
    {
        CloseUI();
        UpdateVisualState();
    }

    void OnOpenButtonClicked()
    {
        if (!IsLoggedIn())
        {
            OpenUI();
        }
    }

    // ====== Core UI open/close ======
    void OpenUI()
    {
        if (targetImage) targetImage.gameObject.SetActive(true);
        if (targetPanel) targetPanel.SetActive(true);
        if (cursorMgr) cursorMgr.SetUIOpen(true);
        InputBlocker.SetBlocked(true);
    }

    public void CloseUI()
    {
        if (targetImage) targetImage.gameObject.SetActive(false);
        if (targetPanel) targetPanel.SetActive(false);
        if (cursorMgr) cursorMgr.SetUIOpen(false);
        InputBlocker.SetBlocked(false);
    }

    // ====== Logout ======
    void DoLogout()
    {
        TokenStore.Clear();
        Debug.Log("[OpenClosePanel] Đã đăng xuất.");
        
        CloseUI();

        UpdateVisualState();

        if (reloadSceneOnLogout && !string.IsNullOrEmpty(sceneNameAfterLogout))
        {
            if (loadDelay <= 0f)
            {
                LoadingTransition.Load(sceneNameAfterLogout);
            }
            else
            {
                StartCoroutine(LoadSceneDelayed());
            }
        }
    }

    System.Collections.IEnumerator LoadSceneDelayed()
    {
        yield return new WaitForSecondsRealtime(loadDelay);
        SceneManager.LoadScene(sceneNameAfterLogout);
        LoadingUI.Show(
                timeoutSeconds: 60f,
                timeoutMessage: "Không thể tải nội dung.\nVui lòng kiểm tra kết nối mạng hoặc thử lại.",
                timeoutHeader:  "Lỗi Mạng"
            );
    }
}
