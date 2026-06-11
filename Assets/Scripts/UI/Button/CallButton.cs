using UnityEngine;
using UnityEngine.UI;

public class CallButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private string phoneNumber = "";

    private const string ErrorPopup = "Thiết bị có thể chưa được cài đặt thiết bị gọi mặt định";
    
    void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        button.onClick.AddListener(MakeCall);
    }

    public void MakeCall()
    {
        string cleanNumber = CleanPhoneNumber(phoneNumber);

        if (string.IsNullOrEmpty(cleanNumber))
        {
            Debug.LogWarning("Số điện thoại không hợp lệ!");
            return;
        }

#if UNITY_IOS
        string url = "tel://" + cleanNumber;
        if (IOSUrlChecker.CanOpen(url))
        {
            LoadingUI.Show(0f, ErrorPopup);
            return;
        }
        Application.OpenURL(url);
#elif UNITY_ANDROID
        Application.OpenURL("tel:" + cleanNumber);
#else
        Debug.LogWarning("Gọi điện không hỗ trợ trên nền tảng này: " + Application.platform);
#endif

        Debug.Log($"Đang gọi tới: {cleanNumber}");
    }

    private string CleanPhoneNumber(string number)
    {
        return number
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("(", "")
            .Replace(")", "")
            .Replace("+", "");
    }
}