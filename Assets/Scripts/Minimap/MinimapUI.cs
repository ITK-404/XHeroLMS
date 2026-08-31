using System;
using UnityEngine;
using UnityEngine.UI;

public class MinimapUI : MonoBehaviour
{
    [Header("Minimap UI")]
    [SerializeField] private GameObject container;
    [SerializeField] private GameObject maskView;
    [SerializeField] private Button turnOnBtn;
    [SerializeField] private Button turnOffBtn;
    [SerializeField] private Button showAreaList;

    public Action TurnOnMinimapAction;
    public Action TurnOffMinimapAction;

    private void Awake()
    {
        Hide();
    }
    
    private void Start()
    {
        ShowBottomViewUI();
        
        turnOffBtn.onClick.AddListener(ClickTurnOffMinimap);
        turnOnBtn.onClick.AddListener(ClickTurnOnMinimap);
    }

    private void OnDestroy()
    {
        turnOffBtn.onClick.RemoveListener(ClickTurnOffMinimap);
        turnOnBtn.onClick.RemoveListener(ClickTurnOnMinimap);    
    }

    private void ClickTurnOnMinimap() => TurnOnMinimapAction?.Invoke();
    private void ClickTurnOffMinimap() => TurnOffMinimapAction?.Invoke();

    public void SetTurnOnInteractable(bool interactable)
    {
        if (turnOnBtn != null)
            turnOnBtn.interactable = interactable;
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
