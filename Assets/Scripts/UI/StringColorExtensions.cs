using UnityEngine;

public static class StringColorExtensions
{
    /// <summary>
    /// Chuyển từ mã hex (VD: "812E11" hoặc "#812E11") sang Color
    /// </summary>
    public static Color ToColor(this string hex)
    {
        if (ColorUtility.TryParseHtmlString(
            hex.StartsWith("#") ? hex : "#" + hex, 
            out Color color))
        {
            return color;
        }

        // fallback nếu parse lỗi
        Debug.LogWarning($"[Color] Parse failed: {hex}");
        return Color.white;
    }
}
