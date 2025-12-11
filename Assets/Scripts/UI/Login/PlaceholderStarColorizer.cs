using UnityEngine;
using TMPro;

public class PlaceholderStarColorizer : MonoBehaviour
{
    [Header("Parent chứa tất cả placeholder")]
    public Transform parent;

    [Header("Màu của dấu *")]
    public Color starColor = Color.red;

    private void Start()
    {
        ApplyColorToAllPlaceholders();
    }

    public void ApplyColorToAllPlaceholders()
    {
        if (parent == null)
        {
            Debug.LogWarning("Parent is not assigned in PlaceholderStarColorizer");
            return;
        }

        string colorHex = ColorUtility.ToHtmlStringRGB(starColor);

        // Tìm tất cả TMP_Text trong parent
        TMP_Text[] texts = parent.GetComponentsInChildren<TMP_Text>(true);

        foreach (var p in texts)
        {
            if (p == null) continue;

            // Chỉ xử lý object có tên chứa "Placeholder"
            if (!p.gameObject.name.Contains("Placeholder")) continue;

            string original = p.text;

            if (string.IsNullOrEmpty(original) || !original.Contains("*"))
                continue;

            string modified = original.Replace("*", $"<color=#{colorHex}>*</color>");

            p.text = modified;
        }
    }
}
