using TMPro;
using UnityEngine;

public class FrameNoCourse : PanelBaseUI
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text subText;

    public void Setup(string title, string subtext)
    {
        if (titleText != null)
            titleText.text = title ?? "";

        if (subText != null)
            subText.text = subtext ?? "";
    }
}