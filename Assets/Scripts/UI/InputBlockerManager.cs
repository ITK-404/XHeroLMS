using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class InputBlockerManager : MonoBehaviour
{
    private static InputBlockerManager _instance;

    void Awake()
    {
        if (_instance != null)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        // Nếu không block thì thoát sớm
        if (!InputBlocker.IsBlocked()) return;

        // Cho phép gõ nếu con trỏ hiện đang focus vào TMP_InputField hoặc InputField
        if (EventSystem.current != null)
        {
            GameObject selected = EventSystem.current.currentSelectedGameObject;

            if (selected != null)
            {
                // Nếu đối tượng đang được chọn là InputField hoặc TMP_InputField => cho phép input
                if (selected.GetComponent<TMP_InputField>() != null ||
                    selected.GetComponent<UnityEngine.UI.InputField>() != null)
                {
                    return; // Cho phép gõ text, KHÔNG chặn input
                }
            }
        }

        // Còn lại thì chặn toàn bộ input
        BlockKeyboardAndMouse();
    }

    private void BlockKeyboardAndMouse()
    {
        // Khóa toàn bộ phím
        foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
        {
            if (Input.GetKey(key) || Input.GetKeyDown(key) || Input.GetKeyUp(key))
            {
                // Ăn phím - không cho các script khác đọc được
                Debug.Log($"[InputBlocker] Đã chặn phím: {key}");
            }
        }

        // Khóa chuột
        if (Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2))
        {
            Debug.Log("[InputBlocker] Chuột bị chặn");
        }

        // Hủy event UI khác (nếu có)
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }
}
