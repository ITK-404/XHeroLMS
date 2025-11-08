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
    [Header("Toggle")]
    [SerializeField] private Image backgroundImg;
    [SerializeField] private Image checkmarkImg;
    [SerializeField] private Image selectImg;
    [Header("Other Stuff")]
    [SerializeField] private TextMeshProUGUI answerTmp;
    [SerializeField] private Button clickBtn;
    [SerializeField] private Image correctImg;
    [Header("Color")]
    public Color selectColor;
    public Color correctColor;
    public Color inCorrectColor;
    
    public Toggle toggle;
    public bool value;
    public Action<AnswerButton> OnSelectButton;
    
    private void Awake()
    {
        correctImg.gameObject.SetActive(false);
        clickBtn.onClick.AddListener(ClickSelectBtn);
    }

    private void OnDestroy()
    {
        clickBtn.onClick.RemoveListener(ClickSelectBtn);
    }

    private void ClickSelectBtn()
    {
        OnSelectButton?.Invoke(this);
    }

    [ContextMenu("Active Multiple Choice")]
    public void ActiveMultipleChoice()
    {
        checkmarkImg.sprite = square_checkmark;
        backgroundImg.sprite = square_none;
    }

    [ContextMenu("Active Single Choice")]
    public void ActiveSingleChoice()
    {
        checkmarkImg.sprite = circle_checkmark;
        backgroundImg.sprite = circle_none;
    }

    public void SetText(string answerText)
    {
        answerTmp.text = answerText;
    }

    public void ActiveSelect(bool isSelect)
    {
        value = isSelect;
        selectImg.gameObject.SetActive(isSelect);
        toggle.isOn = isSelect;
        
     
    }

    public Sprite correctSprite;
    public Sprite inCorrectSprite;
    public void SetCorrectColor()
    {
        correctImg.gameObject.SetActive(true);
        selectImg.color = correctColor;
        correctImg.sprite = correctSprite;
    }

    public void SetInCorrectColor()
    {
        correctImg.gameObject.SetActive(true);
        selectImg.color = inCorrectColor;
        correctImg.sprite = inCorrectSprite;
        
    }
}