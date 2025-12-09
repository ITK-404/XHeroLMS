using MacacaGames;
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Windows;

public static class InputBlocker
{
    private static bool _blocked = false;

    /// <summary>
    /// Bật hoặc tắt khóa bàn phím/chuột.
    /// </summary>
    public static void SetBlocked(bool value)
    {
        _blocked = value;
        Debug.Log($"[InputBlocker] {(value ? "Input locked" : "Input unlocked")}");
    }

    /// <summary>
    /// Trả về trạng thái đang khóa input không.
    /// </summary>
    public static bool IsBlocked()
    {
        return _blocked;
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