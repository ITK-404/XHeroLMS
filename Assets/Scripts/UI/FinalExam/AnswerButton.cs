using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AnswerButton : MonoBehaviour
{
    [Header("Multiple choice sprite")]
    [SerializeField] private Sprite square_checkmark;
    [SerializeField] private Sprite square_none;
    [Header("Multiple choice sprite")]
    [SerializeField] private Sprite circle_checkmark;
    [SerializeField] private Sprite circle_none;

    [SerializeField] private Image backgroundImg;
    [SerializeField] private Image checkmarkImg;
    [SerializeField] private Image selectImg;
    [SerializeField] private TextMeshProUGUI answerTmp;
    public Toggle toggle;
    private bool value;
    public Action<AnswerButton> OnSelectButton;
    private void Awake()
    {
        toggle.onValueChanged.AddListener(ChangedValue);
    }

    private void OnDestroy()
    {
        toggle.onValueChanged.RemoveListener(ChangedValue);
    }

    private void ChangedValue(bool value)
    {
        this.value = value;
        OnSelectButton?.Invoke(this);
    }

    [ContextMenu("Active Multiple Choice")]
    public void ActiveMultipleChoice()
    {
        backgroundImg.sprite = square_checkmark;
        checkmarkImg.sprite = square_none;
    }

    [ContextMenu("Active Single Choice")]
    public void ActiveSingleChoice()
    {
        backgroundImg.sprite = circle_checkmark;
        checkmarkImg.sprite = circle_none;
    }

    public void SetText(string answerText)
    {
        answerTmp.text = answerText;
    }

    public void ActiveSelect(bool isSelect)
    {
        selectImg.gameObject.SetActive(isSelect);
    }
}

public class AnswerButtonManager : MonoBehaviour
{
    
}