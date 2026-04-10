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
    

    void OnEnable()
    {
        LoginController.OnLoginComplete += HandleLoginComplete;
        var controller = PlayerPanelUI.Instance.controllerUI;
        // gán click handler an toàn
        if (controller != null)
        {
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

        var controller = PlayerPanelUI.Instance.controllerUI;
        // gán click handler an toàn
        if (controller != null)
        {
            controller.OnLoginBtnClicked += OnOpenButtonClicked;
        }
        
        if (buttonClose != null) buttonClose.onClick.RemoveListener(CloseUI);


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
}
