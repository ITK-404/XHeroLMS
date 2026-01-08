using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class MinimapUI : MonoBehaviour
{
    [Header("Minimap UI")]
    [SerializeField] private GameObject container;
    [SerializeField] private GameObject maskView;
    public Button turnOnBtn;
    public Button turnOffBtn;
    public Button showAreaList;
    private void Start()
    {
        // if (TokenStore.IsAuthenticated)
        // {
        //     Show();
        // }
        // else
        // {
        //     LoginController.OnLoginComplete += Show;
        // }
        Show();
        ShowBottomViewUI();
    }

    private void OnDestroy()
    {
        LoginController.OnLoginComplete -= Show;
    }

    public void Show()
    {
        container.gameObject.SetActive(true);
    }

    public void Hide()
    {
        container.gameObject.SetActive(false);
    }

    public void ShowBottomViewUI()
    {
        turnOnBtn.gameObject.SetActive(true);
        turnOffBtn.gameObject.SetActive(false);
        maskView.gameObject.SetActive(true);
    }

    public void ShowTopViewUI()
    {
        turnOnBtn.gameObject.SetActive(false);
        turnOffBtn.gameObject.SetActive(true);
        maskView.gameObject.SetActive(false);
    }
    
}