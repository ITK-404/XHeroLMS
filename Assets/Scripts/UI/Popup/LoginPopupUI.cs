using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

public class LoginPopupUI : PopupBaseUI
{
    public TextMeshProUGUI textHeader;
    public TextMeshProUGUI textDescription;
    public Button returnBtn;

    public void SetHeader(string header)
    {
        if (textHeader != null)
            textHeader.text = header;
    }

    public void SetTextDescription(string description)
    {
        if (textDescription != null)
            textDescription.text = description;
    }

    /// <summary>
    /// Hàm khởi tạo đầy đủ cho popup.
    /// </summary>
    public void Init(string header, string description, UnityAction onReturn = null)
    {
        SetHeader(header);
        SetTextDescription(description);

        if (returnBtn == null) return;

        returnBtn.onClick.RemoveAllListeners();

        // Callback bên ngoài (hủy API, stop coroutine, v.v.)
        if (onReturn != null)
            returnBtn.onClick.AddListener(onReturn);

        // Mặc định: chỉ tự destroy chính nó
        returnBtn.onClick.AddListener(() =>
        {
            Destroy(gameObject);
        });
    }
}
