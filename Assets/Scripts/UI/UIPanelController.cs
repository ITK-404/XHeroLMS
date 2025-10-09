using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class UIPanelController : MonoBehaviour
{
    [Header("Refs")]
    [Tooltip("Nút bấm để hiện/tắt Panel")]
    public Button toggleButton;
    [Tooltip("Root của Panel (GameObject)")]
    public GameObject panelRoot;

    [Header("Behavior")]
    [Tooltip("Bật tắt bằng phím (mặc định Esc)")]
    public KeyCode toggleKey = KeyCode.Escape;
    [Tooltip("Panel bật ngay khi Start")]
    public bool startVisible = false;

    [Header("Events")]
    public UnityEvent<bool> onPanelVisibilityChanged; // true = mở, false = tắt

    bool isOpen;
    CursorGameManager cursorMgr;

    void Awake()
    {
        if (panelRoot == null)
        {
            Debug.LogError("[UIPanelController] panelRoot chưa gán!");
        }
        cursorMgr = FindAnyObjectByType<CursorGameManager>();
    }

    void Start()
    {
        SetOpen(startVisible, invokeEvent:false);

        if (toggleButton != null)
            toggleButton.onClick.AddListener(Toggle);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            Toggle();
    }

    public void Toggle()
    {
        SetOpen(!isOpen, invokeEvent:true);
    }

    public void Open()
    {
        SetOpen(true, invokeEvent:true);
    }

    public void Close()
    {
        SetOpen(false, invokeEvent:true);
    }

    void SetOpen(bool open, bool invokeEvent)
    {
        isOpen = open;
        if (panelRoot) panelRoot.SetActive(isOpen);

        // Thông báo cho CursorGameManager để xử lý chuột & camera
        if (cursorMgr) cursorMgr.SetUIOpen(isOpen);

        if (invokeEvent)
            onPanelVisibilityChanged?.Invoke(isOpen);
    }

    // Cho script khác kiểm tra trạng thái
    public bool IsOpen() => isOpen;
}
