using UnityEngine;
using UnityEngine.UI;

public class InDevelopmentButton : MonoBehaviour
{
    [SerializeField] private Button btn;

    private void Awake()
    {
        btn = GetComponent<Button>();
        if(btn)
            btn.onClick.AddListener(OnShowPopup);
    }

    private void OnDestroy()
    {
        if(btn)
            btn.onClick.RemoveListener(OnShowPopup);
    }

    private void OnShowPopup()
    {
        string message = "Tính năng hiện còn đang trong quá trình phát triển";
        string header = "Thông báo";
        LoadingUI.ShowErrorPopup(message,header);
    }
}