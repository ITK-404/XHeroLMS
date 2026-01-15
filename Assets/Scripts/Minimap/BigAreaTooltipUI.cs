using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BigAreaTooltipUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI informationTmp;

    [SerializeField] private Button findPathBtn;
    [SerializeField] private Button closeBgBtn; // using for hide self tooltip

    [SerializeField] private GameObject container;

    [SerializeField] private Image areaIconImg;

    private void Awake()
    {
        closeBgBtn.onClick.AddListener(Hide);
        Hide();
    }

    private void OnDestroy()
    {
        closeBgBtn.onClick.RemoveListener(Hide);
    }

    public void Show()
    {
        container.gameObject.SetActive(true);
    }

    public void Hide()
    {
        container.gameObject.SetActive(false);
    }
    
    public void ShowTooltip(BigArea bigaArea)
    {
        informationTmp.text = "";
        areaIconImg.sprite = bigaArea.Data.displayIcon;
    }
}