using UnityEngine;
using TMPro;

public class TermsLinkOpener : MonoBehaviour
{
    public TMP_Text targetText;
    public string privacyPolicyUrl;
    public string termsOfUseUrl;

    private TMP_TextEventHandler _handler;

    private void Awake()
    {
        if (!targetText) targetText = GetComponent<TMP_Text>();

        _handler = GetComponent<TMP_TextEventHandler>();
        if (!_handler) _handler = gameObject.AddComponent<TMP_TextEventHandler>();

        _handler.onLinkSelection.AddListener(OnLinkClicked);
    }

    private void OnDestroy()
    {
        if (_handler) _handler.onLinkSelection.RemoveListener(OnLinkClicked);
    }

    private void OnLinkClicked(string linkId, string linkText, int linkIndex)
    {
        Debug.Log($"Clicked link: id={linkId}, text={linkText}, index={linkIndex}");

        if (linkId == "privacy" && !string.IsNullOrEmpty(privacyPolicyUrl))
            Application.OpenURL(privacyPolicyUrl);

        if (linkId == "terms" && !string.IsNullOrEmpty(termsOfUseUrl))
            Application.OpenURL(termsOfUseUrl);
    }
}
