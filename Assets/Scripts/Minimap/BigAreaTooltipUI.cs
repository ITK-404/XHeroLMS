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
        findPathBtn.onClick.AddListener(Hide);
        Hide();
    }

    private void OnDestroy()
    {
        findPathBtn.onClick.RemoveListener(Hide);
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
        informationTmp.text = "Nhân tướng học là nơi học tập và tìm hiểu các kiến thức cơ bản về nhân tướng theo phong thủy một cách trực quan. ";
        areaIconImg.sprite = bigaArea.Data.displayIcon;
    }
}

public class ReviewBigAreaUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI reviewInformationTmp;
    [SerializeField] private 
}