using MacacaGames;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public static class InputBlocker
{
    private static bool _blocked = false;
    private static int blockCount = 0;
    private static int suppressInputFrame = -1;
    private static float suppressInputUntilTime = -1f;
    private static bool suppressUntilPointerReleased = false;
    
    /// <summary>
    /// Bật hoặc tắt khóa bàn phím/chuột.
    /// </summary>
    public static void SetBlocked(bool value)
    {
        if (value)
        {
            blockCount++;
        }
        else
        {
            blockCount = Mathf.Max(0, blockCount - 1);
        }

        UpdateInputState();
        Debug.Log($"[InputBlocker] {(value ? "Input locked" : "Input unlocked")}");
    }

    private static void UpdateInputState()
    {
        bool shouldBlock = blockCount > 0;
        _blocked = shouldBlock;
    }
    /// <summary>
    /// Trả về trạng thái đang khóa input không.
    /// </summary>
    public static bool IsBlocked()
    {
        return _blocked || IsInputSuppressed();
    }

    public static int GetBlockCount()
    {
        return blockCount;
    }

    public static void ClearBlock()
    {
        blockCount = 0;
        UpdateInputState();
    }

    public static void SuppressGameplayInput(float seconds = 0.08f)
    {
        suppressInputFrame = Mathf.Max(suppressInputFrame, Time.frameCount);
        suppressInputUntilTime = Mathf.Max(suppressInputUntilTime, Time.unscaledTime + Mathf.Max(0f, seconds));

        if (IsPointerActive())
            suppressUntilPointerReleased = true;
    }

    private static bool IsInputSuppressed()
    {
        if (Time.frameCount <= suppressInputFrame)
            return true;

        if (suppressUntilPointerReleased)
        {
            if (IsPointerActive())
                return true;

            suppressUntilPointerReleased = false;
        }

        return Time.unscaledTime < suppressInputUntilTime && IsPointerActive();
    }

    private static bool IsPointerActive()
    {
        if (UnityEngine.Input.touchCount > 0)
            return true;

        return UnityEngine.Input.GetMouseButton(0)
               || UnityEngine.Input.GetMouseButtonDown(0)
               || UnityEngine.Input.GetMouseButtonUp(0);
    }
}
public class InputManager : Singleton<InputManager>
{
    public InputHandler InputHandler;

    public InputManager()
    {
        InputHandler = new InputHandler();
    }
 
}
public class BaseInput : MonoBehaviour
{
    public InputHandler InputHandler;
    public bool IsClicked = false;
    public Vector2 MoveVector;

    private void Awake()
    {
        InputHandler = InputManager.Instance.InputHandler;
        InputHandler.Enable();
        InputHandler.Player.Enable();
    }
}

public class MobileInput : MonoBehaviour
{
    
}
