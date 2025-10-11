using UnityEngine;

[DisallowMultipleComponent]
public class CursorGameManager : MonoBehaviour
{
    [Header("Camera Look Scripts")]
    [Tooltip("Kéo thả các component điều khiển look/camera cần tắt khi hiện UI (vd: FPSLookController, Cinemachine POV, v.v.)")]
    public Behaviour[] lookScriptsToDisable;

    [Header("Cursor Behavior")]
    [Tooltip("Khóa chuột khi bắt đầu vào game")]
    public bool lockCursorOnStart = true;

    [Tooltip("Giữ Alt để tạm hiện chuột khi đang chơi (UI đang đóng)")]
    public bool holdAltToShowCursor = true;

    [Tooltip("Cần khóa chuột ngay khi UI đóng lại")]
    public bool relockCursorWhenUICloses = true;

    bool uiOpen = false;         // do UIPanelController gọi
    bool altHeldLastFrame = false;

    void Start()
    {
        if (lockCursorOnStart && !uiOpen)
            LockCursor();
        else
            UnlockCursor();
    }

    void Update()
    {
        // Nếu UI đang mở -> luôn hiện chuột, đảm bảo look bị tắt
        if (uiOpen)
        {
            EnsureCursorUnlocked();
            SetLookEnabled(false);
            return;
        }

        // UI đang ĐÓNG: cho phép giữ Alt để tạm hiện chuột
        bool altHeld = holdAltToShowCursor && (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt));

        if (altHeld)
        {
            // Tạm mở chuột khi giữ Alt
            EnsureCursorUnlocked();
            SetLookEnabled(false);
        }
        else
        {
            // Trả về trạng thái gameplay: khóa chuột + bật look
            EnsureCursorLocked();
            SetLookEnabled(true);
        }

        altHeldLastFrame = altHeld;
    }

    // --- API được UIPanelController gọi ---
    public void SetUIOpen(bool open)
    {
        uiOpen = open;

        if (uiOpen)
        {
            UnlockCursor();
            SetLookEnabled(false);
        }
        else
        {
            // Khi UI tắt, nếu không giữ Alt thì khóa chuột lại (nếu tùy chọn bật)
            if (relockCursorWhenUICloses && !(Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt)))
            {
                LockCursor();
                SetLookEnabled(true);
            }
        }
    }

    void SetLookEnabled(bool enabled)
    {
        if (lookScriptsToDisable == null) return;
        foreach (var b in lookScriptsToDisable)
        {
            if (b == null) continue;
            if (b.enabled != enabled) b.enabled = enabled;
        }
    }

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void EnsureCursorLocked()
    {
        if (Cursor.lockState != CursorLockMode.Locked || Cursor.visible)
            LockCursor();
    }

    void EnsureCursorUnlocked()
    {
        if (Cursor.lockState != CursorLockMode.None || !Cursor.visible)
            UnlockCursor();
    }
}
