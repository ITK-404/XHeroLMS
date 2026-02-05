using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UpdatePopupUI : PopupBaseUI
{
    [Header("Texts")]
    public TextMeshProUGUI textDescription;

    [Header("Buttons")]
    public Button returnBtn;

    public void SetTextDescription(string description)
    {
        if (textDescription == null) return;
        textDescription.text = description ?? "";
    }

    public void Init(string description, UnityAction onReturn = null)
    {
        SetTextDescription(description);
        BindReturn(onReturn);
    }

    private void BindReturn(UnityAction onReturn)
    {
        if (returnBtn == null) return;

        returnBtn.onClick.RemoveAllListeners();

        if (onReturn != null)
            returnBtn.onClick.AddListener(onReturn);

        returnBtn.onClick.AddListener(() => Destroy(gameObject));
    }
}
