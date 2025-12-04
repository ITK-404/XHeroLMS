using DG.Tweening;
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
    [Header("Hover")]
    public Image hoverImg;

    [Header("Other")]
    public Toggle toggle;
    public Action<AnswerButton> OnSelectButton;

    private Color originalColor;
    // hiện tại đang dùng để kiểm tra đã chọn hay chưa
    // nếu đã chọn thì không cho hover nữa
    public bool isSelect;
    public bool IsOnReviewAnswer = false;

    private void Awake()
    {
        correctImg.gameObject.SetActive(false);
        clickBtn.onClick.AddListener(ClickSelectBtn);

        originalColor = answerTmp.color;

        SetHover(false, true);
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
        this.isSelect = isSelect;
        selectImg.gameObject.SetActive(isSelect);
        toggle.isOn = isSelect;

        if (isSelect)
        {
            SetHover(false, true);
        }
    }

    public Sprite correctSprite;
    public Sprite inCorrectSprite;

    public void SetCorrectColor()
    {
        correctImg.gameObject.SetActive(true);
        selectImg.color = correctColor;
        correctImg.sprite = correctSprite;

        IsOnReviewAnswer = true;
    }

    public void SetInCorrectColor()
    {
        correctImg.gameObject.SetActive(true);
        selectImg.color = inCorrectColor;
        correctImg.sprite = inCorrectSprite;

        IsOnReviewAnswer = true;
    }

    public void SetHover(bool isHover, bool isImmediate = false)
    {
        hoverImg.DOKill();
        hoverImg.DOFade(isHover ? 1f : 0f, isImmediate ? 0 : 0.1f);
        // lý do dùng màu trắng là vì bật gradient thì nó sẽ pha trộn màu trắng vào
        answerTmp.color = isHover ? Color.white : originalColor;
        answerTmp.enableVertexGradient = isHover;
    }
}
