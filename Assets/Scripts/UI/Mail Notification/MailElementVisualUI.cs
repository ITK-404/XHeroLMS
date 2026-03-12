using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MailElementVisualUI : MonoBehaviour
{
    [Header("Settings")]
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
        if (isPreview && currentConfig != null)
        {
            ReloadConfig();
        }
    }

    public void SetConfig(MailTextConfig config)
    {
        currentConfig = config;
        ReloadConfig();
    }

    private void ReloadConfig()
    {
        if (currentConfig == null) return;

        if (titleTmp != null) titleTmp.color = currentConfig.titleColor;
        if (descriptionTmp != null) descriptionTmp.color = currentConfig.descriptionColor;
        if (timeSinceTmp != null) timeSinceTmp.color = currentConfig.timeSinceColor;
        if (mailReadStateTmp != null) mailReadStateTmp.color = currentConfig.readStateColor;

        if (bgImg != null) bgImg.sprite = currentConfig.bgSprite;
        if (stateImg != null) stateImg.sprite = currentConfig.readStateSprite;
        if (iconImg != null) iconImg.material = currentConfig.iconMaterial;
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

    public void SetReadStateText(string readStateText)
    {
        if (mailReadStateTmp != null)
            mailReadStateTmp.text = readStateText ?? "";
    }
}