using TMPro;
using UnityEngine;

public class CopymarkVideoText : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI copyMarkText;

    private void Start()
    {
        if (TokenStore.IsAuthenticated)
        {
            copyMarkText.text = $"{TokenStore.FullName} - {TokenStore.Username} - {TokenStore.Email}";
        }
    }
}