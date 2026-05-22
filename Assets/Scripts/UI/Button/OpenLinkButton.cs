using UnityEngine;
using UnityEngine.UI;

public class OpenLinkButton : MonoBehaviour
{
    public string openUrl;

    private Button btn;

    private void Awake()
    {
        btn = GetComponent<Button>();
        
        btn.onClick.AddListener(OnClickBtn);
    }

    private void OnDestroy()
    {
        btn.onClick.RemoveListener(OnClickBtn);
    }


    private void OnClickBtn()
    {
        if (string.IsNullOrEmpty(openUrl))
        {
            return;
        }
        Application.OpenURL(openUrl);
    }
}