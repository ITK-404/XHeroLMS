using System;
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

    private void Awake()
    {
        ApplyTmp(titleTmp);
        ApplyTmp(descriptionTmp);
        ApplyTmp(mailReadStateTmp);
    }
    private void ApplyTmp(TextMeshProUGUI tmp)
    {
        if (tmp == null) return;
        tmp.richText = true;
    }
    
    private void OnDrawGizmosSelected()
    {
        if (isPreview && currentConfig != null)
            ReloadConfig();
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

    public void BindData(string title, string description, string readStateText)
    {
        if (titleTmp != null)
            titleTmp.text = TMPMailTextFormatter.Format(title) ?? "";

        if (descriptionTmp != null)
            descriptionTmp.text = TMPMailTextFormatter.Format(description) ?? "";

        if (mailReadStateTmp != null)
            mailReadStateTmp.text = TMPMailTextFormatter.Format(readStateText) ?? "";
    }

    public void SetReadStateText(string readStateText)
    {
        if (mailReadStateTmp != null)
            mailReadStateTmp.text = readStateText ?? "";
    }

    public void SetTimeFromApi(NotificationMailTime t)
    {
        if (timeSinceTmp == null)
            return;

        if (t == null)
        {
            timeSinceTmp.text = "";
            return;
        }

        string value = (t.time ?? "").Trim();
        string key = (t.key ?? "").Trim().ToLower();

        // 1. Nếu API trả số bình thường: 4, 10, 2...
        if (int.TryParse(value, out int number))
        {
            switch (key)
            {
                case "second":
                    if (number < 5)
                        timeSinceTmp.text = "Vừa xong";
                    else if (number < 15)
                        timeSinceTmp.text = "Vài giây trước";
                    else
                        timeSinceTmp.text = $"{number} giây trước";
                    return;

                case "minute":
                    timeSinceTmp.text = number <= 1 ? "1 phút trước" : $"{number} phút trước";
                    return;

                case "hour":
                    timeSinceTmp.text = number <= 1 ? "1 giờ trước" : $"{number} giờ trước";
                    return;

                case "day":
                    timeSinceTmp.text = number <= 1 ? "1 ngày trước" : $"{number} ngày trước";
                    return;

                case "month":
                    timeSinceTmp.text = number <= 1 ? "1 tháng trước" : $"{number} tháng trước";
                    return;

                case "year":
                    timeSinceTmp.text = number <= 1 ? "1 năm trước" : $"{number} năm trước";
                    return;
            }
        }

        // 2. Nếu API trả ngày dạng string: 08/04/2026
        if (DateTime.TryParseExact(
                value,
                "dd/MM/yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None,
                out DateTime parsedDate))
        {
            TimeSpan diff = DateTime.Now.Date - parsedDate.Date;

            if (diff.TotalDays < 0)
            {
                timeSinceTmp.text = value;
                return;
            }

            if (diff.TotalDays == 0)
            {
                timeSinceTmp.text = "Hôm nay";
                return;
            }

            if (diff.TotalDays == 1)
            {
                timeSinceTmp.text = "1 ngày trước";
                return;
            }

            if (diff.TotalDays < 7)
            {
                timeSinceTmp.text = $"{(int)diff.TotalDays} ngày trước";
                return;
            }

            if (diff.TotalDays < 30)
            {
                int weeks = Mathf.FloorToInt((float)diff.TotalDays / 7f);
                timeSinceTmp.text = weeks <= 1 ? "1 tuần trước" : $"{weeks} tuần trước";
                return;
            }

            if (diff.TotalDays < 365)
            {
                int months = Mathf.FloorToInt((float)diff.TotalDays / 30f);
                timeSinceTmp.text = months <= 1 ? "1 tháng trước" : $"{months} tháng trước";
                return;
            }

            int years = Mathf.FloorToInt((float)diff.TotalDays / 365f);
            timeSinceTmp.text = years <= 1 ? "1 năm trước" : $"{years} năm trước";
            return;
        }

        // 3. Fallback cuối cùng
        timeSinceTmp.text = value;
    }
}