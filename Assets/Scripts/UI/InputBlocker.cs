using UnityEngine;

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
