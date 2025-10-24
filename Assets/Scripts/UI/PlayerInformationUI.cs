using TMPro;
using UnityEngine;

public class PlayerInformationUI : MonoBehaviour
{
    public TextMeshProUGUI playerName;

    private void Awake()
    {
        LoginController.OnLoginComplete += FillData;
    }

    private void OnDestroy()
    {
        LoginController.OnLoginComplete -= FillData;
    }

    public void FillData()
    {
        // playerName.text = TokenStore.FullName;
        playerName.text = string.IsNullOrEmpty(TokenStore.FullName) ? "(no name)" : TokenStore.FullName;
    }
}