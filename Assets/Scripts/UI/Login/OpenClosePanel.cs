using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class OpenClosePanel : MonoBehaviour
{
    [Header("UI References")]
    public Button buttonOpen;
    public Button buttonClose;
    public Image  targetImage;          // nền mờ (optional)
    public GameObject targetPanel;      // panel đăng nhập

    [Header("Texts")]
    public TextMeshProUGUI openButtonText;
    public string labelLogin  = "ĐĂNG NHẬP";
    public string labelLogout = "ĐĂNG XUẤT";

    public GameObject[] showWhenLoggedIn;
    
    public GameObject[] showWhenLoggedOut;

    [Header("Logout behavior")]
    public bool reloadSceneOnLogout = true;
    public string sceneNameAfterLogout = "NewScene";
    public float loadDelay = 0f;

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

        UpdateVisualState();
    }

    void OnDisable()
    {
        LoginController.OnLoginComplete -= HandleLoginComplete;

        if (buttonOpen != null)  buttonOpen.onClick.RemoveListener(OnOpenButtonClicked);
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

    void UpdateOpenButtonLabel()
    {
        if (!openButtonText) return;
        openButtonText.text = IsLoggedIn() ? labelLogout : labelLogin;
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
        UpdateOpenButtonLabel();

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
        if (IsLoggedIn())
        {
            DoLogout();
        }
        else
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
        LoadingUI.Show();
    }
}
