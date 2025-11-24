using TMPro;
using UnityEngine.UI;

public class LoginPopupUI : PopupBaseUI
{
    public TextMeshProUGUI textHeader;
    public TextMeshProUGUI textDescription;
    public Button returnBtn;
    public void SetHeader(string header)
    {
        textHeader.text = header;
    }

    public void SetTextDescription(string description)
    {
        textDescription.text = description;
    }
}