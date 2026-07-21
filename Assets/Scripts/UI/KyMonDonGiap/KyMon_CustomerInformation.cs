using TMPro;
using UnityEngine;

public class KyMon_CustomerInformation : MonoBehaviour
{
    [SerializeField] private TMP_InputField userNameInputField;
    [SerializeField] private TMP_InputField phoneInputField;

    private void Start()
    {
        if (!TokenStore.IsAuthenticated)
        {
            Debug.LogError("[KyMon_CustomerInformation] User is not login, please check again");
        }
        FillData(TokenStore.FullName, TokenStore.Username);
    }
    
    public string GetUserName()
    {
        return userNameInputField.text;
    }

    public string GetPhoneNumber()
    {
        return phoneInputField.text;
    }
    
    private void FillData(string userName, string phoneNumber)
    {
        Debug.Log($"UserName {userName} PhoneNumber {phoneNumber}");
        
        userNameInputField.text = userName;
        phoneInputField.text = phoneNumber;
    }
}