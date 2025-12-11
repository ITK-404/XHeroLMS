using System;
using UnityEngine;
using UnityEngine.UI;

public class TryLogoutButton : MonoBehaviour
{
    public static event Action OnTryLogout;
    private Button btn;

    private void Awake()
    {
        btn = GetComponent<Button>();
        btn.onClick.AddListener(TryLogout);
    }

    private void OnDestroy()
    {
        btn.onClick.RemoveListener(TryLogout);
    }
    private void TryLogout()
    {
        OnTryLogout?.Invoke();
    }
 
}
