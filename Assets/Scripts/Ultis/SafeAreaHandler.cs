using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Attach this script to a top-level UI Panel (RectTransform) inside your Canvas.
/// It will automatically adjust the panel to fit within the device's safe area.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SafeAreaHandler : MonoBehaviour
{
    private RectTransform panel;
    private Rect lastSafeArea = new Rect(0, 0, 0, 0);
    private Vector2Int lastScreenSize = new Vector2Int(0, 0);
    private ScreenOrientation lastOrientation = ScreenOrientation.AutoRotation;

    void Awake()
    {
        panel = GetComponent<RectTransform>();
        ApplySafeArea();
    }

    void Update()
    {
        // Re-apply if screen size, orientation, or safe area changes
        if (Screen.safeArea != lastSafeArea ||
            Screen.width != lastScreenSize.x ||
            Screen.height != lastScreenSize.y ||
            Screen.orientation != lastOrientation)
        {
            ApplySafeArea();
        }
    }

    void ApplySafeArea()
    {
        Rect safeArea = Screen.safeArea;

        // Save state
        lastSafeArea = safeArea;
        lastScreenSize = new Vector2Int(Screen.width, Screen.height);
        lastOrientation = Screen.orientation;

        // Convert safe area rectangle from absolute pixels to normalized anchor coordinates
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        panel.anchorMin = anchorMin;
        panel.anchorMax = anchorMax;

        Debug.Log($"[SafeAreaHandler] Applied safe area: {safeArea}");
    }
}
