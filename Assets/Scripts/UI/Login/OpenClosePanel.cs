using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;


#if ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
#endif

public class OpenClosePanel : MonoBehaviour
{
    [Header("UI References")]

    public Button buttonClose;
    public Image  targetImage;          // nền mờ (optional)
    public GameObject targetPanel;      // panel đăng nhập
    public GameObject[] showWhenLoggedIn;
    
    public GameObject[] showWhenLoggedOut;

    [Header("Logout behavior")]
    public bool reloadSceneOnLogout = true;
    public string sceneNameAfterLogout = "NewScene";
    public float loadDelay = 0f;
    CursorGameManager cursorMgr;
    static bool triedRestoreSession;
    bool hasVisualState;
    bool lastLoggedIn;
    bool inputBlockedByThisPanel;
    

    void OnEnable()
    {
        LoginController.OnLoginComplete -= HandleLoginComplete;
        LoginController.OnLoginComplete += HandleLoginComplete;
        var controller = GetPlayerController();
        // gán click handler an toàn
        if (controller != null)
        {
            controller.OnLoginBtnClicked -= OnOpenButtonClicked;
            controller.OnLoginBtnClicked += OnOpenButtonClicked;
        }
        if (buttonClose != null)
        {
            buttonClose.onClick.RemoveListener(CloseUI);
            buttonClose.onClick.AddListener(CloseUI);
        }

        UpdateVisualState();
    }
    void OnDisable()
    {
        LoginController.OnLoginComplete -= HandleLoginComplete;

        var controller = GetPlayerController();
        // gán click handler an toàn
        if (controller != null)
        {
            controller.OnLoginBtnClicked -= OnOpenButtonClicked;
        }
        
        if (buttonClose != null) buttonClose.onClick.RemoveListener(CloseUI);

        if (inputBlockedByThisPanel)
        {
            InputBlocker.SetBlocked(false);
            inputBlockedByThisPanel = false;
        }

        if (cursorMgr != null)
            cursorMgr.SetUIOpen(false);

    }

    void Start()
    {
        cursorMgr = FindAnyObjectByType<CursorGameManager>();

        // bảo đảm UI khởi tạo đúng
        if (targetImage) targetImage.gameObject.SetActive(false);
        if (targetPanel) targetPanel.SetActive(false);

        UpdateVisualState();
    }

    void LateUpdate()
    {
        UpdateVisualState();
        SyncInputBlockerWithPanelState();
    }

    PlayerControllerUI GetPlayerController()
    {
        return PlayerPanelUI.Instance != null ? PlayerPanelUI.Instance.controllerUI : null;
    }
    
    bool IsLoggedIn()
    {
        TryRestoreSessionOnce();
        return TokenStore.IsAuthenticated && !string.IsNullOrEmpty(TokenStore.AccessToken);
    }

    static void TryRestoreSessionOnce()
    {
        if (triedRestoreSession || TokenStore.IsAuthenticated)
            return;

        triedRestoreSession = true;
        TokenStore.TryRestoreFromDisk();
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
        if (!hasVisualState || lastLoggedIn != loggedIn)
        {
            hasVisualState = true;
            lastLoggedIn = loggedIn;
            ToggleGroup(showWhenLoggedIn, loggedIn);
            ToggleGroup(showWhenLoggedOut, !loggedIn);
        }

        // Nếu đã login mà panel vẫn mở, đóng lại cho chắc
        if (loggedIn && IsPanelOpen())
            CloseUI();
    }
    
    void HandleLoginComplete()
    {
        CloseUI();
        UpdateVisualState();
    }

    void OnOpenButtonClicked()
    {
        if (IsLoggedIn())
        {
            CloseUI();
        }
        else
        {
            OpenUI();
        }
    }

    public void OpenFromExternalLoginButton()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        OnOpenButtonClicked();
    }

    // ====== Core UI open/close ======
    void OpenUI()
    {
        if (IsLoggedIn())
        {
            CloseUI();
            return;
        }

        if (targetImage) targetImage.gameObject.SetActive(true);
        if (targetPanel) targetPanel.SetActive(true);
        if (cursorMgr) cursorMgr.SetUIOpen(true);

        if (!inputBlockedByThisPanel)
        {
            InputBlocker.SetBlocked(true);
            inputBlockedByThisPanel = true;
        }
    }

    bool IsPanelOpen()
    {
        return (targetPanel != null && targetPanel.activeSelf)
               || (targetImage != null && targetImage.gameObject.activeSelf);
    }

    void SyncInputBlockerWithPanelState()
    {
        bool shouldBlock = IsPanelOpen();

        if (shouldBlock)
        {
            if (cursorMgr) cursorMgr.SetUIOpen(true);

            if (!inputBlockedByThisPanel || InputBlocker.GetBlockCount() <= 0)
            {
                InputBlocker.SetBlocked(true);
                inputBlockedByThisPanel = true;
            }
        }
        else if (inputBlockedByThisPanel)
        {
            InputBlocker.SetBlocked(false);
            inputBlockedByThisPanel = false;
            InputBlocker.SuppressGameplayInput();
        }
    }

    public void CloseUI()
    {
        if (targetImage) targetImage.gameObject.SetActive(false);
        if (targetPanel) targetPanel.SetActive(false);
        if (cursorMgr) cursorMgr.SetUIOpen(false);

        if (inputBlockedByThisPanel)
        {
            InputBlocker.SetBlocked(false);
            inputBlockedByThisPanel = false;
        }

        InputBlocker.SuppressGameplayInput();
    }
}
