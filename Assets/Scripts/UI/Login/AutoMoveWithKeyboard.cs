using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class AutoMoveWithKeyboard : MonoBehaviour
{
    [Header("Parent chứa UI Input (Panel cần đẩy lên)")]
    public RectTransform targetPanel;

    [Header("Khoảng cách thêm (nếu muốn đẩy cao hơn)")]
    public float extraPadding = 50f;

    private Vector2 originalPos;
    private bool isKeyboardVisible = false;

    void Start()
    {
        if (targetPanel == null)
            targetPanel = GetComponent<RectTransform>();

        originalPos = targetPanel.anchoredPosition;
    }

    void Update()
    {
#if UNITY_ANDROID || UNITY_IOS
        float kbHeight = GetKeyboardHeight();

        bool nowVisible = kbHeight > 10f;

        // Khi bàn phím bật
        if (nowVisible && !isKeyboardVisible)
        {
            MoveUp(kbHeight);
            isKeyboardVisible = true;
        }
        // Khi bàn phím tắt
        else if (!nowVisible && isKeyboardVisible)
        {
            MoveDown();
            isKeyboardVisible = false;
        }
#endif
    }

    float GetKeyboardHeight()
    {
#if UNITY_EDITOR
        return 0f;
#elif UNITY_ANDROID || UNITY_IOS
        return TouchScreenKeyboard.area.height;
#else
        return 0f;
#endif
    }

    void MoveUp(float keyboardHeight)
    {
        // Convert pixel height -> local UI height theo Canvas Scale
        Canvas canvas = targetPanel.GetComponentInParent<Canvas>();
        float scale = canvas ? canvas.scaleFactor : 1f;

        float offset = (keyboardHeight / scale) + extraPadding;

        targetPanel.anchoredPosition = new Vector2(
            originalPos.x,
            originalPos.y + offset
        );
    }

    void MoveDown()
    {
        targetPanel.anchoredPosition = originalPos;
    }
}
