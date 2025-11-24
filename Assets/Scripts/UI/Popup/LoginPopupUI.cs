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

        if (returnBtn != null)
        {
            returnBtn.onClick.RemoveAllListeners();

            if (onReturn != null)
                returnBtn.onClick.AddListener(onReturn);

            returnBtn.onClick.AddListener(() =>
            {
                Destroy(gameObject);
            });
        }
    }
}
