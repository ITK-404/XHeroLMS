using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BigAreaTooltipUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI informationTmp;

    [SerializeField] private Button findPathBtn;
    [SerializeField] private Button closeBgBtn; // using for hide self tooltip

    [SerializeField] private GameObject container;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image areaIconImg;
    public Action OnClickFindPathAction;
    private void Awake()
    {
        closeBgBtn.onClick.AddListener(Hide);
        findPathBtn.onClick.AddListener(ClickFindPath);
        Hide();
    }

    private void OnDestroy()
    {
        findPathBtn.onClick.RemoveListener(ClickFindPath);
        closeBgBtn.onClick.RemoveListener(Hide);
    }

    public void Show()
    {
        canvasGroup.DOFade(0, 0);
        canvasGroup.DOFade(1, 0.2f);
        container.gameObject.SetActive(true);
    }


    public void ClickFindPath()
    {
        OnClickFindPathAction?.Invoke();
        Hide();
    }
    public void Hide()
    {
        container.gameObject.SetActive(false);
    }

    public void ShowTooltip(BigArea bigaArea)
    {
        informationTmp.text =
            "Nhân tướng học là nơi học tập và tìm hiểu các kiến thức cơ bản về nhân tướng theo phong thủy một cách trực quan. ";
        areaIconImg.sprite = bigaArea.Data.displayIcon;
    }
}