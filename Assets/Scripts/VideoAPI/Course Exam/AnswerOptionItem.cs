using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AnswerOptionItem : MonoBehaviour
{
    [Header("UI")]
    public Toggle toggle;
    public TMP_Text label;

    // dữ liệu đi kèm
    public int optionIndex;            // chỉ số trong danh sách options của câu hỏi hiện tại
    public string optionText;          // text đã sạch

    // callback cho controller
    public Action<AnswerOptionItem, bool> OnToggled;

    public void Setup(int index, string text, bool isOn = false)
    {
        optionIndex = index;
        optionText = text ?? "";
        if (label) label.text = optionText;
        if (toggle)
        {
            toggle.isOn = isOn;
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener((v) => OnToggled?.Invoke(this, v));
        }
    }

    public void SetOnSilently(bool v)
    {
        if (!toggle) return;
        toggle.onValueChanged.RemoveAllListeners();
        toggle.isOn = v;
        toggle.onValueChanged.AddListener((val) => OnToggled?.Invoke(this, val));
    }

    public bool IsOn() => toggle ? toggle.isOn : false;
}
