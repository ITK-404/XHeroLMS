using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MailElementVisualUI : MonoBehaviour
{
    [Header("Settings")]
    // trạng thái đọc hay chưa đọc của thư (sprite trắng đen)
    [SerializeField] private MailTextConfig currentConfig;
    [SerializeField] private bool isPreview = false;
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI titleTmp;
    [SerializeField] private TextMeshProUGUI descriptionTmp;
    [SerializeField] private TextMeshProUGUI timeSinceTmp;
    [SerializeField] private TextMeshProUGUI mailReadStateTmp;
    [SerializeField] private Image iconImg;
    [SerializeField] private Image stateImg;
    [SerializeField] private Image bgImg;

    private void OnDrawGizmosSelected()
    {
        if (isPreview && currentConfig)
        {
            ReloadConfig();
        }
    }

    public void SetConfig(MailTextConfig config)
    {
        this.currentConfig = config;
        ReloadConfig();
    }

    private void ReloadConfig()
    {
        // color
        titleTmp.color = currentConfig.titleColor;
        descriptionTmp.color = currentConfig.descriptionColor;
        timeSinceTmp.color = currentConfig.timeSinceColor;
        mailReadStateTmp.color = currentConfig.readStateColor;
        // image
        bgImg.sprite = currentConfig.bgSprite;
        stateImg.sprite = currentConfig.readStateSprite;

        iconImg.material = currentConfig.iconMaterial;
    }

    public void BindData(string title, string description, string timeText, string readStateText)
    {
        if (titleTmp != null)
            titleTmp.text = title ?? "";

        if (descriptionTmp != null)
            descriptionTmp.text = description ?? "";

        if (timeSinceTmp != null)
            timeSinceTmp.text = timeText ?? "";

        if (mailReadStateTmp != null)
            mailReadStateTmp.text = readStateText ?? "";
    }
}