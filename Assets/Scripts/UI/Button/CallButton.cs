using UnityEngine;
using UnityEngine.UI;

public class CallButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private string phoneNumber = "";

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
        Application.OpenURL("tel://" + cleanNumber);
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